namespace GDShrapt.Reader
{
    /// <summary>
    /// Base class for all lint rules.
    /// </summary>
    public abstract class GDLintRule : GDVisitor
    {
        private GDLintResult _result;
        private GDLinterOptions _options;

        /// <summary>
        /// Unique identifier for this rule (e.g., "GDL001", "naming-class-case").
        /// </summary>
        public abstract string RuleId { get; }

        /// <summary>
        /// Human-readable name of the rule.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Description of what this rule checks.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Category this rule belongs to.
        /// </summary>
        public abstract GDLintCategory Category { get; }

        /// <summary>
        /// Default severity for violations of this rule.
        /// </summary>
        public abstract GDLintSeverity DefaultSeverity { get; }

        /// <summary>
        /// Whether this rule is enabled by default.
        /// </summary>
        public virtual bool EnabledByDefault => true;

        /// <summary>
        /// Current linter options.
        /// </summary>
        protected GDLinterOptions Options => _options;

        /// <summary>
        /// Runs this rule on the given node.
        /// </summary>
        internal void Run(GDNode node, GDLintResult result, GDLinterOptions options)
        {
            _result = result;
            _options = options;
            node?.WalkIn(this);
        }

        /// <summary>
        /// Reports an issue with the default severity.
        /// </summary>
        protected void ReportIssue(string message, GDSyntaxToken token, string suggestion = null)
        {
            ReportIssue(DefaultSeverity, message, token, suggestion);
        }

        /// <summary>
        /// Reports an issue with a specific severity.
        /// </summary>
        protected void ReportIssue(GDLintSeverity severity, string message, GDSyntaxToken token, string suggestion = null)
        {
            _result?.AddIssue(new GDLintIssue(
                RuleId,
                Name,
                severity,
                Category,
                message,
                token,
                suggestion));
        }
    }
}
