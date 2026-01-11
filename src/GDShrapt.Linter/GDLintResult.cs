using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Result of linting a GDScript file.
    /// </summary>
    public class GDLintResult
    {
        private readonly List<GDLintIssue> _issues;

        /// <summary>
        /// All issues found during linting.
        /// </summary>
        public IReadOnlyList<GDLintIssue> Issues => _issues;

        /// <summary>
        /// Whether any errors were found.
        /// </summary>
        public bool HasErrors => _issues.Any(i => i.Severity == GDLintSeverity.Error);

        /// <summary>
        /// Whether any warnings were found.
        /// </summary>
        public bool HasWarnings => _issues.Any(i => i.Severity == GDLintSeverity.Warning);

        /// <summary>
        /// Whether the code passes all lint checks (no errors or warnings).
        /// </summary>
        public bool IsClean => !HasErrors && !HasWarnings;

        /// <summary>
        /// Total count of issues.
        /// </summary>
        public int TotalCount => _issues.Count;

        /// <summary>
        /// Count of errors.
        /// </summary>
        public int ErrorCount => _issues.Count(i => i.Severity == GDLintSeverity.Error);

        /// <summary>
        /// Count of warnings.
        /// </summary>
        public int WarningCount => _issues.Count(i => i.Severity == GDLintSeverity.Warning);

        /// <summary>
        /// Count of info messages.
        /// </summary>
        public int InfoCount => _issues.Count(i => i.Severity == GDLintSeverity.Info);

        /// <summary>
        /// Count of hints.
        /// </summary>
        public int HintCount => _issues.Count(i => i.Severity == GDLintSeverity.Hint);

        public GDLintResult()
        {
            _issues = new List<GDLintIssue>();
        }

        internal void AddIssue(GDLintIssue issue)
        {
            _issues.Add(issue);
        }

        /// <summary>
        /// Gets issues filtered by severity.
        /// </summary>
        public IEnumerable<GDLintIssue> GetIssuesBySeverity(GDLintSeverity severity)
        {
            return _issues.Where(i => i.Severity == severity);
        }

        /// <summary>
        /// Gets issues filtered by category.
        /// </summary>
        public IEnumerable<GDLintIssue> GetIssuesByCategory(GDLintCategory category)
        {
            return _issues.Where(i => i.Category == category);
        }

        /// <summary>
        /// Gets issues filtered by rule ID.
        /// </summary>
        public IEnumerable<GDLintIssue> GetIssuesByRule(string ruleId)
        {
            return _issues.Where(i => i.RuleId == ruleId);
        }

        /// <summary>
        /// Gets issues at a specific line.
        /// </summary>
        public IEnumerable<GDLintIssue> GetIssuesAtLine(int line)
        {
            return _issues.Where(i => i.StartLine <= line && i.EndLine >= line);
        }

        /// <summary>
        /// Gets all errors.
        /// </summary>
        public IEnumerable<GDLintIssue> GetErrors() => GetIssuesBySeverity(GDLintSeverity.Error);

        /// <summary>
        /// Gets all warnings.
        /// </summary>
        public IEnumerable<GDLintIssue> GetWarnings() => GetIssuesBySeverity(GDLintSeverity.Warning);

        /// <summary>
        /// Removes issues that are suppressed by inline comments.
        /// </summary>
        /// <param name="context">The suppression context containing parsed directives.</param>
        internal void FilterSuppressed(GDSuppressionContext context)
        {
            if (context == null)
                return;

            _issues.RemoveAll(issue => context.IsSuppressed(issue));
        }
    }
}
