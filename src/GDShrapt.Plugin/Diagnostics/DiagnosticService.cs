using System.Collections.Concurrent;

namespace GDShrapt.Plugin;

/// <summary>
/// Central service for collecting and managing diagnostics across the project.
/// Aggregates results from all lint rules and provides notification events.
/// </summary>
internal class DiagnosticService : IDisposable
{
    private readonly GDScriptProject _scriptProject;
    private readonly GDConfigManager _configManager;
    private readonly CacheManager? _cacheManager;

    private readonly ConcurrentDictionary<string, IReadOnlyList<Diagnostic>> _diagnostics = new();
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
    public DiagnosticService(GDScriptProject ScriptProject, GDConfigManager configManager, CacheManager? cacheManager = null)
    {
        _scriptProject = ScriptProject;
        _configManager = configManager;
        _cacheManager = cacheManager;

        // Subscribe to config changes to re-analyze
        _configManager.OnConfigChanged += OnConfigChanged;
    }

    /// <summary>
    /// Gets diagnostics for a specific script.
    /// </summary>
    public IReadOnlyList<Diagnostic> GetDiagnostics(GDScriptFile map)
    {
        if (_diagnostics.TryGetValue(map.FullPath, out var diagnostics))
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
    public DiagnosticSummary GetScriptSummary(GDScriptFile map)
    {
        var diagnostics = GetDiagnostics(map);

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
    public async Task AnalyzeScriptAsync(GDScriptFile ScriptFile, CancellationToken cancellationToken = default)
    {
        if (!_configManager.Config.Linting.Enabled)
        {
            Logger.Debug($"Linting disabled, skipping {ScriptFile.FullPath}");
            ClearDiagnostics(ScriptFile);
            return;
        }

        try
        {
            Logger.Info("Wait for lock..");

            await _analysisLock.WaitAsync(cancellationToken);
            Logger.Info("Lock entered");

            var content = await GetScriptContent(ScriptFile);
            Logger.Info("Got script content");

            if (string.IsNullOrEmpty(content))
            {
                Logger.Info("The content is empty");
                ClearDiagnostics(ScriptFile);
                return;
            }

            // Check cache first
            var contentHash = ComputeHash(content);

            Logger.Info("Content hash " + contentHash);
            if (_cacheManager != null && _cacheManager.TryGetLintCache(ScriptFile, contentHash, out var cached))
            {
                _diagnostics[ScriptFile.FullPath] = cached;
                NotifyDiagnosticsChanged(ScriptFile, cached);
                Logger.Debug($"Using cached diagnostics for {ScriptFile.FullPath}");
                return;
            }

            Logger.Info("Wait for binding");

            // Wait for script to be parsed
            //await ScriptFile.GetOrWaitAnalyzer();

            Logger.Info("Ready");

            if (ScriptFile.Class == null)
            {
                Logger.Debug($"Script has no AST class, skipping {ScriptFile.FullPath}");
                ClearDiagnostics(ScriptFile);
                return;
            }

            Logger.Info("Get service");
            // Create diagnostics service from config and run analysis asynchronously
            var diagnosticsService = GDDiagnosticsService.FromConfig(_configManager.Config);

            Logger.Info("Diagnosing initialised");

            // IMPORTANT: Use Diagnose(GDScriptFile) not Diagnose(GDClassDeclaration)
            // to get semantic model with inheritance support for proper member resolution
            var result = await Task.Run(() =>
                {
                    Logger.Info("Diagnosing started");
                    // Pass full ScriptFile to use semantic model with inheritance support
                    return diagnosticsService.Diagnose(ScriptFile);
                },
                cancellationToken);
          
            Logger.Info("Diagnosing finished");

            // Convert to Plugin diagnostics
            var diagnostics = result.Diagnostics
                .Select(d => PluginDiagnosticAdapter.Convert(d, ScriptFile))
                .ToList();

            // Store results
            _diagnostics[ScriptFile.FullPath] = diagnostics;

            // Cache results
            _cacheManager?.StoreLintCache(ScriptFile, contentHash, diagnostics);

            NotifyDiagnosticsChanged(ScriptFile, diagnostics);

            Logger.Debug($"Analyzed {ScriptFile.FullPath}: {diagnostics.Count} diagnostics");
        }
        catch (OperationCanceledException)
        {
            Logger.Debug($"Analysis cancelled for {ScriptFile.FullPath}");
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

        Logger.Info("Starting project-wide analysis...");

        try
        {
            var maps = _scriptProject.ScriptFiles.ToList();
            Logger.Info($"Found {maps.Count} scripts to analyze");

            if (maps.Count == 0)
            {
                Logger.Info("No scripts found in project map. Skipping analysis.");
                return;
            }

            int i = 0;
            foreach (var map in maps)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.Info("Script analysing.. " + i++);
                await AnalyzeScriptAsync(map, cancellationToken);
                Logger.Info("Script complete " + (i - 1));
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
    public void ClearDiagnostics(GDScriptFile script)
    {
        if (_diagnostics.TryRemove(script.FullPath, out _))
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
    public async Task<string?> ApplyFormattingFixesAsync(GDScriptFile ScriptFile)
    {
        var diagnostics = GetDiagnostics(ScriptFile);
        var formattingDiags = diagnostics
            .Where(d => d.Category == GDDiagnosticCategory.Formatting && d.Fixes.Count > 0)
            .ToList();

        if (formattingDiags.Count == 0)
            return null;

        var content = await GetScriptContent(ScriptFile);
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

    private async Task<string?> GetScriptContent(GDScriptFile ScriptFile)
    {
        try
        {
            Logger.Debug($"Wait for binding");
            // Wait for script to be parsed
            ScriptFile.Reload();

            Logger.Debug($"Binding ready");

            // Re-analyze the script to populate semantic model
            // This is needed because Reload() clears the Analyzer
            // The semantic model provides inheritance-aware symbol resolution
            var runtimeProvider = _scriptProject.CreateRuntimeProvider();
            ScriptFile.Analyze(runtimeProvider);

            Logger.Debug($"Script analyzed, Analyzer={ScriptFile.Analyzer != null}");

            // Read current content from disk
            if (System.IO.File.Exists(ScriptFile.FullPath))
            {
                return await System.IO.File.ReadAllTextAsync(ScriptFile.FullPath);
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

    private void NotifyDiagnosticsChanged(GDScriptFile script, IReadOnlyList<Diagnostic> diagnostics)
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
