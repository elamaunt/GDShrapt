using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns when an else follows an if block that ends with return.
    /// If the if block returns, the else is unnecessary - just use the code without else.
    /// Compatible with gdtoolkit's no-else-return rule.
    /// </summary>
    public class GDNoElseReturnRule : GDLintRule
    {
        public override string RuleId => "GDL217";
        public override string Name => "no-else-return";
        public override string Description => "Unnecessary else after return in if block";
        public override GDLintCategory Category => GDLintCategory.Style;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public override void Visit(GDIfStatement ifStatement)
        {
            // Check if the if block (and all elif blocks if any) end with return
            bool ifEndsWithReturn = BlockEndsWithReturn(ifStatement.IfBranch);

            // Only check else if the main if block ends with return
            if (ifEndsWithReturn &&
                ifStatement.ElseBranch != null &&
                ifStatement.TypedForm.Token2 != null &&
                ifStatement.ElseBranch.ElseKeyword != null)
            {
                // Also check that any elif blocks also return (for complete early return pattern)
                bool allBranchesReturn = true;
                if (ifStatement.ElifBranchesList != null)
                {
                    foreach (var elifBranch in ifStatement.ElifBranchesList.OfType<GDElifBranch>())
                    {
                        if (!ElifBlockEndsWithReturn(elifBranch))
                        {
                            allBranchesReturn = false;
                            break;
                        }
                    }
                }

                // Only report if all previous branches end with return
                if (allBranchesReturn)
                {
                    ReportIssue(
                        "Unnecessary 'else' after 'return' in if block",
                        ifStatement.ElseBranch.ElseKeyword,
                        "Remove 'else' since the if block already returns");
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

        private bool ElifBlockEndsWithReturn(GDElifBranch branch)
        {
            if (branch == null)
                return false;

            // Check for single-line expression
            if (branch.Expression is GDReturnExpression)
                return true;

            // Check statements list
            if (branch.Statements == null || !branch.Statements.Any())
                return false;

            var lastStatement = branch.Statements.OfType<GDExpressionStatement>().LastOrDefault();
            return lastStatement?.Expression is GDReturnExpression;
        }
    }
}
