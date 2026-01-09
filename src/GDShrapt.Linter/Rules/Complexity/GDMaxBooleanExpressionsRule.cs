namespace GDShrapt.Reader
{
    /// <summary>
    /// Warns when a condition has too many boolean expressions (and/or operators).
    /// Complex conditions are hard to understand and should be simplified.
    /// </summary>
    public class GDMaxBooleanExpressionsRule : GDLintRule
    {
        public override string RuleId => "GDL229";
        public override string Name => "max-boolean-expressions";
        public override string Description => "Warn when condition has too many boolean expressions";
        public override GDLintCategory Category => GDLintCategory.Complexity;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public const int DefaultMaxBooleanExpressions = 5;

        public override void Visit(GDIfBranch ifBranch)
        {
            if (ifBranch.Condition != null)
            {
                CheckConditionComplexity(ifBranch.Condition, ifBranch.IfKeyword);
            }
            base.Visit(ifBranch);
        }

        public override void Visit(GDElifBranch elifBranch)
        {
            if (elifBranch.Condition != null)
            {
                CheckConditionComplexity(elifBranch.Condition, elifBranch.ElifKeyword);
            }
            base.Visit(elifBranch);
        }

        public override void Visit(GDWhileStatement whileStmt)
        {
            if (whileStmt.Condition != null)
            {
                CheckConditionComplexity(whileStmt.Condition, whileStmt.WhileKeyword);
            }
            base.Visit(whileStmt);
        }

        private void CheckConditionComplexity(GDExpression condition, GDSyntaxToken reportToken)
        {
            var maxExpressions = Options?.MaxBooleanExpressions ?? DefaultMaxBooleanExpressions;
            if (maxExpressions <= 0)
                return; // Disabled

            // Count boolean operators in the condition
            int booleanOpCount = CountBooleanOperators(condition);

            // Number of conditions = operators + 1
            int conditionCount = booleanOpCount + 1;

            if (conditionCount > maxExpressions)
            {
                ReportIssue(
                    $"Condition has {conditionCount} boolean expressions (max {maxExpressions})",
                    reportToken,
                    "Consider extracting complex conditions into named boolean variables or functions");
            }
        }

        private int CountBooleanOperators(GDExpression expression)
        {
            int count = 0;

            foreach (var node in expression.AllNodes)
            {
                if (node is GDDualOperatorExpression dualOp)
                {
                    var opType = dualOp.OperatorType;
                    if (opType == GDDualOperatorType.And ||
                        opType == GDDualOperatorType.And2 ||
                        opType == GDDualOperatorType.Or ||
                        opType == GDDualOperatorType.Or2)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
