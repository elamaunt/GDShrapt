using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Manages suppression state for a file during linting.
    /// </summary>
    public class GDSuppressionContext
    {
        private readonly List<GDSuppressionDirective> _directives;

        /// <summary>
        /// Gets all parsed suppression directives.
        /// </summary>
        public IReadOnlyList<GDSuppressionDirective> Directives => _directives;

        /// <summary>
        /// Creates a new suppression context with the given directives.
        /// </summary>
        /// <param name="directives">The parsed suppression directives.</param>
        public GDSuppressionContext(List<GDSuppressionDirective> directives)
        {
            _directives = directives ?? new List<GDSuppressionDirective>();
        }

        /// <summary>
        /// Checks if a rule is suppressed at the given line.
        /// </summary>
        /// <param name="ruleId">The rule ID (e.g., "GDL001").</param>
        /// <param name="ruleName">The rule name (e.g., "naming-class-case").</param>
        /// <param name="line">The line number to check (1-based).</param>
        /// <returns>True if the rule is suppressed at this line.</returns>
        public bool IsSuppressed(string ruleId, string ruleName, int line)
        {
            // 1. Check gdlint:ignore directives
            foreach (var directive in _directives.Where(d => d.Type == GDSuppressionType.Ignore))
            {
                if (!directive.AppliesToRule(ruleId, ruleName))
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

            // 2. Check gdlint:disable/enable directives
            bool disabled = false;
            foreach (var directive in _directives.OrderBy(d => d.Line))
            {
                // Only consider directives before or at the current line
                if (directive.Line > line)
                    break;

                if (directive.Type == GDSuppressionType.Disable)
                {
                    if (directive.AppliesToRule(ruleId, ruleName))
                        disabled = true;
                }
                else if (directive.Type == GDSuppressionType.Enable)
                {
                    if (directive.AppliesToRule(ruleId, ruleName))
                        disabled = false;
                }
            }

            return disabled;
        }

        /// <summary>
        /// Checks if a lint issue is suppressed.
        /// </summary>
        /// <param name="issue">The lint issue to check.</param>
        /// <returns>True if the issue is suppressed.</returns>
        public bool IsSuppressed(GDLintIssue issue)
        {
            if (issue == null)
                return false;

            return IsSuppressed(issue.RuleId, issue.RuleName, issue.StartLine);
        }
    }
}
