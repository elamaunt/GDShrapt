using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Parses suppression directives from GDScript comments.
    /// Compatible with gdtoolkit/gdlint syntax.
    /// </summary>
    internal static class GDSuppressionParser
    {
        // Regex patterns for gdlint directives
        // gdlint:ignore = rule1, rule2  or  gdlint:ignore
        private static readonly Regex IgnorePattern = new Regex(
            @"#\s*gdlint\s*:\s*ignore\s*(=\s*([\w\-]+(?:\s*,\s*[\w\-]+)*))?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // gdlint: disable=rule1,rule2  or  gdlint: disable
        private static readonly Regex DisablePattern = new Regex(
            @"#\s*gdlint\s*:\s*disable\s*(=\s*([\w\-]+(?:\s*,\s*[\w\-]+)*))?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // gdlint: enable=rule1,rule2  or  gdlint: enable
        private static readonly Regex EnablePattern = new Regex(
            @"#\s*gdlint\s*:\s*enable\s*(=\s*([\w\-]+(?:\s*,\s*[\w\-]+)*))?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Parses all suppression directives from an AST node.
        /// </summary>
        /// <param name="node">The root node to parse.</param>
        /// <returns>A suppression context containing all parsed directives.</returns>
        public static GDSuppressionContext Parse(GDNode node)
        {
            var directives = new List<GDSuppressionDirective>();

            if (node == null)
                return new GDSuppressionContext(directives);

            foreach (var token in node.AllTokens)
            {
                if (token is GDComment comment)
                {
                    var directive = ParseComment(comment);
                    if (directive != null)
                        directives.Add(directive);
                }
            }

            return new GDSuppressionContext(directives);
        }

        /// <summary>
        /// Parses a single comment token for suppression directives.
        /// </summary>
        /// <param name="comment">The comment token to parse.</param>
        /// <returns>A suppression directive, or null if not a suppression comment.</returns>
        internal static GDSuppressionDirective ParseComment(GDComment comment)
        {
            if (comment == null)
                return null;

            var text = comment.Sequence;
            if (string.IsNullOrEmpty(text))
                return null;

            var line = comment.StartLine;

            // Check if comment is inline (has code before it on the same line)
            bool isInline = IsInlineComment(comment);

            // Try each pattern
            var ignoreMatch = IgnorePattern.Match(text);
            if (ignoreMatch.Success)
            {
                var ruleIds = ParseRuleIds(ignoreMatch.Groups[2].Value);
                return new GDSuppressionDirective(GDSuppressionType.Ignore, ruleIds, line, isInline);
            }

            var disableMatch = DisablePattern.Match(text);
            if (disableMatch.Success)
            {
                var ruleIds = ParseRuleIds(disableMatch.Groups[2].Value);
                return new GDSuppressionDirective(GDSuppressionType.Disable, ruleIds, line, isInline);
            }

            var enableMatch = EnablePattern.Match(text);
            if (enableMatch.Success)
            {
                var ruleIds = ParseRuleIds(enableMatch.Groups[2].Value);
                return new GDSuppressionDirective(GDSuppressionType.Enable, ruleIds, line, isInline);
            }

            return null;
        }

        /// <summary>
        /// Parses comma-separated rule IDs from a match group.
        /// </summary>
        private static HashSet<string> ParseRuleIds(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null; // No rules specified = all rules

            var ruleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parts = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    ruleIds.Add(trimmed);
            }

            return ruleIds.Count > 0 ? ruleIds : null;
        }

        /// <summary>
        /// Determines if a comment is inline (has code before it on the same line).
        /// </summary>
        private static bool IsInlineComment(GDComment comment)
        {
            // Walk backwards from the comment to see if there's code on the same line
            var prev = comment.GlobalPreviousToken;
            while (prev != null)
            {
                // If we hit a newline, we're at the start of the line
                if (prev is GDNewLine)
                    return false;

                // If we hit non-whitespace that's not a newline, it's inline
                if (!(prev is GDSpace) && !(prev is GDIntendation))
                    return true;

                prev = prev.GlobalPreviousToken;
            }

            // Reached the start of file - not inline
            return false;
        }
    }
}
