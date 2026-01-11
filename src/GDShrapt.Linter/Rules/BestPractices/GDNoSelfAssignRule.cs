using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns about self-assignment (x = x) which is usually a mistake.
    /// </summary>
    public class GDNoSelfAssignRule : GDLintRule
    {
        public override string RuleId => "GDL230";
        public override string Name => "no-self-assign";
        public override string Description => "Warn about self-assignment";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => true;

        public override void Visit(GDDualOperatorExpression expr)
        {
            // Only check simple assignments (not compound like +=)
            if (expr.OperatorType != GDDualOperatorType.Assignment)
            {
                base.Visit(expr);
                return;
            }

            var left = expr.LeftExpression;
            var right = expr.RightExpression;

            if (left == null || right == null)
            {
                base.Visit(expr);
                return;
            }

            // Compare string representations for simple cases
            var leftStr = left.ToString()?.Trim();
            var rightStr = right.ToString()?.Trim();

            if (string.IsNullOrEmpty(leftStr) || string.IsNullOrEmpty(rightStr))
            {
                base.Visit(expr);
                return;
            }

            if (leftStr == rightStr)
            {
                ReportIssue(
                    $"Self-assignment of '{leftStr}'",
                    left.FirstChildToken,
                    "Remove this statement or fix the assignment");
            }

            base.Visit(expr);
        }
    }
}
