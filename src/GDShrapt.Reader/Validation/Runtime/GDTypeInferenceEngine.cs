namespace GDShrapt.Reader
{
    /// <summary>
    /// Infers types of expressions using the RuntimeProvider and scope information.
    /// Supports generic types like Array[T] and Dictionary[K,V].
    /// </summary>
    public class GDTypeInferenceEngine
    {
        private readonly IGDRuntimeProvider _runtimeProvider;
        private readonly GDScopeStack _scopes;

        /// <summary>
        /// Creates a new type inference engine.
        /// </summary>
        public GDTypeInferenceEngine(IGDRuntimeProvider runtimeProvider, GDScopeStack scopes = null)
        {
            _runtimeProvider = runtimeProvider ?? GDDefaultRuntimeProvider.Instance;
            _scopes = scopes;
        }

        /// <summary>
        /// Infers the type of an expression as a string.
        /// Returns null if the type cannot be determined.
        /// </summary>
        public string InferType(GDExpression expression)
        {
            return InferTypeNode(expression)?.BuildName();
        }

        /// <summary>
        /// Infers the full type node of an expression, including generic type arguments.
        /// Returns null if the type cannot be determined.
        /// </summary>
        public GDTypeNode InferTypeNode(GDExpression expression)
        {
            if (expression == null)
                return null;

            switch (expression)
            {
                // Literals - known types
                case GDNumberExpression numExpr:
                    return CreateSimpleType(InferNumberType(numExpr));

                case GDStringExpression _:
                    return CreateSimpleType("String");

                case GDBoolExpression _:
                    return CreateSimpleType("bool");

                case GDArrayInitializerExpression _:
                    return CreateSimpleType("Array");

                case GDDictionaryInitializerExpression _:
                    return CreateSimpleType("Dictionary");

                // Identifiers - look up in scope or RuntimeProvider
                case GDIdentifierExpression identExpr:
                    return InferIdentifierTypeNode(identExpr);

                // Call expressions - return type from function/method
                case GDCallExpression callExpr:
                    return CreateSimpleType(InferCallType(callExpr));

                // Member access - property type from RuntimeProvider
                case GDMemberOperatorExpression memberExpr:
                    return CreateSimpleType(InferMemberType(memberExpr));

                // Index access - element type from generic container
                case GDIndexerExpression indexerExpr:
                    return InferIndexerTypeNode(indexerExpr);

                // Operators
                case GDDualOperatorExpression dualOp:
                    return CreateSimpleType(InferDualOperatorType(dualOp));

                case GDSingleOperatorExpression singleOp:
                    return CreateSimpleType(InferSingleOperatorType(singleOp));

                // Ternary (if expression)
                case GDIfExpression ifExpr:
                    return InferTypeNode(ifExpr.TrueExpression);

                // Bracket (parenthesized)
                case GDBracketExpression bracketExpr:
                    return InferTypeNode(bracketExpr.InnerExpression);

                // Lambda - Callable
                case GDMethodExpression _:
                    return CreateSimpleType("Callable");

                // Await - same as inner expression
                case GDAwaitExpression awaitExpr:
                    return InferTypeNode(awaitExpr.Expression);

                // Yield - Signal (in Godot 4)
                case GDYieldExpression _:
                    return CreateSimpleType("Signal");

                // Get node ($) - Node
                case GDGetNodeExpression _:
                    return CreateSimpleType("Node");

                // Get unique node (%) - Node
                case GDGetUniqueNodeExpression _:
                    return CreateSimpleType("Node");

                // Match case variable - bound type from pattern
                case GDMatchCaseVariableExpression _:
                    return null;

                // Pass expression
                case GDPassExpression _:
                    return CreateSimpleType("void");

                default:
                    return null;
            }
        }

        /// <summary>
        /// Infers the element type when indexing into a container.
        /// </summary>
        private GDTypeNode InferIndexerTypeNode(GDIndexerExpression indexerExpr)
        {
            var containerTypeNode = InferTypeNode(indexerExpr.CallerExpression);
            if (containerTypeNode == null)
                return null;

            // For typed arrays: Array[T] -> T
            if (containerTypeNode is GDArrayTypeNode arrayType)
            {
                return arrayType.InnerType;
            }

            // For typed dictionaries: Dictionary[K, V] -> V
            if (containerTypeNode is GDDictionaryTypeNode dictType)
            {
                return dictType.ValueType;
            }

            // Untyped containers return null (unknown element type)
            return null;
        }

        private GDTypeNode InferIdentifierTypeNode(GDIdentifierExpression identExpr)
        {
            var name = identExpr.Identifier?.Sequence;
            if (string.IsNullOrEmpty(name))
                return null;

            // Check built-in constants
            switch (name)
            {
                case "true":
                case "false":
                    return CreateSimpleType("bool");
                case "null":
                    return CreateSimpleType("null");
                case "PI":
                case "TAU":
                case "INF":
                case "NAN":
                    return CreateSimpleType("float");
                case "self":
                    return CreateSimpleType("self");
                case "super":
                    return CreateSimpleType("super");
            }

            // Check scope for declared symbols with full TypeNode
            if (_scopes != null)
            {
                var symbol = _scopes.Lookup(name);
                if (symbol != null)
                {
                    // Prefer TypeNode if available (has generic type info)
                    if (symbol.TypeNode != null)
                        return symbol.TypeNode;
                    // Fall back to TypeName
                    if (!string.IsNullOrEmpty(symbol.TypeName))
                        return CreateSimpleType(symbol.TypeName);
                }
            }

            // Check if it's a known type (type as value, like Vector2)
            if (_runtimeProvider.IsKnownType(name))
                return CreateSimpleType(name);

            // Check global class
            var globalClass = _runtimeProvider.GetGlobalClass(name);
            if (globalClass != null)
                return CreateSimpleType(name);

            return null;
        }

        private string InferNumberType(GDNumberExpression numExpr)
        {
            var num = numExpr.Number;
            if (num == null)
                return "int";

            var seq = num.Sequence;
            if (seq != null && (seq.Contains(".") || seq.Contains("e") || seq.Contains("E")))
                return "float";

            return "int";
        }

        private string InferCallType(GDCallExpression callExpr)
        {
            var caller = callExpr.CallerExpression;

            // Direct function call
            if (caller is GDIdentifierExpression identExpr)
            {
                var funcName = identExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(funcName))
                {
                    var funcInfo = _runtimeProvider.GetGlobalFunction(funcName);
                    if (funcInfo != null)
                        return funcInfo.ReturnType;

                    // Could be type constructor (Vector2(), Color(), etc.)
                    if (_runtimeProvider.IsKnownType(funcName))
                        return funcName;
                }
            }
            // Method call on object
            else if (caller is GDMemberOperatorExpression memberExpr)
            {
                var methodName = memberExpr.Identifier?.Sequence;
                var callerType = InferType(memberExpr.CallerExpression);

                if (!string.IsNullOrEmpty(callerType) && !string.IsNullOrEmpty(methodName))
                {
                    var memberInfo = _runtimeProvider.GetMember(callerType, methodName);
                    if (memberInfo != null && memberInfo.Kind == GDRuntimeMemberKind.Method)
                        return memberInfo.Type;
                }
            }

            return null;
        }

        private string InferMemberType(GDMemberOperatorExpression memberExpr)
        {
            var memberName = memberExpr.Identifier?.Sequence;
            var callerType = InferType(memberExpr.CallerExpression);

            if (!string.IsNullOrEmpty(callerType) && !string.IsNullOrEmpty(memberName))
            {
                var memberInfo = _runtimeProvider.GetMember(callerType, memberName);
                if (memberInfo != null)
                    return memberInfo.Type;
            }

            return null;
        }

        private string InferDualOperatorType(GDDualOperatorExpression dualOp)
        {
            var opType = dualOp.Operator?.OperatorType;
            if (opType == null)
                return null;

            var leftType = InferType(dualOp.LeftExpression);
            var rightType = InferType(dualOp.RightExpression);

            switch (opType)
            {
                // Comparison operators - always bool
                case GDDualOperatorType.Equal:
                case GDDualOperatorType.NotEqual:
                case GDDualOperatorType.LessThan:
                case GDDualOperatorType.MoreThan:
                case GDDualOperatorType.LessThanOrEqual:
                case GDDualOperatorType.MoreThanOrEqual:
                case GDDualOperatorType.Is:
                case GDDualOperatorType.In:
                    return "bool";

                // Logical operators - always bool
                case GDDualOperatorType.And:
                case GDDualOperatorType.Or:
                    return "bool";

                // Arithmetic operators - promote to float if either is float
                case GDDualOperatorType.Addition:
                case GDDualOperatorType.Subtraction:
                case GDDualOperatorType.Multiply:
                    if (leftType == "String" || rightType == "String")
                        return "String"; // String concatenation
                    if (leftType == "float" || rightType == "float")
                        return "float";
                    if (leftType == "int" && rightType == "int")
                        return "int";
                    // Could be vector math - return left type
                    return leftType;

                case GDDualOperatorType.Division:
                case GDDualOperatorType.Power:
                    return "float"; // Division always returns float in GDScript

                case GDDualOperatorType.Mod:
                    if (leftType == "int" && rightType == "int")
                        return "int";
                    return "float";

                // Bitwise operators - always int
                case GDDualOperatorType.BitwiseAnd:
                case GDDualOperatorType.BitwiseOr:
                case GDDualOperatorType.Xor:
                case GDDualOperatorType.BitShiftLeft:
                case GDDualOperatorType.BitShiftRight:
                    return "int";

                // Assignment operators - return left type
                case GDDualOperatorType.Assignment:
                case GDDualOperatorType.AddAndAssign:
                case GDDualOperatorType.SubtractAndAssign:
                case GDDualOperatorType.MultiplyAndAssign:
                case GDDualOperatorType.DivideAndAssign:
                case GDDualOperatorType.ModAndAssign:
                case GDDualOperatorType.BitwiseAndAndAssign:
                case GDDualOperatorType.BitwiseOrAndAssign:
                case GDDualOperatorType.PowerAndAssign:
                case GDDualOperatorType.BitShiftLeftAndAssign:
                case GDDualOperatorType.BitShiftRightAndAssign:
                case GDDualOperatorType.XorAndAssign:
                    return leftType;

                default:
                    return null;
            }
        }

        private string InferSingleOperatorType(GDSingleOperatorExpression singleOp)
        {
            var opType = singleOp.Operator?.OperatorType;
            if (opType == null)
                return null;

            switch (opType)
            {
                case GDSingleOperatorType.Not:
                case GDSingleOperatorType.Not2:
                    return "bool";

                case GDSingleOperatorType.Negate:
                    return InferType(singleOp.TargetExpression);

                case GDSingleOperatorType.BitwiseNegate:
                    return "int";

                default:
                    return null;
            }
        }

        /// <summary>
        /// Checks if two types are compatible for assignment.
        /// </summary>
        public bool AreTypesCompatible(string sourceType, string targetType)
        {
            if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
                return true; // Unknown types are assumed compatible

            if (sourceType == targetType)
                return true;

            // null is assignable to any reference type
            if (sourceType == "null")
                return true;

            // Use RuntimeProvider for detailed compatibility check
            return _runtimeProvider.IsAssignableTo(sourceType, targetType);
        }

        /// <summary>
        /// Creates a simple type node for a type name without generic arguments.
        /// </summary>
        private GDSingleTypeNode CreateSimpleType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            return new GDSingleTypeNode { Type = new GDType { Sequence = typeName } };
        }
    }
}
