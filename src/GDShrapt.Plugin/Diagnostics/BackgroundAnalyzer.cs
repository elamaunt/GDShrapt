using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Manages background analysis of scripts with debouncing and prioritization.
/// </summary>
internal class BackgroundAnalyzer : IDisposable
{
    private readonly DiagnosticService _diagnosticService;
    private readonly GDProjectMap _projectMap;

    private readonly ConcurrentQueue<AnalysisRequest> _queue = new();
    private readonly ConcurrentDictionary<string, DateTime> _pendingScripts = new();
    private readonly SemaphoreSlim _queueSignal = new(0);
    private readonly CancellationTokenSource _cts = new();

    private Task? _workerTask;
    private bool _disposedValue;
    private string? _priorityScript;

    private const double DebounceDelayMs = 500;
    private const int MaxQueueSize = 100;

    /// <summary>
    /// Creates a new BackgroundAnalyzer.
    /// </summary>
    public BackgroundAnalyzer(DiagnosticService diagnosticService, GDProjectMap projectMap)
    {
        _diagnosticService = diagnosticService;
        _projectMap = projectMap;
    }

    /// <summary>
    /// Starts the background worker.
    /// </summary>
    public void Start()
    {
        if (_workerTask != null)
            return;

        _workerTask = Task.Run(WorkerLoop);
        Logger.Debug("Background analyzer started");
    }

    /// <summary>
    /// Stops the background worker.
    /// </summary>
    public void Stop()
    {
        _cts.Cancel();
        _queueSignal.Release(); // Unblock worker

        try
        {
            _workerTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException ex)
        {
            Logger.Debug($"Background analyzer stop timeout: {ex.InnerExceptions.Count} task(s) did not complete");
        }

        Logger.Debug("Background analyzer stopped");
    }

    /// <summary>
    /// Queues a script for analysis with debouncing.
    /// </summary>
    public void QueueScriptAnalysis(ScriptReference script, bool priority = false)
    {
        if (_cts.IsCancellationRequested)
            return;

        var path = script.FullPath;
        var now = DateTime.UtcNow;

        // Update pending timestamp (for debouncing)
        _pendingScripts[path] = now;

        // Set as priority if requested (e.g., currently open file)
        if (priority)
            _priorityScript = path;

        // Don't queue if already pending
        if (_queue.Count < MaxQueueSize)
        {
            _queue.Enqueue(new AnalysisRequest
            {
                Script = script,
                QueuedAt = now,
                IsPriority = priority
            });

            _queueSignal.Release();
        }
    }

    /// <summary>
    /// Queues all project scripts for analysis.
    /// </summary>
    public void QueueProjectAnalysis()
    {
        if (_cts.IsCancellationRequested)
            return;

        foreach (var script in _projectMap.Scripts)
        {
            QueueScriptAnalysis(script.Reference, priority: false);
        }

        Logger.Debug("Queued scripts for analysis");
    }

    /// <summary>
    /// Sets the priority script (currently open file).
    /// </summary>
    public void SetPriorityScript(ScriptReference? script)
    {
        _priorityScript = script?.FullPath;
    }

    private async Task WorkerLoop()
    {
        Logger.Debug("Background analyzer worker started");

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                // Wait for work
                await _queueSignal.WaitAsync(_cts.Token);

                // Debounce - wait a bit before processing
                await Task.Delay(TimeSpan.FromMilliseconds(DebounceDelayMs), _cts.Token);

                // Process queue
                while (_queue.TryDequeue(out var request))
                {
                    if (_cts.IsCancellationRequested)
                        break;

                    // Check if this request is stale (newer request pending)
                    if (_pendingScripts.TryGetValue(request.Script.FullPath, out var lastQueued))
                    {
                        if (request.QueuedAt < lastQueued)
                        {
                            // Skip - newer request pending
                            continue;
                        }
                    }

                    await ProcessRequest(request);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error($"Background analyzer error: {ex.Message}");
                await Task.Delay(1000); // Prevent tight loop on repeated errors
            }
        }

        Logger.Debug("Background analyzer worker stopped");
    }

    private async Task ProcessRequest(AnalysisRequest request)
    {
        try
        {
            var scriptMap = _projectMap.GetScriptMap(request.Script);
            if (scriptMap == null)
                return;

            // Check if this is priority (process immediately)
            var isPriority = request.IsPriority || request.Script.FullPath == _priorityScript;

            if (!isPriority)
            {
                // Small delay between non-priority analyses to avoid blocking
                await Task.Delay(50, _cts.Token);
            }

            await _diagnosticService.AnalyzeScriptAsync(scriptMap, _cts.Token);

            // Remove from pending
            _pendingScripts.TryRemove(request.Script.FullPath, out _);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Debug($"Failed to analyze {request.Script.FullPath}: {ex.Message}");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Stop();
                _cts.Dispose();
                _queueSignal.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private class AnalysisRequest
    {
        public required ScriptReference Script { get; init; }
        public DateTime QueuedAt { get; init; }
        public bool IsPriority { get; init; }
    }
}
