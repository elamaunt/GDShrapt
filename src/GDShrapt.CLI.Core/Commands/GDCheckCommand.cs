using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Checks a GDScript project for errors. Returns exit code 1 if errors found.
/// Designed for CI/CD pipelines.
/// </summary>
public class GDCheckCommand : IGDCommand
{
    private readonly string _projectPath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;
    private readonly bool _quiet;
    private readonly GDProjectConfig? _config;

    public string Name => "check";
    public string Description => "Check a GDScript project for errors (for CI/CD)";

    public GDCheckCommand(string projectPath, IGDOutputFormatter formatter, TextWriter? output = null, bool quiet = false, GDProjectConfig? config = null)
    {
        _projectPath = projectPath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _quiet = quiet;
        _config = config;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var projectRoot = GDProjectLoader.FindProjectRoot(_projectPath);
            if (projectRoot == null)
            {
                if (!_quiet)
                {
                    _formatter.WriteError(_output, $"Could not find project.godot in or above: {_projectPath}");
                }
                return Task.FromResult(2);
            }

            // Load config from project or use provided
            var config = _config ?? GDConfigLoader.LoadConfig(projectRoot);

            using var project = GDProjectLoader.LoadProject(projectRoot);

            var errorCount = 0;
            var warningCount = 0;
            var hintCount = 0;
            var fileCount = 0;

            // Create linter and validator if linting is enabled
            GDLinter? linter = null;
            GDValidator? validator = null;
            GDValidationOptions? validationOptions = null;

            if (config.Linting.Enabled)
            {
                linter = new GDLinter(GDAnalyzeCommand.CreateLinterOptionsFromConfig(config));
                validator = new GDValidator();
                validationOptions = new GDValidationOptions
                {
                    CheckSyntax = true,
                    CheckScope = true,
                    CheckTypes = true,
                    CheckCalls = true,
                    CheckControlFlow = true,
                    CheckIndentation = config.Linting.FormattingLevel != GDFormattingLevel.Off
                };
            }

            foreach (var script in project.ScriptFiles)
            {
                var relativePath = Path.GetRelativePath(projectRoot, script.Reference.FullPath);

                // Check if file should be excluded
                if (GDConfigLoader.ShouldExclude(relativePath, config.Cli.Exclude))
                    continue;

                fileCount++;

                if (script.WasReadError)
                {
                    errorCount++;
                    continue;
                }

                if (script.Class != null)
                {
                    // Parse errors
                    errorCount += script.Class.AllInvalidTokens.Count();

                    // Validator diagnostics
                    if (validator != null && validationOptions != null)
                    {
                        var validationResult = validator.Validate(script.Class, validationOptions);
                        foreach (var diag in validationResult.Diagnostics)
                        {
                            var ruleId = diag.CodeString;
                            if (!IsRuleEnabled(config, ruleId))
                                continue;

                            switch (diag.Severity)
                            {
                                case Reader.GDDiagnosticSeverity.Error:
                                    errorCount++;
                                    break;
                                case Reader.GDDiagnosticSeverity.Warning:
                                    warningCount++;
                                    break;
                                default:
                                    hintCount++;
                                    break;
                            }
                        }
                    }

                    // Linter issues
                    if (linter != null)
                    {
                        var lintResult = linter.Lint(script.Class);
                        foreach (var issue in lintResult.Issues)
                        {
                            if (!IsRuleEnabled(config, issue.RuleId))
                                continue;

                            switch (issue.Severity)
                            {
                                case GDLintSeverity.Error:
                                    errorCount++;
                                    break;
                                case GDLintSeverity.Warning:
                                    warningCount++;
                                    break;
                                default:
                                    hintCount++;
                                    break;
                            }
                        }
                    }
                }
            }

            // Determine if check failed
            var failed = errorCount > 0;
            if (!failed && config.Cli.FailOnWarning && warningCount > 0)
                failed = true;
            if (!failed && config.Cli.FailOnHint && hintCount > 0)
                failed = true;

            if (!_quiet)
            {
                if (!failed)
                {
                    _formatter.WriteMessage(_output, $"OK: {fileCount} files checked, {errorCount} errors, {warningCount} warnings, {hintCount} hints.");
                }
                else
                {
                    _formatter.WriteMessage(_output, $"FAILED: {fileCount} files checked, {errorCount} errors, {warningCount} warnings, {hintCount} hints.");
                }
            }

            return Task.FromResult(failed ? 1 : 0);
        }
        catch (Exception ex)
        {
            if (!_quiet)
            {
                _formatter.WriteError(_output, ex.Message);
            }
            return Task.FromResult(2);
        }
    }

    private static bool IsRuleEnabled(GDProjectConfig config, string ruleId)
    {
        if (config.Linting.Rules.TryGetValue(ruleId, out var ruleConfig))
        {
            return ruleConfig.Enabled;
        }
        return true;
    }
}
