namespace GDShrapt.Reader
{
    /// <summary>
    /// Detects comparisons of a value with itself (e.g., x == x, a != a).
    /// Such comparisons are usually bugs or dead code.
    /// </summary>
    public class GDSelfComparisonRule : GDLintRule
    {
        public override string RuleId => "GDL213";
        public override string Name => "self-comparison";
        public override string Description => "Warn when comparing a value with itself";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;

        public override void Visit(GDDualOperatorExpression expr)
        {
            if (expr?.Operator == null)
                return;

            var opType = expr.OperatorType;

            // Only check comparison operators
            if (opType != GDDualOperatorType.Equal &&
                opType != GDDualOperatorType.NotEqual &&
                opType != GDDualOperatorType.LessThan &&
                opType != GDDualOperatorType.MoreThan &&
                opType != GDDualOperatorType.LessThanOrEqual &&
                opType != GDDualOperatorType.MoreThanOrEqual)
                return;

            var left = expr.LeftExpression?.ToString();
            var right = expr.RightExpression?.ToString();

            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                return;

            if (left == right)
            {
                var resultDesc = GetComparisonResult(opType);
                ReportIssue(
                    $"Comparing '{left}' with itself is {resultDesc}",
                    expr.Operator,
                    "This comparison is likely a bug. Did you mean to compare with a different value?");
            }
        }

        private static string GetComparisonResult(GDDualOperatorType opType)
        {
            switch (opType)
            {
                case GDDualOperatorType.Equal:
                case GDDualOperatorType.LessThanOrEqual:
                case GDDualOperatorType.MoreThanOrEqual:
                    return "always true";
                case GDDualOperatorType.NotEqual:
                case GDDualOperatorType.LessThan:
                case GDDualOperatorType.MoreThan:
                    return "always false";
                default:
                    return "suspicious";
            }
        }
    }
}
