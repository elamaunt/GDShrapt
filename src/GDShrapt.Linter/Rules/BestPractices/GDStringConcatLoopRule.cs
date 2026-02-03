using GDShrapt.Reader;

namespace GDShrapt.Linter
{
    /// <summary>
    /// Warns about string concatenation with += inside loops.
    /// String concatenation in loops is inefficient; use PackedStringArray and join() instead.
    /// </summary>
    public class GDStringConcatLoopRule : GDLintRule
    {
        public override string RuleId => "GDL242";
        public override string Name => "string-concat-loop";
        public override string Description => "Warn about string concatenation in loops";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public override void Visit(GDExpressionStatement stmt)
        {
            if (Options?.WarnStringConcatInLoop != true)
                return;

            // Check for str += "..." pattern (compound assignment)
            if (stmt.Expression is GDDualOperatorExpression dual &&
                dual.OperatorType == GDDualOperatorType.AddAndAssign)
            {
                if (IsStringConcatenation(dual) && IsInsideLoop(stmt))
                {
                    ReportIssue(
                        "String concatenation in loop is inefficient",
                        dual.Operator,
                        "Use PackedStringArray and join(), or accumulate in an array");
                }
            }
        }

        private bool IsStringConcatenation(GDDualOperatorExpression dual)
        {
            // Check if right side is string literal
            if (dual.RightExpression is GDStringExpression)
                return true;

            // Check if right side is string concatenation
            if (dual.RightExpression is GDDualOperatorExpression innerDual &&
                innerDual.OperatorType == GDDualOperatorType.Addition)
            {
                return IsStringConcatenation(innerDual) ||
                       innerDual.LeftExpression is GDStringExpression ||
                       innerDual.RightExpression is GDStringExpression;
            }

            // Check for str() call (string conversion)
            if (dual.RightExpression is GDCallExpression call)
            {
                if (call.CallerExpression is GDIdentifierExpression idExpr &&
                    idExpr.Identifier?.Sequence == "str")
                    return true;
            }

            return false;
        }

        private bool IsInsideLoop(GDNode node)
        {
            foreach (var parent in node.Parents)
            {
                if (parent is GDForStatement || parent is GDWhileStatement)
                    return true;

                // Stop at function boundary
                if (parent is GDMethodDeclaration)
                    break;

                // Also stop at lambda boundary
                if (parent is GDMethodExpression)
                    break;
            }
            return false;
        }
    }
}
