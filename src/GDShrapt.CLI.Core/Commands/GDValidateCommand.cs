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
/// Validates GDScript syntax and semantics.
/// This command runs only the validator (not the linter).
/// </summary>
public class GDValidateCommand : IGDCommand
{
    private readonly string _projectPath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;
    private readonly GDProjectConfig? _config;
    private readonly GDValidationChecks _checks;
    private readonly bool _strict;

    public string Name => "validate";
    public string Description => "Validate GDScript syntax and semantics";

    /// <summary>
    /// Creates a new validate command.
    /// </summary>
    /// <param name="projectPath">Path to the Godot project.</param>
    /// <param name="formatter">Output formatter.</param>
    /// <param name="output">Output writer.</param>
    /// <param name="config">Project configuration (optional).</param>
    /// <param name="checks">Which validation checks to run.</param>
    /// <param name="strict">If true, all issues are reported as errors.</param>
    public GDValidateCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        GDValidationChecks checks = GDValidationChecks.All,
        bool strict = false)
    {
        _projectPath = projectPath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _config = config;
        _checks = checks;
        _strict = strict;
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

            var result = BuildValidationResult(project, projectRoot, config);
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

    private GDAnalysisResult BuildValidationResult(GDScriptProject project, string projectRoot, GDProjectConfig config)
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

        // Create validator with options from flags
        var validator = new GDValidator();
        var validationOptions = new GDValidationOptions
        {
            CheckSyntax = _checks.HasFlag(GDValidationChecks.Syntax),
            CheckScope = _checks.HasFlag(GDValidationChecks.Scope),
            CheckTypes = _checks.HasFlag(GDValidationChecks.Types),
            CheckCalls = _checks.HasFlag(GDValidationChecks.Calls),
            CheckControlFlow = _checks.HasFlag(GDValidationChecks.ControlFlow),
            CheckIndentation = _checks.HasFlag(GDValidationChecks.Indentation)
        };

        foreach (var script in project.ScriptFiles)
        {
            var relativePath = GetRelativePath(script.Reference.FullPath, projectRoot);

            // Check if file should be excluded
            if (GDConfigLoader.ShouldExclude(relativePath, config.Cli.Exclude))
                continue;

            var fileDiags = new GDFileDiagnostics
            {
                FilePath = relativePath
            };

            // Check for parse errors
            if (script.WasReadError)
            {
                fileDiags.Diagnostics.Add(new GDDiagnosticInfo
                {
                    Code = "GD0001",
                    Message = "Failed to parse file",
                    Severity = GDSeverity.Error,
                    Line = 1,
                    Column = 1
                });
                totalErrors++;
            }

            if (script.Class != null)
            {
                // Check for invalid tokens in AST
                var invalidTokens = script.Class.AllInvalidTokens;
                foreach (var token in invalidTokens)
                {
                    fileDiags.Diagnostics.Add(new GDDiagnosticInfo
                    {
                        Code = "GD0002",
                        Message = $"Invalid token: {token.ToString()?.Trim() ?? "unknown"}",
                        Severity = GDSeverity.Error,
                        Line = token.StartLine,
                        Column = token.StartColumn
                    });
                    totalErrors++;
                }

                // Run validator
                var validationResult = validator.Validate(script.Class, validationOptions);
                foreach (var diagnostic in validationResult.Diagnostics)
                {
                    var severity = _strict ? GDSeverity.Error : GDSeverityHelper.FromValidator(diagnostic.Severity);

                    // Check if rule is enabled in config
                    var ruleId = diagnostic.CodeString;
                    if (!IsRuleEnabled(config, ruleId))
                        continue;

                    fileDiags.Diagnostics.Add(new GDDiagnosticInfo
                    {
                        Code = ruleId,
                        Message = diagnostic.Message,
                        Severity = _strict ? GDSeverity.Error : GDSeverityHelper.GetConfigured(config, ruleId, severity),
                        Line = diagnostic.StartLine,
                        Column = diagnostic.StartColumn,
                        EndLine = diagnostic.EndLine,
                        EndColumn = diagnostic.EndColumn
                    });

                    UpdateCounts(ref totalErrors, ref totalWarnings, ref totalHints, severity);
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

/// <summary>
/// Flags for selecting which validation checks to run.
/// </summary>
[Flags]
public enum GDValidationChecks
{
    /// <summary>
    /// No checks.
    /// </summary>
    None = 0,

    /// <summary>
    /// Check for syntax errors (invalid tokens, parse failures).
    /// </summary>
    Syntax = 1,

    /// <summary>
    /// Check for scope errors (undefined variables, duplicate declarations).
    /// </summary>
    Scope = 2,

    /// <summary>
    /// Check for type errors (type mismatches).
    /// </summary>
    Types = 4,

    /// <summary>
    /// Check for call errors (wrong number of arguments, undefined functions).
    /// </summary>
    Calls = 8,

    /// <summary>
    /// Check for control flow errors (break/continue outside loop).
    /// </summary>
    ControlFlow = 16,

    /// <summary>
    /// Check for indentation issues.
    /// </summary>
    Indentation = 32,

    /// <summary>
    /// All validation checks.
    /// </summary>
    All = Syntax | Scope | Types | Calls | ControlFlow | Indentation
}
