using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using GDShrapt.Semantics.Validator;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Validates GDScript syntax and semantics.
/// Exit codes: 0=Success, 1=Warnings/Hints (if fail-on configured), 2=Errors, 3=Fatal.
/// This command runs only the validator (not the linter).
/// Uses the unified GDDiagnosticsService for consistent validation.
/// </summary>
public class GDValidateCommand : GDProjectCommandBase
{
    private readonly GDValidationChecks _checks;
    private readonly bool _strict;
    private readonly GDSeverity? _minSeverity;
    private readonly int? _maxIssues;
    private readonly GDGroupBy _groupBy;

    public override string Name => "validate";
    public override string Description => "Validate GDScript syntax and semantics";

    public GDValidateCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDProjectConfig? config = null,
        GDValidationChecks checks = GDValidationChecks.All,
        bool strict = false,
        GDSeverity? minSeverity = null,
        int? maxIssues = null,
        GDGroupBy groupBy = GDGroupBy.File,
        IGDLogger? logger = null,
        IReadOnlyList<string>? cliExcludePatterns = null)
        : base(projectPath, formatter, output, config, logger, cliExcludePatterns)
    {
        _checks = checks;
        _strict = strict;
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
        var result = BuildValidationResult(project, projectRoot, config);
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

    private GDAnalysisResult BuildValidationResult(GDScriptProject project, string projectRoot, GDProjectConfig config)
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

        var diagnosticsService = new GDDiagnosticsService(
            validationOptions,
            linterOptions: null,
            config,
            generateFixes: false);

        foreach (var script in project.ScriptFiles)
        {
            if (maxIssues > 0 && totalIssuesReported >= maxIssues)
                break;

            var relativePath = GetRelativePath(script.Reference.FullPath, projectRoot);

            if (GDConfigLoader.ShouldExclude(relativePath, config.Cli.Exclude))
                continue;

            var fileDiags = new GDFileDiagnostics
            {
                FilePath = relativePath
            };

            var semanticOptions = new GDSemanticValidatorOptions
            {
                CheckTypes = _checks.HasFlag(GDValidationChecks.Types),
                CheckMemberAccess = _checks.HasFlag(GDValidationChecks.MemberAccess),
                CheckArgumentTypes = _checks.HasFlag(GDValidationChecks.Calls),
                CheckSignalTypes = _checks.HasFlag(GDValidationChecks.Signals),
                CheckGenericTypes = _checks.HasFlag(GDValidationChecks.Types)
            };
            var diagnosticsResult = GDDiagnosticsHandler.DiagnoseWithSemantics(
                script, diagnosticsService, semanticOptions, config);

            foreach (var diagnostic in diagnosticsResult.Diagnostics)
            {
                if (maxIssues > 0 && totalIssuesReported >= maxIssues)
                    break;

                var severity = _strict
                    ? GDSeverity.Error
                    : GDSeverityHelper.FromUnified(diagnostic.Severity);

                if (_minSeverity.HasValue && severity > _minSeverity.Value)
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

                UpdateSeverityCounts(ref totalErrors, ref totalWarnings, ref totalHints, severity);
                totalIssuesReported++;
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

/// <summary>
/// Flags for selecting which validation checks to run.
/// </summary>
[System.Flags]
public enum GDValidationChecks
{
    None = 0,
    Syntax = 1,
    Scope = 2,
    Types = 4,
    Calls = 8,
    ControlFlow = 16,
    Indentation = 32,
    MemberAccess = 64,
    Abstract = 128,
    Signals = 256,
    ResourcePaths = 512,
    Basic = Syntax | Scope | Types | Calls | ControlFlow | Indentation,
    All = Basic | MemberAccess | Abstract | Signals | ResourcePaths
}
