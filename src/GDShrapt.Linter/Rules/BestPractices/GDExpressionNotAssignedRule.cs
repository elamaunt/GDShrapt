using System.Collections.Generic;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Warns when an expression result is not assigned or used.
    /// This is a conservative rule that only reports obvious cases.
    /// </summary>
    public class GDExpressionNotAssignedRule : GDLintRule
    {
        public override string RuleId => "GDL224";
        public override string Name => "expression-not-assigned";
        public override string Description => "Warn when expression result is not used";
        public override GDLintCategory Category => GDLintCategory.BestPractices;
        public override GDLintSeverity DefaultSeverity => GDLintSeverity.Warning;
        public override bool EnabledByDefault => false;

        public override void Visit(GDExpressionStatement exprStmt)
        {
            if (!Options?.WarnExpressionNotAssigned ?? false)
            {
                base.Visit(exprStmt);
                return;
            }

            var expr = exprStmt.Expression;
            if (expr == null)
            {
                base.Visit(exprStmt);
                return;
            }

            // Skip expressions that are typically used for side effects
            if (IsAcceptableExpression(expr))
            {
                base.Visit(exprStmt);
                return;
            }

            // Report unused literals, identifiers, and pure operations
            if (expr is GDNumberExpression ||
                expr is GDStringExpression ||
                expr is GDBoolExpression ||
                expr is GDArrayInitializerExpression ||
                expr is GDDictionaryInitializerExpression)
            {
                ReportIssue(
                    "Expression result is not used",
                    GetFirstToken(expr),
                    "Remove this statement or assign the result to a variable");
            }
            else if (expr is GDIdentifierExpression identExpr)
            {
                // Just reading a variable without using it
                ReportIssue(
                    $"Expression result of '{identExpr.Identifier?.Sequence}' is not used",
                    identExpr.Identifier,
                    "Remove this statement or use the value");
            }
            else if (expr is GDDualOperatorExpression dualOp)
            {
                // Pure operations like "1 + 2" without assignment
                if (IsPureOperator(dualOp.OperatorType))
                {
                    ReportIssue(
                        "Expression result is not used",
                        GetFirstToken(expr),
                        "Remove this statement or assign the result to a variable");
                }
            }

            base.Visit(exprStmt);
        }

        private bool IsAcceptableExpression(GDExpression expr)
        {
            // Assignments are always acceptable (including compound assignments)
            if (expr is GDDualOperatorExpression dualOp && IsAssignmentOperator(dualOp.OperatorType))
                return true;

            // Yield/await are statements
            if (expr is GDYieldExpression)
                return true;

            // Function calls may have side effects
            if (expr is GDCallExpression)
                return true;

            // Member access (could be a call in disguise or signal emission)
            if (expr is GDMemberOperatorExpression)
                return true;

            // Indexer access could be a call
            if (expr is GDIndexerExpression)
                return true;

            return false;
        }

        private bool IsPureOperator(GDDualOperatorType opType)
        {
            // These operators are pure and their result should be used
            switch (opType)
            {
                case GDDualOperatorType.Addition:
                case GDDualOperatorType.Subtraction:
                case GDDualOperatorType.Multiply:
                case GDDualOperatorType.Division:
                case GDDualOperatorType.Mod:
                case GDDualOperatorType.Power:
                case GDDualOperatorType.Equal:
                case GDDualOperatorType.NotEqual:
                case GDDualOperatorType.LessThan:
                case GDDualOperatorType.LessThanOrEqual:
                case GDDualOperatorType.MoreThan:
                case GDDualOperatorType.MoreThanOrEqual:
                case GDDualOperatorType.And:
                case GDDualOperatorType.And2:
                case GDDualOperatorType.Or:
                case GDDualOperatorType.Or2:
                case GDDualOperatorType.BitwiseAnd:
                case GDDualOperatorType.BitwiseOr:
                case GDDualOperatorType.Xor:
                case GDDualOperatorType.BitShiftLeft:
                case GDDualOperatorType.BitShiftRight:
                    return true;
                default:
                    return false;
            }
        }

        private GDSyntaxToken GetFirstToken(GDExpression expr)
        {
            return expr?.FirstChildToken;
        }

        private bool IsAssignmentOperator(GDDualOperatorType opType)
        {
            switch (opType)
            {
                case GDDualOperatorType.Assignment:
                case GDDualOperatorType.AddAndAssign:
                case GDDualOperatorType.SubtractAndAssign:
                case GDDualOperatorType.MultiplyAndAssign:
                case GDDualOperatorType.DivideAndAssign:
                case GDDualOperatorType.ModAndAssign:
                case GDDualOperatorType.PowerAndAssign:
                case GDDualOperatorType.BitwiseAndAndAssign:
                case GDDualOperatorType.BitwiseOrAndAssign:
                case GDDualOperatorType.XorAndAssign:
                case GDDualOperatorType.BitShiftLeftAndAssign:
                case GDDualOperatorType.BitShiftRightAndAssign:
                    return true;
                default:
                    return false;
            }
        }
    }
}
