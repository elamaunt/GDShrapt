using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using System.Collections.Generic;

namespace GDShrapt.Semantics.Validator
{
    /// <summary>
    /// Type checking for expressions and statements.
    /// Uses GDSemanticModel for type inference via public API.
    /// </summary>
    public class GDTypeValidator : GDValidationVisitor
    {
        private readonly GDSemanticModel? _semanticModel;

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

        public GDTypeValidator(GDValidationContext context, GDSemanticModel? semanticModel = null)
            : base(context)
        {
            _semanticModel = semanticModel;
        }

        public void Validate(GDNode node)
        {
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

            // Enter method scope and register parameters
            Context.EnterScope(GDScopeType.Method, methodDeclaration);
            RegisterMethodParameters(methodDeclaration);
        }

        public override void Left(GDMethodDeclaration methodDeclaration)
        {
            if (_functionStack.Count > 0)
                _functionStack.Pop();

            Context.ExitScope();
        }

        public override void Visit(GDMethodExpression methodExpression)
        {
            // Lambda expressions also create a new function context
            _functionStack.Push(FunctionContext.FromLambda(methodExpression));

            // Enter lambda scope and register parameters
            Context.EnterScope(GDScopeType.Lambda, methodExpression);
            RegisterLambdaParameters(methodExpression);
        }

        public override void Left(GDMethodExpression methodExpression)
        {
            if (_functionStack.Count > 0)
                _functionStack.Pop();

            Context.ExitScope();
        }

        public override void Visit(GDReturnExpression returnExpression)
        {
            ValidateReturnType(returnExpression);
        }

        public override void Visit(GDVariableDeclarationStatement variableDeclaration)
        {
            // Register local variable in scope for type tracking
            RegisterLocalVariable(variableDeclaration);

            ValidateVariableDeclaration(variableDeclaration);
        }

        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            ValidateClassVariableDeclaration(variableDeclaration);
        }

        private void RegisterMethodParameters(GDMethodDeclaration method)
        {
            if (method.Parameters == null)
                return;

            foreach (var param in method.Parameters)
            {
                var paramName = param.Identifier?.Sequence;
                if (string.IsNullOrEmpty(paramName))
                    continue;

                var typeName = param.Type?.BuildName();
                Context.Declare(GDSymbol.Parameter(paramName, param, typeName: typeName, typeNode: param.Type));
            }
        }

        private void RegisterLambdaParameters(GDMethodExpression lambda)
        {
            if (lambda.Parameters == null)
                return;

            foreach (var param in lambda.Parameters)
            {
                var paramName = param.Identifier?.Sequence;
                if (string.IsNullOrEmpty(paramName))
                    continue;

                var typeName = param.Type?.BuildName();
                Context.Declare(GDSymbol.Parameter(paramName, param, typeName: typeName, typeNode: param.Type));
            }
        }

        private void RegisterLocalVariable(GDVariableDeclarationStatement varDecl)
        {
            var varName = varDecl.Identifier?.Sequence;
            if (string.IsNullOrEmpty(varName))
                return;

            // Use explicit type if present (var x: int = ...)
            var typeName = varDecl.Type?.BuildName();
            GDTypeNode typeNode = varDecl.Type;

            // Only infer type if := is used (Colon present but no Type node)
            // var x := 42  → Colon != null, Type == null → infer int
            // var x = 42   → Colon == null → Variant (dynamic)
            // var x: int = 42 → Type != null → use explicit type (already set above)
            if (string.IsNullOrEmpty(typeName) && varDecl.Initializer != null && varDecl.Colon != null)
            {
                typeName = InferSimpleType(varDecl.Initializer);
            }

            // Fallback to Variant for dynamic typing
            if (string.IsNullOrEmpty(typeName) || typeName == "Unknown")
            {
                typeName = "Variant";
            }

            Context.Declare(GDSymbol.Variable(varName, varDecl, typeName: typeName, typeNode: typeNode));
        }

        private void ValidateVariableDeclaration(GDVariableDeclarationStatement varDecl)
        {
            // Skip if no type annotation
            var declaredType = varDecl.Type?.BuildName();
            if (string.IsNullOrEmpty(declaredType))
                return;

            // Skip if no initializer
            var initializer = varDecl.Initializer;
            if (initializer == null)
                return;

            // Infer the type of the initializer
            var initType = InferSimpleType(initializer);

            // Skip validation if type couldn't be inferred
            if (initType == null || initType == "Unknown")
                return;

            // Check type compatibility
            if (!AreTypesCompatibleForAssignment(initType, declaredType))
            {
                ReportWarning(
                    GDDiagnosticCode.TypeAnnotationMismatch,
                    $"Type mismatch: cannot assign '{initType}' to variable of type '{declaredType}'",
                    varDecl);
            }
        }

        private void ValidateClassVariableDeclaration(GDVariableDeclaration varDecl)
        {
            // Skip if no type annotation
            var declaredType = varDecl.Type?.BuildName();
            if (string.IsNullOrEmpty(declaredType))
                return;

            // Skip if no initializer
            var initializer = varDecl.Initializer;
            if (initializer == null)
                return;

            // Infer the type of the initializer
            var initType = InferSimpleType(initializer);

            // Skip validation if type couldn't be inferred
            if (initType == null || initType == "Unknown")
                return;

            // Check type compatibility
            if (!AreTypesCompatibleForAssignment(initType, declaredType))
            {
                ReportWarning(
                    GDDiagnosticCode.TypeAnnotationMismatch,
                    $"Type mismatch: cannot assign '{initType}' to variable of type '{declaredType}'",
                    varDecl);
            }
        }

        private bool AreTypesCompatibleForAssignment(string sourceType, string targetType)
        {
            if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
                return true;

            // Same type is always compatible
            if (sourceType == targetType)
                return true;

            // null is compatible with any reference type
            if (sourceType == "null")
                return true;

            // Variant accepts anything
            if (targetType == "Variant")
                return true;

            // Extract base types for generics (Array[int] -> Array)
            var sourceBase = ExtractBaseTypeName(sourceType);
            var targetBase = ExtractBaseTypeName(targetType);

            // Generic type is assignable to its non-generic base (Array[int] -> Array)
            if (sourceBase == targetBase && sourceBase != sourceType)
                return true;

            // Use semantic model for detailed compatibility check
            if (_semanticModel != null)
                return _semanticModel.AreTypesCompatible(sourceType, targetType);

            return false;
        }

        /// <summary>
        /// Extracts the base type name from a generic type.
        /// For example: "Array[int]" -> "Array", "Dictionary[String, int]" -> "Dictionary"
        /// </summary>
        private static string ExtractBaseTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeName;

            var bracketIndex = typeName.IndexOf('[');
            if (bracketIndex > 0)
                return typeName.Substring(0, bracketIndex);

            return typeName;
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

            // Use semantic model for detailed compatibility check
            if (_semanticModel != null)
                return _semanticModel.AreTypesCompatible(actualType, declaredType);

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

            // Check if target is an untyped variable (Variant) - allow any assignment
            if (IsUntypedVariable(target))
                return;

            // Use semantic model for compatibility check
            if (_semanticModel != null && !_semanticModel.AreTypesCompatible(valueType, targetType))
            {
                ReportWarning(
                    GDDiagnosticCode.TypeMismatch,
                    $"Type mismatch in assignment: cannot assign '{valueType}' to '{targetType}'",
                    reportOn);
            }
        }

        /// <summary>
        /// Checks if the expression refers to an untyped variable (declared without := or type annotation).
        /// Untyped variables in GDScript are Variant and can be reassigned to any type.
        /// </summary>
        private bool IsUntypedVariable(GDExpression expr)
        {
            if (expr is not GDIdentifierExpression identExpr)
                return false;

            var name = identExpr.Identifier?.Sequence;
            if (string.IsNullOrEmpty(name))
                return false;

            // Look up the symbol
            var symbol = Context.Scopes.Lookup(name);
            if (symbol == null)
                return false;

            // Check if it's a local variable declaration without type annotation
            if (symbol.Declaration is GDVariableDeclarationStatement varDeclStmt)
            {
                // var x := 42  → Colon != null, Type == null → typed via inference
                // var x: int = 42 → Type != null → explicitly typed
                // var x = 42   → Colon == null → Variant (untyped)
                return varDeclStmt.Type == null && varDeclStmt.Colon == null;
            }

            // Check if it's a class-level variable declaration
            if (symbol.Declaration is GDVariableDeclaration varDecl)
            {
                // var x := 42  → TypeColon != null, Type == null → typed via inference
                // var x: int = 42 → Type != null → explicitly typed
                // var x = 42   → TypeColon == null → Variant (untyped)
                return varDecl.Type == null && varDecl.TypeColon == null;
            }

            return false;
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
        /// Infers type using the semantic model and local scope.
        /// Returns "Unknown" if type cannot be determined.
        /// </summary>
        private string InferSimpleType(GDExpression expr)
        {
            // First check local scope for variables registered during validation
            if (expr is GDIdentifierExpression identExpr)
            {
                var name = identExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(name))
                {
                    var symbol = Context.Scopes.Lookup(name);
                    if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName))
                    {
                        return symbol.TypeName;
                    }
                }
            }

            // Fall back to semantic model for complex expressions
            var type = _semanticModel?.GetExpressionType(expr);
            return type ?? "Unknown";
        }

        private bool AreTypesCompatibleForArithmetic(string left, string right)
        {
            if (left == "Unknown" || right == "Unknown")
                return true;

            // Variant is dynamically typed and can hold any value, so arithmetic is allowed
            if (left == "Variant" || right == "Variant")
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
