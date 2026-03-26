using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
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
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pendingSemanticUpdates = new();
    private readonly TimeSpan _debounceDelay;
    private readonly TimeSpan _semanticDebounceDelay;
    private readonly Task? _analysisReady;
    private GDDiagnosticsService _diagnosticsService;
    private GDProjectConfig? _config;
    private GDProjectSemanticModel? _projectModel;
    private bool _disposed;

    /// <summary>
    /// Creates a new diagnostic publisher.
    /// </summary>
    /// <param name="transport">The JSON-RPC transport for sending notifications.</param>
    /// <param name="project">The project for script analysis.</param>
    /// <param name="debounceDelay">Delay before publishing diagnostics (default 300ms).</param>
    /// <param name="config">Optional project configuration for diagnostics.</param>
    /// <param name="analysisReady">Task that completes when initial project analysis is done.</param>
    public GDDiagnosticPublisher(
        IGDJsonRpcTransport transport,
        GDScriptProject project,
        TimeSpan? debounceDelay = null,
        GDProjectConfig? config = null,
        Task? analysisReady = null)
    {
        _transport = transport;
        _project = project;
        _debounceDelay = debounceDelay ?? TimeSpan.FromMilliseconds(300);
        _semanticDebounceDelay = TimeSpan.FromMilliseconds(800);
        _config = config;
        _analysisReady = analysisReady;
        _diagnosticsService = config != null
            ? GDDiagnosticsService.FromConfig(config)
            : new GDDiagnosticsService();
    }

    /// <summary>
    /// Sets the project semantic model for rebuilding analysis after edits.
    /// </summary>
    public void SetProjectModel(GDProjectSemanticModel? projectModel)
    {
        _projectModel = projectModel;
    }

    /// <summary>
    /// Updates the diagnostics service with new configuration.
    /// </summary>
    public void UpdateConfig(GDProjectConfig config)
    {
        _config = config;
        _diagnosticsService = GDDiagnosticsService.FromConfig(config);
    }

    /// <summary>
    /// Schedules a diagnostic update for the specified document.
    /// Uses two-tier debouncing: fast syntax diagnostics + delayed semantic diagnostics.
    /// </summary>
    public void ScheduleUpdate(string uri, int? version = null)
    {
        if (_disposed)
            return;

        // Cancel any pending syntax update for this URI
        if (_pendingUpdates.TryRemove(uri, out var existingCts))
        {
            existingCts.Cancel();
            existingCts.Dispose();
        }

        // Cancel any pending semantic update for this URI
        if (_pendingSemanticUpdates.TryRemove(uri, out var existingSemanticCts))
        {
            existingSemanticCts.Cancel();
            existingSemanticCts.Dispose();
        }

        // Tier 1: Fast syntax + linting diagnostics (short debounce)
        var syntaxCts = new CancellationTokenSource();
        _pendingUpdates[uri] = syntaxCts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_debounceDelay, syntaxCts.Token).ConfigureAwait(false);

                if (!syntaxCts.Token.IsCancellationRequested)
                {
                    await PublishSyntaxDiagnosticsAsync(uri, version).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                GDLspPerformanceTrace.Log("diagnostics", $"SYNTAX-ERROR {uri}: {ex}");
            }
            finally
            {
                _pendingUpdates.TryRemove(uri, out _);
                syntaxCts.Dispose();
            }
        });

        // Tier 2: Full semantic diagnostics (longer debounce)
        var semanticCts = new CancellationTokenSource();
        _pendingSemanticUpdates[uri] = semanticCts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_semanticDebounceDelay, semanticCts.Token).ConfigureAwait(false);

                if (!semanticCts.Token.IsCancellationRequested)
                {
                    await PublishDiagnosticsAsync(uri, version).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                GDLspPerformanceTrace.Log("diagnostics", $"SEMANTIC-ERROR {uri}: {ex}");
            }
            finally
            {
                _pendingSemanticUpdates.TryRemove(uri, out _);
                semanticCts.Dispose();
            }
        });
    }

    /// <summary>
    /// Publishes fast syntax + linting diagnostics (Tier 1).
    /// Does NOT wait for initial analysis or rebuild semantic model.
    /// </summary>
    private async Task PublishSyntaxDiagnosticsAsync(string uri, int? version = null)
    {
        if (_disposed)
            return;

        // Skip syntax-only tier if initial analysis hasn't completed — full diagnostics will handle it
        if (_analysisReady != null && !_analysisReady.IsCompleted)
            return;

        var filename = System.IO.Path.GetFileName(GDDocumentManager.UriToPath(uri));
        GDLspPerformanceTrace.Log("diagnostics", $"SYNTAX-START {filename}");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var filePath = GDDocumentManager.UriToPath(uri);
        var script = _project.GetScript(filePath);

        GDLspDiagnostic[] diagnostics;

        if (script != null)
        {
            // Syntax validation + linting only (no semantic model needed)
            var result = _diagnosticsService.Diagnose(script);
            diagnostics = result.Diagnostics.Select(d => GDDiagnosticAdapter.FromUnifiedDiagnostic(d)).ToArray();
        }
        else if (GDSceneDiagnosticsHandler.IsSceneFile(filePath))
        {
            var sceneHandler = new GDSceneDiagnosticsHandler(_project);
            var sceneDiags = sceneHandler.AnalyzeScene(filePath);
            diagnostics = sceneDiags.Select(d => GDDiagnosticAdapter.FromUnifiedDiagnostic(d)).ToArray();
        }
        else
        {
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

        sw.Stop();
        GDLspPerformanceTrace.Log("diagnostics", $"SYNTAX-END {filename} {sw.ElapsedMilliseconds}ms count={diagnostics.Length}");
    }

    /// <summary>
    /// Immediately publishes full diagnostics for the specified document.
    /// Includes syntax + validator + linter + semantic validator.
    /// </summary>
    public async Task PublishDiagnosticsAsync(string uri, int? version = null)
    {
        if (_disposed)
            return;

        var filename = System.IO.Path.GetFileName(GDDocumentManager.UriToPath(uri));

        // Wait for initial analysis to complete before publishing diagnostics.
        // This prevents false positives from missing runtime provider (GD2001, GD3005, GD3006).
        if (_analysisReady != null && !_analysisReady.IsCompleted)
        {
            GDLspPerformanceTrace.Log("diagnostics", $"WAIT-ANALYSIS {filename}");
            try
            {
                await _analysisReady.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                GDLspPerformanceTrace.Log("diagnostics", $"ANALYSIS-FAILED {filename}: {ex.Message}");
            }
            GDLspPerformanceTrace.Log("diagnostics", $"ANALYSIS-READY {filename}");
        }

        GDLspPerformanceTrace.Log("diagnostics", $"START {filename}");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var filePath = GDDocumentManager.UriToPath(uri);
        var script = _project.GetScript(filePath);

        // Rebuild semantic model if it was cleared by Reload (e.g. after didChange)
        if (script != null && script.SemanticModel == null && _projectModel != null)
        {
            GDLspPerformanceTrace.Log("diagnostics", $"REBUILD-MODEL {filename}");
            var rebuildSw = System.Diagnostics.Stopwatch.StartNew();
            _projectModel.GetSemanticModel(script);
            rebuildSw.Stop();
            GDLspPerformanceTrace.Log("diagnostics", $"REBUILD-DONE {filename} {rebuildSw.ElapsedMilliseconds}ms hasModel={script.SemanticModel != null}");
        }

        GDLspDiagnostic[] diagnostics;

        if (script != null)
        {
            // Use unified pipeline: syntax + validator + linter + semantic validator
            var result = GDDiagnosticsHandler.DiagnoseWithSemantics(script, _diagnosticsService, config: _config);
            diagnostics = result.Diagnostics.Select(d => GDDiagnosticAdapter.FromUnifiedDiagnostic(d)).ToArray();
        }
        else if (GDSceneDiagnosticsHandler.IsSceneFile(filePath))
        {
            var sceneHandler = new GDSceneDiagnosticsHandler(_project);
            var sceneDiags = sceneHandler.AnalyzeScene(filePath);
            diagnostics = sceneDiags.Select(d => GDDiagnosticAdapter.FromUnifiedDiagnostic(d)).ToArray();
        }
        else
        {
            // No script found - clear diagnostics
            diagnostics = [];
        }

        var diagnoseMs = sw.ElapsedMilliseconds;

        var @params = new GDPublishDiagnosticsParams
        {
            Uri = uri,
            Version = version,
            Diagnostics = diagnostics
        };

        await _transport.SendNotificationAsync("textDocument/publishDiagnostics", @params)
            .ConfigureAwait(false);

        sw.Stop();
        GDLspPerformanceTrace.Log("diagnostics", $"END {filename} diagnose={diagnoseMs}ms total={sw.ElapsedMilliseconds}ms hasModel={script?.SemanticModel != null} count={diagnostics.Length}");
    }

    /// <summary>
    /// Clears diagnostics for the specified document.
    /// </summary>
    public async Task ClearDiagnosticsAsync(string uri)
    {
        if (_disposed)
            return;

        // Cancel any pending updates
        if (_pendingUpdates.TryRemove(uri, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
        if (_pendingSemanticUpdates.TryRemove(uri, out var semanticCts))
        {
            semanticCts.Cancel();
            semanticCts.Dispose();
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

        foreach (var kvp in _pendingSemanticUpdates)
        {
            kvp.Value.Cancel();
            kvp.Value.Dispose();
        }
        _pendingSemanticUpdates.Clear();

        await Task.CompletedTask;
    }
}
