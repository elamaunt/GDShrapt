using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Analyzes a GDScript project and outputs diagnostics.
/// Exit codes: 0=Success, 1=Warnings/Hints (if fail-on configured), 2=Errors, 3=Fatal.
/// Uses the unified GDDiagnosticsService for consistent diagnostics across CLI, LSP, and Plugin.
/// </summary>
public class GDAnalyzeCommand : IGDCommand
{
    private readonly string _projectPath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;
    private readonly GDProjectConfig? _config;
    private readonly GDSeverity? _minSeverity;
    private readonly int? _maxIssues;
    private readonly GDGroupBy _groupBy;

    public string Name => "analyze";
    public string Description => "Analyze a GDScript project and output diagnostics";

    public GDAnalyzeCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        GDSeverity? minSeverity = null,
        int? maxIssues = null,
        GDGroupBy groupBy = GDGroupBy.File)
    {
        _projectPath = projectPath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _config = config;
        _minSeverity = minSeverity;
        _maxIssues = maxIssues;
        _groupBy = groupBy;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var projectRoot = GDProjectLoader.FindProjectRoot(_projectPath);
            if (projectRoot == null)
            {
                _formatter.WriteError(_output, $"Could not find project.godot in or above: {_projectPath}");
                return Task.FromResult(GDExitCode.Fatal);
            }

            // Load config from project or use provided
            var config = _config ?? GDConfigLoader.LoadConfig(projectRoot);

            using var project = GDProjectLoader.LoadProject(projectRoot);

            var result = BuildAnalysisResult(project, projectRoot, config, _minSeverity, _maxIssues);
            result.GroupBy = _groupBy;
            _formatter.WriteAnalysisResult(_output, result);

            // Determine exit code using new exit code system
            var exitCode = GDExitCode.FromResults(
                result.TotalErrors,
                result.TotalWarnings,
                result.TotalHints,
                config.Cli.FailOnWarning,
                config.Cli.FailOnHint);

            return Task.FromResult(exitCode);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(GDExitCode.Fatal);
        }
    }

    private static GDAnalysisResult BuildAnalysisResult(
        GDScriptProject project,
        string projectRoot,
        GDProjectConfig config,
        GDSeverity? minSeverity = null,
        int? maxIssues = null)
    {
        var result = new GDAnalysisResult
        {
            ProjectPath = projectRoot,
            TotalFiles = project.ScriptFiles.Count()
        };

        var filesWithErrors = 0;
        var totalErrors = 0;
        var totalWarnings = 0;
        var totalHints = 0;
        var totalIssuesReported = 0;
        var maxReached = false;

        // Use unified diagnostics service - handles validator, linter, and config consistently
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        foreach (var script in project.ScriptFiles)
        {
            if (maxReached)
                break;

            var relativePath = GetRelativePath(script.Reference.FullPath, projectRoot);

            // Check if file should be excluded
            if (GDConfigLoader.ShouldExclude(relativePath, config.Cli.Exclude))
                continue;

            var fileDiags = new GDFileDiagnostics
            {
                FilePath = relativePath
            };

            // Use unified diagnostics service - handles parse errors, invalid tokens,
            // validation, linting, config overrides, and comment suppression
            var diagnosticsResult = diagnosticsService.Diagnose(script);

            foreach (var diagnostic in diagnosticsResult.Diagnostics)
            {
                var severity = GDSeverityHelper.FromUnified(diagnostic.Severity);

                // Filter by minimum severity
                if (minSeverity.HasValue && severity > minSeverity.Value)
                    continue;

                // Check max issues limit
                if (maxIssues.HasValue && maxIssues.Value > 0 && totalIssuesReported >= maxIssues.Value)
                {
                    maxReached = true;
                    break;
                }

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

                totalIssuesReported++;

                // Update counts
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

            if (fileDiags.Diagnostics.Count > 0)
            {
                filesWithErrors++;
                result.Files.Add(fileDiags);
            }
        }

        result.FilesWithErrors = filesWithErrors;
        result.TotalErrors = totalErrors;
        result.TotalWarnings = totalWarnings;
        result.TotalHints = totalHints;

        return result;
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        try
        {
            return Path.GetRelativePath(basePath, fullPath);
        }
        catch
        {
            return fullPath;
        }
    }
}
