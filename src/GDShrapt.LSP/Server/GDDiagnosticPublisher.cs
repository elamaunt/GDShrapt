using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.LSP;

/// <summary>
/// Publishes diagnostics to the LSP client.
/// Supports debouncing to avoid excessive updates during rapid typing.
/// Uses GDDiagnosticsService for unified validation and linting.
/// </summary>
public class GDDiagnosticPublisher : IAsyncDisposable
{
    private readonly IGDJsonRpcTransport _transport;
    private readonly GDScriptProject _project;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pendingUpdates = new();
    private readonly TimeSpan _debounceDelay;
    private GDDiagnosticsService _diagnosticsService;
    private bool _disposed;

    /// <summary>
    /// Creates a new diagnostic publisher.
    /// </summary>
    /// <param name="transport">The JSON-RPC transport for sending notifications.</param>
    /// <param name="project">The project for script analysis.</param>
    /// <param name="debounceDelay">Delay before publishing diagnostics (default 300ms).</param>
    /// <param name="config">Optional project configuration for diagnostics.</param>
    public GDDiagnosticPublisher(
        IGDJsonRpcTransport transport,
        GDScriptProject project,
        TimeSpan? debounceDelay = null,
        GDProjectConfig? config = null)
    {
        _transport = transport;
        _project = project;
        _debounceDelay = debounceDelay ?? TimeSpan.FromMilliseconds(300);
        _diagnosticsService = config != null
            ? GDDiagnosticsService.FromConfig(config)
            : new GDDiagnosticsService();
    }

    /// <summary>
    /// Updates the diagnostics service with new configuration.
    /// </summary>
    public void UpdateConfig(GDProjectConfig config)
    {
        _diagnosticsService = GDDiagnosticsService.FromConfig(config);
    }

    /// <summary>
    /// Schedules a diagnostic update for the specified document.
    /// Uses debouncing to avoid excessive updates.
    /// </summary>
    public void ScheduleUpdate(string uri, int? version = null)
    {
        if (_disposed)
            return;

        // Cancel any pending update for this URI
        if (_pendingUpdates.TryRemove(uri, out var existingCts))
        {
            existingCts.Cancel();
            existingCts.Dispose();
        }

        // Create new cancellation token for this update
        var cts = new CancellationTokenSource();
        _pendingUpdates[uri] = cts;

        // Schedule the update with debounce delay
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_debounceDelay, cts.Token).ConfigureAwait(false);

                if (!cts.Token.IsCancellationRequested)
                {
                    await PublishDiagnosticsAsync(uri, version).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when update is cancelled
            }
            finally
            {
                _pendingUpdates.TryRemove(uri, out _);
                cts.Dispose();
            }
        });
    }

    /// <summary>
    /// Immediately publishes diagnostics for the specified document.
    /// Uses GDDiagnosticsService for unified validation and linting.
    /// </summary>
    public async Task PublishDiagnosticsAsync(string uri, int? version = null)
    {
        if (_disposed)
            return;

        var filePath = GDDocumentManager.UriToPath(uri);
        var script = _project.GetScript(filePath);

        GDLspDiagnostic[] diagnostics;

        if (script != null)
        {
            // Use diagnostics service with both validator and linter
            diagnostics = GDDiagnosticAdapter.FromScript(script, _diagnosticsService);
        }
        else
        {
            // No script found - clear diagnostics
            diagnostics = [];
        }

        var @params = new GDPublishDiagnosticsParams
        {
            Uri = uri,
            Version = version,
            Diagnostics = diagnostics
        };

        await _transport.SendNotificationAsync("textDocument/publishDiagnostics", @params)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Clears diagnostics for the specified document.
    /// </summary>
    public async Task ClearDiagnosticsAsync(string uri)
    {
        if (_disposed)
            return;

        // Cancel any pending update
        if (_pendingUpdates.TryRemove(uri, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        var @params = new GDPublishDiagnosticsParams
        {
            Uri = uri,
            Diagnostics = []
        };

        await _transport.SendNotificationAsync("textDocument/publishDiagnostics", @params)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes diagnostics for all open documents.
    /// </summary>
    public async Task PublishAllAsync(GDDocumentManager documentManager)
    {
        if (_disposed)
            return;

        foreach (var doc in documentManager.GetAllDocuments())
        {
            await PublishDiagnosticsAsync(doc.Uri, doc.Version).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Cancel all pending updates
        foreach (var kvp in _pendingUpdates)
        {
            kvp.Value.Cancel();
            kvp.Value.Dispose();
        }

        _pendingUpdates.Clear();

        await Task.CompletedTask;
    }
}
