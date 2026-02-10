using System;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.LSP;

/// <summary>
/// GDScript Language Server implementation.
/// </summary>
public class GDLanguageServer : IGDLanguageServer
{
    private GDScriptProject? _project;
    private GDServiceRegistry? _registry;
    private GDDocumentManager? _documentManager;
    private GDDiagnosticPublisher? _diagnosticPublisher;
    private IGDJsonRpcTransport? _transport;
    private TaskCompletionSource? _shutdownTcs;
    private bool _disposed;

    public bool IsInitialized { get; private set; }
    public bool IsShuttingDown { get; private set; }

    public async Task InitializeAsync(IGDJsonRpcTransport transport, CancellationToken cancellationToken)
    {
        _transport = transport;
        _shutdownTcs = new TaskCompletionSource();

        // Register handlers
        RegisterHandlers();

        // Start transport
        await transport.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_shutdownTcs == null)
            throw new InvalidOperationException("Server not initialized");

        using var registration = cancellationToken.Register(() => _shutdownTcs.TrySetCanceled());
        await _shutdownTcs.Task.ConfigureAwait(false);
    }

    private void RegisterHandlers()
    {
        if (_transport == null)
            return;

        // Lifecycle
        _transport.OnRequest<GDInitializeParams, GDInitializeResult>("initialize", HandleInitializeAsync);
        _transport.OnNotification<object>("initialized", HandleInitializedAsync);
        _transport.OnRequest<object, object>("shutdown", HandleShutdownAsync);
        _transport.OnNotification<object>("exit", HandleExitAsync);

        // Document sync
        _transport.OnNotification<GDDidOpenTextDocumentParams>("textDocument/didOpen", HandleDidOpenAsync);
        _transport.OnNotification<GDDidChangeTextDocumentParams>("textDocument/didChange", HandleDidChangeAsync);
        _transport.OnNotification<GDDidCloseTextDocumentParams>("textDocument/didClose", HandleDidCloseAsync);
        _transport.OnNotification<GDDidSaveTextDocumentParams>("textDocument/didSave", HandleDidSaveAsync);

        // Workspace
        _transport.OnNotification<GDDidChangeConfigurationParams>("workspace/didChangeConfiguration", HandleDidChangeConfigurationAsync);
        _transport.OnRequest<GDWorkspaceSymbolParams, GDLspSymbolInformation[]?>("workspace/symbol", HandleWorkspaceSymbolAsync);

        // Language features
        _transport.OnRequest<GDDefinitionParams, GDLspLocation?>("textDocument/definition", HandleDefinitionAsync);
        _transport.OnRequest<GDReferencesParams, GDLspLocation[]?>("textDocument/references", HandleReferencesAsync);
        _transport.OnRequest<GDHoverParams, GDLspHover?>("textDocument/hover", HandleHoverAsync);
        _transport.OnRequest<GDDocumentSymbolParams, GDLspDocumentSymbol[]?>("textDocument/documentSymbol", HandleDocumentSymbolAsync);
        _transport.OnRequest<GDCompletionParams, GDLspCompletionList?>("textDocument/completion", HandleCompletionAsync);
        _transport.OnRequest<GDRenameParams, GDWorkspaceEdit?>("textDocument/rename", HandleRenameAsync);
        _transport.OnRequest<GDDocumentFormattingParams, GDLspTextEdit[]?>("textDocument/formatting", HandleFormattingAsync);
        _transport.OnRequest<GDCodeActionParams, GDLspCodeAction[]?>("textDocument/codeAction", HandleCodeActionAsync);
        _transport.OnRequest<GDSignatureHelpParams, GDLspSignatureHelp?>("textDocument/signatureHelp", HandleSignatureHelpAsync);
        _transport.OnRequest<GDInlayHintParams, GDLspInlayHint[]?>("textDocument/inlayHint", HandleInlayHintAsync);
    }

    #region Lifecycle Handlers

    private Task<GDInitializeResult?> HandleInitializeAsync(GDInitializeParams @params, CancellationToken ct)
    {
        // Determine project root
        var rootPath = @params.RootUri != null
            ? GDDocumentManager.UriToPath(@params.RootUri)
            : @params.RootPath;

        if (!string.IsNullOrEmpty(rootPath))
        {
            GDScriptProject? project = null;
            try
            {
                var projectRoot = GDProjectLoader.FindProjectRoot(rootPath);
                if (projectRoot != null)
                {
                    // Load project WITHOUT analysis - analysis will run in background after initialized
                    project = GDProjectLoader.LoadProjectWithoutAnalysis(projectRoot);
                    _documentManager = new GDDocumentManager(project);
                    _diagnosticPublisher = new GDDiagnosticPublisher(_transport!, project);
                    _project = project;

                    // Initialize service registry with base module
                    // NOTE: Pro module is NOT loaded in LSP (by design - Strict mode only)
                    _registry = new GDServiceRegistry();
                    _registry.LoadModules(project, new GDBaseModule());

                    project = null; // Successfully transferred ownership
                }
            }
            catch (Exception)
            {
                // Failed to load project, will work without project context
            }
            finally
            {
                // Dispose partially created project on error
                project?.Dispose();
            }
        }

        var result = new GDInitializeResult
        {
            Capabilities = new GDServerCapabilities
            {
                TextDocumentSync = new GDTextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = GDTextDocumentSyncKind.Full,
                    Save = new GDSaveOptions { IncludeText = false }
                },
                HoverProvider = true,
                DefinitionProvider = true,
                ReferencesProvider = true,
                DocumentSymbolProvider = true,
                RenameProvider = true,
                DocumentFormattingProvider = true,
                CompletionProvider = new GDCompletionOptions
                {
                    TriggerCharacters = [".", ":", "("],
                    ResolveProvider = false
                },
                CodeActionProvider = true,
                SignatureHelpProvider = new GDSignatureHelpOptions
                {
                    TriggerCharacters = ["(", ","],
                    RetriggerCharacters = [","]
                },
                InlayHintProvider = new GDInlayHintOptions
                {
                    ResolveProvider = false
                },
                WorkspaceSymbolProvider = true
            },
            ServerInfo = new GDServerInfo
            {
                Name = "GDShrapt LSP",
                Version = "1.0.0"
            }
        };

        return Task.FromResult<GDInitializeResult?>(result);
    }

    private Task HandleInitializedAsync(object? @params)
    {
        IsInitialized = true;

        // Subscribe to scene-triggered script reanalysis
        if (_project != null)
        {
            _project.SceneScriptsChanged += OnSceneScriptsChanged;
        }

        // Start background analysis after initialization completes
        // This prevents blocking the initialize request and allows the client to proceed
        if (_project != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Run analysis in background
                    _project.AnalyzeAll();

                    // Publish initial diagnostics for all loaded scripts
                    if (_diagnosticPublisher != null && _documentManager != null)
                    {
                        await _diagnosticPublisher.PublishAllAsync(_documentManager).ConfigureAwait(false);
                    }
                }
                catch (Exception)
                {
                    // Analysis failed, but server can still function with reduced capabilities
                }
            });
        }

        return Task.CompletedTask;
    }

    private void OnSceneScriptsChanged(object? sender, GDSceneAffectedScriptsEventArgs e)
    {
        if (_diagnosticPublisher == null)
            return;

        _ = Task.Run(async () =>
        {
            foreach (var script in e.AffectedScripts)
            {
                if (script.FullPath == null)
                    continue;

                var uri = GDDocumentManager.PathToUri(script.FullPath);
                await _diagnosticPublisher.PublishDiagnosticsAsync(uri).ConfigureAwait(false);
            }
        });
    }

    private Task<object?> HandleShutdownAsync(object? @params, CancellationToken ct)
    {
        IsShuttingDown = true;
        return Task.FromResult<object?>(null);
    }

    private Task HandleExitAsync(object? @params)
    {
        _shutdownTcs?.TrySetResult();
        return Task.CompletedTask;
    }

    #endregion

    #region Document Sync Handlers

    private Task HandleDidOpenAsync(GDDidOpenTextDocumentParams @params)
    {
        _documentManager?.OpenDocument(
            @params.TextDocument.Uri,
            @params.TextDocument.Text,
            @params.TextDocument.Version);

        // Schedule diagnostics update
        _diagnosticPublisher?.ScheduleUpdate(@params.TextDocument.Uri, @params.TextDocument.Version);

        return Task.CompletedTask;
    }

    private Task HandleDidChangeAsync(GDDidChangeTextDocumentParams @params)
    {
        if (@params.ContentChanges.Length > 0)
        {
            // For full sync, take the last change
            var content = @params.ContentChanges[@params.ContentChanges.Length - 1].Text;
            _documentManager?.UpdateDocument(
                @params.TextDocument.Uri,
                content,
                @params.TextDocument.Version);
        }

        // Schedule diagnostics update with debouncing
        _diagnosticPublisher?.ScheduleUpdate(@params.TextDocument.Uri, @params.TextDocument.Version);

        return Task.CompletedTask;
    }

    private async Task HandleDidCloseAsync(GDDidCloseTextDocumentParams @params)
    {
        _documentManager?.CloseDocument(@params.TextDocument.Uri);

        // Clear diagnostics for closed document
        if (_diagnosticPublisher != null)
        {
            await _diagnosticPublisher.ClearDiagnosticsAsync(@params.TextDocument.Uri).ConfigureAwait(false);
        }
    }

    private Task HandleDidSaveAsync(GDDidSaveTextDocumentParams @params)
    {
        // Optionally trigger full analysis on save
        return Task.CompletedTask;
    }

    #endregion

    #region Workspace Handlers

    private async Task HandleDidChangeConfigurationAsync(GDDidChangeConfigurationParams @params)
    {
        // Reload configuration and refresh diagnostics
        // The settings are in @params.Settings which can contain gdshrapt configuration
        // For now, just refresh diagnostics for all open documents
        if (_diagnosticPublisher != null && _documentManager != null)
        {
            await _diagnosticPublisher.PublishAllAsync(_documentManager).ConfigureAwait(false);
        }
    }

    private Task<GDLspSymbolInformation[]?> HandleWorkspaceSymbolAsync(GDWorkspaceSymbolParams @params, CancellationToken ct)
    {
        if (_project == null)
            return Task.FromResult<GDLspSymbolInformation[]?>(null);

        var query = @params.Query?.ToLowerInvariant() ?? "";
        var results = new System.Collections.Generic.List<GDLspSymbolInformation>();

        foreach (var script in _project.ScriptFiles)
        {
            if (script.Class == null || script.FullPath == null)
                continue;

            var uri = GDDocumentManager.PathToUri(script.FullPath);

            // Search class members
            foreach (var member in script.Class.Members)
            {
                string? name = null;
                GDLspSymbolKind kind = GDLspSymbolKind.Variable;
                int line = 0;

                if (member is Reader.GDMethodDeclaration method)
                {
                    name = method.Identifier?.ToString();
                    kind = GDLspSymbolKind.Method;
                    line = method.StartLine;
                }
                else if (member is Reader.GDVariableDeclaration variable)
                {
                    name = variable.Identifier?.ToString();
                    kind = variable.IsConstant ? GDLspSymbolKind.Constant : GDLspSymbolKind.Variable;
                    line = variable.StartLine;
                }
                else if (member is Reader.GDSignalDeclaration signal)
                {
                    name = signal.Identifier?.ToString();
                    kind = GDLspSymbolKind.Event;
                    line = signal.StartLine;
                }
                else if (member is Reader.GDEnumDeclaration enumDecl)
                {
                    name = enumDecl.Identifier?.ToString();
                    kind = GDLspSymbolKind.Enum;
                    line = enumDecl.StartLine;
                }
                else if (member is Reader.GDInnerClassDeclaration innerClass)
                {
                    name = innerClass.Identifier?.ToString();
                    kind = GDLspSymbolKind.Class;
                    line = innerClass.StartLine;
                }

                if (name != null && (string.IsNullOrEmpty(query) || name.ToLowerInvariant().Contains(query)))
                {
                    results.Add(new GDLspSymbolInformation
                    {
                        Name = name,
                        Kind = kind,
                        Location = new GDLspLocation
                        {
                            Uri = uri,
                            Range = new GDLspRange
                            {
                                Start = new GDLspPosition { Line = line, Character = 0 },
                                End = new GDLspPosition { Line = line, Character = 0 }
                            }
                        }
                    });
                }
            }
        }

        return Task.FromResult<GDLspSymbolInformation[]?>(results.ToArray());
    }

    #endregion

    #region Language Feature Handlers

    private Task<GDLspLocation?> HandleDefinitionAsync(GDDefinitionParams @params, CancellationToken ct)
    {
        if (_registry == null)
            return Task.FromResult<GDLspLocation?>(null);

        var coreHandler = _registry.GetService<IGDGoToDefHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspLocation?>(null);

        var handler = new GDDefinitionHandler(coreHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspLocation[]?> HandleReferencesAsync(GDReferencesParams @params, CancellationToken ct)
    {
        if (_registry == null)
            return Task.FromResult<GDLspLocation[]?>(null);

        var findRefsHandler = _registry.GetService<IGDFindRefsHandler>();
        var goToDefHandler = _registry.GetService<IGDGoToDefHandler>();
        if (findRefsHandler == null || goToDefHandler == null)
            return Task.FromResult<GDLspLocation[]?>(null);

        var handler = new GDReferencesHandler(findRefsHandler, goToDefHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspHover?> HandleHoverAsync(GDHoverParams @params, CancellationToken ct)
    {
        if (_registry == null)
            return Task.FromResult<GDLspHover?>(null);

        var coreHandler = _registry.GetService<IGDHoverHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspHover?>(null);

        var handler = new GDLspHoverHandler(coreHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspDocumentSymbol[]?> HandleDocumentSymbolAsync(GDDocumentSymbolParams @params, CancellationToken ct)
    {
        if (_registry == null)
            return Task.FromResult<GDLspDocumentSymbol[]?>(null);

        var coreHandler = _registry.GetService<IGDSymbolsHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspDocumentSymbol[]?>(null);

        var handler = new GDDocumentSymbolHandler(coreHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspCompletionList?> HandleCompletionAsync(GDCompletionParams @params, CancellationToken ct)
    {
        if (_registry == null)
            return Task.FromResult<GDLspCompletionList?>(null);

        var coreHandler = _registry.GetService<IGDCompletionHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspCompletionList?>(null);

        var handler = new GDLspCompletionHandler(coreHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDWorkspaceEdit?> HandleRenameAsync(GDRenameParams @params, CancellationToken ct)
    {
        if (_registry == null)
            return Task.FromResult<GDWorkspaceEdit?>(null);

        var renameHandler = _registry.GetService<IGDRenameHandler>();
        var goToDefHandler = _registry.GetService<IGDGoToDefHandler>();
        if (renameHandler == null || goToDefHandler == null)
            return Task.FromResult<GDWorkspaceEdit?>(null);

        var handler = new GDLspRenameHandler(renameHandler, goToDefHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspTextEdit[]?> HandleFormattingAsync(GDDocumentFormattingParams @params, CancellationToken ct)
    {
        if (_registry == null)
            return Task.FromResult<GDLspTextEdit[]?>(null);

        var coreHandler = _registry.GetService<IGDFormatHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspTextEdit[]?>(null);

        var handler = new GDFormattingHandler(coreHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspCodeAction[]?> HandleCodeActionAsync(GDCodeActionParams @params, CancellationToken ct)
    {
        if (_registry == null)
            return Task.FromResult<GDLspCodeAction[]?>(null);

        var coreHandler = _registry.GetService<IGDCodeActionHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspCodeAction[]?>(null);

        var handler = new GDLspCodeActionHandler(coreHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspSignatureHelp?> HandleSignatureHelpAsync(GDSignatureHelpParams @params, CancellationToken ct)
    {
        if (_registry == null)
            return Task.FromResult<GDLspSignatureHelp?>(null);

        var coreHandler = _registry.GetService<IGDSignatureHelpHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspSignatureHelp?>(null);

        var handler = new GDLspSignatureHelpHandler(coreHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspInlayHint[]?> HandleInlayHintAsync(GDInlayHintParams @params, CancellationToken ct)
    {
        if (_registry == null)
            return Task.FromResult<GDLspInlayHint[]?>(null);

        var coreHandler = _registry.GetService<IGDInlayHintHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspInlayHint[]?>(null);

        var handler = new GDLspInlayHintHandler(coreHandler);
        return handler.HandleAsync(@params, ct);
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_project != null)
        {
            _project.SceneScriptsChanged -= OnSceneScriptsChanged;
        }

        if (_diagnosticPublisher != null)
        {
            await _diagnosticPublisher.DisposeAsync().ConfigureAwait(false);
        }

        if (_transport != null)
        {
            await _transport.DisposeAsync().ConfigureAwait(false);
        }

        _project?.Dispose();
    }
}

/// <summary>
/// Helper for loading GDScript projects.
/// </summary>
internal static class GDProjectLoader
{
    public static string? FindProjectRoot(string startPath)
    {
        var dir = System.IO.Directory.Exists(startPath)
            ? startPath
            : System.IO.Path.GetDirectoryName(startPath);

        while (!string.IsNullOrEmpty(dir))
        {
            var projectFile = System.IO.Path.Combine(dir, "project.godot");
            if (System.IO.File.Exists(projectFile))
                return dir;

            dir = System.IO.Path.GetDirectoryName(dir);
        }

        return null;
    }

    /// <summary>
    /// Loads project without analysis (fast for initialization).
    /// Call AnalyzeAllAsync() separately for background analysis.
    /// </summary>
    public static GDScriptProject LoadProjectWithoutAnalysis(string projectRoot)
    {
        var context = new GDDefaultProjectContext(projectRoot);
        var project = new GDScriptProject(context, new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = true,
            EnableFileWatcher = true,
            EnableSceneChangeReanalysis = true
        });

        project.LoadScripts();
        project.LoadScenes();
        // NOTE: AnalyzeAll() is NOT called here - do it asynchronously after initialized

        // Enable scene file watcher for real-time scene change detection
        project.SceneTypesProvider?.EnableFileWatcher();

        return project;
    }

    /// <summary>
    /// Loads project with full analysis (blocking, for CLI use).
    /// </summary>
    public static GDScriptProject LoadProject(string projectRoot)
    {
        var project = LoadProjectWithoutAnalysis(projectRoot);
        project.AnalyzeAll();
        return project;
    }
}
