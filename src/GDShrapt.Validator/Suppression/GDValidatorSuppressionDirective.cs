using System.Collections.Generic;

namespace GDShrapt.Validator;

/// <summary>
/// Represents a single suppression directive parsed from a comment.
/// </summary>
public class GDValidatorSuppressionDirective
{
    /// <summary>
    /// The type of suppression (Ignore, Disable, Enable).
    /// </summary>
    public GDValidatorSuppressionType Type { get; }

    /// <summary>
    /// The rule IDs to suppress. Null means all rules.
    /// </summary>
    public HashSet<string>? RuleIds { get; }

    /// <summary>
    /// The line number where this directive appears (1-based).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Whether this directive appears at the end of a line (inline with code).
    /// </summary>
    public bool IsInline { get; }

    /// <summary>
    /// Creates a new suppression directive.
    /// </summary>
    /// <param name="type">Type of suppression.</param>
    /// <param name="ruleIds">Rule IDs to suppress, or null for all rules.</param>
    /// <param name="line">Line number where the directive appears.</param>
    /// <param name="isInline">Whether the comment is inline with code.</param>
    public GDValidatorSuppressionDirective(GDValidatorSuppressionType type, HashSet<string>? ruleIds, int line, bool isInline = false)
    {
        Type = type;
        RuleIds = ruleIds;
        Line = line;
        IsInline = isInline;
    }

    /// <summary>
    /// Checks if this directive applies to a specific rule.
    /// </summary>
    /// <param name="ruleId">Rule ID (e.g., "GD1001", "GD3009").</param>
    /// <returns>True if this directive suppresses the specified rule.</returns>
    public bool AppliesToRule(string ruleId)
    {
        // Null means all rules
        if (RuleIds == null)
            return true;

        // Check by rule ID (case-insensitive)
        if (!string.IsNullOrEmpty(ruleId) && RuleIds.Contains(ruleId))
            return true;

        return false;
    }
}
