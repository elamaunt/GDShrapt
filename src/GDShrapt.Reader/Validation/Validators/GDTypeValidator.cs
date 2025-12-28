namespace GDShrapt.Reader
{
    /// <summary>
    /// Basic type checking for operators. Reports warnings for type mismatches.
    /// Only infers types for literals (numbers, strings, bools, arrays, dicts).
    /// </summary>
    public class GDTypeValidator : GDValidationVisitor
    {
        public GDTypeValidator(GDValidationContext context) : base(context)
        {
        }

        public void Validate(GDNode node)
        {
            node?.WalkIn(this);
        }

        public override void Visit(GDDualOperatorExpression dualOperator)
        {
            if (dualOperator.Operator != null)
            {
                ValidateDualOperator(dualOperator);
            }
        }

        public override void Visit(GDSingleOperatorExpression singleOperator)
        {
            if (singleOperator.Operator != null)
            {
                ValidateSingleOperator(singleOperator);
            }
        }

        public override void Visit(GDAwaitExpression awaitExpression)
        {
            var expr = awaitExpression.Expression;
            if (expr == null)
                return;

            // Await on literals is definitely wrong
            if (expr is GDNumberExpression ||
                expr is GDStringExpression ||
                expr is GDBoolExpression ||
                expr is GDArrayInitializerExpression ||
                expr is GDDictionaryInitializerExpression)
            {
                ReportWarning(
                    GDDiagnosticCode.AwaitOnNonAwaitable,
                    "'await' should be used with signals or coroutines, not literals",
                    awaitExpression);
            }
        }

        private void ValidateDualOperator(GDDualOperatorExpression expr)
        {
            var left = expr.LeftExpression;
            var right = expr.RightExpression;
            var op = expr.Operator?.OperatorType;

            if (left == null || right == null || op == null)
                return;

            var leftType = InferSimpleType(left);
            var rightType = InferSimpleType(right);

            if (leftType == null || rightType == null)
                return;

            switch (op)
            {
                case GDDualOperatorType.Addition:
                case GDDualOperatorType.Subtraction:
                case GDDualOperatorType.Multiply:
                case GDDualOperatorType.Division:
                case GDDualOperatorType.Mod:
                    if (!AreTypesCompatibleForArithmetic(leftType, rightType))
                    {
                        // String + anything is allowed in GDScript
                        if (op == GDDualOperatorType.Addition && (leftType == "String" || rightType == "String"))
                            break;

                        ReportWarning(
                            GDDiagnosticCode.InvalidOperandType,
                            $"Potential type mismatch in arithmetic operation: {leftType} {GetOperatorSymbol(op.Value)} {rightType}",
                            expr);
                    }
                    break;

                case GDDualOperatorType.BitwiseAnd:
                case GDDualOperatorType.BitwiseOr:
                case GDDualOperatorType.Xor:
                case GDDualOperatorType.BitShiftLeft:
                case GDDualOperatorType.BitShiftRight:
                    // Bitwise ops require int
                    if (leftType != "int" || rightType != "int")
                    {
                        if (leftType != "Unknown" && rightType != "Unknown")
                        {
                            ReportWarning(
                                GDDiagnosticCode.InvalidOperandType,
                                $"Bitwise operations are only valid for integers: {leftType} {GetOperatorSymbol(op.Value)} {rightType}",
                                expr);
                        }
                    }
                    break;
            }
        }

        private void ValidateSingleOperator(GDSingleOperatorExpression expr)
        {
            var operand = expr.TargetExpression;
            var op = expr.Operator?.OperatorType;

            if (operand == null || op == null)
                return;

            var operandType = InferSimpleType(operand);
            if (operandType == null)
                return;

            switch (op)
            {
                case GDSingleOperatorType.Negate:
                    if (!IsNumericType(operandType) && operandType != "Unknown")
                    {
                        ReportWarning(
                            GDDiagnosticCode.InvalidOperandType,
                            $"Negation operator requires numeric type, got {operandType}",
                            expr);
                    }
                    break;

                case GDSingleOperatorType.BitwiseNegate:
                    if (operandType != "int" && operandType != "Unknown")
                    {
                        ReportWarning(
                            GDDiagnosticCode.InvalidOperandType,
                            $"Bitwise negation requires integer type, got {operandType}",
                            expr);
                    }
                    break;
            }
        }

        /// <summary>
        /// Infers type from literal expressions only.
        /// </summary>
        private string InferSimpleType(GDExpression expr)
        {
            switch (expr)
            {
                case GDNumberExpression numExpr:
                    var num = numExpr.Number;
                    if (num != null)
                    {
                        var seq = num.Sequence;
                        if (seq != null && (seq.Contains(".") || seq.Contains("e") || seq.Contains("E")))
                            return "float";
                        return "int";
                    }
                    return "Unknown";

                case GDStringExpression _:
                    return "String";

                case GDBoolExpression _:
                    return "bool";

                case GDArrayInitializerExpression _:
                    return "Array";

                case GDDictionaryInitializerExpression _:
                    return "Dictionary";

                default:
                    return "Unknown";
            }
        }

        private bool AreTypesCompatibleForArithmetic(string left, string right)
        {
            if (left == "Unknown" || right == "Unknown")
                return true;

            if (IsNumericType(left) && IsNumericType(right))
                return true;

            if (left == right)
                return true;

            return false;
        }

        private bool IsNumericType(string type)
        {
            return type == "int" || type == "float";
        }

        private string GetOperatorSymbol(GDDualOperatorType op)
        {
            switch (op)
            {
                case GDDualOperatorType.Addition: return "+";
                case GDDualOperatorType.Subtraction: return "-";
                case GDDualOperatorType.Multiply: return "*";
                case GDDualOperatorType.Division: return "/";
                case GDDualOperatorType.Mod: return "%";
                case GDDualOperatorType.And: return "and";
                case GDDualOperatorType.Or: return "or";
                case GDDualOperatorType.BitwiseAnd: return "&";
                case GDDualOperatorType.BitwiseOr: return "|";
                case GDDualOperatorType.Xor: return "^";
                case GDDualOperatorType.BitShiftLeft: return "<<";
                case GDDualOperatorType.BitShiftRight: return ">>";
                default: return op.ToString();
            }
        }
    }
}
