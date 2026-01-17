using GDShrapt.Abstractions;
using System.Collections.Generic;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Provides code fix descriptors for lint issues.
    /// </summary>
    public class GDLintFixProvider
    {
        /// <summary>
        /// Gets all applicable fixes for a lint issue.
        /// </summary>
        public IEnumerable<GDFixDescriptor> GetFixes(GDLintIssue issue)
        {
            // Always offer suppression fix
            yield return new GDSuppressionFixDescriptor
            {
                DiagnosticCode = issue.RuleId,
                TargetLine = issue.StartLine,
                IsInline = true
            };

            // Rule-specific fixes
            foreach (var fix in GetRuleSpecificFixes(issue))
            {
                yield return fix;
            }
        }

        /// <summary>
        /// Gets rule-specific fixes based on the rule ID.
        /// </summary>
        private IEnumerable<GDFixDescriptor> GetRuleSpecificFixes(GDLintIssue issue)
        {
            switch (issue.RuleId)
            {
                // Naming rules (GDL001-009) - rename fixes
                case "GDL001": // class-name-case
                case "GDL002": // function-name-case
                case "GDL003": // variable-name-case
                case "GDL004": // constant-name-case
                case "GDL005": // signal-name-case
                case "GDL006": // enum-name-case
                case "GDL007": // enum-value-case
                case "GDL008": // private-prefix
                case "GDL009": // inner-class-name-case
                    foreach (var fix in CreateRenameFixes(issue))
                        yield return fix;
                    break;
            }
        }

        /// <summary>
        /// Creates rename fix descriptors for naming rule violations.
        /// </summary>
        private IEnumerable<GDFixDescriptor> CreateRenameFixes(GDLintIssue issue)
        {
            // Try to extract the suggested name from the suggestion text
            // Format: "Rename to 'suggested_name'"
            var suggestion = issue.Suggestion;
            if (string.IsNullOrEmpty(suggestion))
                yield break;

            var suggestedName = ExtractSuggestedName(suggestion);
            if (string.IsNullOrEmpty(suggestedName))
                yield break;

            yield return GDTextEditFixDescriptor.Replace(
                $"Rename to '{suggestedName}'",
                issue.StartLine,
                issue.StartColumn,
                issue.EndColumn,
                suggestedName)
                .WithKind(GDFixKind.ReplaceIdentifier);
        }

        /// <summary>
        /// Extracts the suggested name from a suggestion string.
        /// Expected format: "Rename to 'name'"
        /// </summary>
        private static string ExtractSuggestedName(string suggestion)
        {
            if (string.IsNullOrEmpty(suggestion))
                return null;

            // Look for pattern: Rename to 'name'
            const string prefix = "Rename to '";
            var startIndex = suggestion.IndexOf(prefix);
            if (startIndex < 0)
                return null;

            startIndex += prefix.Length;
            var endIndex = suggestion.IndexOf('\'', startIndex);
            if (endIndex < 0)
                return null;

            return suggestion.Substring(startIndex, endIndex - startIndex);
        }
    }
}
