using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Validator;

namespace GDShrapt.Semantics;

/// <summary>
/// Unified diagnostics service combining syntax checking, validation, and linting.
/// Used by CLI, LSP, and Plugin for consistent diagnostics.
///
/// Note: For type-aware validation (member access, argument types, indexers, signals, generics),
/// use GDSemanticValidator from GDShrapt.Semantics.Validator package separately.
/// </summary>
public class GDDiagnosticsService
{
    private readonly GDValidator _validator;
    private readonly GDValidationOptions _validationOptions;
    private readonly GDLinter? _linter;
    private readonly GDProjectConfig? _config;
    private readonly GDFixProvider _fixProvider;
    private readonly bool _generateFixes;

    /// <summary>
    /// Creates a new diagnostics service with the specified options.
    /// </summary>
    /// <param name="validationOptions">Options for validation checks.</param>
    /// <param name="linterOptions">Options for linting (null to disable linting).</param>
    /// <param name="config">Project configuration for rule overrides (optional).</param>
    /// <param name="generateFixes">Whether to generate code fixes for diagnostics.</param>
    public GDDiagnosticsService(
        GDValidationOptions? validationOptions = null,
        GDLinterOptions? linterOptions = null,
        GDProjectConfig? config = null,
        bool generateFixes = true)
    {
        _validator = new GDValidator();
        _validationOptions = validationOptions ?? new GDValidationOptions();
        _linter = linterOptions != null ? new GDLinter(linterOptions) : null;
        _config = config;
        _fixProvider = new GDFixProvider();
        _generateFixes = generateFixes;
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

        return new GDDiagnosticsService(validationOptions, linterOptions, config, generateFixes: true);
    }

    /// <summary>
    /// Runs all diagnostics on a class declaration.
    /// </summary>
    /// <param name="classDeclaration">The parsed class to diagnose.</param>
    /// <returns>Combined diagnostics result.</returns>
    public GDDiagnosticsResult Diagnose(GDClassDeclaration classDeclaration)
    {
        return DiagnoseInternal(classDeclaration, _validationOptions);
    }

    /// <summary>
    /// Internal method that runs diagnostics with specified options.
    /// </summary>
    private GDDiagnosticsResult DiagnoseInternal(GDClassDeclaration classDeclaration, GDValidationOptions options)
    {
        var result = new GDDiagnosticsResult();

        // 1. Check for syntax errors (invalid tokens) - only when CheckSyntax is enabled
        if (options.CheckSyntax)
        {
            foreach (var token in classDeclaration.AllInvalidTokens)
            {
                result.Add(new GDUnifiedDiagnostic
                {
                    Code = "GD0002",
                    Message = $"Invalid token: {token.ToString()?.Trim() ?? "unknown"}",
                    Severity = GDUnifiedDiagnosticSeverity.Error,
                    Source = GDDiagnosticSource.Syntax,
                    StartLine = token.StartLine,
                    StartColumn = token.StartColumn,
                    EndLine = token.EndLine,
                    EndColumn = token.EndColumn
                });
            }
        }

        // 2. Run validator with provided options (may include project-aware runtime provider)
        var validationResult = _validator.Validate(classDeclaration, options);
        foreach (var diagnostic in validationResult.Diagnostics)
        {
            var ruleId = diagnostic.CodeString;

            // Check if rule is enabled in config
            if (_config != null && !IsRuleEnabled(ruleId))
                continue;

            var severity = GDSeverityMapper.FromValidator(diagnostic.Severity);
            severity = ApplyConfiguredSeverity(ruleId, severity);

            var unifiedDiagnostic = new GDUnifiedDiagnostic
            {
                Code = ruleId,
                Message = diagnostic.Message,
                Severity = severity,
                Source = GDDiagnosticSource.Validator,
                StartLine = diagnostic.StartLine,
                StartColumn = diagnostic.StartColumn,
                EndLine = diagnostic.EndLine,
                EndColumn = diagnostic.EndColumn
            };

            // Generate fixes if enabled and node is available
            if (_generateFixes && diagnostic.Node != null)
            {
                unifiedDiagnostic.FixDescriptors = _fixProvider.GetFixes(
                    ruleId,
                    diagnostic.Node,
                    options.MemberAccessAnalyzer,
                    options.RuntimeProvider
                ).ToList();
            }

            result.Add(unifiedDiagnostic);
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

                var unifiedDiagnostic = new GDUnifiedDiagnostic
                {
                    Code = issue.RuleId,
                    RuleName = issue.RuleName,
                    Message = issue.Message,
                    Severity = severity,
                    Source = GDDiagnosticSource.Linter,
                    StartLine = issue.StartLine,
                    StartColumn = issue.StartColumn,
                    EndLine = issue.EndLine,
                    EndColumn = issue.EndColumn
                };

                // Generate fixes if enabled and token is available
                if (_generateFixes && issue.Token is GDNode node)
                {
                    unifiedDiagnostic.FixDescriptors = _fixProvider.GetFixes(
                        issue.RuleId,
                        node,
                        options.MemberAccessAnalyzer,
                        options.RuntimeProvider
                    ).ToList();
                }

                result.Add(unifiedDiagnostic);
            }
        }

        // 4. Apply comment-based suppression
        if (options.EnableCommentSuppression)
        {
            var suppressionContext = GDValidatorSuppressionParser.Parse(classDeclaration);
            result.FilterSuppressed(suppressionContext);
        }

        return result;
    }

    /// <summary>
    /// Runs diagnostics on a script file.
    /// Uses the script's analyzer for enhanced type-aware validation when available.
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
                Severity = GDUnifiedDiagnosticSeverity.Error,
                Source = GDDiagnosticSource.Syntax,
                StartLine = 1,
                StartColumn = 1,
                EndLine = 1,
                EndColumn = 1
            });
        }

        if (script.Class != null)
        {
            // Use semantic model's runtime provider when available for enhanced validation
            var runtimeProvider = script.SemanticModel?.RuntimeProvider ?? _validationOptions.RuntimeProvider;
            var semanticModel = script.SemanticModel;

            var options = CreateOptionsForScript(runtimeProvider, semanticModel);

            var classResult = DiagnoseInternal(script.Class, options);
            result.AddRange(classResult.Diagnostics);
        }

        return result;
    }

    /// <summary>
    /// Creates validation options with a specific runtime provider and member access analyzer.
    /// </summary>
    private GDValidationOptions CreateOptionsForScript(IGDRuntimeProvider? runtimeProvider, IGDMemberAccessAnalyzer? analyzer)
    {
        return new GDValidationOptions
        {
            RuntimeProvider = runtimeProvider ?? _validationOptions.RuntimeProvider,
            MemberAccessAnalyzer = analyzer ?? _validationOptions.MemberAccessAnalyzer,
            CheckSyntax = _validationOptions.CheckSyntax,
            CheckScope = _validationOptions.CheckScope,
            CheckTypes = _validationOptions.CheckTypes,
            CheckCalls = _validationOptions.CheckCalls,
            CheckControlFlow = _validationOptions.CheckControlFlow,
            CheckIndentation = _validationOptions.CheckIndentation,
            CheckMemberAccess = _validationOptions.CheckMemberAccess,
            MemberAccessSeverity = _validationOptions.MemberAccessSeverity,
            CheckAbstract = _validationOptions.CheckAbstract,
            CheckSignals = _validationOptions.CheckSignals,
            CheckResourcePaths = _validationOptions.CheckResourcePaths,
            EnableCommentSuppression = _validationOptions.EnableCommentSuppression
        };
    }

    private bool IsRuleEnabled(string ruleId)
    {
        if (_config?.Linting.Rules.TryGetValue(ruleId, out var ruleConfig) == true)
        {
            return ruleConfig.Enabled;
        }
        return true; // Enabled by default
    }

    private GDUnifiedDiagnosticSeverity ApplyConfiguredSeverity(string ruleId, GDUnifiedDiagnosticSeverity defaultSeverity)
    {
        if (_config?.Linting.Rules.TryGetValue(ruleId, out var ruleConfig) == true && ruleConfig.Severity.HasValue)
        {
            return MapConfigSeverity(ruleConfig.Severity.Value);
        }
        return defaultSeverity;
    }

    private static GDUnifiedDiagnosticSeverity MapConfigSeverity(GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDDiagnosticSeverity.Error => GDUnifiedDiagnosticSeverity.Error,
            GDDiagnosticSeverity.Warning => GDUnifiedDiagnosticSeverity.Warning,
            GDDiagnosticSeverity.Hint => GDUnifiedDiagnosticSeverity.Hint,
            _ => GDUnifiedDiagnosticSeverity.Info
        };
    }
}
