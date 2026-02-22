using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Base class for all lint rules.
    /// Thread-safe: uses [ThreadStatic] fields for per-invocation state.
    /// </summary>
    public abstract class GDLintRule : GDVisitor
    {
        [ThreadStatic] private static GDLintResult t_result;
        [ThreadStatic] private static GDLinterOptions t_options;

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
        protected GDLinterOptions Options => t_options;

        /// <summary>
        /// Runs this rule on the given node.
        /// </summary>
        internal virtual void Run(GDNode node, GDLintResult result, GDLinterOptions options)
        {
            t_result = result;
            t_options = options;
            try
            {
                node?.WalkIn(this);
            }
            finally
            {
                t_result = null;
                t_options = null;
            }
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
            t_result?.AddIssue(new GDLintIssue(
                RuleId,
                Name,
                severity,
                Category,
                message,
                token,
                suggestion));
        }

        /// <summary>
        /// Reports an issue with explicit line/column coordinates (1-based line, 0-based column).
        /// </summary>
        protected void ReportIssue(string message, int startLine, int startColumn, string suggestion = null)
        {
            t_result?.AddIssue(new GDLintIssue(
                RuleId,
                Name,
                DefaultSeverity,
                Category,
                message,
                startLine,
                startColumn,
                startLine,
                startColumn,
                suggestion));
        }

        /// <summary>
        /// Reports an issue with fix descriptors.
        /// </summary>
        protected void ReportIssue(string message, GDSyntaxToken token, string suggestion, IEnumerable<GDFixDescriptor> fixes)
        {
            ReportIssue(DefaultSeverity, message, token, suggestion, fixes);
        }

        /// <summary>
        /// Reports an issue with specific severity and fix descriptors.
        /// </summary>
        protected void ReportIssue(GDLintSeverity severity, string message, GDSyntaxToken token, string suggestion, IEnumerable<GDFixDescriptor> fixes)
        {
            var issue = new GDLintIssue(
                RuleId,
                Name,
                severity,
                Category,
                message,
                token,
                suggestion);

            if (fixes != null)
            {
                issue.FixDescriptors = fixes.ToList();
            }

            t_result?.AddIssue(issue);
        }
    }
}
