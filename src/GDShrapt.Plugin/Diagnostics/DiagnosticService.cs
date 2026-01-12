using GDShrapt.Semantics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Central service for collecting and managing diagnostics across the project.
/// Aggregates results from all lint rules and provides notification events.
/// </summary>
internal class DiagnosticService : IDisposable
{
    private readonly GDProjectMap _projectMap;
    private readonly GDConfigManager _configManager;
    private readonly CacheManager? _cacheManager;

    private readonly ConcurrentDictionary<ScriptReference, IReadOnlyList<Diagnostic>> _diagnostics = new();
    private readonly SemaphoreSlim _analysisLock = new(1, 1);

    private bool _disposedValue;

    /// <summary>
    /// Event fired when diagnostics change for a script.
    /// </summary>
    public event Action<DiagnosticsChangedEventArgs>? OnDiagnosticsChanged;

    /// <summary>
    /// Event fired when project-wide analysis completes.
    /// </summary>
    public event Action<ProjectAnalysisCompletedEventArgs>? OnProjectAnalysisCompleted;

    /// <summary>
    /// Creates a new DiagnosticService.
    /// </summary>
    public DiagnosticService(GDProjectMap projectMap, GDConfigManager configManager, CacheManager? cacheManager = null)
    {
        _projectMap = projectMap;
        _configManager = configManager;
        _cacheManager = cacheManager;

        // Subscribe to config changes to re-analyze
        _configManager.OnConfigChanged += OnConfigChanged;
    }

    /// <summary>
    /// Gets diagnostics for a specific script.
    /// </summary>
    public IReadOnlyList<Diagnostic> GetDiagnostics(ScriptReference script)
    {
        if (_diagnostics.TryGetValue(script, out var diagnostics))
        {
            return diagnostics;
        }

        return Array.Empty<Diagnostic>();
    }

    /// <summary>
    /// Gets all diagnostics across the project.
    /// </summary>
    public IReadOnlyList<Diagnostic> GetAllDiagnostics()
    {
        return _diagnostics.Values.SelectMany(d => d).ToList();
    }

    /// <summary>
    /// Gets a summary of all diagnostics.
    /// </summary>
    public DiagnosticSummary GetProjectSummary()
    {
        var all = GetAllDiagnostics();

        return new DiagnosticSummary
        {
            ErrorCount = all.Count(d => d.Severity == GDDiagnosticSeverity.Error),
            WarningCount = all.Count(d => d.Severity == GDDiagnosticSeverity.Warning),
            HintCount = all.Count(d => d.Severity == GDDiagnosticSeverity.Hint || d.Severity == GDDiagnosticSeverity.Info),
            AffectedFileCount = _diagnostics.Count(kv => kv.Value.Count > 0),
            HasFormattingIssues = all.Any(d => d.Category == GDDiagnosticCategory.Formatting && d.Fixes.Count > 0)
        };
    }

    /// <summary>
    /// Gets a summary for a specific script.
    /// </summary>
    public DiagnosticSummary GetScriptSummary(ScriptReference script)
    {
        var diagnostics = GetDiagnostics(script);

        return new DiagnosticSummary
        {
            ErrorCount = diagnostics.Count(d => d.Severity == GDDiagnosticSeverity.Error),
            WarningCount = diagnostics.Count(d => d.Severity == GDDiagnosticSeverity.Warning),
            HintCount = diagnostics.Count(d => d.Severity == GDDiagnosticSeverity.Hint || d.Severity == GDDiagnosticSeverity.Info),
            AffectedFileCount = diagnostics.Count > 0 ? 1 : 0,
            HasFormattingIssues = diagnostics.Any(d => d.Category == GDDiagnosticCategory.Formatting && d.Fixes.Count > 0)
        };
    }

    /// <summary>
    /// Analyzes a single script and updates diagnostics.
    /// Uses GDDiagnosticsService from Semantics kernel for unified diagnostics.
    /// </summary>
    public async Task AnalyzeScriptAsync(GDScriptMap scriptMap, CancellationToken cancellationToken = default)
    {
        if (!_configManager.Config.Linting.Enabled)
        {
            ClearDiagnostics(scriptMap.Reference);
            return;
        }

        try
        {
            await _analysisLock.WaitAsync(cancellationToken);

            var content = await GetScriptContent(scriptMap);
            if (string.IsNullOrEmpty(content))
            {
                ClearDiagnostics(scriptMap.Reference);
                return;
            }

            // Check cache first
            var contentHash = ComputeHash(content);
            if (_cacheManager != null && _cacheManager.TryGetLintCache(scriptMap.Reference, contentHash, out var cached))
            {
                _diagnostics[scriptMap.Reference] = cached;
                NotifyDiagnosticsChanged(scriptMap.Reference, cached);
                Logger.Debug($"Using cached diagnostics for {scriptMap.Reference.FullPath}");
                return;
            }

            // Wait for script to be parsed
            await scriptMap.GetOrWaitAnalyzer();

            if (scriptMap.Class == null)
            {
                ClearDiagnostics(scriptMap.Reference);
                return;
            }

            // Create diagnostics service from config and run analysis asynchronously
            var diagnosticsService = GDDiagnosticsService.FromConfig(_configManager.Config);

            var result = await Task.Run(() =>
                diagnosticsService.Diagnose(scriptMap.Class),
                cancellationToken);

            // Convert to Plugin diagnostics
            var diagnostics = result.Diagnostics
                .Select(d => PluginDiagnosticAdapter.Convert(d, scriptMap.Reference))
                .ToList();

            // Store results
            _diagnostics[scriptMap.Reference] = diagnostics;

            // Cache results
            _cacheManager?.StoreLintCache(scriptMap.Reference, contentHash, diagnostics);

            NotifyDiagnosticsChanged(scriptMap.Reference, diagnostics);

            Logger.Debug($"Analyzed {scriptMap.Reference.FullPath}: {diagnostics.Count} diagnostics");
        }
        catch (OperationCanceledException)
        {
            Logger.Debug($"Analysis cancelled for {scriptMap.Reference.FullPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error analyzing script: {ex.Message}");
        }
        finally
        {
            _analysisLock.Release();
        }
    }

    /// <summary>
    /// Analyzes all scripts in the project.
    /// </summary>
    public async Task AnalyzeProjectAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var filesAnalyzed = 0;

        Logger.Debug("Starting project-wide analysis...");

        try
        {
            var scripts = _projectMap.Scripts.ToList();

            foreach (var script in scripts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await AnalyzeScriptAsync(script, cancellationToken);
                filesAnalyzed++;
            }

            var duration = DateTime.UtcNow - startTime;
            var summary = GetProjectSummary();

            Logger.Debug($"Project analysis completed in {duration.TotalMilliseconds:F0}ms: {summary}");

            OnProjectAnalysisCompleted?.Invoke(new ProjectAnalysisCompletedEventArgs
            {
                Summary = summary,
                Duration = duration,
                FilesAnalyzed = filesAnalyzed
            });
        }
        catch (OperationCanceledException)
        {
            Logger.Debug("Project analysis was cancelled");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error during project analysis: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears diagnostics for a script.
    /// </summary>
    public void ClearDiagnostics(ScriptReference script)
    {
        if (_diagnostics.TryRemove(script, out _))
        {
            NotifyDiagnosticsChanged(script, Array.Empty<Diagnostic>());
        }
    }

    /// <summary>
    /// Clears all diagnostics.
    /// </summary>
    public void ClearAllDiagnostics()
    {
        _diagnostics.Clear();
    }

    /// <summary>
    /// Applies all available formatting fixes to a script.
    /// </summary>
    public async Task<string?> ApplyFormattingFixesAsync(GDScriptMap scriptMap)
    {
        var diagnostics = GetDiagnostics(scriptMap.Reference);
        var formattingDiags = diagnostics
            .Where(d => d.Category == GDDiagnosticCategory.Formatting && d.Fixes.Count > 0)
            .ToList();

        if (formattingDiags.Count == 0)
            return null;

        var content = await GetScriptContent(scriptMap);
        if (string.IsNullOrEmpty(content))
            return null;

        // Apply fixes (in reverse order to preserve line numbers)
        var sortedDiags = formattingDiags
            .OrderByDescending(d => d.StartLine)
            .ThenByDescending(d => d.StartColumn)
            .ToList();

        foreach (var diag in sortedDiags)
        {
            if (diag.Fixes.Count > 0)
            {
                var fix = diag.Fixes[0]; // Apply first available fix
                content = fix.Apply(content);
            }
        }

        Logger.Debug($"Applied {sortedDiags.Count} formatting fixes");
        return content;
    }

    private async Task<string?> GetScriptContent(GDScriptMap scriptMap)
    {
        try
        {
            // Wait for script to be parsed
            await scriptMap.GetOrWaitAnalyzer();

            // Read current content from disk
            if (System.IO.File.Exists(scriptMap.Reference.FullPath))
            {
                return await System.IO.File.ReadAllTextAsync(scriptMap.Reference.FullPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error reading script content: {ex.Message}");
        }

        return null;
    }

    private static string ComputeHash(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private void NotifyDiagnosticsChanged(ScriptReference script, IReadOnlyList<Diagnostic> diagnostics)
    {
        OnDiagnosticsChanged?.Invoke(new DiagnosticsChangedEventArgs
        {
            Script = script,
            Diagnostics = diagnostics,
            Summary = GetScriptSummary(script)
        });
    }

    private void OnConfigChanged(GDProjectConfig config)
    {
        Logger.Debug("Configuration changed, re-analyzing project...");
        // Clear cache when config changes
        _cacheManager?.InvalidateAll();
        // Re-analyze is triggered by BackgroundAnalyzer
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _configManager.OnConfigChanged -= OnConfigChanged;
                _analysisLock.Dispose();
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
