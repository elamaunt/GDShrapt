using System;
using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics
{
    /// <summary>
    /// Infers types of expressions using the RuntimeProvider and scope information.
    /// Supports generic types like Array[T] and Dictionary[K,V].
    /// Supports bidirectional type inference (forward and backward).
    /// </summary>
    internal class GDTypeInferenceEngine
    {
        private readonly IGDRuntimeProvider _runtimeProvider;
        private readonly GDScopeStack _scopes;
        private readonly IGDRuntimeTypeInjector _typeInjector;
        private readonly GDTypeInjectionContext _injectionContext;

        // Cache for computed types to avoid recomputation
        private readonly Dictionary<GDNode, string> _typeCache;
        private readonly Dictionary<GDNode, GDTypeNode> _typeNodeCache;

        // Optional provider for inferred container element types
        private Func<string, GDContainerElementType> _containerTypeProvider;

        // Guard against infinite recursion when inferring method return types
        private readonly HashSet<string> _methodsBeingInferred = new HashSet<string>();

        // Guard against infinite recursion when inferring expression types
        private readonly HashSet<GDExpression> _expressionsBeingInferred = new HashSet<GDExpression>();
        private const int MaxInferenceDepth = 50;

        // Optional provider for narrowed types from control flow analysis (e.g., after "if x is Type:")
        private Func<string, string> _narrowingTypeProvider;

        // Specialized analyzers (lazy initialized)
        private GDContainerTypeAnalyzer _containerAnalyzer;
        private GDSignalTypeAnalyzer _signalAnalyzer;
        private GDMethodReturnTypeAnalyzer _methodReturnAnalyzer;

        /// <summary>
        /// Gets the runtime provider.
        /// </summary>
        public IGDRuntimeProvider RuntimeProvider => _runtimeProvider;

        /// <summary>
        /// Gets the scope stack.
        /// </summary>
        public GDScopeStack Scopes => _scopes;

        /// <summary>
        /// Creates a new type inference engine.
        /// </summary>
        public GDTypeInferenceEngine(IGDRuntimeProvider runtimeProvider, GDScopeStack scopes = null)
            : this(runtimeProvider, scopes, null, null)
        {
        }

        /// <summary>
        /// Creates a new type inference engine with optional type injector.
        /// </summary>
        /// <param name="runtimeProvider">Runtime type provider</param>
        /// <param name="scopes">Scope stack for symbol lookup</param>
        /// <param name="typeInjector">Optional injector for runtime-derived types</param>
        /// <param name="injectionContext">Context for type injection</param>
        public GDTypeInferenceEngine(
            IGDRuntimeProvider runtimeProvider,
            GDScopeStack scopes,
            IGDRuntimeTypeInjector typeInjector,
            GDTypeInjectionContext injectionContext)
        {
            _runtimeProvider = runtimeProvider ?? GDDefaultRuntimeProvider.Instance;
            _scopes = scopes;
            _typeInjector = typeInjector;
            _injectionContext = injectionContext ?? new GDTypeInjectionContext();
            _typeCache = new Dictionary<GDNode, string>();
            _typeNodeCache = new Dictionary<GDNode, GDTypeNode>();
        }

        /// <summary>
        /// Finds a member in a type, traversing the inheritance chain if necessary.
        /// </summary>
        private GDRuntimeMemberInfo FindMemberWithInheritance(string typeName, string memberName)
        {
            var visited = new HashSet<string>();
            var current = typeName;
            while (!string.IsNullOrEmpty(current))
            {
                // Prevent infinite loop on cyclic inheritance
                if (!visited.Add(current))
                    return null;

                var memberInfo = _runtimeProvider.GetMember(current, memberName);
                if (memberInfo != null)
                    return memberInfo;

                current = _runtimeProvider.GetBaseType(current);
            }
            return null;
        }

        /// <summary>
        /// For Variant callers, tries to find a method in common GDScript types.
        /// This handles cases like item.to_upper() where item is Variant but we can
        /// still infer the return type based on the method name.
        /// </summary>
        private string FindMethodReturnTypeInCommonTypes(string methodName)
        {
            // Common types to check for method existence
            string[] commonTypes = { "String", "Array", "Dictionary", "int", "float", "bool", "Vector2", "Vector3", "Color" };

            foreach (var typeName in commonTypes)
            {
                var memberInfo = _runtimeProvider.GetMember(typeName, methodName);
                if (memberInfo != null && memberInfo.Kind == GDRuntimeMemberKind.Method)
                    return memberInfo.Type;
            }

            return null;
        }

        /// <summary>
        /// Sets a provider function for inferring container element types.
        /// Used to integrate with usage-based type inference from semantic analysis.
        /// </summary>
        /// <param name="provider">Function that takes a variable name and returns inferred container type, or null</param>
        public void SetContainerTypeProvider(Func<string, GDContainerElementType> provider)
        {
            _containerTypeProvider = provider;
        }

        /// <summary>
        /// Sets a provider function for narrowed types from control flow analysis.
        /// Used to integrate with type narrowing analysis (e.g., "if x is Type:" guards).
        /// </summary>
        /// <param name="provider">Function that takes a variable name and returns the narrowed type, or null</param>
        public void SetNarrowingTypeProvider(Func<string, string> provider)
        {
            _narrowingTypeProvider = provider;
        }

        #region Analyzer Accessors (Lazy Initialization)

        /// <summary>
        /// Gets the container type analyzer (Array/Dictionary analysis).
        /// </summary>
        private GDContainerTypeAnalyzer ContainerAnalyzer =>
            _containerAnalyzer ??= new GDContainerTypeAnalyzer(_scopes, InferType);

        /// <summary>
        /// Gets the signal type analyzer (signals and await).
        /// </summary>
        private GDSignalTypeAnalyzer SignalAnalyzer =>
            _signalAnalyzer ??= new GDSignalTypeAnalyzer(
                _typeInjector,
                _injectionContext,
                InferType,
                FindMemberWithInheritance);

        /// <summary>
        /// Gets the method return type analyzer.
        /// </summary>
        private GDMethodReturnTypeAnalyzer MethodReturnAnalyzer =>
            _methodReturnAnalyzer ??= new GDMethodReturnTypeAnalyzer(
                InferType,
                InferTypeNode,
                InferCallType);

        #endregion

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

            // Check cache first
            if (_typeNodeCache.TryGetValue(expression, out var cachedType))
                return cachedType;

            // Guard against infinite recursion
            if (_expressionsBeingInferred.Contains(expression))
                return null; // Already inferring this expression - break cycle

            if (_expressionsBeingInferred.Count >= MaxInferenceDepth)
                return null; // Too deep - likely infinite recursion

            _expressionsBeingInferred.Add(expression);
            try
            {
                var result = InferTypeNodeCore(expression);
                if (result != null)
                    _typeNodeCache[expression] = result;
                return result;
            }
            finally
            {
                _expressionsBeingInferred.Remove(expression);
            }
        }

        /// <summary>
        /// Core implementation of InferTypeNode without recursion guard.
        /// </summary>
        private GDTypeNode InferTypeNodeCore(GDExpression expression)
        {
            switch (expression)
            {
                // Literals - known types
                case GDNumberExpression numExpr:
                    return CreateSimpleType(InferNumberType(numExpr));

                case GDStringExpression _:
                    return CreateSimpleType("String");

                case GDBoolExpression _:
                    return CreateSimpleType("bool");

                case GDArrayInitializerExpression arrayInit:
                {
                    var elementUnion = ExtractArrayElementTypes(arrayInit);
                    if (!string.IsNullOrEmpty(elementUnion))
                        return CreateSimpleType($"Array[{elementUnion}]");
                    return CreateSimpleType("Array");
                }

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

                // Lambda as expression - always Callable type
                // Use InferLambdaReturnType() to get the return type of the lambda body
                case GDMethodExpression _:
                    return CreateSimpleType("Callable");

                // Await - returns signal emission type or coroutine return type
                case GDAwaitExpression awaitExpr:
                    return InferAwaitType(awaitExpr);

                // Yield - Signal (in Godot 4)
                case GDYieldExpression _:
                    return CreateSimpleType("Signal");

                // Get node ($) - try type injector first, fallback to Node
                case GDGetNodeExpression getNodeExpr:
                    if (_typeInjector != null)
                    {
                        var injectedType = _typeInjector.InjectType(getNodeExpr, _injectionContext);
                        if (!string.IsNullOrEmpty(injectedType))
                            return CreateSimpleType(injectedType);
                    }
                    return CreateSimpleType("Node");

                // Get unique node (%) - try type injector first, fallback to Node
                case GDGetUniqueNodeExpression getUniqueExpr:
                    if (_typeInjector != null)
                    {
                        var injectedType = _typeInjector.InjectType(getUniqueExpr, _injectionContext);
                        if (!string.IsNullOrEmpty(injectedType))
                            return CreateSimpleType(injectedType);
                    }
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

            // For untyped Array/Dictionary, try to infer from usage
            var containerType = containerTypeNode?.BuildName();

            // For untyped Dictionary, try key-specific type inference first
            if (containerType == "Dictionary")
            {
                // Try to extract the key as a static string
                var resolver = GDStaticStringExtractor.CreateScopeResolver(_scopes, indexerExpr.RootClassDeclaration);
                var keyStr = GDStaticStringExtractor.TryExtractString(indexerExpr.InnerExpression, resolver);

                if (!string.IsNullOrEmpty(keyStr))
                {
                    var specificType = InferDictionaryValueTypeForKey(indexerExpr.CallerExpression, keyStr);
                    if (!string.IsNullOrEmpty(specificType))
                        return CreateSimpleType(specificType);
                }
            }

            if (containerType == "Array" || containerType == "Dictionary")
            {
                // Try to get inferred element type from container usage analysis
                if (_containerTypeProvider != null)
                {
                    var varName = GetRootVariableName(indexerExpr.CallerExpression);
                    if (!string.IsNullOrEmpty(varName))
                    {
                        var inferredType = _containerTypeProvider(varName);
                        if (inferredType != null && inferredType.HasElementTypes)
                        {
                            var effectiveType = inferredType.EffectiveElementType;
                            if (!string.IsNullOrEmpty(effectiveType) && effectiveType != "Variant")
                            {
                                return CreateSimpleType(effectiveType);
                            }
                        }
                    }
                }

                // Fallback to Variant
                return CreateVariantTypeNode();
            }

            // For PackedArrays (PackedByteArray, PackedInt32Array, etc.)
            if (!string.IsNullOrEmpty(containerType))
            {
                var elementType = GetPackedArrayElementType(containerType);
                if (elementType != null)
                {
                    return CreateSimpleType(elementType);
                }
            }

            // String indexing returns String (single character)
            if (containerType == "String")
            {
                return CreateSimpleType("String");
            }

            // Unknown container type - return Variant as safe fallback
            if (containerTypeNode != null)
            {
                return CreateVariantTypeNode();
            }

            return null;
        }

        /// <summary>
        /// Gets the element type for packed array types.
        /// </summary>
        private static string GetPackedArrayElementType(string packedArrayType)
        {
            switch (packedArrayType)
            {
                case "PackedByteArray":
                    return "int";
                case "PackedInt32Array":
                    return "int";
                case "PackedInt64Array":
                    return "int";
                case "PackedFloat32Array":
                    return "float";
                case "PackedFloat64Array":
                    return "float";
                case "PackedStringArray":
                    return "String";
                case "PackedVector2Array":
                    return "Vector2";
                case "PackedVector3Array":
                    return "Vector3";
                case "PackedColorArray":
                    return "Color";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Creates a Variant type node.
        /// </summary>
        private static GDTypeNode CreateVariantTypeNode()
        {
            return new GDSingleTypeNode { Type = new GDType { Sequence = "Variant" } };
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
                    // super refers to the parent class type
                    var parentType = GetParentClassType(identExpr);
                    return CreateSimpleType(parentType ?? "RefCounted");
            }

            // Check narrowing context first (type guards like "if x is Type:")
            // This allows type narrowing to override the declared type within guarded branches
            if (_narrowingTypeProvider != null)
            {
                var narrowedType = _narrowingTypeProvider(name);
                if (!string.IsNullOrEmpty(narrowedType))
                    return CreateSimpleType(narrowedType);
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

                    // Handle enum symbols - enum type is the enum name itself
                    // This allows AIState.PATROL where AIState is a local enum
                    if (symbol.Kind == GDSymbolKind.Enum)
                    {
                        return CreateSimpleType(symbol.Name);
                    }

                    // Handle inner class symbols - class type is the class name
                    if (symbol.Kind == GDSymbolKind.Class)
                    {
                        return CreateSimpleType(symbol.Name);
                    }

                    // Fallback: infer type from initializer for understanding expression type
                    // Handle local variables (statements)
                    if (symbol.Declaration is GDVariableDeclarationStatement varDeclStmt &&
                        varDeclStmt.Initializer != null)
                    {
                        return InferTypeNode(varDeclStmt.Initializer);
                    }
                    // Handle class-level variables (declarations)
                    if (symbol.Declaration is GDVariableDeclaration varDecl &&
                        varDecl.Initializer != null)
                    {
                        return InferTypeNode(varDecl.Initializer);
                    }
                }
            }

            // Check class members (for member variables like 'config')
            var classDecl = identExpr.RootClassDeclaration;
            if (classDecl != null)
            {
                foreach (var member in classDecl.Members ?? System.Linq.Enumerable.Empty<GDClassMember>())
                {
                    if (member is GDVariableDeclaration varDecl &&
                        varDecl.Identifier?.Sequence == name)
                    {
                        // Check explicit type first
                        if (varDecl.Type != null)
                            return varDecl.Type;
                        // Infer from initializer
                        if (varDecl.Initializer != null)
                            return InferTypeNode(varDecl.Initializer);
                    }

                    // Check if it's a signal declaration
                    if (member is GDSignalDeclaration signalDecl &&
                        signalDecl.Identifier?.Sequence == name)
                    {
                        return CreateSimpleType("Signal");
                    }
                }

                // Check inherited members from base class via implicit self
                // When identifier like 'position' is used without 'self.', resolve from parent class
                // This handles cases like: extends Node2D; func test(): var d = position.distance_to(...)
                var baseTypeName = GetClassBaseType(classDecl);
                if (!string.IsNullOrEmpty(baseTypeName))
                {
                    var memberInfo = FindMemberWithInheritance(baseTypeName, name);
                    if (memberInfo != null)
                        return CreateSimpleType(memberInfo.Type);
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

        /// <summary>
        /// Gets the parent class type from the extends clause of the containing class.
        /// Used to resolve the type of 'super' keyword.
        /// </summary>
        private static string? GetParentClassType(GDIdentifierExpression identExpr)
        {
            // First check if we're inside an inner class
            var innerClass = identExpr.InnerClassDeclaration;
            if (innerClass != null)
            {
                // Use inner class's BaseType directly (not Extends.Type - Extends is just the keyword)
                var baseType = innerClass.BaseType;
                if (baseType != null)
                {
                    var typeName = baseType.BuildName();
                    if (!string.IsNullOrEmpty(typeName))
                        return typeName;
                }
                // Inner classes without extends default to RefCounted
                return "RefCounted";
            }

            // Fall back to root class for outer class context
            var classDecl = identExpr.RootClassDeclaration;
            if (classDecl == null)
                return null;

            // Get extends clause
            var extendsClause2 = classDecl.Extends;
            if (extendsClause2?.Type != null)
            {
                // Get the type name from extends clause using BuildName()
                // This works for both simple types (Node2D) and complex types
                var typeName = extendsClause2.Type.BuildName();
                if (!string.IsNullOrEmpty(typeName))
                    return typeName;
            }

            // If no extends, default to RefCounted
            return "RefCounted";
        }

        /// <summary>
        /// Gets the base type name from the extends clause of the class declaration.
        /// </summary>
        private static string? GetClassBaseType(GDClassDeclaration classDecl)
        {
            var extendsClause = classDecl?.Extends;
            if (extendsClause?.Type != null)
                return extendsClause.Type.BuildName();
            return null;
        }

        private string InferCallType(GDCallExpression callExpr)
        {
            // Try type injector first for preload(), load(), get_node(), etc.
            if (_typeInjector != null)
            {
                var injectedType = _typeInjector.InjectType(callExpr, _injectionContext);
                if (!string.IsNullOrEmpty(injectedType))
                    return injectedType;
            }

            var caller = callExpr.CallerExpression;

            // Check for emit_signal and connect calls - they have known return types
            var signalCallType = InferSignalCallType(callExpr, caller);
            if (signalCallType != null)
                return signalCallType;

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

                    // Try to find local method declaration for return type
                    var methodDecl = FindLocalMethodDeclaration(funcName, callExpr);
                    if (methodDecl != null)
                    {
                        // First try explicit return type
                        if (methodDecl.ReturnType != null)
                            return methodDecl.ReturnType.BuildName();

                        // No explicit type - try to infer from method body with recursion guard
                        if (!_methodsBeingInferred.Contains(funcName))
                        {
                            _methodsBeingInferred.Add(funcName);
                            try
                            {
                                var inferredReturn = InferMethodReturnType(methodDecl);
                                if (!string.IsNullOrEmpty(inferredReturn))
                                    return inferredReturn;
                            }
                            finally
                            {
                                _methodsBeingInferred.Remove(funcName);
                            }
                        }
                    }
                }
            }
            // Chained call: obj.method1().method2() - inner call is also a GDCallExpression
            else if (caller is GDCallExpression innerCall)
            {
                // First infer the type of the inner call
                var innerType = InferCallType(innerCall);
                if (!string.IsNullOrEmpty(innerType))
                {
                    // The outer call is on the result of inner call
                    // But we need to check if there's a member access between them
                    // This case handles: get_something().method()
                    // The caller would be the call expression, but we need the method being called
                    // Actually this pattern is: (call).identifier() which is GDMemberOperatorExpression(call, identifier)
                    // So this branch handles direct call chaining like func()() which is rare in GDScript
                    return innerType;
                }
            }
            // Method call on object
            else if (caller is GDMemberOperatorExpression memberExpr)
            {
                var methodName = memberExpr.Identifier?.Sequence;

                // Handle .new() constructor - returns the class type itself
                if (methodName == "new")
                {
                    var constructorType = InferType(memberExpr.CallerExpression);
                    if (!string.IsNullOrEmpty(constructorType))
                    {
                        // Verify it's a valid class type that can be instantiated
                        if (_runtimeProvider.IsKnownType(constructorType) ||
                            _runtimeProvider.GetGlobalClass(constructorType) != null)
                        {
                            return constructorType;
                        }
                        // Even for unknown types, .new() likely returns that type
                        // (e.g., user-defined classes not yet in runtime provider)
                        return constructorType;
                    }
                }

                // Handle chained calls: obj.a().b() where memberExpr.CallerExpression is a call
                string callerType;
                if (memberExpr.CallerExpression is GDCallExpression innerCallExpr)
                {
                    callerType = InferCallType(innerCallExpr);
                }
                else
                {
                    callerType = InferType(memberExpr.CallerExpression);
                }

                if (!string.IsNullOrEmpty(methodName))
                {
                    if (!string.IsNullOrEmpty(callerType))
                    {
                        // Special handling for Dictionary.get("key") - try to infer value type for specific key
                        if (callerType == "Dictionary" && methodName == "get")
                        {
                            var dictValueType = InferDictionaryGetType(callExpr, memberExpr.CallerExpression);
                            if (!string.IsNullOrEmpty(dictValueType))
                                return dictValueType;
                        }

                        // Special handling for Object.get("property") - infer property type
                        if (methodName == "get" && callerType != "Dictionary")
                        {
                            var propType = InferObjectGetType(callExpr, callerType);
                            if (!string.IsNullOrEmpty(propType))
                                return propType;
                        }

                        // Special handling for call/callv - infer return type from method name
                        if (methodName == "call" || methodName == "callv")
                        {
                            var dynamicReturnType = InferDynamicCallType(callExpr, callerType);
                            if (!string.IsNullOrEmpty(dynamicReturnType))
                                return dynamicReturnType;
                        }

                        var memberInfo = FindMemberWithInheritance(callerType, methodName);
                        if (memberInfo != null && memberInfo.Kind == GDRuntimeMemberKind.Method)
                            return memberInfo.Type;
                    }

                    // For unknown/Variant caller type, try to find the method in common types
                    // This handles cases like item.to_upper() or item.keys() where item is Variant or untyped parameter
                    if (string.IsNullOrEmpty(callerType) || callerType == "Variant")
                    {
                        var fallbackType = FindMethodReturnTypeInCommonTypes(methodName);
                        if (!string.IsNullOrEmpty(fallbackType))
                            return fallbackType;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts the Union type of elements from an Array initializer expression.
        /// Delegates to GDContainerTypeAnalyzer.
        /// </summary>
        private string ExtractArrayElementTypes(GDArrayInitializerExpression arrayInit)
            => ContainerAnalyzer.ExtractArrayElementTypes(arrayInit);

        /// <summary>
        /// Extracts the Union type of values from a Dictionary initializer expression.
        /// Delegates to GDContainerTypeAnalyzer.
        /// </summary>
        private string ExtractDictionaryValueTypes(GDDictionaryInitializerExpression dictInit)
            => ContainerAnalyzer.ExtractDictionaryValueTypes(dictInit);

        /// <summary>
        /// Infers the type for Dictionary.get("key") with key-specific type lookup.
        /// Delegates to GDContainerTypeAnalyzer.
        /// </summary>
        private string? InferDictionaryGetType(GDCallExpression callExpr, GDExpression dictExpr)
            => ContainerAnalyzer.InferDictionaryGetType(callExpr, dictExpr);

        /// <summary>
        /// Gets the value type for a specific key in a Dictionary initializer.
        /// Delegates to GDContainerTypeAnalyzer.
        /// </summary>
        private string? InferDictionaryValueTypeForKey(GDExpression dictExpr, string key)
            => ContainerAnalyzer.InferDictionaryValueTypeForKey(dictExpr, key);

        /// <summary>
        /// Finds the dictionary initializer expression for a dictionary variable.
        /// Delegates to GDContainerTypeAnalyzer.
        /// </summary>
        private GDDictionaryInitializerExpression? FindDictionaryInitializer(GDExpression dictExpr)
            => ContainerAnalyzer.FindDictionaryInitializer(dictExpr);

        private string? InferDictionaryValueType(GDExpression dictExpr)
            => ContainerAnalyzer.InferDictionaryValueType(dictExpr);

        /// <summary>
        /// Infers the type for Object.get("property") by looking up the property in the type.
        /// </summary>
        private string? InferObjectGetType(GDCallExpression callExpr, string callerType)
        {
            var args = callExpr.Parameters?.ToList();
            if (args == null || args.Count == 0)
                return null;

            // Try to extract the property name as a static string
            var resolver = GDStaticStringExtractor.CreateScopeResolver(_scopes, callExpr.RootClassDeclaration);
            var propName = GDStaticStringExtractor.TryExtractString(args[0], resolver);

            if (string.IsNullOrEmpty(propName))
                return null; // Dynamic property name - cannot resolve

            // Look up the property in the type
            var memberInfo = FindMemberWithInheritance(callerType, propName);
            if (memberInfo != null && memberInfo.Kind == GDRuntimeMemberKind.Property)
                return memberInfo.Type;

            return null;
        }

        /// <summary>
        /// Infers the return type for call()/callv() with a static method name.
        /// </summary>
        private string? InferDynamicCallType(GDCallExpression callExpr, string callerType)
        {
            var args = callExpr.Parameters?.ToList();
            if (args == null || args.Count == 0)
                return null;

            // Try to extract the method name as a static string
            var resolver = GDStaticStringExtractor.CreateScopeResolver(_scopes, callExpr.RootClassDeclaration);
            var targetMethodName = GDStaticStringExtractor.TryExtractString(args[0], resolver);

            if (string.IsNullOrEmpty(targetMethodName))
                return null; // Dynamic method name - cannot resolve

            // If caller type is known, look up the method
            if (!string.IsNullOrEmpty(callerType) && callerType != "Variant")
            {
                var memberInfo = FindMemberWithInheritance(callerType, targetMethodName);
                if (memberInfo != null && memberInfo.Kind == GDRuntimeMemberKind.Method)
                    return memberInfo.Type;
            }

            // For unknown caller type, try common types (duck typing)
            return FindMethodReturnTypeInCommonTypes(targetMethodName);
        }

        /// <summary>
        /// Infers the type for signal-related calls (emit_signal, connect, disconnect, etc.).
        /// Delegates to GDSignalTypeAnalyzer.
        /// </summary>
        private string InferSignalCallType(GDCallExpression callExpr, GDExpression caller)
            => SignalAnalyzer.InferSignalCallType(callExpr, caller);

        /// <summary>
        /// Finds a method declaration in the current class context.
        /// </summary>
        private GDMethodDeclaration FindLocalMethodDeclaration(string methodName, GDNode context)
        {
            var classDecl = context.RootClassDeclaration;
            if (classDecl == null)
                return null;

            foreach (var member in classDecl.Members ?? System.Linq.Enumerable.Empty<GDClassMember>())
            {
                if (member is GDMethodDeclaration methodDecl &&
                    methodDecl.Identifier?.Sequence == methodName)
                {
                    return methodDecl;
                }
            }

            return null;
        }

        /// <summary>
        /// Infers the return type of a method by analyzing its return statements.
        /// Delegates to GDMethodReturnTypeAnalyzer.
        /// </summary>
        private string InferMethodReturnType(GDMethodDeclaration method)
            => MethodReturnAnalyzer.InferMethodReturnType(method);

        /// <summary>
        /// Infers the return type of a lambda expression by analyzing its body.
        /// Delegates to GDMethodReturnTypeAnalyzer.
        /// </summary>
        public string InferLambdaReturnType(GDMethodExpression lambda)
            => MethodReturnAnalyzer.InferLambdaReturnType(lambda);

        /// <summary>
        /// Infers the return type node of a lambda expression by analyzing its body.
        /// Delegates to GDMethodReturnTypeAnalyzer.
        /// </summary>
        private GDTypeNode InferLambdaReturnTypeNode(GDMethodExpression lambda)
            => MethodReturnAnalyzer.InferLambdaReturnTypeNode(lambda);

        private string InferMemberType(GDMemberOperatorExpression memberExpr)
        {
            var memberName = memberExpr.Identifier?.Sequence;
            var callerType = InferType(memberExpr.CallerExpression);

            if (!string.IsNullOrEmpty(callerType) && !string.IsNullOrEmpty(memberName))
            {
                var memberInfo = FindMemberWithInheritance(callerType, memberName);
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

            // Special handling for 'as' operator - right side IS the type name, not an expression
            if (opType == GDDualOperatorType.As)
            {
                var typeName = GetTypeNameFromExpression(dualOp.RightExpression);
                return typeName ?? "Variant";
            }

            var leftType = InferType(dualOp.LeftExpression);
            var rightType = InferType(dualOp.RightExpression);

            // Delegate to the centralized operator type resolver
            return GDOperatorTypeResolver.ResolveOperatorType(opType.Value, leftType, rightType);
        }

        /// <summary>
        /// Extracts a type name from an expression used in type context (e.g., 'as' operator, 'is' check).
        /// </summary>
        private static string? GetTypeNameFromExpression(GDExpression? expr)
        {
            if (expr == null)
                return null;

            // Simple identifier: Dictionary, Array, Node, Sprite2D, etc.
            if (expr is GDIdentifierExpression identExpr)
                return identExpr.Identifier?.Sequence;

            // Member expressions like SomeClass.InnerType
            if (expr is GDMemberOperatorExpression memberExpr)
            {
                var caller = GetTypeNameFromExpression(memberExpr.CallerExpression);
                var member = memberExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(caller) && !string.IsNullOrEmpty(member))
                    return $"{caller}.{member}";
            }

            // Fallback
            return expr.ToString();
        }

        private string InferSingleOperatorType(GDSingleOperatorExpression singleOp)
        {
            var opType = singleOp.Operator?.OperatorType;
            if (opType == null)
                return null;

            var operandType = InferType(singleOp.TargetExpression);

            // Delegate to the centralized operator type resolver
            return GDOperatorTypeResolver.ResolveSingleOperatorType(opType.Value, operandType);
        }

        /// <summary>
        /// Infers the type of an await expression.
        /// Delegates to GDSignalTypeAnalyzer.
        /// </summary>
        private GDTypeNode InferAwaitType(GDAwaitExpression awaitExpr)
            => SignalAnalyzer.InferAwaitType(awaitExpr, InferCallType);

        /// <summary>
        /// Finds a signal declaration in the current class context.
        /// Delegates to GDSignalTypeAnalyzer.
        /// </summary>
        private GDSignalDeclaration FindLocalSignalDeclaration(string signalName, GDNode context)
            => SignalAnalyzer.FindLocalSignalDeclaration(signalName, context);

        /// <summary>
        /// Gets the emission type from a signal declaration.
        /// Delegates to GDSignalTypeAnalyzer.
        /// </summary>
        private string GetSignalEmissionTypeFromDecl(GDSignalDeclaration signalDecl)
            => SignalAnalyzer.GetSignalEmissionTypeFromDecl(signalDecl);

        /// <summary>
        /// Gets the emission type from signal parameter types list.
        /// Delegates to GDSignalTypeAnalyzer.
        /// </summary>
        private string GetSignalEmissionType(IReadOnlyList<string> paramTypes)
            => SignalAnalyzer.GetSignalEmissionType(paramTypes);

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
        /// Parser for parsing type strings into type nodes.
        /// </summary>
        private static readonly GDScriptReader TypeParser = new GDScriptReader();

        /// <summary>
        /// Creates a type node for a type name. Supports both simple types and generic types.
        /// </summary>
        /// <param name="typeName">The type name (e.g., "int", "Array[int]", "Dictionary[String, int]")</param>
        /// <returns>The appropriate GDTypeNode, or null if typeName is null/empty</returns>
        private GDTypeNode CreateSimpleType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName) || string.IsNullOrWhiteSpace(typeName))
                return null;

            // Check if this is a simple type (no generic brackets, dots, or union |)
            // Simple types can be created directly for performance
            if (typeName.IndexOf('[') < 0 && typeName.IndexOf('.') < 0 && typeName.IndexOf('|') < 0)
            {
                // Validate that it's a valid identifier before creating
                // Must start with letter or underscore, contain only letters, digits, underscores
                if (IsValidIdentifier(typeName))
                {
                    return new GDSingleTypeNode { Type = new GDType { Sequence = typeName } };
                }
                // Invalid identifier - return null rather than throwing
                return null;
            }

            // Union types cannot be represented as GDTypeNode since GDType validates identifiers.
            // Return null here - union types will be handled directly in InferType for call expressions.
            if (typeName.Contains("|"))
            {
                // We'll handle this in GetTypeForNode by checking InferCallType directly
                return null;
            }

            // Complex types (generics, nested types) need to be parsed
            return TypeParser.ParseType(typeName);
        }

        /// <summary>
        /// Validates that a string is a valid GDScript identifier.
        /// </summary>
        private static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // Must not start with a number
            if (char.IsDigit(value[0]))
                return false;

            // Must contain only letters, digits, and underscores
            foreach (var c in value)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the root variable name from an expression chain.
        /// Traverses through member and indexer expressions to find the base identifier.
        /// </summary>
        /// <param name="expr">The expression to analyze</param>
        /// <returns>The root variable name, or null if not an identifier-based expression</returns>
        private static string GetRootVariableName(GDExpression expr)
        {
            while (expr is GDMemberOperatorExpression member)
                expr = member.CallerExpression;
            while (expr is GDIndexerExpression indexer)
                expr = indexer.CallerExpression;

            return (expr as GDIdentifierExpression)?.Identifier?.Sequence;
        }

        #region Extended Type Inference API

        /// <summary>
        /// Gets the inferred type for any AST node.
        /// Supports expressions, declarations, statements, and other node types.
        /// </summary>
        /// <param name="node">The AST node to get the type for</param>
        /// <returns>The inferred type name, or null if cannot be determined</returns>
        public string GetTypeForNode(GDNode node)
        {
            if (node == null)
                return null;

            // Check cache first
            if (_typeCache.TryGetValue(node, out var cachedType))
                return cachedType;

            // Try type injector first
            if (_typeInjector != null)
            {
                var injectedType = _typeInjector.InjectType(node, _injectionContext);
                if (injectedType != null)
                {
                    _typeCache[node] = injectedType;
                    return injectedType;
                }
            }

            string type = null;

            // Handle expressions
            if (node is GDExpression expression)
            {
                // Special handling for call expressions - InferCallType may return union types
                // which cannot be represented as GDTypeNode, so we get the type string directly
                if (expression is GDCallExpression callExpr)
                {
                    type = InferCallType(callExpr);
                }
                else
                {
                    type = InferType(expression);
                }
            }
            // Handle declarations
            else if (node is GDVariableDeclaration varDecl)
            {
                type = varDecl.Type?.BuildName();
                if (string.IsNullOrEmpty(type) && varDecl.Initializer != null)
                {
                    // Special handling for dictionary literals - show value union type
                    if (varDecl.Initializer is GDDictionaryInitializerExpression dictInit)
                    {
                        var valueUnion = ExtractDictionaryValueTypes(dictInit);
                        type = !string.IsNullOrEmpty(valueUnion)
                            ? $"Dictionary[{valueUnion}]"
                            : "Dictionary";
                    }
                    // Special handling for array literals - show element union type
                    else if (varDecl.Initializer is GDArrayInitializerExpression arrayInit)
                    {
                        var elementUnion = ExtractArrayElementTypes(arrayInit);
                        type = !string.IsNullOrEmpty(elementUnion)
                            ? $"Array[{elementUnion}]"
                            : "Array";
                    }
                    else
                    {
                        type = InferType(varDecl.Initializer);
                    }
                }
            }
            else if (node is GDVariableDeclarationStatement varStmt)
            {
                type = varStmt.Type?.BuildName();
                if (string.IsNullOrEmpty(type) && varStmt.Initializer != null)
                {
                    // Special handling for dictionary literals - show value union type
                    if (varStmt.Initializer is GDDictionaryInitializerExpression dictInit)
                    {
                        var valueUnion = ExtractDictionaryValueTypes(dictInit);
                        type = !string.IsNullOrEmpty(valueUnion)
                            ? $"Dictionary[{valueUnion}]"
                            : "Dictionary";
                    }
                    // Special handling for array literals - show element union type
                    else if (varStmt.Initializer is GDArrayInitializerExpression arrayInit)
                    {
                        var elementUnion = ExtractArrayElementTypes(arrayInit);
                        type = !string.IsNullOrEmpty(elementUnion)
                            ? $"Array[{elementUnion}]"
                            : "Array";
                    }
                    else
                    {
                        type = InferType(varStmt.Initializer);
                    }
                }
            }
            else if (node is GDParameterDeclaration paramDecl)
            {
                type = paramDecl.Type?.BuildName();
                if (string.IsNullOrEmpty(type) && paramDecl.DefaultValue != null)
                    type = InferType(paramDecl.DefaultValue);
            }
            else if (node is GDMethodDeclaration methodDecl)
            {
                if (methodDecl.ReturnType != null)
                {
                    type = methodDecl.ReturnType.BuildName();
                }
                else
                {
                    // Fallback: infer return type from method body
                    var inferredType = InferMethodReturnType(methodDecl);
                    type = !string.IsNullOrEmpty(inferredType) ? inferredType : "void";
                }
            }
            else if (node is GDSignalDeclaration signalDecl)
            {
                var parameters = signalDecl.Parameters;
                if (parameters == null || !System.Linq.Enumerable.Any(parameters))
                {
                    type = "Signal";
                }
                else
                {
                    var paramStrings = new List<string>();
                    foreach (var param in parameters)
                    {
                        var paramName = param.Identifier?.Sequence ?? "?";
                        var paramType = param.Type?.BuildName() ?? "Variant";
                        paramStrings.Add($"{paramName}: {paramType}");
                    }
                    type = $"Signal({string.Join(", ", paramStrings)})";
                }
            }
            else if (node is GDEnumDeclaration enumDecl)
            {
                type = enumDecl.Identifier?.Sequence ?? "int";
            }
            else if (node is GDEnumValueDeclaration)
            {
                type = "int";
            }
            else if (node is GDInnerClassDeclaration innerClass)
            {
                type = innerClass.Identifier?.Sequence;
            }
            // Handle return expression
            else if (node is GDReturnExpression returnExpr)
            {
                type = returnExpr.Expression != null ? InferType(returnExpr.Expression) : "void";
            }
            else if (node is GDExpressionStatement exprStmt)
            {
                type = InferType(exprStmt.Expression);
            }

            // Cache and return
            if (type != null)
                _typeCache[node] = type;

            return type;
        }

        /// <summary>
        /// Gets the full type node for any AST node.
        /// </summary>
        /// <param name="node">The AST node</param>
        /// <returns>The type node with generic type information, or null</returns>
        public GDTypeNode GetTypeNodeForNode(GDNode node)
        {
            if (node == null)
                return null;

            // Check cache first
            if (_typeNodeCache.TryGetValue(node, out var cachedTypeNode))
                return cachedTypeNode;

            GDTypeNode typeNode = null;

            if (node is GDExpression expression)
            {
                typeNode = InferTypeNode(expression);
            }
            else if (node is GDVariableDeclaration varDecl)
            {
                typeNode = varDecl.Type;
                if (typeNode == null && varDecl.Initializer != null)
                    typeNode = InferTypeNode(varDecl.Initializer);
            }
            else if (node is GDVariableDeclarationStatement varStmt)
            {
                typeNode = varStmt.Type;
                if (typeNode == null && varStmt.Initializer != null)
                    typeNode = InferTypeNode(varStmt.Initializer);
            }
            else if (node is GDParameterDeclaration paramDecl)
            {
                typeNode = paramDecl.Type;
                if (typeNode == null && paramDecl.DefaultValue != null)
                    typeNode = InferTypeNode(paramDecl.DefaultValue);
            }
            else if (node is GDMethodDeclaration methodDecl)
            {
                if (methodDecl.ReturnType != null)
                {
                    typeNode = methodDecl.ReturnType;
                }
                else
                {
                    // Fallback: infer return type from method body
                    var inferredType = InferMethodReturnType(methodDecl);
                    typeNode = !string.IsNullOrEmpty(inferredType)
                        ? CreateSimpleType(inferredType)
                        : CreateSimpleType("void");
                }
            }

            // Cache and return
            if (typeNode != null)
                _typeNodeCache[node] = typeNode;

            return typeNode;
        }

        /// <summary>
        /// Infers the expected type at a position based on context (reverse type inference).
        /// This is useful for autocomplete and type checking when assigning to a typed variable.
        /// </summary>
        /// <param name="targetNode">The node where a value is expected</param>
        /// <returns>The expected type, or null if any type is accepted</returns>
        public string InferExpectedType(GDNode targetNode)
        {
            if (targetNode == null)
                return null;

            // Get parent to understand context
            var parent = targetNode.Parent;
            if (parent == null)
                return null;

            // Assignment: right side should match left side type
            if (parent is GDDualOperatorExpression dualOp)
            {
                var opType = dualOp.Operator?.OperatorType;
                if (opType != null && IsAssignmentOperator(opType.Value))
                {
                    if (targetNode == dualOp.RightExpression)
                    {
                        return InferType(dualOp.LeftExpression);
                    }
                }
            }

            // Variable initialization: should match declared type
            if (parent is GDVariableDeclaration varDecl && targetNode == varDecl.Initializer)
            {
                return varDecl.Type?.BuildName();
            }

            if (parent is GDVariableDeclarationStatement varStmt && targetNode == varStmt.Initializer)
            {
                return varStmt.Type?.BuildName();
            }

            // Function argument: should match parameter type
            if (parent is GDExpressionsList exprList)
            {
                var callParent = exprList.Parent;
                if (callParent is GDCallExpression callExpr)
                {
                    var argIndex = GetIndexInList(exprList, targetNode);
                    if (argIndex >= 0)
                    {
                        return InferParameterType(callExpr, argIndex);
                    }
                }
            }

            // Return expression: should match method return type
            if (parent is GDReturnExpression)
            {
                var methodScope = _scopes?.GetEnclosingFunction();
                if (methodScope?.Node is GDMethodDeclaration methodDecl)
                {
                    return methodDecl.ReturnType?.BuildName();
                }
            }

            // Array element: should match array element type
            if (parent is GDArrayInitializerExpression arrayInit)
            {
                var arrayType = InferTypeNode(arrayInit);
                if (arrayType is GDArrayTypeNode typedArray)
                {
                    return typedArray.InnerType?.BuildName();
                }
            }

            return null;
        }

        /// <summary>
        /// Clears the type cache. Call when AST or scope changes.
        /// </summary>
        public void ClearCache()
        {
            _typeCache.Clear();
            _typeNodeCache.Clear();
        }

        private int GetIndexInList(GDExpressionsList list, GDNode node)
        {
            int index = 0;
            foreach (var item in list)
            {
                if (item == node)
                    return index;
                index++;
            }
            return -1;
        }

        private string InferParameterType(GDCallExpression callExpr, int argIndex)
        {
            var caller = callExpr.CallerExpression;

            // Direct function call
            if (caller is GDIdentifierExpression identExpr)
            {
                var funcName = identExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(funcName))
                {
                    var funcInfo = _runtimeProvider.GetGlobalFunction(funcName);
                    if (funcInfo?.Parameters != null && argIndex < funcInfo.Parameters.Count)
                    {
                        return funcInfo.Parameters[argIndex].Type;
                    }
                }
            }
            // Method call
            else if (caller is GDMemberOperatorExpression memberExpr)
            {
                var methodName = memberExpr.Identifier?.Sequence;
                var callerType = InferType(memberExpr.CallerExpression);

                if (!string.IsNullOrEmpty(callerType) && !string.IsNullOrEmpty(methodName))
                {
                    var memberInfo = FindMemberWithInheritance(callerType, methodName);
                    if (memberInfo?.Kind == GDRuntimeMemberKind.Method)
                    {
                        // Type injector may know the parameter types
                        if (_typeInjector != null)
                        {
                            var paramTypes = _typeInjector.GetSignalParameterTypes(methodName, callerType);
                            if (paramTypes != null && argIndex < paramTypes.Count)
                            {
                                return paramTypes[argIndex];
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static bool IsAssignmentOperator(GDDualOperatorType opType)
        {
            switch (opType)
            {
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
                    return true;
                default:
                    return false;
            }
        }

        #endregion
    }

}
