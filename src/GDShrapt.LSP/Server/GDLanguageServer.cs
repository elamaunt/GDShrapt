using System;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.LSP.Handlers;
using GDShrapt.LSP.Protocol;
using GDShrapt.LSP.Protocol.Types;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.Server;

/// <summary>
/// GDScript Language Server implementation.
/// </summary>
public class GDLanguageServer : IGDLanguageServer
{
    private GDScriptProject? _project;
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

        // Language features
        _transport.OnRequest<GDDefinitionParams, GDLspLocation?>("textDocument/definition", HandleDefinitionAsync);
        _transport.OnRequest<GDReferencesParams, GDLspLocation[]?>("textDocument/references", HandleReferencesAsync);
        _transport.OnRequest<GDHoverParams, GDLspHover?>("textDocument/hover", HandleHoverAsync);
        _transport.OnRequest<GDDocumentSymbolParams, GDLspDocumentSymbol[]?>("textDocument/documentSymbol", HandleDocumentSymbolAsync);
        _transport.OnRequest<GDCompletionParams, GDLspCompletionList?>("textDocument/completion", HandleCompletionAsync);
        _transport.OnRequest<GDRenameParams, GDWorkspaceEdit?>("textDocument/rename", HandleRenameAsync);
        _transport.OnRequest<GDDocumentFormattingParams, GDLspTextEdit[]?>("textDocument/formatting", HandleFormattingAsync);
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
                    project = GDProjectLoader.LoadProject(projectRoot);
                    _documentManager = new GDDocumentManager(project);
                    _diagnosticPublisher = new GDDiagnosticPublisher(_transport!, project);
                    _project = project;
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
                }
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
        return Task.CompletedTask;
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

    #region Language Feature Handlers

    private Task<GDLspLocation?> HandleDefinitionAsync(GDDefinitionParams @params, CancellationToken ct)
    {
        if (_project == null)
            return Task.FromResult<GDLspLocation?>(null);

        var handler = new GDDefinitionHandler(_project);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspLocation[]?> HandleReferencesAsync(GDReferencesParams @params, CancellationToken ct)
    {
        if (_project == null)
            return Task.FromResult<GDLspLocation[]?>(null);

        var handler = new GDReferencesHandler(_project);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspHover?> HandleHoverAsync(GDHoverParams @params, CancellationToken ct)
    {
        if (_project == null)
            return Task.FromResult<GDLspHover?>(null);

        var handler = new GDHoverHandler(_project);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspDocumentSymbol[]?> HandleDocumentSymbolAsync(GDDocumentSymbolParams @params, CancellationToken ct)
    {
        if (_project == null)
            return Task.FromResult<GDLspDocumentSymbol[]?>(null);

        var handler = new GDDocumentSymbolHandler(_project);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspCompletionList?> HandleCompletionAsync(GDCompletionParams @params, CancellationToken ct)
    {
        if (_project == null)
            return Task.FromResult<GDLspCompletionList?>(null);

        var handler = new GDCompletionHandler(_project);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDWorkspaceEdit?> HandleRenameAsync(GDRenameParams @params, CancellationToken ct)
    {
        if (_project == null)
            return Task.FromResult<GDWorkspaceEdit?>(null);

        var handler = new GDRenameHandler(_project);
        return handler.HandleAsync(@params, ct);
    }

    private Task<GDLspTextEdit[]?> HandleFormattingAsync(GDDocumentFormattingParams @params, CancellationToken ct)
    {
        if (_project == null)
            return Task.FromResult<GDLspTextEdit[]?>(null);

        var handler = new GDFormattingHandler(_project);
        return handler.HandleAsync(@params, ct);
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

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

    public static GDScriptProject LoadProject(string projectRoot)
    {
        var context = new GDDefaultProjectContext(projectRoot);
        var project = new GDScriptProject(context, new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = true
        });

        project.LoadScripts();
        project.LoadScenes();
        project.AnalyzeAll();

        return project;
    }
}
