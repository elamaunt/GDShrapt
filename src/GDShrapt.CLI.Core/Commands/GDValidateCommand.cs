using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Validates GDScript syntax and semantics.
/// Exit codes: 0=Success, 1=Warnings/Hints (if fail-on configured), 2=Errors, 3=Fatal.
/// This command runs only the validator (not the linter).
/// Uses the unified GDDiagnosticsService for consistent validation.
/// </summary>
public class GDValidateCommand : IGDCommand
{
    private readonly string _projectPath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;
    private readonly GDProjectConfig? _config;
    private readonly GDValidationChecks _checks;
    private readonly bool _strict;
    private readonly GDSeverity? _minSeverity;
    private readonly int? _maxIssues;
    private readonly GDGroupBy _groupBy;

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
    /// <param name="minSeverity">Minimum severity to report.</param>
    /// <param name="maxIssues">Maximum number of issues to report (0 = unlimited).</param>
    /// <param name="groupBy">How to group the output (default: by file).</param>
    public GDValidateCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        GDValidationChecks checks = GDValidationChecks.All,
        bool strict = false,
        GDSeverity? minSeverity = null,
        int? maxIssues = null,
        GDGroupBy groupBy = GDGroupBy.File)
    {
        _projectPath = projectPath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _config = config;
        _checks = checks;
        _strict = strict;
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

            var result = BuildValidationResult(project, projectRoot, config);
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
        var totalIssuesReported = 0;
        var maxIssues = _maxIssues ?? 0; // 0 means unlimited

        // Build validation options from CLI flags
        var validationOptions = new GDValidationOptions
        {
            CheckSyntax = _checks.HasFlag(GDValidationChecks.Syntax),
            CheckScope = _checks.HasFlag(GDValidationChecks.Scope),
            CheckTypes = _checks.HasFlag(GDValidationChecks.Types),
            CheckCalls = _checks.HasFlag(GDValidationChecks.Calls),
            CheckControlFlow = _checks.HasFlag(GDValidationChecks.ControlFlow),
            CheckIndentation = _checks.HasFlag(GDValidationChecks.Indentation),
            CheckMemberAccess = _checks.HasFlag(GDValidationChecks.MemberAccess),
            CheckAbstract = _checks.HasFlag(GDValidationChecks.Abstract),
            CheckSignals = _checks.HasFlag(GDValidationChecks.Signals),
            CheckResourcePaths = _checks.HasFlag(GDValidationChecks.ResourcePaths)
        };

        // Use unified diagnostics service with validation-only (no linter)
        var diagnosticsService = new GDDiagnosticsService(
            validationOptions,
            linterOptions: null,  // No linting for validate command
            config,
            generateFixes: false);

        foreach (var script in project.ScriptFiles)
        {
            // Check if we've reached the max issues limit
            if (maxIssues > 0 && totalIssuesReported >= maxIssues)
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
            // validation, config overrides, and comment suppression
            var diagnosticsResult = diagnosticsService.Diagnose(script);

            foreach (var diagnostic in diagnosticsResult.Diagnostics)
            {
                // Check if we've reached the max issues limit
                if (maxIssues > 0 && totalIssuesReported >= maxIssues)
                    break;

                // Apply strict mode: all issues become errors
                var severity = _strict
                    ? GDSeverity.Error
                    : GDSeverityHelper.FromUnified(diagnostic.Severity);

                // Filter by minimum severity
                if (_minSeverity.HasValue && severity < _minSeverity.Value)
                    continue;

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

                // Update counts
                switch (severity)
                {
                    case GDSeverity.Error:
                        totalErrors++;
                        break;
                    case GDSeverity.Warning:
                        totalWarnings++;
                        break;
                    case GDSeverity.Hint:
                    case GDSeverity.Information:
                        totalHints++;
                        break;
                }
                totalIssuesReported++;
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
    /// Check for member access on typed/untyped expressions (GD7xxx).
    /// </summary>
    MemberAccess = 64,

    /// <summary>
    /// Check for @abstract annotation rules (GD8xxx).
    /// </summary>
    Abstract = 128,

    /// <summary>
    /// Check for signal operation validation.
    /// </summary>
    Signals = 256,

    /// <summary>
    /// Check for resource path validation in load/preload calls.
    /// </summary>
    ResourcePaths = 512,

    /// <summary>
    /// Basic validation checks (syntax, scope, types, calls, control flow, indentation).
    /// </summary>
    Basic = Syntax | Scope | Types | Calls | ControlFlow | Indentation,

    /// <summary>
    /// All validation checks including advanced checks.
    /// </summary>
    All = Basic | MemberAccess | Abstract | Signals | ResourcePaths
}
