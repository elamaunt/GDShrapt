using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Lints GDScript files for style and best practices issues.
/// Exit codes: 0=Success, 1=Warnings/Hints (if fail-on configured), 2=Errors, 3=Fatal.
/// This command runs only the linter (not the validator).
/// </summary>
public class GDLintCommand : IGDCommand
{
    private readonly string _projectPath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;
    private readonly GDProjectConfig? _config;
    private readonly HashSet<string>? _onlyRules;
    private readonly HashSet<GDLintCategory>? _categories;
    private readonly GDLintSeverity? _minSeverity;
    private readonly int? _maxIssues;
    private readonly GDGroupBy _groupBy;
    private readonly GDLinterOptionsOverrides? _optionsOverrides;

    public string Name => "lint";
    public string Description => "Lint GDScript files for style and best practices";

    /// <summary>
    /// Creates a new lint command.
    /// </summary>
    /// <param name="projectPath">Path to the Godot project.</param>
    /// <param name="formatter">Output formatter.</param>
    /// <param name="output">Output writer.</param>
    /// <param name="config">Project configuration (optional).</param>
    /// <param name="onlyRules">Only run these specific rules (e.g., "GDL001,GDL003").</param>
    /// <param name="categories">Only run rules in these categories.</param>
    /// <param name="minSeverity">Minimum severity to report.</param>
    /// <param name="maxIssues">Maximum number of issues to report (0 = unlimited).</param>
    /// <param name="groupBy">How to group the output (default: by file).</param>
    /// <param name="optionsOverrides">CLI options overrides.</param>
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
        GDLinterOptionsOverrides? optionsOverrides = null)
    {
        _projectPath = projectPath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _config = config;
        _optionsOverrides = optionsOverrides;

        // Rules and categories can come from overrides or direct parameters
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

            var result = BuildLintResult(project, projectRoot, config);
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
        var maxIssues = _maxIssues ?? 0; // 0 means unlimited

        // Create linter with options from config (using factory from Semantics)
        var linterOptions = GDLinterOptionsFactory.FromConfig(config);

        // Apply CLI overrides on top of config
        _optionsOverrides?.ApplyTo(linterOptions);

        var linter = new GDLinter(linterOptions);

        // If specific rules are requested, filter them
        if (_onlyRules != null && _onlyRules.Count > 0)
        {
            var rulesToRemove = linter.Rules
                .Where(r => !_onlyRules.Contains(r.RuleId))
                .Select(r => r.RuleId)
                .ToList();

            foreach (var ruleId in rulesToRemove)
            {
                linter.RemoveRule(ruleId);
            }
        }

        foreach (var script in project.ScriptFiles)
        {
            // Check if we've reached the max issues limit
            if (maxIssues > 0 && totalIssuesReported >= maxIssues)
                break;

            var relativePath = GetRelativePath(script.Reference.FullPath, projectRoot);

            // Check if file should be excluded
            if (GDConfigLoader.ShouldExclude(relativePath, config.Cli.Exclude))
                continue;

            if (script.Class == null)
                continue;

            var fileDiags = new GDFileDiagnostics
            {
                FilePath = relativePath
            };

            // Run linter
            var lintResult = linter.Lint(script.Class);
            foreach (var issue in lintResult.Issues)
            {
                // Check if we've reached the max issues limit
                if (maxIssues > 0 && totalIssuesReported >= maxIssues)
                    break;

                // Filter by category if specified
                if (_categories != null && _categories.Count > 0)
                {
                    var rule = linter.GetRule(issue.RuleId);
                    if (rule != null && !_categories.Contains(rule.Category))
                        continue;
                }

                // Filter by minimum severity
                if (_minSeverity.HasValue && issue.Severity < _minSeverity.Value)
                    continue;

                var severity = GDSeverityHelper.FromLinter(issue.Severity);

                // Check if rule is enabled in config
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

                UpdateCounts(ref totalErrors, ref totalWarnings, ref totalHints, severity);
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
        {
            return ruleConfig.Enabled;
        }
        return true; // Enabled by default
    }

    private static void UpdateCounts(ref int errors, ref int warnings, ref int hints, GDSeverity severity)
    {
        switch (severity)
        {
            case GDSeverity.Error:
                errors++;
                break;
            case GDSeverity.Warning:
                warnings++;
                break;
            case GDSeverity.Hint:
            case GDSeverity.Information:
                hints++;
                break;
        }
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
