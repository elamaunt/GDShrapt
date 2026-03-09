using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using System.Collections.Generic;
using System.Linq;

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

        public override void Visit(GDForStatement forStatement)
        {
            RegisterForLoopVariable(forStatement);
            ValidateForLoopVariable(forStatement);
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

            // Try to get expected parameter types from call context (filter, map, reduce, etc.)
            var expectedTypes = TryGetExpectedLambdaParameterTypes(lambda);

            int paramIndex = 0;
            foreach (var param in lambda.Parameters)
            {
                var paramName = param.Identifier?.Sequence;
                if (string.IsNullOrEmpty(paramName))
                {
                    paramIndex++;
                    continue;
                }

                // 1. Explicit type annotation takes priority
                var typeName = param.Type?.BuildName();

                // 2. Infer from call context if no explicit type
                if (string.IsNullOrEmpty(typeName) && expectedTypes != null && paramIndex < expectedTypes.Count)
                {
                    typeName = expectedTypes[paramIndex];
                }

                Context.Declare(GDSymbol.Parameter(paramName, param, typeName: typeName, typeNode: param.Type));
                paramIndex++;
            }
        }

        /// <summary>
        /// Tries to infer lambda parameter types from the call context.
        /// For example, in arr.filter(func(x): return x > 2), x should be inferred from arr's element type.
        /// </summary>
        private IReadOnlyList<string>? TryGetExpectedLambdaParameterTypes(GDMethodExpression lambda)
        {
            // Find the parent call expression (arr.filter(...))
            var callExpr = FindParentCallExpression(lambda);
            if (callExpr == null)
                return null;

            // Get the caller type and method name
            var (callerType, methodName) = GetCallerTypeAndMethod(callExpr);
            if (callerType == null || methodName == null)
                return null;

            // Get method info with callable metadata
            var callerSemantic = GDSemanticType.FromRuntimeTypeName(callerType);
            var baseCallerType = callerSemantic is GDContainerSemanticType ct
                ? (ct.IsDictionary ? "Dictionary" : "Array")
                : callerType;
            var methodInfo = _semanticModel?.RuntimeProvider?.GetMember(baseCallerType, methodName);
            if (methodInfo?.Parameters == null)
                return null;

            // Find which argument position the lambda is at
            var lambdaArgIndex = GetLambdaArgumentIndex(callExpr, lambda);
            if (lambdaArgIndex < 0 || lambdaArgIndex >= methodInfo.Parameters.Count)
                return null;

            // Get callable metadata from the parameter
            var paramInfo = methodInfo.Parameters[lambdaArgIndex];
            if (paramInfo.CallableReceivesType == null)
                return null;

            // Get the container's element type
            var containerElementType = GetContainerElementType(callExpr, callerType);
            if (containerElementType != null)
                return BuildLambdaParameterTypes(paramInfo.CallableReceivesType, containerElementType, paramInfo.CallableParameterCount ?? 1);

            // Element type could not be determined. Check if the array is a local variable
            // with no elements (empty array literal or no appends) — callback never runs,
            // so suppress GD3020 by returning "Variant" as safe placeholder.
            if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp2)
            {
                var callerExpr2 = memberOp2.CallerExpression;

                // Local variable: arr.filter(...)
                if (callerExpr2 is GDIdentifierExpression callerIdent)
                {
                    var varName = callerIdent.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(varName))
                    {
                        var symbol = Context.Scopes.Lookup(varName);
                        if (symbol?.Declaration is GDVariableDeclarationStatement)
                        {
                            var profile = _semanticModel?.TypeSystem.GetContainerProfile(varName);
                            if (profile == null || profile.ValueUsages.Count == 0)
                            {
                                return BuildLambdaParameterTypes(paramInfo.CallableReceivesType, "Variant", paramInfo.CallableParameterCount ?? 1);
                            }
                        }
                    }
                }

                // Class-level member: h.data.filter(...) or Holder.data.filter(...)
                if (callerExpr2 is GDMemberOperatorExpression callerMemberOp2)
                {
                    var memberName = callerMemberOp2.Identifier?.Sequence;
                    var ownerExpr = callerMemberOp2.CallerExpression;
                    if (!string.IsNullOrEmpty(memberName) && ownerExpr != null)
                    {
                        var ownerTypeInfo = _semanticModel?.TypeSystem.GetType(ownerExpr);
                        var ownerType = ownerTypeInfo?.IsVariant == true ? null : ownerTypeInfo?.DisplayName;
                        if (!string.IsNullOrEmpty(ownerType))
                        {
                            var profile = _semanticModel?.TypeSystem.GetClassContainerProfile(ownerType, memberName);
                            // Only suppress if profile exists and is confirmed empty (no appends tracked)
                            if (profile != null && profile.ValueUsages.Count == 0)
                            {
                                return BuildLambdaParameterTypes(paramInfo.CallableReceivesType, "Variant", paramInfo.CallableParameterCount ?? 1);
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the parent GDCallExpression that contains the lambda.
        /// </summary>
        private static GDCallExpression? FindParentCallExpression(GDMethodExpression lambda)
        {
            GDNode? current = lambda.Parent;
            while (current != null)
            {
                if (current is GDCallExpression call)
                    return call;
                current = current.Parent;
            }
            return null;
        }

        /// <summary>
        /// Gets the caller type and method name from a call expression.
        /// For arr.filter(...) returns (Array[int], "filter").
        /// </summary>
        private (string? CallerType, string? MethodName) GetCallerTypeAndMethod(GDCallExpression call)
        {
            if (call.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                var methodName = memberOp.Identifier?.Sequence;
                if (methodName == null)
                    return (null, null);

                // Get the type of the caller (e.g., arr in arr.filter())
                var callerExpr = memberOp.CallerExpression;
                if (callerExpr == null)
                    return (null, methodName);

                var callerTypeInfo = _semanticModel?.TypeSystem.GetType(callerExpr);
                var callerType = callerTypeInfo?.IsVariant == true ? null : callerTypeInfo?.DisplayName;
                return (callerType, methodName);
            }

            return (null, null);
        }

        /// <summary>
        /// Gets the index of the lambda expression in the call arguments.
        /// </summary>
        private static int GetLambdaArgumentIndex(GDCallExpression call, GDMethodExpression lambda)
        {
            if (call.Parameters == null)
                return -1;

            int index = 0;
            foreach (var arg in call.Parameters)
            {
                if (arg == lambda || ContainsNode(arg, lambda))
                    return index;
                index++;
            }
            return -1;
        }

        /// <summary>
        /// Checks if a node contains another node (for nested expressions).
        /// </summary>
        private static bool ContainsNode(GDNode parent, GDNode target)
        {
            if (parent == target)
                return true;

            var foundNodes = new List<GDNode>();
            parent.WalkIn((GDVisitor)new NodeFinder(target, foundNodes));
            return foundNodes.Count > 0;
        }

        private class NodeFinder : GDVisitor
        {
            private readonly GDNode _target;
            private readonly List<GDNode> _found;

            public NodeFinder(GDNode target, List<GDNode> found)
            {
                _target = target;
                _found = found;
            }

            public override void WillVisit(GDNode node)
            {
                if (node == _target)
                    _found.Add(node);
            }
        }

        /// <summary>
        /// Gets the element type of a container (Array[T] -> T, or inferred from usage).
        /// </summary>
        private string? GetContainerElementType(GDCallExpression call, string callerType)
        {
            // Use structural type first
            if (call.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                var callerExpr = memberOp.CallerExpression;
                if (callerExpr != null)
                {
                    var semanticType = _semanticModel?.TypeSystem.GetType(callerExpr);
                    if (semanticType is GDContainerSemanticType container)
                    {
                        if (container.IsArray && container.ElementType != null && !container.ElementType.IsVariant)
                            return container.ElementType.DisplayName;
                        if (container.IsDictionary && container.ElementType != null && !container.ElementType.IsVariant)
                            return container.ElementType.DisplayName;
                    }

                    // Fallback: use container type inference (tracks append/[]= usage)
                    if (callerExpr is GDIdentifierExpression identExpr)
                    {
                        var varName = identExpr.Identifier?.Sequence;
                        if (!string.IsNullOrEmpty(varName))
                        {
                            var containerType = _semanticModel?.TypeSystem.GetContainerElementType(varName);
                            if (containerType != null && containerType.HasElementTypes)
                            {
                                var effectiveElement = containerType.EffectiveElementType;
                                if (!effectiveElement.IsVariant)
                                    return effectiveElement.DisplayName;
                            }
                        }
                    }

                    // Fallback: class-level container profiles (cross-file member access)
                    if (callerExpr is GDMemberOperatorExpression callerMemberOp)
                    {
                        var memberName = callerMemberOp.Identifier?.Sequence;
                        if (!string.IsNullOrEmpty(memberName))
                        {
                            var ownerExpr = callerMemberOp.CallerExpression;
                            if (ownerExpr != null)
                            {
                                var ownerTypeInfo = _semanticModel?.TypeSystem.GetType(ownerExpr);
                                var ownerType = ownerTypeInfo?.IsVariant == true ? null : ownerTypeInfo?.DisplayName;
                                if (!string.IsNullOrEmpty(ownerType))
                                {
                                    var containerType = _semanticModel?.TypeSystem.GetClassContainerElementType(ownerType, memberName);
                                    if (containerType != null && containerType.HasElementTypes)
                                    {
                                        var effectiveElement = containerType.EffectiveElementType;
                                        if (!effectiveElement.IsVariant)
                                            return effectiveElement.DisplayName;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Structural type from callerType string (for cases without caller expression)
            var callerSemantic = GDSemanticType.FromRuntimeTypeName(callerType);
            if (callerSemantic is GDContainerSemanticType ct)
            {
                if (ct.IsArray && ct.ElementType != null && !ct.ElementType.IsVariant)
                    return ct.ElementType.DisplayName;
                if (ct.IsDictionary && ct.ElementType != null && !ct.ElementType.IsVariant)
                    return ct.ElementType.DisplayName;
            }

            return null;
        }

        /// <summary>
        /// Builds list of parameter types based on CallableReceivesType metadata.
        /// </summary>
        private static IReadOnlyList<string> BuildLambdaParameterTypes(string receivesType, string elementType, int paramCount)
        {
            return receivesType switch
            {
                "element" => new[] { elementType },
                "element_element" => new[] { elementType, elementType },
                "accumulator_element" => new[] { "Variant", elementType },
                "key" => new[] { elementType }, // For dictionary key iteration
                "value" => new[] { elementType }, // For dictionary value iteration
                "key_value" => new[] { "Variant", elementType }, // For dictionary key-value iteration
                _ => Enumerable.Repeat("Variant", paramCount).ToArray()
            };
        }

        private void RegisterLocalVariable(GDVariableDeclarationStatement varDecl)
        {
            var varName = varDecl.Identifier?.Sequence;
            if (string.IsNullOrEmpty(varName))
                return;

            // Use explicit type if present (var x: int = ...)
            var typeName = varDecl.Type?.BuildName();
            GDTypeNode typeNode = varDecl.Type;

            // Infer type from initializer if no explicit type annotation
            // var x := 42  → Colon != null, Type == null → infer int
            // var x = 42   → Colon == null → also infer int from literal
            // var x: int = 42 → Type != null → use explicit type (already set above)
            // NOTE: Removed `&& varDecl.Colon != null` check to fix GD3020 false positives
            // for `var x = 0` style declarations with literal initializers
            if (string.IsNullOrEmpty(typeName) && varDecl.Initializer != null)
            {
                typeName = InferSimpleType(varDecl.Initializer);
            }

            // Fallback to Variant for dynamic typing
            if (string.IsNullOrEmpty(typeName) || GDSemanticType.FromRuntimeTypeName(typeName).IsType("Unknown"))
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
            var initSemantic = GDSemanticType.FromRuntimeTypeName(initType);

            // Skip validation if type couldn't be inferred
            if (initType == null || initSemantic.IsType("Unknown"))
                return;

            // Check both directions: upcast (init IS-A declared) and downcast (declared IS-A init)
            // GDScript allows implicit downcasts: var sprite: Sprite2D = get_node("...")
            if (!AreTypesCompatibleForAssignment(initType, declaredType) &&
                !AreTypesCompatibleForAssignment(declaredType, initType))
            {
                var message = BuildTypeMismatchMessage(initType, declaredType, varDecl.Identifier?.Sequence, varDecl);
                ReportWarning(GDDiagnosticCode.TypeAnnotationMismatch, message, varDecl);
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
            var initSemantic = GDSemanticType.FromRuntimeTypeName(initType);

            // Skip validation if type couldn't be inferred
            if (initType == null || initSemantic.IsType("Unknown"))
                return;

            // Check both directions: upcast (init IS-A declared) and downcast (declared IS-A init)
            // GDScript allows implicit downcasts: @export var res: MyResource = load("...")
            if (!AreTypesCompatibleForAssignment(initType, declaredType) &&
                !AreTypesCompatibleForAssignment(declaredType, initType))
            {
                var message = BuildTypeMismatchMessage(initType, declaredType, varDecl.Identifier?.Sequence, varDecl);
                ReportWarning(GDDiagnosticCode.TypeAnnotationMismatch, message, varDecl);
            }
        }

        private string BuildTypeMismatchMessage(string initType, string declaredType, string? varName, GDNode node)
        {
            if (_semanticModel != null && varName != null)
            {
                var flowVar = _semanticModel.GetFlowVariableType(varName, node);
                if (flowVar != null)
                {
                    var initSemanticType = GDSemanticType.FromRuntimeTypeName(initType);
                    var origins = flowVar.CurrentType.GetOrigins(initSemanticType);
                    if (origins.Count > 0)
                    {
                        var origin = origins[0];
                        var originDesc = origin.Description ?? origin.Kind.ToString();
                        return $"Type mismatch: '{initType}' (from {originDesc}) is not assignable to '{declaredType}'";
                    }
                }
            }
            return $"Type mismatch: cannot assign '{initType}' to variable of type '{declaredType}'";
        }

        private void RegisterForLoopVariable(GDForStatement forStmt)
        {
            var varName = forStmt.Variable?.Sequence;
            if (string.IsNullOrEmpty(varName))
                return;

            var typeName = forStmt.VariableType?.BuildName();
            GDTypeNode typeNode = forStmt.VariableType;

            if (string.IsNullOrEmpty(typeName) && forStmt.Collection != null)
            {
                typeName = InferCollectionElementType(forStmt.Collection);
            }

            if (string.IsNullOrEmpty(typeName) || GDSemanticType.FromRuntimeTypeName(typeName).IsType("Unknown"))
                typeName = "Variant";

            Context.Declare(GDSymbol.Variable(varName, forStmt, typeName: typeName, typeNode: typeNode));
        }

        private void ValidateForLoopVariable(GDForStatement forStmt)
        {
            var declaredType = forStmt.VariableType?.BuildName();
            if (string.IsNullOrEmpty(declaredType))
                return;

            if (forStmt.Collection == null)
                return;

            var elementType = InferCollectionElementType(forStmt.Collection);
            var elementSemantic = GDSemanticType.FromRuntimeTypeName(elementType);

            if (elementType == null || elementSemantic.IsType("Unknown") || elementSemantic.IsVariant)
                return;

            // Variant annotation when a narrower type is known — unnecessary widening
            var declaredSemantic = GDSemanticType.FromRuntimeTypeName(declaredType);
            if (declaredSemantic.IsVariant)
            {
                ReportWarning(
                    GDDiagnosticCode.AnnotationWiderThanInferred,
                    $"Unnecessary 'Variant' annotation: for-loop iterates '{elementType}' elements. Use '{elementType}' instead",
                    forStmt);
                return;
            }

            // Check both directions: upcast (element IS-A declared) and downcast (declared IS-A element)
            // Downcast is valid in GDScript for-loops: `for p: TacticsPawn in get_children()` narrows Node to TacticsPawn
            if (!AreTypesCompatibleForAssignment(elementType, declaredType) &&
                !AreTypesCompatibleForAssignment(declaredType, elementType))
            {
                ReportWarning(
                    GDDiagnosticCode.TypeAnnotationMismatch,
                    $"Type mismatch: for-loop iterates '{elementType}' elements but variable is typed as '{declaredType}'",
                    forStmt);
            }
        }

        private string? InferCollectionElementType(GDExpression collection)
        {
            // Check for range() call directly — range() always yields int
            if (collection is GDCallExpression callExpr &&
                callExpr.CallerExpression is GDIdentifierExpression callerIdent &&
                callerIdent.Identifier?.Sequence == "range")
            {
                return "int";
            }

            var semanticType = _semanticModel?.TypeSystem.GetType(collection);
            if (semanticType != null && !semanticType.IsVariant)
            {
                if (semanticType is GDContainerSemanticType container)
                {
                    if (container.IsArray && container.ElementType != null)
                        return container.ElementType.DisplayName;
                    if (container.IsDictionary)
                        return "Variant";
                }

                if (semanticType.IsNumeric)
                    return "int";

                if (semanticType.IsString)
                    return "String";

                var packedElement = GDPackedArrayTypes.GetElementType(semanticType.DisplayName);
                if (packedElement != null)
                    return packedElement;

                return "Variant";
            }

            var collectionType = InferSimpleType(collection);
            if (collectionType == null || GDSemanticType.FromRuntimeTypeName(collectionType).IsType("Unknown"))
                return null;

            return "Variant";
        }

        private bool AreTypesCompatibleForAssignment(string sourceType, string targetType)
        {
            if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
                return true;

            var sourceSemantic = GDSemanticType.FromRuntimeTypeName(sourceType);
            var targetSemantic = GDSemanticType.FromRuntimeTypeName(targetType);

            // Same type is always compatible
            if (sourceSemantic.Equals(targetSemantic))
                return true;

            // null is compatible with any reference type
            if (sourceSemantic.IsNull)
                return true;

            // Variant accepts anything
            if (targetSemantic.IsVariant)
                return true;

            // Local enum ↔ int compatibility
            if (_semanticModel != null)
            {
                if ((sourceSemantic.IsType("int") && _semanticModel.IsLocalEnumType(targetType)) ||
                    (targetSemantic.IsType("int") && _semanticModel.IsLocalEnumType(sourceType)))
                    return true;
            }

            // Qualified type name matching (Constants.TowerType == TowerType)
            if (sourceType.Contains('.') || targetType.Contains('.'))
            {
                var sourceName = sourceType.Contains('.') ? sourceType.Substring(sourceType.LastIndexOf('.') + 1) : sourceType;
                var targetName = targetType.Contains('.') ? targetType.Substring(targetType.LastIndexOf('.') + 1) : targetType;
                if (sourceName == targetName)
                    return true;
            }

            // Generic type is assignable to its non-generic base (Array[int] -> Array)
            if (sourceSemantic is GDContainerSemanticType sc && targetSemantic is GDContainerSemanticType tc &&
                sc.IsArray == tc.IsArray && sc.IsDictionary == tc.IsDictionary)
                return true;
            if (sourceSemantic.IsContainer &&
                ((sourceSemantic.IsArray && targetSemantic.IsArray) ||
                 (sourceSemantic.IsDictionary && targetSemantic.IsDictionary)))
                return true;

            // Use semantic model for detailed compatibility check
            if (_semanticModel != null)
                return _semanticModel.AreTypesCompatible(sourceType, targetType);

            return false;
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
            var declaredReturnSemantic = GDSemanticType.FromRuntimeTypeName(declaredReturnType);
            if (declaredReturnSemantic.IsType("void"))
            {
                if (returnExpr.Expression != null)
                {
                    var returnedType = InferSimpleType(returnExpr.Expression);
                    var returnedSemantic = GDSemanticType.FromRuntimeTypeName(returnedType);
                    if (!returnedSemantic.IsType("void") && !returnedSemantic.IsType("Unknown"))
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
            var actualSemantic = GDSemanticType.FromRuntimeTypeName(actualType);

            // Skip validation if type couldn't be inferred
            if (actualType == null || actualSemantic.IsType("Unknown"))
                return;

            // Check both directions: upcast and downcast
            // GDScript allows implicit downcasts in returns (e.g., return get_node() for -> Sprite2D)
            if (!AreReturnTypesCompatible(actualType, declaredReturnType) &&
                !AreReturnTypesCompatible(declaredReturnType, actualType))
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

            var actualSemantic = GDSemanticType.FromRuntimeTypeName(actualType);
            var declaredSemantic = GDSemanticType.FromRuntimeTypeName(declaredType);

            // Same type is always compatible
            if (actualSemantic.Equals(declaredSemantic))
                return true;

            // 'self' is compatible with any declared return type (it's the current class or subclass)
            if (actualType == "self")
                return true;

            // null is compatible with any reference type
            if (actualSemantic.IsNull)
                return true;

            // Local enum ↔ int compatibility
            if (_semanticModel != null)
            {
                if ((actualSemantic.IsType("int") && _semanticModel.IsLocalEnumType(declaredType)) ||
                    (declaredSemantic.IsType("int") && _semanticModel.IsLocalEnumType(actualType)))
                    return true;
            }

            // Qualified type name matching (Constants.TowerType == TowerType)
            if (actualType.Contains('.') || declaredType.Contains('.'))
            {
                var actualName = actualType.Contains('.') ? actualType.Substring(actualType.LastIndexOf('.') + 1) : actualType;
                var declaredName = declaredType.Contains('.') ? declaredType.Substring(declaredType.LastIndexOf('.') + 1) : declaredType;
                if (actualName == declaredName)
                    return true;
            }

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

            var leftSemantic = GDSemanticType.FromRuntimeTypeName(leftType);
            var rightSemantic = GDSemanticType.FromRuntimeTypeName(rightType);

            switch (op)
            {
                case GDDualOperatorType.Addition:
                case GDDualOperatorType.Subtraction:
                case GDDualOperatorType.Multiply:
                case GDDualOperatorType.Division:
                case GDDualOperatorType.Mod:
                    if (!AreTypesCompatibleForArithmetic(left, right, leftType, rightType))
                    {
                        // String + anything is allowed in GDScript
                        if (op == GDDualOperatorType.Addition && (leftSemantic.IsString || rightSemantic.IsString))
                            break;

                        // String % anything is string formatting in GDScript
                        if (op == GDDualOperatorType.Mod && leftSemantic.IsString)
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
                    if (!leftSemantic.IsType("int") || !rightSemantic.IsType("int"))
                    {
                        if (!leftSemantic.IsType("Unknown") && !rightSemantic.IsType("Unknown"))
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

                // Ordered comparison operators - check for null and type compatibility
                case GDDualOperatorType.LessThan:
                case GDDualOperatorType.MoreThan:
                case GDDualOperatorType.LessThanOrEqual:
                case GDDualOperatorType.MoreThanOrEqual:
                    ValidateComparisonOperator(expr, left, right, leftType, rightType, op.Value);
                    break;
            }
        }

        /// <summary>
        /// Validates ordered comparison operators (&lt;, &gt;, &lt;=, &gt;=).
        /// These operators do NOT work with null - they cause runtime errors.
        /// </summary>
        private void ValidateComparisonOperator(
            GDDualOperatorExpression expr,
            GDExpression left,
            GDExpression right,
            string leftType,
            string rightType,
            GDDualOperatorType op)
        {
            // 1. Check for exact null type - this is an error (runtime crash)
            // Also check for null literal directly
            var leftCompSemantic = GDSemanticType.FromRuntimeTypeName(leftType);
            var rightCompSemantic = GDSemanticType.FromRuntimeTypeName(rightType);
            bool leftIsNull = leftCompSemantic.IsNull || IsNullLiteral(left) || IsNullInitializedVariable(left);
            bool rightIsNull = rightCompSemantic.IsNull || IsNullLiteral(right) || IsNullInitializedVariable(right);

            if (leftIsNull || rightIsNull)
            {
                ReportError(
                    GDDiagnosticCode.ComparisonWithNull,
                    $"Cannot use '{GetComparisonOperatorSymbol(op)}' with null (causes runtime error: 'Invalid operands')",
                    expr);
                return;
            }

            // 2. Check for potentially null variables (only if not already handled as exact null)
            CheckPotentiallyNullComparison(left, leftType, expr, op);
            CheckPotentiallyNullComparison(right, rightType, expr, op);

            // 3. Check for incompatible types
            if (!AreTypesCompatibleForComparison(leftType, rightType))
            {
                ReportWarning(
                    GDDiagnosticCode.IncompatibleComparisonTypes,
                    $"Incompatible types for comparison: {leftType} {GetComparisonOperatorSymbol(op)} {rightType}",
                    expr);
            }
        }

        /// <summary>
        /// Checks if an expression is a null literal.
        /// </summary>
        private static bool IsNullLiteral(GDExpression? expr)
        {
            if (expr is GDIdentifierExpression identExpr)
            {
                return identExpr.Identifier?.Sequence == "null";
            }
            return false;
        }

        /// <summary>
        /// Checks if a variable is explicitly initialized to null (var x = null).
        /// </summary>
        private bool IsNullInitializedVariable(GDExpression? expr)
        {
            if (expr is not GDIdentifierExpression identExpr)
                return false;

            var varName = identExpr.Identifier?.Sequence;
            if (string.IsNullOrEmpty(varName))
                return false;

            // Check if semantic model knows about null initialization
            if (_semanticModel != null)
            {
                var typeInfo = _semanticModel.TypeSystem.GetType(expr);
                if (typeInfo.IsNull)
                    return true;
            }

            // Also check local scope
            var symbol = Context.Scopes.Lookup(varName);
            if (symbol?.Declaration is GDVariableDeclarationStatement varDeclStmt)
            {
                // Check if initializer is null
                if (varDeclStmt.Initializer is GDIdentifierExpression initIdent &&
                    initIdent.Identifier?.Sequence == "null")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a variable used in comparison might be null.
        /// </summary>
        private void CheckPotentiallyNullComparison(
            GDExpression expr,
            string exprType,
            GDDualOperatorExpression comparisonExpr,
            GDDualOperatorType op)
        {
            // Skip if type is known and non-null
            var exprSemantic = GDSemanticType.FromRuntimeTypeName(exprType);
            if (!exprSemantic.IsType("Unknown") && !exprSemantic.IsVariant)
                return;

            // Check if this is an identifier (variable)
            if (expr is not GDIdentifierExpression identExpr)
                return;

            var varName = identExpr.Identifier?.Sequence;
            if (string.IsNullOrEmpty(varName))
                return;

            // Check if variable is guarded by a null check in the condition
            if (IsGuardedByNullCheck(comparisonExpr, varName))
                return;

            // Look up the symbol
            var symbol = Context.Scopes.Lookup(varName);
            if (symbol == null)
                return;

            // If it's a typed parameter (explicit annotation or inferred), it's safe
            if (symbol.Declaration is GDParameterDeclaration paramDecl &&
                (paramDecl.Type != null || !string.IsNullOrEmpty(symbol.TypeName)))
                return;

            // Untyped parameter or untyped variable could be null
            if (symbol.Declaration is GDParameterDeclaration ||
                (symbol.Declaration is GDVariableDeclarationStatement varDeclStmt && varDeclStmt.Type == null && varDeclStmt.Colon == null))
            {
                ReportWarning(
                    GDDiagnosticCode.ComparisonWithPotentiallyNull,
                    $"Variable '{varName}' may be null; comparison with '{GetComparisonOperatorSymbol(op)}' would cause runtime error",
                    comparisonExpr);
            }
        }

        /// <summary>
        /// Checks if a variable is guarded by a null check in the same 'and' expression.
        /// Handles patterns like: x != null and x < 5, x is int and x < 5
        /// </summary>
        private static bool IsGuardedByNullCheck(GDDualOperatorExpression comparisonExpr, string varName)
        {
            // Walk up to find parent 'and' expression
            var current = comparisonExpr.Parent as GDNode;
            while (current != null)
            {
                if (current is GDDualOperatorExpression andExpr)
                {
                    var opType = andExpr.Operator?.OperatorType;
                    if (opType == GDDualOperatorType.And || opType == GDDualOperatorType.And2)
                    {
                        // Check if the comparison is on the RIGHT side of 'and'
                        // (i.e., a descendant of the right expression)
                        if (IsDescendantOf(comparisonExpr, andExpr.RightExpression))
                        {
                            // Check if the left side is a null guard for our variable
                            if (IsNullGuardFor(andExpr.LeftExpression, varName))
                            {
                                return true;
                            }
                        }
                    }
                }

                current = current.Parent as GDNode;
            }
            return false;
        }

        /// <summary>
        /// Checks if the child node is a descendant of the parent node.
        /// </summary>
        private static bool IsDescendantOf(GDNode? child, GDNode? parent)
        {
            if (child == null || parent == null)
                return false;

            var current = child;
            while (current != null)
            {
                if (current == parent)
                    return true;
                current = current.Parent as GDNode;
            }
            return false;
        }

        /// <summary>
        /// Checks if the expression is a null guard for the specified variable.
        /// Recognizes: var != null, var is Type, is_instance_valid(var)
        /// </summary>
        private static bool IsNullGuardFor(GDExpression? expr, string varName)
        {
            if (expr == null)
                return false;

            // var != null
            if (expr is GDDualOperatorExpression eqOp &&
                eqOp.Operator?.OperatorType == GDDualOperatorType.NotEqual)
            {
                if (IsNullLiteral(eqOp.RightExpression) &&
                    eqOp.LeftExpression is GDIdentifierExpression leftIdent &&
                    leftIdent.Identifier?.Sequence == varName)
                    return true;

                if (IsNullLiteral(eqOp.LeftExpression) &&
                    eqOp.RightExpression is GDIdentifierExpression rightIdent &&
                    rightIdent.Identifier?.Sequence == varName)
                    return true;
            }

            // var is Type (type guard implies non-null)
            if (expr is GDDualOperatorExpression isOp &&
                isOp.Operator?.OperatorType == GDDualOperatorType.Is)
            {
                if (isOp.LeftExpression is GDIdentifierExpression leftIsIdent &&
                    leftIsIdent.Identifier?.Sequence == varName)
                    return true;
            }

            // is_instance_valid(var)
            if (expr is GDCallExpression callExpr)
            {
                if (callExpr.CallerExpression is GDIdentifierExpression funcIdent &&
                    funcIdent.Identifier?.Sequence == "is_instance_valid")
                {
                    var args = callExpr.Parameters?.ToList();
                    if (args != null && args.Count > 0 && args[0] is GDIdentifierExpression argIdent)
                    {
                        if (argIdent.Identifier?.Sequence == varName)
                            return true;
                    }
                }
            }

            // Recursively check left side of nested 'and' expressions
            if (expr is GDDualOperatorExpression andExpr)
            {
                var opType = andExpr.Operator?.OperatorType;
                if (opType == GDDualOperatorType.And || opType == GDDualOperatorType.And2)
                {
                    // Check the left part of and
                    if (IsNullGuardFor(andExpr.LeftExpression, varName))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if two types are compatible for ordered comparison.
        /// </summary>
        private static bool AreTypesCompatibleForComparison(string left, string right)
        {
            var leftSemantic = GDSemanticType.FromRuntimeTypeName(left);
            var rightSemantic = GDSemanticType.FromRuntimeTypeName(right);

            // Unknown types - assume compatible (can't verify)
            if (leftSemantic.IsType("Unknown") || rightSemantic.IsType("Unknown"))
                return true;

            // Variant is dynamically typed - allow comparison
            if (leftSemantic.IsVariant || rightSemantic.IsVariant)
                return true;

            // Same type is always compatible
            if (leftSemantic.Equals(rightSemantic))
                return true;

            // Numeric types are compatible with each other
            if (leftSemantic.IsNumeric && rightSemantic.IsNumeric)
                return true;

            // String types are compatible
            if (leftSemantic.IsString && rightSemantic.IsString)
                return true;

            // Vector types of the same dimension are comparable
            if (AreVectorTypesComparable(left, right))
                return true;

            return false;
        }

        private static bool IsNumericTypeStatic(string type) =>
            type == "int" || type == "float";

        private static bool IsStringType(string type) =>
            type == "String" || type == "StringName";

        private static bool AreVectorTypesComparable(string left, string right)
        {
            // Vector2/Vector2i, Vector3/Vector3i, Vector4/Vector4i
            if ((left == "Vector2" || left == "Vector2i") &&
                (right == "Vector2" || right == "Vector2i"))
                return true;

            if ((left == "Vector3" || left == "Vector3i") &&
                (right == "Vector3" || right == "Vector3i"))
                return true;

            if ((left == "Vector4" || left == "Vector4i") &&
                (right == "Vector4" || right == "Vector4i"))
                return true;

            return false;
        }

        private static string GetComparisonOperatorSymbol(GDDualOperatorType op) =>
            op switch
            {
                GDDualOperatorType.LessThan => "<",
                GDDualOperatorType.MoreThan => ">",
                GDDualOperatorType.LessThanOrEqual => "<=",
                GDDualOperatorType.MoreThanOrEqual => ">=",
                _ => op.ToString()
            };

        private void ValidateAssignment(GDExpression target, GDExpression value, string targetType, string valueType, GDNode reportOn)
        {
            var targetAssignSemantic = GDSemanticType.FromRuntimeTypeName(targetType);
            var valueAssignSemantic = GDSemanticType.FromRuntimeTypeName(valueType);

            if (targetAssignSemantic.IsType("Unknown") || valueAssignSemantic.IsType("Unknown"))
                return;

            // Skip if types match
            if (targetAssignSemantic.Equals(valueAssignSemantic))
                return;

            // Qualified type name matching (Constants.TowerType == TowerType)
            if (targetType.Contains('.') || valueType.Contains('.'))
            {
                var targetName = targetType.Contains('.') ? targetType.Substring(targetType.LastIndexOf('.') + 1) : targetType;
                var valueName = valueType.Contains('.') ? valueType.Substring(valueType.LastIndexOf('.') + 1) : valueType;
                if (targetName == valueName)
                    return;
            }

            // Local enum ↔ int compatibility
            if (_semanticModel != null)
            {
                if ((valueAssignSemantic.IsType("int") && _semanticModel.IsLocalEnumType(targetType)) ||
                    (targetAssignSemantic.IsType("int") && _semanticModel.IsLocalEnumType(valueType)))
                    return;
            }

            // Check if target is an untyped variable (Variant) - allow any assignment
            if (IsUntypedVariable(target))
                return;

            // Use semantic model for compatibility check (bidirectional for implicit downcasts)
            if (_semanticModel != null &&
                !_semanticModel.AreTypesCompatible(valueType, targetType) &&
                !_semanticModel.AreTypesCompatible(targetType, valueType))
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

            var operandSemantic = GDSemanticType.FromRuntimeTypeName(operandType);

            switch (op)
            {
                case GDSingleOperatorType.Negate:
                    if (!operandSemantic.IsNumeric && !IsVectorType(operandType) &&
                        !operandSemantic.IsType("Color") && !operandSemantic.IsType("Quaternion") &&
                        !operandSemantic.IsType("Unknown"))
                    {
                        ReportWarning(
                            GDDiagnosticCode.InvalidOperandType,
                            $"Negation operator requires numeric type, got {operandType}",
                            expr);
                    }
                    break;

                case GDSingleOperatorType.BitwiseNegate:
                    if (!operandSemantic.IsType("int") && !operandSemantic.IsType("Unknown"))
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
            var typeInfo = _semanticModel?.TypeSystem.GetType(expr);
            if (typeInfo == null || typeInfo.IsVariant)
                return "Unknown";
            return typeInfo.DisplayName;
        }

        private bool AreTypesCompatibleForArithmetic(
            GDExpression leftExpr, GDExpression rightExpr,
            string leftType, string rightType)
        {
            var leftArithSemantic = GDSemanticType.FromRuntimeTypeName(leftType);
            var rightArithSemantic = GDSemanticType.FromRuntimeTypeName(rightType);

            if (leftArithSemantic.IsType("Unknown") || rightArithSemantic.IsType("Unknown"))
                return true;

            // Variant is dynamically typed and can hold any value, so arithmetic is allowed
            if (leftArithSemantic.IsVariant || rightArithSemantic.IsVariant)
                return true;

            if (leftArithSemantic.IsNumeric && rightArithSemantic.IsNumeric)
                return true;

            if (leftArithSemantic.Equals(rightArithSemantic))
                return true;

            // Array concatenation: use GDTypeNode to check array types (no string parsing)
            if (AreArrayTypesForConcatenation(leftExpr, rightExpr))
                return true;

            // Vector * scalar and scalar * Vector operations are valid
            // This includes *, / for scaling vectors
            if (IsVectorType(leftType) && rightArithSemantic.IsNumeric)
                return true;

            if (leftArithSemantic.IsNumeric && IsVectorType(rightType))
                return true;

            // Transform * Vector and Vector * Transform operations are also valid
            if (IsTransformType(leftType) && IsVectorType(rightType))
                return true;

            if (IsVectorType(leftType) && IsTransformType(rightType))
                return true;

            // Color * scalar operations
            if (leftArithSemantic.IsType("Color") && rightArithSemantic.IsNumeric)
                return true;

            if (leftArithSemantic.IsNumeric && rightArithSemantic.IsType("Color"))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if both expressions are array types using GDTypeNode (no string parsing).
        /// </summary>
        private bool AreArrayTypesForConcatenation(GDExpression left, GDExpression right)
        {
            var leftTypeNode = _semanticModel?.TypeSystem.GetTypeNode(left);
            var rightTypeNode = _semanticModel?.TypeSystem.GetTypeNode(right);

            return leftTypeNode is GDArrayTypeNode && rightTypeNode is GDArrayTypeNode;
        }

        private bool IsNumericType(string type)
        {
            return type == "int" || type == "float";
        }

        private static bool IsVectorType(string type)
        {
            return type == "Vector2" || type == "Vector2i" ||
                   type == "Vector3" || type == "Vector3i" ||
                   type == "Vector4" || type == "Vector4i";
        }

        private static bool IsTransformType(string type)
        {
            return type == "Transform2D" || type == "Transform3D" ||
                   type == "Basis" || type == "Projection";
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
