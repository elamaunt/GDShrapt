using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Validator;

/// <summary>
/// Manages suppression state for a file during validation.
/// </summary>
public class GDValidatorSuppressionContext
{
    private readonly List<GDValidatorSuppressionDirective> _directives;

    /// <summary>
    /// Gets all parsed suppression directives.
    /// </summary>
    public IReadOnlyList<GDValidatorSuppressionDirective> Directives => _directives;

    /// <summary>
    /// Creates a new suppression context with the given directives.
    /// </summary>
    /// <param name="directives">The parsed suppression directives.</param>
    public GDValidatorSuppressionContext(List<GDValidatorSuppressionDirective> directives)
    {
        _directives = directives ?? new List<GDValidatorSuppressionDirective>();
    }

    /// <summary>
    /// Creates an empty suppression context.
    /// </summary>
    public static GDValidatorSuppressionContext Empty => new GDValidatorSuppressionContext(new List<GDValidatorSuppressionDirective>());

    /// <summary>
    /// Checks if a rule is suppressed at the given line.
    /// </summary>
    /// <param name="ruleId">The rule ID (e.g., "GD1001", "GD3009").</param>
    /// <param name="line">The line number to check (1-based).</param>
    /// <returns>True if the rule is suppressed at this line.</returns>
    public bool IsSuppressed(string ruleId, int line)
    {
        // 1. Check gd:ignore directives
        foreach (var directive in _directives.Where(d => d.Type == GDValidatorSuppressionType.Ignore))
        {
            if (!directive.AppliesToRule(ruleId))
                continue;

            if (directive.IsInline)
            {
                // Inline comment suppresses the same line
                if (directive.Line == line)
                    return true;
            }
            else
            {
                // Standalone comment suppresses the next line
                if (directive.Line == line - 1)
                    return true;
            }
        }

        // 2. Check gd:disable/enable directives
        bool disabled = false;
        foreach (var directive in _directives.OrderBy(d => d.Line))
        {
            // Only consider directives before or at the current line
            if (directive.Line > line)
                break;

            if (directive.Type == GDValidatorSuppressionType.Disable)
            {
                if (directive.AppliesToRule(ruleId))
                    disabled = true;
            }
            else if (directive.Type == GDValidatorSuppressionType.Enable)
            {
                if (directive.AppliesToRule(ruleId))
                    disabled = false;
            }
        }

        return disabled;
    }
}
