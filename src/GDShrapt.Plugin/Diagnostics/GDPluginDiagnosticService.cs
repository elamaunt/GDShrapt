using System.Collections.Concurrent;
using GDShrapt.Semantics.Validator;

namespace GDShrapt.Plugin;

/// <summary>
/// Central service for collecting and managing diagnostics across the project.
/// Aggregates results from all lint rules and provides notification events.
/// </summary>
internal class GDPluginDiagnosticService : IDisposable
{
    private readonly GDScriptProject _scriptProject;
    private readonly GDConfigManager _configManager;
    private readonly GDCacheManager? _cacheManager;

    private readonly ConcurrentDictionary<string, IReadOnlyList<GDPluginDiagnostic>> _diagnostics = new();

    // Use Interlocked for analysis guards instead of SemaphoreSlim to allow parallelism
    private int _projectAnalysisInProgress = 0;
    private int _scriptAnalysisInProgress = 0;

    private bool _disposedValue;

    /// <summary>
    /// Event fired when diagnostics change for a script.
    /// </summary>
    public event Action<GDDiagnosticsChangedEventArgs>? OnDiagnosticsChanged;

    /// <summary>
    /// Event fired when project-wide analysis completes.
    /// </summary>
    public event Action<GDPluginProjectAnalysisCompletedEventArgs>? OnProjectAnalysisCompleted;

    /// <summary>
    /// Creates a new GDPluginDiagnosticService.
    /// </summary>
    public GDPluginDiagnosticService(GDScriptProject ScriptProject, GDConfigManager configManager, GDCacheManager? cacheManager = null)
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
    public IReadOnlyList<GDPluginDiagnostic> GetDiagnostics(GDScriptFile map)
    {
        if (_diagnostics.TryGetValue(map.FullPath, out var diagnostics))
        {
            return diagnostics;
        }

        return Array.Empty<GDPluginDiagnostic>();
    }

    /// <summary>
    /// Gets all diagnostics across the project.
    /// </summary>
    public IReadOnlyList<GDPluginDiagnostic> GetAllDiagnostics()
    {
        return _diagnostics.Values.SelectMany(d => d).ToList();
    }

    /// <summary>
    /// Gets a summary of all diagnostics.
    /// </summary>
    public GDDiagnosticSummary GetProjectSummary()
    {
        var all = GetAllDiagnostics();

        return new GDDiagnosticSummary
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
    public GDDiagnosticSummary GetScriptSummary(GDScriptFile map)
    {
        var diagnostics = GetDiagnostics(map);

        return new GDDiagnosticSummary
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
    /// <param name="ScriptFile">The script to analyze.</param>
    /// <param name="forceRefresh">If true, skips cache and forces re-analysis.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AnalyzeScriptAsync(GDScriptFile ScriptFile, bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (!_configManager.Config.Linting.Enabled)
        {
            Logger.Debug($"Linting disabled, skipping {ScriptFile.FullPath}");
            ClearDiagnostics(ScriptFile);
            return;
        }

        // Track analysis in progress but don't block - allow parallel script analysis
        Interlocked.Increment(ref _scriptAnalysisInProgress);

        try
        {
            Logger.Verbose($"Analyzing script: {ScriptFile.FullPath}");

            var content = await GetScriptContent(ScriptFile);

            if (string.IsNullOrEmpty(content))
            {
                Logger.Verbose($"Empty content: {ScriptFile.FullPath}");
                ClearDiagnostics(ScriptFile);
                return;
            }

            // Check cache first (skip if forceRefresh)
            var contentHash = ComputeHash(content);

            if (!forceRefresh && _cacheManager != null && _cacheManager.TryGetLintCache(ScriptFile, contentHash, out var cached))
            {
                _diagnostics[ScriptFile.FullPath] = cached;
                NotifyDiagnosticsChanged(ScriptFile, cached);
                Logger.Verbose($"Using cached diagnostics for {ScriptFile.FullPath}");
                return;
            }

            if (ScriptFile.Class == null)
            {
                Logger.Verbose($"Script has no AST class, skipping {ScriptFile.FullPath}");
                ClearDiagnostics(ScriptFile);
                return;
            }

            // Create diagnostics service from config and run analysis
            var diagnosticsService = GDDiagnosticsService.FromConfig(_configManager.Config);

            // IMPORTANT: Use Diagnose(GDScriptFile) not Diagnose(GDClassDeclaration)
            // to get semantic model with inheritance support for proper member resolution
            var result = await Task.Run(() => diagnosticsService.Diagnose(ScriptFile), cancellationToken);

            // Convert to Plugin diagnostics
            var diagnostics = result.Diagnostics
                .Select(d => GDPluginDiagnosticAdapter.Convert(d, ScriptFile))
                .ToList();

            // Run semantic validator for type-aware validation (member access, argument types, indexers, signals, generics)
            if (ScriptFile.SemanticModel != null)
            {
                Logger.Verbose($"Running semantic validation for {ScriptFile.FullPath}");
                var semanticValidatorOptions = new GDSemanticValidatorOptions
                {
                    CheckTypes = true,
                    CheckMemberAccess = true,
                    CheckArgumentTypes = true,
                    CheckIndexers = true,
                    CheckSignalTypes = true,
                    CheckGenericTypes = true
                };

                var semanticValidator = new GDSemanticValidator(ScriptFile.SemanticModel, semanticValidatorOptions);
                var semanticResult = await Task.Run(() => semanticValidator.Validate(ScriptFile.Class), cancellationToken);

                Logger.Verbose($"Semantic validation found {semanticResult.Diagnostics.Count} diagnostics");

                // Convert semantic diagnostics to plugin diagnostics
                foreach (var diagnostic in semanticResult.Diagnostics)
                {
                    var pluginDiagnostic = GDPluginDiagnosticAdapter.ConvertFromValidator(diagnostic, ScriptFile);
                    diagnostics.Add(pluginDiagnostic);
                }
            }

            // Store results
            _diagnostics[ScriptFile.FullPath] = diagnostics;

            // Cache results
            _cacheManager?.StoreLintCache(ScriptFile, contentHash, diagnostics);

            NotifyDiagnosticsChanged(ScriptFile, diagnostics);

            Logger.Verbose($"Analyzed {ScriptFile.FullPath}: {diagnostics.Count} diagnostics");
        }
        catch (OperationCanceledException)
        {
            Logger.Debug($"Analysis cancelled for {ScriptFile.FullPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error analyzing script {ScriptFile.FullPath}: {ex.Message}");
        }
        finally
        {
            Interlocked.Decrement(ref _scriptAnalysisInProgress);
        }
    }

    /// <summary>
    /// Analyzes all scripts in the project using parallel analysis from the semantic core.
    /// This delegates to GDScriptProject.AnalyzeAllAsync() which uses Parallel.ForEach
    /// with GDSemanticsConfig settings for optimal performance.
    /// </summary>
    /// <param name="forceRefresh">If true, skips cache and forces re-analysis of all scripts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AnalyzeProjectAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        // Prevent concurrent project-wide analysis
        if (Interlocked.CompareExchange(ref _projectAnalysisInProgress, 1, 0) != 0)
        {
            Logger.Debug("Project analysis already in progress, skipping");
            return;
        }

        var startTime = DateTime.UtcNow;

        try
        {
            var scriptFiles = _scriptProject.ScriptFiles.ToList();
            Logger.Info($"Starting project analysis: {scriptFiles.Count} scripts...");

            if (scriptFiles.Count == 0)
            {
                Logger.Info("No scripts found in project. Skipping analysis.");
                return;
            }

            // STEP 1: Use GDScriptProject.AnalyzeAllAsync() for parallel semantic analysis
            // This leverages existing Parallel.ForEach with GDSemanticsConfig settings
            Logger.Debug("Running parallel semantic analysis via GDScriptProject.AnalyzeAllAsync()");
            await _scriptProject.AnalyzeAllAsync(cancellationToken);

            // STEP 2: Collect diagnostics from analyzed scripts (parallel)
            Logger.Debug("Collecting diagnostics from analyzed scripts...");
            await CollectDiagnosticsAsync(scriptFiles, forceRefresh, cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            var summary = GetProjectSummary();

            Logger.Info($"Project analysis completed in {duration.TotalMilliseconds:F0}ms: {summary}");

            OnProjectAnalysisCompleted?.Invoke(new GDPluginProjectAnalysisCompletedEventArgs
            {
                Summary = summary,
                Duration = duration,
                FilesAnalyzed = scriptFiles.Count
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
        finally
        {
            Interlocked.Exchange(ref _projectAnalysisInProgress, 0);
        }
    }

    /// <summary>
    /// Collects diagnostics from already-analyzed scripts.
    /// Called after GDScriptProject.AnalyzeAllAsync() completes.
    /// </summary>
    private async Task CollectDiagnosticsAsync(
        IReadOnlyList<GDScriptFile> scriptFiles,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        // Get parallelism from project options
        var maxParallelism = _scriptProject.Options?.SemanticsConfig?.MaxDegreeOfParallelism ?? -1;
        if (maxParallelism < 0)
            maxParallelism = Environment.ProcessorCount;
        if (maxParallelism == 0)
            maxParallelism = 1; // Sequential

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxParallelism,
            CancellationToken = cancellationToken
        };

        // Create diagnostics service once (thread-safe)
        var diagnosticsService = GDDiagnosticsService.FromConfig(_configManager.Config);

        await Task.Run(() =>
        {
            Parallel.ForEach(scriptFiles, parallelOptions, scriptFile =>
            {
                try
                {
                    CollectScriptDiagnostics(scriptFile, diagnosticsService, forceRefresh);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error collecting diagnostics for {scriptFile.FullPath}: {ex.Message}");
                }
            });
        }, cancellationToken);
    }

    /// <summary>
    /// Collects diagnostics for a single script (called from parallel loop).
    /// </summary>
    private void CollectScriptDiagnostics(GDScriptFile scriptFile, GDDiagnosticsService diagnosticsService, bool forceRefresh)
    {
        if (!_configManager.Config.Linting.Enabled)
        {
            ClearDiagnostics(scriptFile);
            return;
        }

        if (scriptFile.Class == null)
        {
            ClearDiagnostics(scriptFile);
            return;
        }

        // Check cache
        var content = scriptFile.Class.ToString();
        var contentHash = ComputeHash(content);

        if (!forceRefresh && _cacheManager != null && _cacheManager.TryGetLintCache(scriptFile, contentHash, out var cached))
        {
            _diagnostics[scriptFile.FullPath] = cached;
            NotifyDiagnosticsChanged(scriptFile, cached);
            return;
        }

        // Run diagnostics
        var result = diagnosticsService.Diagnose(scriptFile);
        var diagnostics = result.Diagnostics
            .Select(d => GDPluginDiagnosticAdapter.Convert(d, scriptFile))
            .ToList();

        // Run semantic validator if available
        if (scriptFile.SemanticModel != null)
        {
            var semanticValidatorOptions = new GDSemanticValidatorOptions
            {
                CheckTypes = true,
                CheckMemberAccess = true,
                CheckArgumentTypes = true,
                CheckIndexers = true,
                CheckSignalTypes = true,
                CheckGenericTypes = true
            };

            var semanticValidator = new GDSemanticValidator(scriptFile.SemanticModel, semanticValidatorOptions);
            var semanticResult = semanticValidator.Validate(scriptFile.Class);

            foreach (var diagnostic in semanticResult.Diagnostics)
            {
                var pluginDiagnostic = GDPluginDiagnosticAdapter.ConvertFromValidator(diagnostic, scriptFile);
                diagnostics.Add(pluginDiagnostic);
            }
        }

        // Store results
        _diagnostics[scriptFile.FullPath] = diagnostics;

        // Cache results
        _cacheManager?.StoreLintCache(scriptFile, contentHash, diagnostics);

        NotifyDiagnosticsChanged(scriptFile, diagnostics);
    }

    /// <summary>
    /// Clears diagnostics for a script.
    /// </summary>
    public void ClearDiagnostics(GDScriptFile script)
    {
        if (_diagnostics.TryRemove(script.FullPath, out _))
        {
            NotifyDiagnosticsChanged(script, Array.Empty<GDPluginDiagnostic>());
        }
    }

    /// <summary>
    /// Clears diagnostics for a specific file path (used for rename operations).
    /// </summary>
    public void ClearDiagnosticsForPath(string fullPath)
    {
        _diagnostics.TryRemove(fullPath, out _);
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

            Logger.Debug($"Script analyzed, SemanticModel={ScriptFile.SemanticModel != null}");

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

    private void NotifyDiagnosticsChanged(GDScriptFile script, IReadOnlyList<GDPluginDiagnostic> diagnostics)
    {
        OnDiagnosticsChanged?.Invoke(new GDDiagnosticsChangedEventArgs
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
