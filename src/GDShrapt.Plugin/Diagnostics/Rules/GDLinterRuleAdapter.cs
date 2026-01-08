using GDShrapt.Plugin.Config;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;

namespace GDShrapt.Plugin.Diagnostics.Rules;

/// <summary>
/// Adapter that wraps GDShrapt.Linter rules to work with the plugin's ILintRule interface.
/// This enables seamless integration of the external GDLinter rules into the plugin's diagnostic system.
/// </summary>
internal class GDLinterRuleAdapter : ILintRule
{
    private readonly GDLintRule _linterRule;

    /// <summary>
    /// Creates an adapter for a GDShrapt.Linter rule.
    /// </summary>
    public GDLinterRuleAdapter(GDLintRule rule)
    {
        _linterRule = rule ?? throw new ArgumentNullException(nameof(rule));
    }

    /// <summary>
    /// The underlying GDShrapt.Linter rule.
    /// </summary>
    public GDLintRule UnderlyingRule => _linterRule;

    public string RuleId => _linterRule.RuleId;
    public string Name => _linterRule.Name;
    public string Description => _linterRule.Description;
    public DiagnosticCategory Category => MapCategory(_linterRule.Category);
    public DiagnosticSeverity DefaultSeverity => MapSeverity(_linterRule.DefaultSeverity);

    /// <summary>
    /// GDLinter rules don't have formatting levels - they always run when enabled.
    /// </summary>
    public FormattingLevel RequiredFormattingLevel => FormattingLevel.Off;

    /// <summary>
    /// Whether the underlying rule is enabled by default.
    /// </summary>
    public bool EnabledByDefault => _linterRule.EnabledByDefault;

    public IEnumerable<Diagnostic> Analyze(
        GDScriptMap scriptMap,
        string content,
        RuleConfig ruleConfig,
        ProjectConfig projectConfig)
    {
        if (scriptMap?.Class == null || string.IsNullOrEmpty(content))
            yield break;

        // Create linter options from plugin config
        var options = LinterOptionsMapper.CreateOptions(projectConfig?.AdvancedLinting);

        // Create a single-rule linter
        var linter = GDLinter.CreateEmpty();
        linter.Options = options;
        linter.AddRule(_linterRule);

        // Run linting
        GDLintResult result;
        try
        {
            result = linter.Lint(scriptMap.Class);
        }
        catch (Exception ex)
        {
            Logger.Error($"GDLinter rule {RuleId} failed: {ex.Message}");
            yield break;
        }

        // Convert issues to diagnostics
        foreach (var issue in result.Issues)
        {
            yield return ConvertToDiagnostic(issue, scriptMap.Reference);
        }
    }

    private Diagnostic ConvertToDiagnostic(GDLintIssue issue, ScriptReference script)
    {
        var builder = Diagnostic.Create(issue.RuleId, issue.Message)
            .WithSeverity(MapSeverity(issue.Severity))
            .WithCategory(MapCategory(issue.Category))
            .AtScript(script);

        // GDLintIssue uses 1-based line/column, Diagnostic uses 0-based
        if (issue.StartLine > 0)
        {
            builder.AtSpan(
                issue.StartLine - 1,
                issue.StartColumn - 1,
                issue.EndLine - 1,
                issue.EndColumn - 1);
        }

        // Add suggestion as a fix if available
        if (!string.IsNullOrEmpty(issue.Suggestion))
        {
            builder.WithFix(issue.Suggestion, source => source); // Placeholder - actual fix would need more info
        }

        return builder.Build();
    }

    private static DiagnosticCategory MapCategory(GDLintCategory category)
    {
        return category switch
        {
            GDLintCategory.Naming => DiagnosticCategory.Style,
            GDLintCategory.Style => DiagnosticCategory.Formatting,
            GDLintCategory.BestPractices => DiagnosticCategory.BestPractice,
            GDLintCategory.Organization => DiagnosticCategory.Style,
            GDLintCategory.Documentation => DiagnosticCategory.Style,
            _ => DiagnosticCategory.Style
        };
    }

    private static DiagnosticSeverity MapSeverity(GDLintSeverity severity)
    {
        return severity switch
        {
            GDLintSeverity.Hint => DiagnosticSeverity.Hint,
            GDLintSeverity.Info => DiagnosticSeverity.Info,
            GDLintSeverity.Warning => DiagnosticSeverity.Warning,
            GDLintSeverity.Error => DiagnosticSeverity.Error,
            _ => DiagnosticSeverity.Warning
        };
    }
}
