using System;
using System.IO;
using System.Runtime.InteropServices;
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
    private GDProjectConfig? _config;
    private IGDJsonRpcTransport? _transport;
    private GDLspLogger? _logger;
    private GDLspTraceLevel _traceLevel = GDLspTraceLevel.Off;
    private string? _initializationError;
    private TaskCompletionSource? _shutdownTcs;
    private TaskCompletionSource<bool>? _analysisComplete;
    private bool _disposed;

    public bool IsInitialized { get; private set; }
    public bool IsShuttingDown { get; private set; }

    public async Task InitializeAsync(IGDJsonRpcTransport transport, CancellationToken cancellationToken)
    {
        _transport = transport;
        _shutdownTcs = new TaskCompletionSource();

        // Create logger and wire to transport
        _logger = new GDLspLogger(transport);
        if (transport is GDStdioJsonRpcTransport stdioTransport)
            stdioTransport.Logger = _logger;
        else if (transport is GDSocketJsonRpcTransport socketTransport)
            socketTransport.Logger = _logger;

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

        // Trace
        _transport.OnNotification<GDSetTraceParams>("$/setTrace", HandleSetTraceAsync);

        // Workspace
        _transport.OnNotification<GDDidChangeConfigurationParams>("workspace/didChangeConfiguration", HandleDidChangeConfigurationAsync);
        _transport.OnRequest<GDWorkspaceSymbolParams, GDLspSymbolInformation[]?>("workspace/symbol", HandleWorkspaceSymbolAsync);
        _transport.OnRequest<GDExecuteCommandParams, object?>("workspace/executeCommand", HandleExecuteCommandAsync);

        // Language features
        _transport.OnRequest<GDDefinitionParams, GDLspLocationLink[]?>("textDocument/definition", HandleDefinitionAsync);
        _transport.OnRequest<GDReferencesParams, GDLspLocation[]?>("textDocument/references", HandleReferencesAsync);
        _transport.OnRequest<GDHoverParams, GDLspHover?>("textDocument/hover", HandleHoverAsync);
        _transport.OnRequest<GDDocumentSymbolParams, GDLspDocumentSymbol[]?>("textDocument/documentSymbol", HandleDocumentSymbolAsync);
        _transport.OnRequest<GDCompletionParams, GDLspCompletionList?>("textDocument/completion", HandleCompletionAsync);
        _transport.OnRequest<GDRenameParams, GDWorkspaceEdit?>("textDocument/rename", HandleRenameAsync);
        _transport.OnRequest<GDPrepareRenameParams, GDPrepareRenameResult?>("textDocument/prepareRename", HandlePrepareRenameAsync);
        _transport.OnRequest<GDDocumentHighlightParams, GDDocumentHighlight[]?>("textDocument/documentHighlight", HandleDocumentHighlightAsync);
        _transport.OnRequest<GDFoldingRangeParams, GDFoldingRange[]?>("textDocument/foldingRange", HandleFoldingRangeAsync);
        _transport.OnRequest<GDDocumentFormattingParams, GDLspTextEdit[]?>("textDocument/formatting", HandleFormattingAsync);
        _transport.OnRequest<GDCodeActionParams, GDLspCodeAction[]?>("textDocument/codeAction", HandleCodeActionAsync);
        _transport.OnRequest<GDSignatureHelpParams, GDLspSignatureHelp?>("textDocument/signatureHelp", HandleSignatureHelpAsync);
        _transport.OnRequest<GDInlayHintParams, GDLspInlayHint[]?>("textDocument/inlayHint", HandleInlayHintAsync);
        _transport.OnRequest<GDCodeLensParams, GDLspCodeLens[]?>("textDocument/codeLens", HandleCodeLensAsync);
        _transport.OnRequest<GDSemanticTokensParams, GDSemanticTokens?>("textDocument/semanticTokens/full", HandleSemanticTokensFullAsync);

        // Call hierarchy
        _transport.OnRequest<GDCallHierarchyPrepareParams, GDLspCallHierarchyItem[]?>("textDocument/prepareCallHierarchy", HandlePrepareCallHierarchyAsync);
        _transport.OnRequest<GDCallHierarchyIncomingCallsParams, GDLspCallHierarchyIncomingCall[]?>("callHierarchy/incomingCalls", HandleIncomingCallsAsync);
        _transport.OnRequest<GDCallHierarchyOutgoingCallsParams, GDLspCallHierarchyOutgoingCall[]?>("callHierarchy/outgoingCalls", HandleOutgoingCallsAsync);

        // Type definition and implementation
        _transport.OnRequest<GDDefinitionParams, GDLspLocationLink[]?>("textDocument/typeDefinition", HandleTypeDefinitionAsync);
        _transport.OnRequest<GDDefinitionParams, GDLspLocation[]?>("textDocument/implementation", HandleImplementationAsync);

        // Custom requests
        _transport.OnRequest<GDCodeLensReferencesParams, GDLspLocation[]?>("gdshrapt/codeLensReferences", HandleCodeLensReferencesAsync);
    }

    #region Lifecycle Handlers

    private Task<GDInitializeResult?> HandleInitializeAsync(GDInitializeParams @params, CancellationToken ct)
    {
        // Parse trace level
        _traceLevel = @params.Trace switch
        {
            "messages" => GDLspTraceLevel.Messages,
            "verbose" => GDLspTraceLevel.Verbose,
            _ => GDLspTraceLevel.Off
        };

        // Determine project root
        var rootPath = @params.RootUri != null
            ? GDDocumentManager.UriToPath(@params.RootUri)
            : @params.RootPath;

        _ = _logger?.InfoAsync($"[initialize] rootUri={@params.RootUri} rootPath={@params.RootPath} resolved={rootPath}");

        if (!string.IsNullOrEmpty(rootPath))
        {
            GDScriptProject? project = null;
            try
            {
                var projectRoot = GDProjectLoader.FindProjectRoot(rootPath);
                _ = _logger?.InfoAsync($"[initialize] projectRoot={projectRoot}");
                if (projectRoot != null)
                {
                    // Load project WITHOUT analysis - analysis will run in background after initialized
                    project = GDProjectLoader.LoadProjectWithoutAnalysis(projectRoot);
                    _documentManager = new GDDocumentManager(project);
                    _config = GDConfigLoader.LoadConfig(project.ProjectPath);
                    _analysisComplete = new TaskCompletionSource<bool>();
                    _diagnosticPublisher = new GDDiagnosticPublisher(_transport!, project, config: _config, analysisReady: _analysisComplete.Task);
                    _project = project;

                    var scriptCount = 0;
                    foreach (var _ in project.ScriptFiles) scriptCount++;
                    _ = _logger?.InfoAsync($"[initialize] loaded {scriptCount} scripts, initializing registry...");

                    // Initialize service registry with base module
                    // NOTE: Pro module is NOT loaded in LSP (by design - Strict mode only)
                    _registry = new GDServiceRegistry();
                    try
                    {
                        _registry.LoadModules(project, new GDBaseModule());
                        var projectModel = _registry.GetService<GDProjectSemanticModel>();
                        _documentManager.SetProjectModel(projectModel);
                        _diagnosticPublisher.SetProjectModel(projectModel);
                        _ = _logger?.InfoAsync($"[initialize] registry initialized successfully");
                    }
                    catch (Exception moduleEx)
                    {
                        _ = _logger?.ErrorAsync($"[initialize] LoadModules failed: {moduleEx}");
                    }

                    // Check semantic model status after module loading
                    var analyzedCount = 0;
                    var totalCount = 0;
                    foreach (var s in project.ScriptFiles)
                    {
                        totalCount++;
                        if (s.SemanticModel != null) analyzedCount++;
                    }
                    _ = _logger?.InfoAsync($"[initialize] semantic models: {analyzedCount}/{totalCount}");

                    project = null; // Successfully transferred ownership
                }
            }
            catch (Exception ex)
            {
                _initializationError = $"Failed to load project: {ex.Message}";
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
                RenameProvider = new GDRenameOptions { PrepareProvider = true },
                DocumentHighlightProvider = true,
                FoldingRangeProvider = true,
                DocumentFormattingProvider = true,
                CompletionProvider = new GDCompletionOptions
                {
                    TriggerCharacters = [".", ":", "(", "$", "/"],
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
                CodeLensProvider = new GDCodeLensOptions
                {
                    ResolveProvider = false
                },
                SemanticTokensProvider = new GDSemanticTokensOptions
                {
                    Legend = new GDSemanticTokensLegend
                    {
                        TokenTypes = GDSemanticTokensHandler.TokenTypes,
                        TokenModifiers = GDSemanticTokensHandler.TokenModifiers
                    },
                    Full = true
                },
                CallHierarchyProvider = true,
                TypeDefinitionProvider = true,
                ImplementationProvider = true,
                WorkspaceSymbolProvider = true,
                ExecuteCommandProvider = new GDExecuteCommandOptions
                {
                    Commands = ["gdshrapt.serverStatus"]
                }
            },
            ServerInfo = new GDServerInfo
            {
                Name = "GDShrapt LSP",
                Version = GDLspVersionInfo.GetVersion()
            }
        };

        return Task.FromResult<GDInitializeResult?>(result);
    }

    private Task HandleInitializedAsync(object? @params)
    {
        IsInitialized = true;

        // Send deferred initialization error if project loading failed
        if (_initializationError != null)
        {
            _ = ShowCriticalErrorAsync(_initializationError);
        }

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
                    _project.BuildCallSiteRegistry();
                    _project.ResolveTresClassNames();
                    GDProjectInitializer.InjectSceneSignalConnections(_project);
                }
                catch (Exception ex)
                {
                    _ = ShowCriticalErrorAsync($"Background analysis failed: {ex.Message}");
                }
                finally
                {
                    // Mark analysis complete so document manager can eagerly rebuild models on edits
                    _documentManager?.SetInitialAnalysisComplete();

                    // Signal that analysis is complete (even if it failed partially)
                    // This unblocks any pending diagnostic publish requests
                    _analysisComplete?.TrySetResult(true);
                }

                if (_diagnosticPublisher != null && _documentManager != null)
                {
                    await _diagnosticPublisher.PublishAllAsync(_documentManager).ConfigureAwait(false);
                }

                // Refresh CodeLens after analysis completes so reference counts are accurate
                try
                {
                    if (_transport != null)
                        await _transport.SendRequestAsync<object?, object?>("workspace/codeLens/refresh", null, CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // Client may not support codeLens/refresh — ignore
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
        var filename = Path.GetFileName(GDDocumentManager.UriToPath(@params.TextDocument.Uri));
        var sw = System.Diagnostics.Stopwatch.StartNew();

        GDLspPerformanceTrace.Log("didChange", $"START {filename} v{@params.TextDocument.Version}");

        if (@params.ContentChanges.Length > 0)
        {
            // For full sync, take the last change
            var content = @params.ContentChanges[@params.ContentChanges.Length - 1].Text;
            _documentManager?.UpdateDocument(
                @params.TextDocument.Uri,
                content,
                @params.TextDocument.Version);
        }

        var updateMs = sw.ElapsedMilliseconds;

        // Schedule diagnostics update with debouncing
        _diagnosticPublisher?.ScheduleUpdate(@params.TextDocument.Uri, @params.TextDocument.Version);

        GDLspPerformanceTrace.Log("didChange", $"END {filename} update={updateMs}ms total={sw.ElapsedMilliseconds}ms");

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
        _diagnosticPublisher?.ScheduleUpdate(@params.TextDocument.Uri);
        return Task.CompletedTask;
    }

    #endregion

    #region Workspace Handlers

    private async Task HandleDidChangeConfigurationAsync(GDDidChangeConfigurationParams @params)
    {
        // Reload configuration from .gdshrapt.json and refresh diagnostics
        if (_project != null)
        {
            _config = GDConfigLoader.LoadConfig(_project.ProjectPath);
            _diagnosticPublisher?.UpdateConfig(_config);
        }

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

    private Task HandleSetTraceAsync(GDSetTraceParams @params)
    {
        _traceLevel = @params.Value switch
        {
            "messages" => GDLspTraceLevel.Messages,
            "verbose" => GDLspTraceLevel.Verbose,
            _ => GDLspTraceLevel.Off
        };
        return Task.CompletedTask;
    }

    private Task<object?> HandleExecuteCommandAsync(GDExecuteCommandParams @params, CancellationToken ct)
    {
        return @params.Command switch
        {
            "gdshrapt.serverStatus" => Task.FromResult<object?>(GetServerStatus()),
            _ => Task.FromResult<object?>(null)
        };
    }

    private object GetServerStatus()
    {
        return new
        {
            version = GDLspVersionInfo.GetVersion(),
            platform = RuntimeInformation.OSDescription,
            projectPath = _project?.ProjectPath,
            initialized = IsInitialized
        };
    }

    #endregion

    #region Language Feature Handlers

    private async Task<GDLspLocationLink[]?> HandleDefinitionAsync(GDDefinitionParams @params, CancellationToken ct)
    {
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("textDocument/definition", _traceLevel == GDLspTraceLevel.Verbose ? $"uri={@params.TextDocument.Uri} line={@params.Position.Line}" : null);

        if (_registry == null)
            return null;

        var coreHandler = _registry.GetService<IGDGoToDefHandler>();
        if (coreHandler == null)
            return null;

        var handler = new GDDefinitionHandler(coreHandler);
        var (links, infoMessage) = await handler.HandleAsync(@params, ct);

        if (infoMessage != null && _transport != null)
        {
            await _transport.SendNotificationAsync("window/showMessage", new GDShowMessageParams
            {
                Type = GDLspMessageType.Info,
                Message = infoMessage
            });
        }

        return links;
    }

    private Task<GDLspLocation[]?> HandleReferencesAsync(GDReferencesParams @params, CancellationToken ct)
    {
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("textDocument/references", _traceLevel == GDLspTraceLevel.Verbose ? $"uri={@params.TextDocument.Uri} line={@params.Position.Line}" : null);

        if (_registry == null)
            return Task.FromResult<GDLspLocation[]?>(null);

        var findRefsHandler = _registry.GetService<IGDFindRefsHandler>();
        var goToDefHandler = _registry.GetService<IGDGoToDefHandler>();
        if (findRefsHandler == null || goToDefHandler == null)
            return Task.FromResult<GDLspLocation[]?>(null);

        var handler = new GDReferencesHandler(findRefsHandler, goToDefHandler);
        return handler.HandleAsync(@params, ct);
    }

    private async Task<GDLspHover?> HandleHoverAsync(GDHoverParams @params, CancellationToken ct)
    {
        var filename = Path.GetFileName(GDDocumentManager.UriToPath(@params.TextDocument.Uri));
        GDLspPerformanceTrace.Log("hover", $"START {filename} L{@params.Position.Line}:{@params.Position.Character}");

        if (_registry == null)
            return null;

        var coreHandler = _registry.GetService<IGDHoverHandler>();
        if (coreHandler == null)
            return null;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var handler = new GDLspHoverHandler(coreHandler);
        var result = await handler.HandleAsync(@params, ct).ConfigureAwait(false);
        sw.Stop();

        GDLspPerformanceTrace.Log("hover", $"END {filename} {sw.ElapsedMilliseconds}ms L{@params.Position.Line}:{@params.Position.Character} hasResult={result != null}");

        return result;
    }

    private Task<GDLspDocumentSymbol[]?> HandleDocumentSymbolAsync(GDDocumentSymbolParams @params, CancellationToken ct)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        _ = _logger?.InfoAsync($"[documentSymbol] uri={@params.TextDocument.Uri} path={filePath} registry={_registry != null} project={_project != null}");

        if (_project != null)
        {
            var script = _project.GetScript(filePath);
            _ = _logger?.InfoAsync($"[documentSymbol] script={script != null} semanticModel={script?.SemanticModel != null} class={script?.Class != null}");
            if (script == null)
            {
                // Log available scripts for debugging
                var count = 0;
                foreach (var s in _project.ScriptFiles)
                {
                    if (count < 5)
                        _ = _logger?.InfoAsync($"[documentSymbol] available script: {s.FullPath}");
                    count++;
                }
                _ = _logger?.InfoAsync($"[documentSymbol] total scripts: {count}");
            }
        }

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
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("textDocument/completion", _traceLevel == GDLspTraceLevel.Verbose ? $"uri={@params.TextDocument.Uri} line={@params.Position.Line}" : null);

        if (_registry == null)
            return Task.FromResult<GDLspCompletionList?>(null);

        var coreHandler = _registry.GetService<IGDCompletionHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspCompletionList?>(null);

        var handler = new GDLspCompletionHandler(coreHandler, _documentManager);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDWorkspaceEdit?> HandleRenameAsync(GDRenameParams @params, CancellationToken ct)
    {
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("textDocument/rename", _traceLevel == GDLspTraceLevel.Verbose ? $"uri={@params.TextDocument.Uri} newName={@params.NewName}" : null);

        if (_registry == null)
            return Task.FromResult<GDWorkspaceEdit?>(null);

        var renameHandler = _registry.GetService<IGDRenameHandler>();
        var goToDefHandler = _registry.GetService<IGDGoToDefHandler>();
        if (renameHandler == null || goToDefHandler == null)
            return Task.FromResult<GDWorkspaceEdit?>(null);

        var handler = new GDLspRenameHandler(renameHandler, goToDefHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDPrepareRenameResult?> HandlePrepareRenameAsync(GDPrepareRenameParams @params, CancellationToken ct)
    {
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("textDocument/prepareRename", _traceLevel == GDLspTraceLevel.Verbose ? $"uri={@params.TextDocument.Uri} line={@params.Position.Line}" : null);

        if (_registry == null)
            return Task.FromResult<GDPrepareRenameResult?>(null);

        var goToDefHandler = _registry.GetService<IGDGoToDefHandler>();
        if (goToDefHandler == null)
            return Task.FromResult<GDPrepareRenameResult?>(null);

        var handler = new GDLspPrepareRenameHandler(goToDefHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDDocumentHighlight[]?> HandleDocumentHighlightAsync(GDDocumentHighlightParams @params, CancellationToken ct)
    {
        if (_registry == null || _project == null)
            return Task.FromResult<GDDocumentHighlight[]?>(null);

        var goToDefHandler = _registry.GetService<IGDGoToDefHandler>();
        if (goToDefHandler == null)
            return Task.FromResult<GDDocumentHighlight[]?>(null);

        var projectModel = _registry.GetService<GDProjectSemanticModel>();
        var handler = new GDDocumentHighlightHandler(_project, projectModel, goToDefHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDFoldingRange[]?> HandleFoldingRangeAsync(GDFoldingRangeParams @params, CancellationToken ct)
    {
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("textDocument/foldingRange", _traceLevel == GDLspTraceLevel.Verbose ? $"uri={@params.TextDocument.Uri}" : null);

        if (_registry == null)
            return Task.FromResult<GDFoldingRange[]?>(null);

        var coreHandler = _registry.GetService<IGDFoldingRangeHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDFoldingRange[]?>(null);

        var handler = new GDLspFoldingRangeHandler(coreHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspTextEdit[]?> HandleFormattingAsync(GDDocumentFormattingParams @params, CancellationToken ct)
    {
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("textDocument/formatting", _traceLevel == GDLspTraceLevel.Verbose ? $"uri={@params.TextDocument.Uri}" : null);

        if (_registry == null)
            return Task.FromResult<GDLspTextEdit[]?>(null);

        var coreHandler = _registry.GetService<IGDFormatHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspTextEdit[]?>(null);

        var handler = new GDFormattingHandler(coreHandler, _config);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspCodeAction[]?> HandleCodeActionAsync(GDCodeActionParams @params, CancellationToken ct)
    {
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("textDocument/codeAction", _traceLevel == GDLspTraceLevel.Verbose ? $"uri={@params.TextDocument.Uri}" : null);

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
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("textDocument/signatureHelp", _traceLevel == GDLspTraceLevel.Verbose ? $"uri={@params.TextDocument.Uri} line={@params.Position.Line}" : null);

        if (_registry == null)
            return Task.FromResult<GDLspSignatureHelp?>(null);

        var coreHandler = _registry.GetService<IGDSignatureHelpHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspSignatureHelp?>(null);

        var handler = new GDLspSignatureHelpHandler(coreHandler);
        return handler.HandleAsync(@params, ct);
    }

    private async Task<GDLspInlayHint[]?> HandleInlayHintAsync(GDInlayHintParams @params, CancellationToken ct)
    {
        var filename = Path.GetFileName(GDDocumentManager.UriToPath(@params.TextDocument.Uri));
        GDLspPerformanceTrace.Log("inlayHint", $"START {filename}");

        if (_registry == null)
            return null;

        var coreHandler = _registry.GetService<IGDInlayHintHandler>();
        if (coreHandler == null)
            return null;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var handler = new GDLspInlayHintHandler(coreHandler);
        var result = await handler.HandleAsync(@params, ct).ConfigureAwait(false);
        sw.Stop();

        GDLspPerformanceTrace.Log("inlayHint", $"END {filename} {sw.ElapsedMilliseconds}ms");

        return result;
    }

    private async Task<GDLspCodeLens[]?> HandleCodeLensAsync(GDCodeLensParams @params, CancellationToken ct)
    {
        var filename = Path.GetFileName(GDDocumentManager.UriToPath(@params.TextDocument.Uri));
        GDLspPerformanceTrace.Log("codeLens", $"START {filename}");

        // Return null before analysis completes — CodeLens will refresh after analysis via workspace/codeLens/refresh
        if (_analysisComplete != null && !_analysisComplete.Task.IsCompleted)
        {
            GDLspPerformanceTrace.Log("codeLens", $"SKIP {filename} (analysis not complete)");
            return null;
        }

        if (_registry == null)
            return null;

        var coreHandler = _registry.GetService<IGDCodeLensHandler>();
        if (coreHandler == null)
            return null;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var handler = new GDLspCodeLensHandler(coreHandler);
        var result = await handler.HandleAsync(@params, ct).ConfigureAwait(false);
        sw.Stop();

        GDLspPerformanceTrace.Log("codeLens", $"END {filename} {sw.ElapsedMilliseconds}ms count={result?.Length ?? 0}");

        return result;
    }

    private Task<GDLspLocation[]?> HandleCodeLensReferencesAsync(GDCodeLensReferencesParams @params, CancellationToken ct)
    {
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("gdshrapt/codeLensReferences", _traceLevel == GDLspTraceLevel.Verbose ? $"symbol={@params.SymbolName}" : null);

        if (_registry == null)
            return Task.FromResult<GDLspLocation[]?>(null);

        var codeLensHandler = _registry.GetService<IGDCodeLensHandler>();
        var findRefsHandler = _registry.GetService<IGDFindRefsHandler>();
        if (codeLensHandler == null || findRefsHandler == null)
            return Task.FromResult<GDLspLocation[]?>(null);

        var handler = new GDCodeLensReferencesHandler(codeLensHandler, findRefsHandler);
        return handler.HandleAsync(@params, ct);
    }

    private async Task<GDSemanticTokens?> HandleSemanticTokensFullAsync(GDSemanticTokensParams @params, CancellationToken ct)
    {
        var filename = Path.GetFileName(GDDocumentManager.UriToPath(@params.TextDocument.Uri));
        GDLspPerformanceTrace.Log("semTokens", $"START {filename}");

        if (_project == null)
            return null;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var projectModel = _registry?.GetService<GDProjectSemanticModel>();
        var handler = new GDSemanticTokensHandler(_project, projectModel);
        var result = await handler.HandleAsync(@params, ct);
        sw.Stop();

        GDLspPerformanceTrace.Log("semTokens", $"END {filename} {sw.ElapsedMilliseconds}ms");

        return result;
    }

    private async Task<GDLspLocationLink[]?> HandleTypeDefinitionAsync(GDDefinitionParams @params, CancellationToken ct)
    {
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("textDocument/typeDefinition", _traceLevel == GDLspTraceLevel.Verbose ? $"uri={@params.TextDocument.Uri}" : null);

        if (_registry == null)
            return null;

        var coreHandler = _registry.GetService<IGDTypeDefinitionHandler>();
        if (coreHandler == null)
            return null;

        var handler = new GDTypeDefinitionLspHandler(coreHandler);
        var (links, infoMessage) = await handler.HandleAsync(@params, ct);

        if (infoMessage != null)
            _ = _transport?.SendNotificationAsync("window/showMessage", new GDShowMessageParams
            {
                Type = GDLspMessageType.Info,
                Message = infoMessage
            });

        return links;
    }

    private Task<GDLspLocation[]?> HandleImplementationAsync(GDDefinitionParams @params, CancellationToken ct)
    {
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("textDocument/implementation", _traceLevel == GDLspTraceLevel.Verbose ? $"uri={@params.TextDocument.Uri}" : null);

        if (_registry == null)
            return Task.FromResult<GDLspLocation[]?>(null);

        var coreHandler = _registry.GetService<IGDImplementationHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspLocation[]?>(null);

        var handler = new GDImplementationLspHandler(coreHandler);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspCallHierarchyItem[]?> HandlePrepareCallHierarchyAsync(GDCallHierarchyPrepareParams @params, CancellationToken ct)
    {
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("textDocument/prepareCallHierarchy", _traceLevel == GDLspTraceLevel.Verbose ? $"uri={@params.TextDocument.Uri}" : null);

        if (_registry == null)
            return Task.FromResult<GDLspCallHierarchyItem[]?>(null);

        var coreHandler = _registry.GetService<IGDCallHierarchyHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspCallHierarchyItem[]?>(null);

        var handler = new GDLspCallHierarchyHandler(coreHandler);
        return handler.HandlePrepareAsync(@params, ct);
    }

    private Task<GDLspCallHierarchyIncomingCall[]?> HandleIncomingCallsAsync(GDCallHierarchyIncomingCallsParams @params, CancellationToken ct)
    {
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("callHierarchy/incomingCalls");

        if (_registry == null)
            return Task.FromResult<GDLspCallHierarchyIncomingCall[]?>(null);

        var coreHandler = _registry.GetService<IGDCallHierarchyHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspCallHierarchyIncomingCall[]?>(null);

        var handler = new GDLspCallHierarchyHandler(coreHandler);
        return handler.HandleIncomingCallsAsync(@params, ct);
    }

    private Task<GDLspCallHierarchyOutgoingCall[]?> HandleOutgoingCallsAsync(GDCallHierarchyOutgoingCallsParams @params, CancellationToken ct)
    {
        if (_traceLevel >= GDLspTraceLevel.Messages)
            _ = TraceAsync("callHierarchy/outgoingCalls");

        if (_registry == null)
            return Task.FromResult<GDLspCallHierarchyOutgoingCall[]?>(null);

        var coreHandler = _registry.GetService<IGDCallHierarchyHandler>();
        if (coreHandler == null)
            return Task.FromResult<GDLspCallHierarchyOutgoingCall[]?>(null);

        var handler = new GDLspCallHierarchyHandler(coreHandler);
        return handler.HandleOutgoingCallsAsync(@params, ct);
    }

    #endregion

    #region Trace and Notifications

    private Task TraceAsync(string message, string? verbose = null)
    {
        if (_traceLevel == GDLspTraceLevel.Off || _transport == null)
            return Task.CompletedTask;

        var @params = new GDLogTraceParams { Message = message };
        if (_traceLevel == GDLspTraceLevel.Verbose && verbose != null)
            @params.Verbose = verbose;

        return _transport.SendNotificationAsync("$/logTrace", @params);
    }

    private async Task ShowCriticalErrorAsync(string message)
    {
        if (_transport == null)
            return;

        try
        {
            await _transport.SendNotificationAsync("window/showMessage", new GDShowMessageParams
            {
                Type = GDLspMessageType.Error,
                Message = message
            }).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort: if transport is broken, nothing we can do
        }
    }

    /// <inheritdoc />
    public Task TryShowErrorAsync(string message) => ShowCriticalErrorAsync(message);

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
            EnableSceneChangeReanalysis = true,
            EnableCallSiteRegistry = true
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
