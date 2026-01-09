using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Unified diagnostics service combining syntax checking, validation, and linting.
/// Used by CLI, LSP, and Plugin for consistent diagnostics.
/// </summary>
public class GDDiagnosticsService
{
    private readonly GDValidator _validator;
    private readonly GDValidationOptions _validationOptions;
    private readonly GDLinter? _linter;
    private readonly GDProjectConfig? _config;

    /// <summary>
    /// Creates a new diagnostics service with the specified options.
    /// </summary>
    /// <param name="validationOptions">Options for validation checks.</param>
    /// <param name="linterOptions">Options for linting (null to disable linting).</param>
    /// <param name="config">Project configuration for rule overrides (optional).</param>
    public GDDiagnosticsService(
        GDValidationOptions? validationOptions = null,
        GDLinterOptions? linterOptions = null,
        GDProjectConfig? config = null)
    {
        _validator = new GDValidator();
        _validationOptions = validationOptions ?? new GDValidationOptions();
        _linter = linterOptions != null ? new GDLinter(linterOptions) : null;
        _config = config;
    }

    /// <summary>
    /// Creates a diagnostics service from project configuration.
    /// </summary>
    /// <param name="config">Project configuration.</param>
    /// <returns>Configured diagnostics service.</returns>
    public static GDDiagnosticsService FromConfig(GDProjectConfig config)
    {
        var validationOptions = new GDValidationOptions
        {
            CheckSyntax = true,
            CheckScope = true,
            CheckTypes = true,
            CheckCalls = true,
            CheckControlFlow = true,
            CheckIndentation = config.Linting.FormattingLevel != GDFormattingLevel.Off
        };

        GDLinterOptions? linterOptions = null;
        if (config.Linting.Enabled)
        {
            linterOptions = GDLinterOptionsFactory.FromConfig(config);
        }

        return new GDDiagnosticsService(validationOptions, linterOptions, config);
    }

    /// <summary>
    /// Runs all diagnostics on a class declaration.
    /// </summary>
    /// <param name="classDeclaration">The parsed class to diagnose.</param>
    /// <returns>Combined diagnostics result.</returns>
    public GDDiagnosticsResult Diagnose(GDClassDeclaration classDeclaration)
    {
        var result = new GDDiagnosticsResult();

        // 1. Check for syntax errors (invalid tokens)
        foreach (var token in classDeclaration.AllInvalidTokens)
        {
            result.Add(new GDUnifiedDiagnostic
            {
                Code = "GD0002",
                Message = $"Invalid token: {token.ToString()?.Trim() ?? "unknown"}",
                Severity = GDDiagnosticSeverity.Error,
                Source = GDDiagnosticSource.Syntax,
                StartLine = token.StartLine,
                StartColumn = token.StartColumn,
                EndLine = token.EndLine,
                EndColumn = token.EndColumn
            });
        }

        // 2. Run validator
        var validationResult = _validator.Validate(classDeclaration, _validationOptions);
        foreach (var diagnostic in validationResult.Diagnostics)
        {
            var ruleId = diagnostic.CodeString;

            // Check if rule is enabled in config
            if (_config != null && !IsRuleEnabled(ruleId))
                continue;

            var severity = GDSeverityMapper.FromValidator(diagnostic.Severity);
            severity = ApplyConfiguredSeverity(ruleId, severity);

            result.Add(new GDUnifiedDiagnostic
            {
                Code = ruleId,
                Message = diagnostic.Message,
                Severity = severity,
                Source = GDDiagnosticSource.Validator,
                StartLine = diagnostic.StartLine,
                StartColumn = diagnostic.StartColumn,
                EndLine = diagnostic.EndLine,
                EndColumn = diagnostic.EndColumn
            });
        }

        // 3. Run linter
        if (_linter != null)
        {
            var lintResult = _linter.Lint(classDeclaration);
            foreach (var issue in lintResult.Issues)
            {
                // Check if rule is enabled in config
                if (_config != null && !IsRuleEnabled(issue.RuleId))
                    continue;

                var severity = GDSeverityMapper.FromLinter(issue.Severity);
                severity = ApplyConfiguredSeverity(issue.RuleId, severity);

                result.Add(new GDUnifiedDiagnostic
                {
                    Code = issue.RuleId,
                    Message = issue.Message,
                    Severity = severity,
                    Source = GDDiagnosticSource.Linter,
                    StartLine = issue.StartLine,
                    StartColumn = issue.StartColumn,
                    EndLine = issue.EndLine,
                    EndColumn = issue.EndColumn
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Runs diagnostics on a script file.
    /// </summary>
    /// <param name="script">The script file to diagnose.</param>
    /// <returns>Combined diagnostics result.</returns>
    public GDDiagnosticsResult Diagnose(GDScriptFile script)
    {
        var result = new GDDiagnosticsResult();

        // Check for parse errors
        if (script.WasReadError)
        {
            result.Add(new GDUnifiedDiagnostic
            {
                Code = "GD0001",
                Message = "Failed to parse file",
                Severity = GDDiagnosticSeverity.Error,
                Source = GDDiagnosticSource.Syntax,
                StartLine = 1,
                StartColumn = 1,
                EndLine = 1,
                EndColumn = 1
            });
        }

        if (script.Class != null)
        {
            var classResult = Diagnose(script.Class);
            result.AddRange(classResult.Diagnostics);
        }

        return result;
    }

    private bool IsRuleEnabled(string ruleId)
    {
        if (_config?.Linting.Rules.TryGetValue(ruleId, out var ruleConfig) == true)
        {
            return ruleConfig.Enabled;
        }
        return true; // Enabled by default
    }

    private GDDiagnosticSeverity ApplyConfiguredSeverity(string ruleId, GDDiagnosticSeverity defaultSeverity)
    {
        if (_config?.Linting.Rules.TryGetValue(ruleId, out var ruleConfig) == true && ruleConfig.Severity.HasValue)
        {
            return ruleConfig.Severity.Value;
        }
        return defaultSeverity;
    }
}
