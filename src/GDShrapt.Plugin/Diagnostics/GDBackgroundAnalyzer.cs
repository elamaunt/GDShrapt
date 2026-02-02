using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Manages background analysis of scripts.
///
/// SIMPLIFIED: The semantic core (GDScriptProject) now handles:
/// - Debouncing via FileChangeDebounceMs
/// - Parallel analysis via AnalyzeAllAsync()
/// - File watching and ScriptChanged events
///
/// This class now only handles:
/// - Priority queue for open tabs (immediate analysis)
/// - Initial project analysis trigger on startup
/// </summary>
internal class GDBackgroundAnalyzer : IDisposable
{
    private readonly GDPluginDiagnosticService _diagnosticService;
    private readonly GDScriptProject _scriptProject;
    private readonly CancellationTokenSource _cts = new();

    // Priority queue for open files (analyze immediately)
    private readonly ConcurrentQueue<GDScriptFile> _priorityQueue = new();
    private readonly SemaphoreSlim _prioritySignal = new(0);

    private Task? _priorityWorkerTask;
    private bool _disposedValue;
    private bool _started;

    public GDBackgroundAnalyzer(GDPluginDiagnosticService diagnosticService, GDScriptProject scriptProject)
    {
        _diagnosticService = diagnosticService;
        _scriptProject = scriptProject;
    }

    /// <summary>
    /// Starts the background analyzer.
    /// </summary>
    public void Start()
    {
        if (_started)
            return;

        _started = true;
        _priorityWorkerTask = Task.Run(PriorityWorkerLoop);
        Logger.Debug("Background analyzer started");
    }

    /// <summary>
    /// Stops the background analyzer.
    /// </summary>
    public void Stop()
    {
        _cts.Cancel();
        _prioritySignal.Release(); // Unblock worker

        try
        {
            _priorityWorkerTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Expected on cancellation
        }

        Logger.Debug("Background analyzer stopped");
    }

    /// <summary>
    /// Queues a script for priority analysis (e.g., currently open file).
    /// These are analyzed immediately without debouncing.
    /// </summary>
    public void QueueScriptAnalysis(GDScriptFile script, bool priority = false)
    {
        if (_cts.IsCancellationRequested)
            return;

        if (priority)
        {
            // Priority scripts go to fast-track queue
            _priorityQueue.Enqueue(script);
            _prioritySignal.Release();
        }
        // Non-priority scripts are handled by the semantic core via ScriptChanged events
    }

    /// <summary>
    /// Triggers project-wide analysis using the parallel analyzer in the semantic core.
    /// </summary>
    public void QueueProjectAnalysis()
    {
        if (_cts.IsCancellationRequested)
            return;

        Logger.Info("Queueing project analysis...");

        // Use the parallel analysis from the semantic core
        Task.Run(async () =>
        {
            try
            {
                await _diagnosticService.AnalyzeProjectAsync(forceRefresh: false, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                Logger.Debug("Project analysis cancelled");
            }
            catch (Exception ex)
            {
                Logger.Error($"Project analysis failed: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Sets the priority script (currently open file).
    /// Queues it for immediate analysis.
    /// </summary>
    public void SetPriorityScript(GDScriptFile? script)
    {
        if (script != null)
        {
            QueueScriptAnalysis(script, priority: true);
        }
    }

    /// <summary>
    /// Worker loop for priority (open tab) scripts.
    /// These bypass debouncing for immediate feedback.
    /// </summary>
    private async Task PriorityWorkerLoop()
    {
        Logger.Verbose("Priority analyzer worker started");

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await _prioritySignal.WaitAsync(_cts.Token);

                while (_priorityQueue.TryDequeue(out var script))
                {
                    if (_cts.IsCancellationRequested)
                        break;

                    try
                    {
                        Logger.Verbose($"Priority analysis: {script.FullPath}");
                        await _diagnosticService.AnalyzeScriptAsync(script, forceRefresh: false, _cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"Priority analysis failed for {script.FullPath}: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error($"Priority worker error: {ex.Message}");
                await Task.Delay(1000);
            }
        }

        Logger.Verbose("Priority analyzer worker stopped");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Stop();
                _cts.Dispose();
                _prioritySignal.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
