using GDShrapt.Abstractions;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Type checking for expressions and statements.
    /// Uses GDTypeInferenceEngine for type inference via RuntimeProvider.
    /// </summary>
    public class GDTypeValidator : GDValidationVisitor
    {
        private GDTypeInferenceEngine _typeInference;

        /// <summary>
        /// Represents a function context (either a method declaration or a lambda expression).
        /// </summary>
        private struct FunctionContext
        {
            public GDTypeNode ReturnType;
            public bool IsLambda;

            public static FunctionContext FromMethod(GDMethodDeclaration method)
            {
                return new FunctionContext { ReturnType = method.ReturnType, IsLambda = false };
            }

            public static FunctionContext FromLambda(GDMethodExpression lambda)
            {
                return new FunctionContext { ReturnType = lambda.ReturnType, IsLambda = true };
            }
        }

        // Stack to track containing functions (methods and lambdas) for return type checking
        private readonly Stack<FunctionContext> _functionStack = new Stack<FunctionContext>();

        public GDTypeValidator(GDValidationContext context) : base(context)
        {
        }

        public void Validate(GDNode node)
        {
            // Create type inference engine with runtime provider and scope stack
            _typeInference = new GDTypeInferenceEngine(Context.RuntimeProvider, Context.Scopes);
            _functionStack.Clear();
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

        #region Method/Return Type Tracking

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            _functionStack.Push(FunctionContext.FromMethod(methodDeclaration));
        }

        public override void Left(GDMethodDeclaration methodDeclaration)
        {
            if (_functionStack.Count > 0)
                _functionStack.Pop();
        }

        public override void Visit(GDMethodExpression methodExpression)
        {
            // Lambda expressions also create a new function context
            _functionStack.Push(FunctionContext.FromLambda(methodExpression));
        }

        public override void Left(GDMethodExpression methodExpression)
        {
            if (_functionStack.Count > 0)
                _functionStack.Pop();
        }

        public override void Visit(GDReturnExpression returnExpression)
        {
            ValidateReturnType(returnExpression);
        }

        private void ValidateReturnType(GDReturnExpression returnExpr)
        {
            // Only validate if we're inside a function
            if (_functionStack.Count == 0)
                return;

            var context = _functionStack.Peek();
            var declaredReturnType = context.ReturnType?.BuildName();

            // If no return type is declared, any return is valid
            if (string.IsNullOrEmpty(declaredReturnType))
                return;

            // If declared as void, no value should be returned
            if (declaredReturnType == "void")
            {
                if (returnExpr.Expression != null)
                {
                    var returnedType = InferSimpleType(returnExpr.Expression);
                    if (returnedType != "void" && returnedType != "Unknown")
                    {
                        ReportWarning(
                            GDDiagnosticCode.IncompatibleReturnType,
                            $"Function with return type 'void' should not return a value (got '{returnedType}')",
                            returnExpr);
                    }
                }
                return;
            }

            // If method has a non-void return type, check the return expression
            if (returnExpr.Expression == null)
            {
                // Return without value in typed function
                ReportWarning(
                    GDDiagnosticCode.IncompatibleReturnType,
                    $"Function expects return type '{declaredReturnType}' but returns nothing",
                    returnExpr);
                return;
            }

            // Infer the type of the returned expression
            var actualType = InferSimpleType(returnExpr.Expression);

            // Skip validation if type couldn't be inferred
            if (actualType == null || actualType == "Unknown")
                return;

            // Check type compatibility
            if (!AreReturnTypesCompatible(actualType, declaredReturnType))
            {
                ReportWarning(
                    GDDiagnosticCode.IncompatibleReturnType,
                    $"Cannot return '{actualType}' from function with return type '{declaredReturnType}'",
                    returnExpr);
            }
        }

        /// <summary>
        /// Checks if the actual return type is compatible with the declared return type.
        /// </summary>
        private bool AreReturnTypesCompatible(string actualType, string declaredType)
        {
            if (string.IsNullOrEmpty(actualType) || string.IsNullOrEmpty(declaredType))
                return true;

            // Same type is always compatible
            if (actualType == declaredType)
                return true;

            // null is compatible with any reference type
            if (actualType == "null")
                return true;

            // Use type inference engine for detailed compatibility check
            if (_typeInference != null)
                return _typeInference.AreTypesCompatible(actualType, declaredType);

            return false;
        }

        #endregion

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

                // Assignment operators - check type compatibility
                case GDDualOperatorType.Assignment:
                    ValidateAssignment(left, right, leftType, rightType, expr);
                    break;
            }
        }

        private void ValidateAssignment(GDExpression target, GDExpression value, string targetType, string valueType, GDNode reportOn)
        {
            if (targetType == "Unknown" || valueType == "Unknown")
                return;

            // Skip if types match
            if (targetType == valueType)
                return;

            // Use type inference engine for compatibility check
            if (_typeInference != null && !_typeInference.AreTypesCompatible(valueType, targetType))
            {
                ReportWarning(
                    GDDiagnosticCode.TypeMismatch,
                    $"Type mismatch in assignment: cannot assign '{valueType}' to '{targetType}'",
                    reportOn);
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
        /// Infers type using the type inference engine.
        /// Returns "Unknown" if type cannot be determined.
        /// </summary>
        private string InferSimpleType(GDExpression expr)
        {
            var type = _typeInference?.InferType(expr);
            return type ?? "Unknown";
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
