using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Detects unreachable code after return, break, or continue statements.
    /// </summary>
    public class GDDeadCodeRule : GDLintRule
    {
        public override string RuleId => "GDL210";
        public override string Name => "dead-code";
        public override string Description => "Warn about unreachable code";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        public override void Visit(GDStatementsList statements)
        {
            if (statements == null)
                return;

            bool unreachable = false;

            foreach (var stmt in statements)
            {
                if (stmt == null)
                    continue;

                if (unreachable)
                {
                    // Find a meaningful token for position reporting
                    var token = stmt.AllTokens.FirstOrDefault(t => !(t is GDSpace) && !(t is GDIntendation));
                    if (token != null)
                    {
                        ReportIssue(
                            "Unreachable code detected",
                            token,
                            "Remove dead code or fix the control flow");
                    }
                    // Only report once per block
                    break;
                }

                // Check if this statement makes following code unreachable
                if (IsTerminatingStatement(stmt))
                {
                    unreachable = true;
                }
            }
        }

        private bool IsTerminatingStatement(GDStatement stmt)
        {
            if (stmt is GDExpressionStatement exprStmt)
            {
                var expr = exprStmt.Expression;
                return expr is GDReturnExpression ||
                       expr is GDBreakExpression ||
                       expr is GDContinueExpression;
            }

            return false;
        }
    }
}
