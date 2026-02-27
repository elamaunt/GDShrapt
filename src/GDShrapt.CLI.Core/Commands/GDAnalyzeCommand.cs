using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Analyzes a GDScript project and outputs diagnostics.
/// </summary>
public class GDAnalyzeCommand : GDProjectCommandBase
{
    private readonly GDSeverity? _minSeverity;
    private readonly int? _maxIssues;
    private readonly GDGroupBy _groupBy;

    public override string Name => "analyze";
    public override string Description => "Analyze a GDScript project and output diagnostics";

    public GDAnalyzeCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        GDSeverity? minSeverity = null,
        int? maxIssues = null,
        GDGroupBy groupBy = GDGroupBy.File,
        IGDLogger? logger = null,
        IReadOnlyList<string>? cliExcludePatterns = null)
        : base(projectPath, formatter, output, config, logger, cliExcludePatterns)
    {
        _minSeverity = minSeverity;
        _maxIssues = maxIssues;
        _groupBy = groupBy;
    }

    protected override Task<int> ExecuteOnProjectAsync(
        GDScriptProject project,
        string projectRoot,
        GDProjectConfig config,
        CancellationToken cancellationToken)
    {
        var result = BuildAnalysisResult(project, projectRoot, config, _minSeverity, _maxIssues);
        result.GroupBy = _groupBy;
        _formatter.WriteAnalysisResult(_output, result);

        var exitCode = GDExitCode.FromResults(
            result.TotalErrors,
            result.TotalWarnings,
            result.TotalHints,
            config.Cli.FailOnWarning,
            config.Cli.FailOnHint);

        return Task.FromResult(exitCode);
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

        var filesWithIssues = 0;
        var totalErrors = 0;
        var totalWarnings = 0;
        var totalHints = 0;
        var totalIssuesReported = 0;
        var maxReached = false;

        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        foreach (var script in project.ScriptFiles)
        {
            if (maxReached)
                break;

            var relativePath = GetRelativePath(script.Reference.FullPath, projectRoot);

            if (GDConfigLoader.ShouldExclude(relativePath, config.Cli.Exclude))
                continue;

            var fileDiags = new GDFileDiagnostics
            {
                FilePath = relativePath
            };

            var diagnosticsResult = GDDiagnosticsHandler.DiagnoseWithSemantics(script, diagnosticsService, config: config);

            foreach (var diagnostic in diagnosticsResult.Diagnostics)
            {
                var severity = GDSeverityHelper.FromUnified(diagnostic.Severity);

                if (minSeverity.HasValue && severity > minSeverity.Value)
                    continue;

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
                UpdateSeverityCounts(ref totalErrors, ref totalWarnings, ref totalHints, severity);
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
}
