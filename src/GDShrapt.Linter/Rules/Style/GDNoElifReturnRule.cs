using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Warns when an elif follows an if block that ends with return.
    /// If the if block returns, the elif is unnecessary - just use a regular if after.
    /// Compatible with gdtoolkit's no-elif-return rule.
    /// </summary>
    public class GDNoElifReturnRule : GDLintRule
    {
        public override string RuleId => "GDL216";
        public override string Name => "no-elif-return";
        public override string Description => "Unnecessary elif after return in if block";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public override void Visit(GDIfStatement ifStatement)
        {
            // Check if the if block ends with a return
            if (BlockEndsWithReturn(ifStatement.IfBranch) &&
                ifStatement.ElifBranchesList != null &&
                ifStatement.TypedForm.Token1 != null)
            {
                // Report issue on each elif branch
                foreach (var elifBranch in ifStatement.ElifBranchesList.OfType<GDElifBranch>())
                {
                    if (elifBranch.ElifKeyword != null)
                    {
                        ReportIssue(
                            "Unnecessary 'elif' after 'return' in if block",
                            elifBranch.ElifKeyword,
                            "Replace 'elif' with 'if' since the previous block returns");
                    }
                }
            }

            base.Visit(ifStatement);
        }

        private bool BlockEndsWithReturn(GDIfBranch branch)
        {
            if (branch == null)
                return false;

            // Check for single-line expression (if x: return y)
            if (branch.Expression is GDReturnExpression)
                return true;

            // Check statements list
            if (branch.Statements == null || !branch.Statements.Any())
                return false;

            // Get the last statement
            var lastStatement = branch.Statements.OfType<GDExpressionStatement>().LastOrDefault();
            return lastStatement?.Expression is GDReturnExpression;
        }
    }
}
