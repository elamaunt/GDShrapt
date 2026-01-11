
namespace GDShrapt.Plugin;

/// <summary>
/// Interface for lint rules that analyze GDScript code.
/// </summary>
internal interface ILintRule
{
    /// <summary>
    /// Unique identifier for the rule (e.g., "GDS001").
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Human-readable name of the rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of what the rule checks.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Category for grouping (Formatting, Style, etc.).
    /// </summary>
    DiagnosticCategory Category { get; }

    /// <summary>
    /// Default severity if not overridden by config.
    /// </summary>
    GDDiagnosticSeverity DefaultSeverity { get; }

    /// <summary>
    /// Minimum formatting level required for this rule to run.
    /// Only applies to Formatting category rules.
    /// </summary>
    GDFormattingLevel RequiredFormattingLevel { get; }

    /// <summary>
    /// Analyzes a script and returns diagnostics.
    /// </summary>
    /// <param name="scriptMap">Parsed script information.</param>
    /// <param name="content">Raw script content.</param>
    /// <param name="ruleConfig">Rule-specific configuration.</param>
    /// <param name="projectConfig">Full project configuration.</param>
    /// <returns>Enumerable of diagnostics found.</returns>
    IEnumerable<Diagnostic> Analyze(
        GDScriptMap scriptMap,
        string content,
        GDRuleConfig ruleConfig,
        ProjectConfig projectConfig);
}
