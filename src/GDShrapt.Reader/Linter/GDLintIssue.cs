namespace GDShrapt.Reader
{
    /// <summary>
    /// Represents a single lint issue found in the code.
    /// </summary>
    public class GDLintIssue
    {
        /// <summary>
        /// The rule that generated this issue.
        /// </summary>
        public string RuleId { get; }

        /// <summary>
        /// Human-readable name of the rule.
        /// </summary>
        public string RuleName { get; }

        /// <summary>
        /// Severity of this issue.
        /// </summary>
        public GDLintSeverity Severity { get; }

        /// <summary>
        /// Category of the rule.
        /// </summary>
        public GDLintCategory Category { get; }

        /// <summary>
        /// Description of the issue.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Optional suggestion for fixing the issue.
        /// </summary>
        public string Suggestion { get; }

        /// <summary>
        /// The token that triggered this issue.
        /// </summary>
        public GDSyntaxToken Token { get; }

        /// <summary>
        /// Starting line number (1-based).
        /// </summary>
        public int StartLine { get; }

        /// <summary>
        /// Starting column number (1-based).
        /// </summary>
        public int StartColumn { get; }

        /// <summary>
        /// Ending line number (1-based).
        /// </summary>
        public int EndLine { get; }

        /// <summary>
        /// Ending column number (1-based).
        /// </summary>
        public int EndColumn { get; }

        public GDLintIssue(
            string ruleId,
            string ruleName,
            GDLintSeverity severity,
            GDLintCategory category,
            string message,
            GDSyntaxToken token,
            string suggestion = null)
        {
            RuleId = ruleId;
            RuleName = ruleName;
            Severity = severity;
            Category = category;
            Message = message;
            Suggestion = suggestion;
            Token = token;

            if (token != null)
            {
                StartLine = token.StartLine;
                StartColumn = token.StartColumn;
                EndLine = token.EndLine;
                EndColumn = token.EndColumn;
            }
        }

        public override string ToString()
        {
            var location = StartLine > 0 ? $"({StartLine}:{StartColumn}) " : "";
            var suggestion = !string.IsNullOrEmpty(Suggestion) ? $" Suggestion: {Suggestion}" : "";
            return $"{location}[{RuleId}] {Severity}: {Message}{suggestion}";
        }
    }
}
