using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Watches for file changes and continuously reports diagnostics.
/// </summary>
public class GDWatchCommand : GDProjectCommandBase
{
    private readonly object _analysisLock = new();
    private volatile int _lastExitCode = GDExitCode.Success;

    private GDScriptProject? _project;
    private string? _projectRoot;
    private GDProjectConfig? _cachedConfig;
    private GDDiagnosticsService? _diagnosticsService;

    public override string Name => "watch";
    public override string Description => "Watch for file changes and report diagnostics in real-time.";

    public GDWatchCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        IGDLogger? logger = null)
        : base(projectPath, formatter, output, config, logger)
    {
    }

    protected override GDScriptProjectOptions? GetProjectOptions()
    {
        return new GDScriptProjectOptions
        {
            Logger = _logger,
            EnableSceneTypesProvider = true,
            EnableFileWatcher = true,
            EnableSceneChangeReanalysis = true
        };
    }

    protected override async Task<int> ExecuteOnProjectAsync(
        GDScriptProject project,
        string projectRoot,
        GDProjectConfig config,
        CancellationToken cancellationToken)
    {
        // Enable scene file watcher
        project.SceneTypesProvider?.EnableFileWatcher();

        _project = project;
        _projectRoot = projectRoot;
        _cachedConfig = config;
        _diagnosticsService = GDDiagnosticsService.FromConfig(config);

        // Run initial analysis and report
        lock (_analysisLock)
        {
            var initialResult = RunAnalysis(project, projectRoot, config, _diagnosticsService);
            WriteResult(initialResult);
            _lastExitCode = GDExitCode.FromResults(
                initialResult.TotalErrors, initialResult.TotalWarnings, initialResult.TotalHints,
                config.Cli.FailOnWarning, config.Cli.FailOnHint);
        }

        Console.Error.WriteLine();
        Console.Error.WriteLine("Watching for changes... Press Ctrl+C to stop.");
        Console.Error.WriteLine();

        // Subscribe with named methods for proper unsubscription
        project.ScriptChanged += OnScriptChanged;
        project.ScriptCreated += OnScriptCreated;
        project.ScriptDeleted += OnScriptDeleted;
        project.SceneScriptsChanged += OnSceneScriptsChanged;

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected - Ctrl+C
        }
        finally
        {
            project.ScriptChanged -= OnScriptChanged;
            project.ScriptCreated -= OnScriptCreated;
            project.ScriptDeleted -= OnScriptDeleted;
            project.SceneScriptsChanged -= OnSceneScriptsChanged;

            _project = null;
            _projectRoot = null;
            _cachedConfig = null;
            _diagnosticsService = null;
        }

        Console.Error.WriteLine("Stopped watching.");
        return _lastExitCode;
    }

    private void OnScriptChanged(object? sender, GDScriptFileEventArgs e)
    {
        _logger.Debug($"Script changed: {e.FullPath}");
        RunAndWriteAnalysis();
    }

    private void OnScriptCreated(object? sender, GDScriptFileEventArgs e)
    {
        _logger.Debug($"Script created: {e.FullPath}");
        RunAndWriteAnalysis();
    }

    private void OnScriptDeleted(object? sender, GDScriptFileEventArgs e)
    {
        _logger.Debug($"Script deleted: {e.FullPath}");
        RunAndWriteAnalysis();
    }

    private void OnSceneScriptsChanged(object? sender, GDSceneAffectedScriptsEventArgs e)
    {
        _logger.Debug($"Scene '{e.ScenePath}' changed, reanalyzing {e.AffectedScripts.Count} script(s)");
        RunAndWriteAnalysis();
    }

    private void RunAndWriteAnalysis()
    {
        lock (_analysisLock)
        {
            if (_project == null || _projectRoot == null || _cachedConfig == null || _diagnosticsService == null)
                return;

            var result = RunAnalysis(_project, _projectRoot, _cachedConfig, _diagnosticsService);
            WriteResult(result);
            _lastExitCode = GDExitCode.FromResults(
                result.TotalErrors, result.TotalWarnings, result.TotalHints,
                _cachedConfig.Cli.FailOnWarning, _cachedConfig.Cli.FailOnHint);
        }
    }

    private GDAnalysisResult RunAnalysis(
        GDScriptProject project,
        string projectRoot,
        GDProjectConfig config,
        GDDiagnosticsService diagnosticsService)
    {
        var result = new GDAnalysisResult
        {
            ProjectPath = projectRoot,
            TotalFiles = project.ScriptFiles.Count()
        };

        var filesWithIssues = 0;
        var totalErrors = 0;
        var totalWarnings = 0;
        var totalHints = 0;

        foreach (var script in project.ScriptFiles)
        {
            var relativePath = GetRelativePath(script.Reference.FullPath, projectRoot);

            if (GDConfigLoader.ShouldExclude(relativePath, config.Cli.Exclude))
                continue;

            var fileDiags = new GDFileDiagnostics
            {
                FilePath = relativePath
            };

            try
            {
                var diagnosticsResult = GDDiagnosticsHandler.DiagnoseWithSemantics(script, diagnosticsService, config: config);

                foreach (var diagnostic in diagnosticsResult.Diagnostics)
                {
                    var severity = GDSeverityHelper.FromUnified(diagnostic.Severity);

                    fileDiags.Diagnostics.Add(new GDDiagnosticInfo
                    {
                        Code = diagnostic.Code,
                        Message = diagnostic.Message,
                        Severity = severity,
                        Line = diagnostic.StartLine,
                        Column = diagnostic.StartColumn,
                        EndLine = diagnostic.EndLine,
                        EndColumn = diagnostic.EndColumn
                    });

                    switch (severity)
                    {
                        case GDSeverity.Error:
                            totalErrors++;
                            break;
                        case GDSeverity.Warning:
                            totalWarnings++;
                            break;
                        default:
                            totalHints++;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Analysis failed for {script.FullPath}: {ex.Message}");
            }

            if (fileDiags.Diagnostics.Count > 0)
            {
                filesWithIssues++;
                result.Files.Add(fileDiags);
            }
        }

        result.FilesWithIssues = filesWithIssues;
        result.TotalErrors = totalErrors;
        result.TotalWarnings = totalWarnings;
        result.TotalHints = totalHints;

        return result;
    }

    private void WriteResult(GDAnalysisResult result)
    {
        Console.Error.Write($"\r[{DateTime.Now:HH:mm:ss}] ");

        if (result.TotalErrors == 0 && result.TotalWarnings == 0)
        {
            Console.Error.WriteLine($"{result.TotalFiles} files, no issues.");
        }
        else
        {
            Console.Error.WriteLine($"{result.TotalFiles} files: {result.TotalErrors} error(s), {result.TotalWarnings} warning(s)");
        }

        _formatter.WriteAnalysisResult(_output, result);
    }
}
