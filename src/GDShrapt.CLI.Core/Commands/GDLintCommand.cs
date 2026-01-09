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
    public GDLintCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        IEnumerable<string>? onlyRules = null,
        IEnumerable<GDLintCategory>? categories = null,
        GDLintSeverity? minSeverity = null)
    {
        _projectPath = projectPath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _config = config;
        _onlyRules = onlyRules?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _categories = categories?.ToHashSet();
        _minSeverity = minSeverity;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var projectRoot = GDProjectLoader.FindProjectRoot(_projectPath);
            if (projectRoot == null)
            {
                _formatter.WriteError(_output, $"Could not find project.godot in or above: {_projectPath}");
                return Task.FromResult(2);
            }

            // Load config from project or use provided
            var config = _config ?? GDConfigLoader.LoadConfig(projectRoot);

            using var project = GDProjectLoader.LoadProject(projectRoot);

            var result = BuildLintResult(project, projectRoot, config);
            _formatter.WriteAnalysisResult(_output, result);

            // Determine exit code
            if (result.TotalErrors > 0)
                return Task.FromResult(1);

            if (config.Cli.FailOnWarning && result.TotalWarnings > 0)
                return Task.FromResult(1);

            if (config.Cli.FailOnHint && result.TotalHints > 0)
                return Task.FromResult(1);

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(2);
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

        // Create linter with options from config (using factory from Semantics)
        var linterOptions = GDLinterOptionsFactory.FromConfig(config);
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
