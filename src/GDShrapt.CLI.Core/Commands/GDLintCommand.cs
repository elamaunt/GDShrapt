using System;
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
/// Lints GDScript files for style and best practices issues.
/// Exit codes: 0=Success, 1=Warnings/Hints (if fail-on configured), 2=Errors, 3=Fatal.
/// This command runs only the linter (not the validator).
/// </summary>
public class GDLintCommand : GDProjectCommandBase
{
    private readonly HashSet<string>? _onlyRules;
    private readonly HashSet<GDLintCategory>? _categories;
    private readonly GDLintSeverity? _minSeverity;
    private readonly int? _maxIssues;
    private readonly GDGroupBy _groupBy;
    private readonly GDLinterOptionsOverrides? _optionsOverrides;

    public override string Name => "lint";
    public override string Description => "Lint GDScript files for style and best practices";

    public GDLintCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        IEnumerable<string>? onlyRules = null,
        IEnumerable<GDLintCategory>? categories = null,
        GDLintSeverity? minSeverity = null,
        int? maxIssues = null,
        GDGroupBy groupBy = GDGroupBy.File,
        GDLinterOptionsOverrides? optionsOverrides = null,
        IGDLogger? logger = null,
        IReadOnlyList<string>? cliExcludePatterns = null)
        : base(projectPath, formatter, output, config, logger, cliExcludePatterns)
    {
        _optionsOverrides = optionsOverrides;

        if (optionsOverrides?.Rules != null)
        {
            _onlyRules = optionsOverrides.Rules
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _onlyRules = onlyRules?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        if (optionsOverrides?.Category != null)
        {
            _categories = optionsOverrides.Category
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(c => c.ToLowerInvariant() switch
                {
                    "naming" => GDLintCategory.Naming,
                    "style" => GDLintCategory.Style,
                    "best-practices" or "bestpractices" => GDLintCategory.BestPractices,
                    "organization" => GDLintCategory.Organization,
                    "documentation" => GDLintCategory.Documentation,
                    _ => GDLintCategory.Naming
                })
                .ToHashSet();
        }
        else
        {
            _categories = categories?.ToHashSet();
        }

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
        var result = BuildLintResult(project, projectRoot, config);
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

    private GDAnalysisResult BuildLintResult(GDScriptProject project, string projectRoot, GDProjectConfig config)
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
        var maxIssues = _maxIssues ?? 0;

        var linterOptions = GDLinterOptionsFactory.FromConfig(config);
        _optionsOverrides?.ApplyTo(linterOptions);

        var linter = new GDLinter(linterOptions);

        if (_onlyRules != null && _onlyRules.Count > 0)
        {
            var rulesToRemove = linter.Rules
                .Where(r => !_onlyRules.Contains(r.RuleId))
                .Select(r => r.RuleId)
                .ToList();

            foreach (var ruleId in rulesToRemove)
                linter.RemoveRule(ruleId);
        }

        foreach (var script in project.ScriptFiles)
        {
            if (maxIssues > 0 && totalIssuesReported >= maxIssues)
                break;

            var relativePath = GetRelativePath(script.Reference.FullPath, projectRoot);

            if (GDConfigLoader.ShouldExclude(relativePath, config.Cli.Exclude))
                continue;

            if (script.Class == null)
                continue;

            var fileDiags = new GDFileDiagnostics
            {
                FilePath = relativePath
            };

            var lintResult = linter.Lint(script.Class);
            foreach (var issue in lintResult.Issues)
            {
                if (maxIssues > 0 && totalIssuesReported >= maxIssues)
                    break;

                if (_categories != null && _categories.Count > 0)
                {
                    var rule = linter.GetRule(issue.RuleId);
                    if (rule != null && !_categories.Contains(rule.Category))
                        continue;
                }

                if (_minSeverity.HasValue && issue.Severity < _minSeverity.Value)
                    continue;

                var severity = GDSeverityHelper.FromLinter(issue.Severity);

                if (!IsRuleEnabled(config, issue.RuleId))
                    continue;

                fileDiags.Diagnostics.Add(new GDDiagnosticInfo
                {
                    Code = issue.RuleId,
                    Message = issue.Message,
                    Severity = GDSeverityHelper.GetConfigured(config, issue.RuleId, severity),
                    Line = issue.StartLine,
                    Column = issue.StartColumn,
                    EndLine = issue.EndLine,
                    EndColumn = issue.EndColumn
                });

                UpdateSeverityCounts(ref totalErrors, ref totalWarnings, ref totalHints, severity);
                totalIssuesReported++;
            }

            if (fileDiags.Diagnostics.Count > 0)
            {
                filesWithIssues++;
                result.Files.Add(fileDiags);
            }
        }

        result.FilesWithErrors = filesWithIssues;
        result.TotalErrors = totalErrors;
        result.TotalWarnings = totalWarnings;
        result.TotalHints = totalHints;

        return result;
    }

    private static bool IsRuleEnabled(GDProjectConfig config, string ruleId)
    {
        if (config.Linting.Rules.TryGetValue(ruleId, out var ruleConfig))
            return ruleConfig.Enabled;
        return true;
    }

}
