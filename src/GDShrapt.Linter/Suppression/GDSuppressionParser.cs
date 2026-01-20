using System;
using System.Collections.Generic;
using System.Linq;
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

        // gdlint:ignore-file = rule1,rule2  or  gdlint:ignore-file
        private static readonly Regex IgnoreFilePattern = new Regex(
            @"#\s*gdlint\s*:\s*ignore-file\s*(=\s*([\w\-]+(?:\s*,\s*[\w\-]+)*))?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // gdlint:ignore-function = rule1,rule2  or  gdlint:ignore-function
        private static readonly Regex IgnoreFunctionPattern = new Regex(
            @"#\s*gdlint\s*:\s*ignore-function\s*(=\s*([\w\-]+(?:\s*,\s*[\w\-]+)*))?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // gdlint:ignore-below = rule1,rule2  or  gdlint:ignore-below
        private static readonly Regex IgnoreBelowPattern = new Regex(
            @"#\s*gdlint\s*:\s*ignore-below\s*(=\s*([\w\-]+(?:\s*,\s*[\w\-]+)*))?",
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

            // Resolve function boundaries for IgnoreFunction directives
            ResolveFunctionBoundaries(node, directives);

            return new GDSuppressionContext(directives);
        }

        /// <summary>
        /// Resolves function end lines for IgnoreFunction directives.
        /// </summary>
        private static void ResolveFunctionBoundaries(GDNode node, List<GDSuppressionDirective> directives)
        {
            var functionDirectives = directives
                .Where(d => d.Type == GDSuppressionType.IgnoreFunction)
                .ToList();

            if (functionDirectives.Count == 0)
                return;

            // Collect all function declarations with their line ranges
            var functions = new List<(int StartLine, int EndLine)>();
            CollectFunctions(node, functions);

            // For each IgnoreFunction directive, find the function that follows it
            foreach (var directive in functionDirectives)
            {
                // Find the first function that starts after (or on) the directive line
                var matchingFunction = functions
                    .Where(f => f.StartLine >= directive.Line)
                    .OrderBy(f => f.StartLine)
                    .FirstOrDefault();

                if (matchingFunction.EndLine > 0)
                {
                    directive.FunctionEndLine = matchingFunction.EndLine;
                }
            }
        }

        /// <summary>
        /// Recursively collects function declarations and their line ranges.
        /// </summary>
        private static void CollectFunctions(GDNode node, List<(int StartLine, int EndLine)> functions)
        {
            if (node == null)
                return;

            if (node is GDMethodDeclaration method)
            {
                functions.Add((method.StartLine, method.EndLine));
            }

            foreach (var child in node.Nodes)
            {
                CollectFunctions(child, functions);
            }
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

            // Try each pattern (order matters: more specific patterns first)

            // Check ignore-file first (before ignore)
            var ignoreFileMatch = IgnoreFilePattern.Match(text);
            if (ignoreFileMatch.Success)
            {
                var ruleIds = ParseRuleIds(ignoreFileMatch.Groups[2].Value);
                return new GDSuppressionDirective(GDSuppressionType.IgnoreFile, ruleIds, line, isInline);
            }

            // Check ignore-function (before ignore)
            var ignoreFunctionMatch = IgnoreFunctionPattern.Match(text);
            if (ignoreFunctionMatch.Success)
            {
                var ruleIds = ParseRuleIds(ignoreFunctionMatch.Groups[2].Value);
                return new GDSuppressionDirective(GDSuppressionType.IgnoreFunction, ruleIds, line, isInline);
            }

            // Check ignore-below (before ignore)
            var ignoreBelowMatch = IgnoreBelowPattern.Match(text);
            if (ignoreBelowMatch.Success)
            {
                var ruleIds = ParseRuleIds(ignoreBelowMatch.Groups[2].Value);
                return new GDSuppressionDirective(GDSuppressionType.IgnoreBelow, ruleIds, line, isInline);
            }

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
