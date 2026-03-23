using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Validator;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for expression type inference.
/// Extracted from GDSemanticModel to reduce its size.
/// </summary>
internal class GDExpressionTypeService : IGDExpressionTypeProvider
{
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly GDTypeInferenceEngine? _typeEngine;
    private readonly GDContainerTypeService _containerTypeService;
    private readonly GDUnionTypeService _unionTypeService;
    private readonly GDDuckTypeService _duckTypeService;
    private readonly GDFlowAnalysisRegistry _flowRegistry;
    private readonly Dictionary<GDNode, GDSemanticType> _nodeTypes;
    private readonly Dictionary<GDNode, GDTypeNode> _nodeTypeNodes;
    private readonly GDMemberResolver _memberResolver;

    // Recursion guard
    private readonly HashSet<GDExpression> _expressionTypeInProgress = new();
    private const int MaxExpressionTypeRecursionDepth = 50;

    // Delegates to avoid circular dependencies
    private Func<string, GDSymbolInfo?>? _findSymbol;
    private Func<IEnumerable<string>>? _getOnreadyVariables;

    // Local type service for enum access
    private GDLocalTypeService? _localTypeService;

    // File path for origin tracking in flow analysis
    private string? _filePath;

    internal GDExpressionTypeService(
        IGDRuntimeProvider? runtimeProvider,
        GDTypeInferenceEngine? typeEngine,
        GDContainerTypeService containerTypeService,
        GDUnionTypeService unionTypeService,
        GDDuckTypeService duckTypeService,
        GDFlowAnalysisRegistry flowRegistry,
        Dictionary<GDNode, GDSemanticType> nodeTypes,
        Dictionary<GDNode, GDTypeNode> nodeTypeNodes)
    {
        _runtimeProvider = runtimeProvider;
        _typeEngine = typeEngine;
        _containerTypeService = containerTypeService;
        _unionTypeService = unionTypeService;
        _duckTypeService = duckTypeService;
        _flowRegistry = flowRegistry;
        _nodeTypes = nodeTypes;
        _nodeTypeNodes = nodeTypeNodes;
        _memberResolver = new GDMemberResolver(runtimeProvider);
    }

    /// <summary>
    /// Sets the local type service for enum access.
    /// </summary>
    internal void SetLocalTypeService(GDLocalTypeService? localTypeService)
    {
        _localTypeService = localTypeService;
    }

    internal void SetFilePath(string? filePath)
    {
        _filePath = filePath;
    }

    /// <summary>
    /// Sets the delegate for finding symbols.
    /// </summary>
    internal void SetFindSymbolDelegate(Func<string, GDSymbolInfo?> findSymbol)
    {
        _findSymbol = findSymbol;
    }

    /// <summary>
    /// Sets the delegate for getting onready variables.
    /// </summary>
    internal void SetGetOnreadyVariablesDelegate(Func<IEnumerable<string>> getOnreadyVariables)
    {
        _getOnreadyVariables = getOnreadyVariables;
    }

    // === IGDExpressionTypeProvider (explicit, no-flow to prevent recursion) ===

    GDSemanticType? IGDExpressionTypeProvider.InferType(GDExpression expression)
    {
        if (expression is GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is GDMemberOperatorExpression callMemberExpr)
            {
                var methodName = callMemberExpr.Identifier?.Sequence;
                if (methodName == GDWellKnownFunctions.Constructor)
                {
                    var callerType = GetExpressionTypeWithoutFlow(callMemberExpr.CallerExpression);
                    if (callerType != null)
                        return callerType;
                }
            }

            var callResult = _typeEngine?.InferSemanticType(expression);
            if (callResult != null)
                return callResult;
        }

        if (expression is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var symbol = _findSymbol?.Invoke(varName);
                if (symbol?.Kind == GDSymbolKind.Method)
                    return GDSemanticType.FromRuntimeTypeName("Callable");

                if (symbol?.Kind == GDSymbolKind.Class)
                    return GDSemanticType.FromRuntimeTypeName(symbol.Name);

                var unionType = _unionTypeService.GetUnionType(varName, symbol, null);
                if (unionType != null && unionType.IsSingleType)
                {
                    var effectiveType = unionType.EffectiveType;
                    if (!effectiveType.IsVariant && !effectiveType.IsNull)
                        return effectiveType;
                }
            }
        }

        return _typeEngine?.InferSemanticType(expression);
    }

    GDSymbol? IGDExpressionTypeProvider.LookupSymbol(string name) =>
        _findSymbol?.Invoke(name)?.Symbol;

    IGDRuntimeProvider? IGDExpressionTypeProvider.RuntimeProvider => _runtimeProvider;

    bool IGDExpressionTypeProvider.IsNumericType(string typeName) =>
        _runtimeProvider?.IsNumericType(typeName) ?? GDWellKnownTypes.IsNumericType(typeName);

    /// <summary>
    /// Gets the semantic type for an expression, preserving GDCallableSemanticType.
    /// Use this when you need callable signature information.
    /// For simple string type names, use GetExpressionType() instead.
    /// </summary>
    public GDSemanticType? GetExpressionSemanticType(GDExpression? expression)
    {
        if (expression == null)
            return null;

        // For identifier expressions, check flow state first
        if (expression is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var scope = expression.GetContainingMethodScope();
                if (scope != null)
                {
                    var flowAnalyzer = GetOrCreateFlowAnalyzer(scope);
                    var flowType = flowAnalyzer?.GetTypeAtLocation(varName, expression);
                    if (flowType != null)
                        return flowType;
                }
            }
        }

        // Fallback: type engine semantic inference (direct path)
        return _typeEngine?.InferSemanticType(expression);
    }

    /// <summary>
    /// Gets the inferred type for an expression.
    /// Uses flow-sensitive analysis when available.
    /// </summary>
    public GDSemanticType? GetExpressionType(GDExpression? expression)
    {
        if (expression == null)
            return null;

        // For identifier expressions, flow analysis takes priority over cache
        if (expression is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var scope = expression.GetContainingMethodScope();
                if (scope != null)
                {
                    var flowAnalyzer = GetOrCreateFlowAnalyzer(scope);
                    var flowType = flowAnalyzer?.GetTypeAtLocation(varName, expression);
                    if (flowType != null && !flowType.IsVariant)
                    {
                        // For untyped containers (Array, Dictionary), check if container profile has refined element types
                        if (GDWellKnownTypes.IsContainerType(flowType.DisplayName))
                        {
                            var containerType = _containerTypeService.GetInferredContainerType(varName);
                            if (containerType != null && containerType.HasElementTypes)
                                return GDSemanticType.FromRuntimeTypeName(containerType.ToString());
                        }
                        // Signal with signature → return base "Signal"
                        if (flowType.IsSignal)
                            return GDSemanticType.FromRuntimeTypeName("Signal");
                        return flowType;
                    }
                }

                // Container usage profile fallback when flow has no data
                var containerFallback = _containerTypeService.GetInferredContainerType(varName);
                if (containerFallback != null && containerFallback.HasElementTypes)
                {
                    return GDSemanticType.FromRuntimeTypeName(containerFallback.ToString());
                }

                // Call-site injected types for parameters — fallback when flow has no data
                var symbol = _findSymbol?.Invoke(varName);
                if (symbol?.Kind == GDSymbolKind.Parameter)
                {
                    var unionType = _unionTypeService.GetUnionType(varName, symbol, null, excludeTypeGuards: true);
                    if (unionType != null && unionType.IsSingleType)
                    {
                        var effectiveType = unionType.EffectiveType;
                        if (!effectiveType.IsVariant && !effectiveType.IsNull)
                            return effectiveType;
                    }
                }
            }
        }

        // For array addition, skip cache and compute union type
        if (expression is GDDualOperatorExpression dualOp &&
            dualOp.Operator?.OperatorType == GDDualOperatorType.Addition)
        {
            var leftContainer = GetContainerTypeForExpression(dualOp.LeftExpression);
            var rightContainer = GetContainerTypeForExpression(dualOp.RightExpression);

            if (leftContainer != null && rightContainer != null &&
                !leftContainer.IsDictionary && !rightContainer.IsDictionary)
            {
                var combined = GDContainerElementType.CombineArrays(leftContainer, rightContainer);
                if (combined != null)
                    return GDSemanticType.FromRuntimeTypeName(combined.ToString());
            }
        }

        // Check cache — skip for member expressions that reference methods (not direct calls),
        // because cache stores return type but we need Callable signature
        bool skipCache = expression is GDMemberOperatorExpression skipMemberExpr
            && !(skipMemberExpr.Parent is GDCallExpression skipCall
                && skipCall.CallerExpression == skipMemberExpr);

        if (!skipCache && _nodeTypes.TryGetValue(expression, out var cachedType))
        {
            return cachedType;
        }

        // Recursion guard
        if (_expressionTypeInProgress.Contains(expression))
            return null;

        if (_expressionTypeInProgress.Count >= MaxExpressionTypeRecursionDepth)
            return null;

        _expressionTypeInProgress.Add(expression);
        try
        {
            return GetExpressionTypeCore(expression);
        }
        finally
        {
            _expressionTypeInProgress.Remove(expression);
        }
    }

    /// <summary>
    /// Core implementation of GetExpressionType without recursion guard.
    /// </summary>
    private GDSemanticType? GetExpressionTypeCore(GDExpression expression)
    {
        // For identifier expressions
        if (expression is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var scope = expression.GetContainingMethodScope();
                if (scope != null)
                {
                    var flowAnalyzer = GetOrCreateFlowAnalyzer(scope);
                    var flowType = flowAnalyzer?.GetTypeAtLocation(varName, expression);
                    if (flowType != null && !flowType.IsVariant)
                    {
                        // For untyped containers, check if container profile has refined element types
                        if (GDWellKnownTypes.IsContainerType(flowType.DisplayName))
                        {
                            var containerType = _containerTypeService.GetInferredContainerType(varName);
                            if (containerType != null && containerType.HasElementTypes)
                                return GDSemanticType.FromRuntimeTypeName(containerType.ToString());
                        }
                        // Signal with signature → return base "Signal"
                        if (flowType.IsSignal)
                            return GDSemanticType.FromRuntimeTypeName("Signal");
                        return flowType;
                    }
                }

                // Container profile fallback when flow has no data
                var containerFallback = _containerTypeService.GetInferredContainerType(varName);
                if (containerFallback != null && containerFallback.HasElementTypes)
                    return GDSemanticType.FromRuntimeTypeName(containerFallback.ToString());

                // Fall back to union type (exclude type guards — they are location-specific narrowing, not global type)
                var symbol = _findSymbol?.Invoke(varName);
                var unionType = _unionTypeService.GetUnionType(varName, symbol, null, excludeTypeGuards: true);
                if (unionType != null && unionType.IsSingleType)
                {
                    var effectiveType = unionType.EffectiveType;
                    if (!effectiveType.IsVariant && !effectiveType.IsNull)
                        return effectiveType;
                }

                // Check if method reference
                if (symbol?.Kind == GDSymbolKind.Method)
                    return GDSemanticType.FromRuntimeTypeName("Callable");

                // Enum type reference (e.g., AIState in AIState.PATROL)
                if (symbol?.Kind == GDSymbolKind.Enum)
                    return GDSemanticType.FromRuntimeTypeName(symbol.Name);

                // Class name identifier represents the class type itself
                if (symbol?.Kind == GDSymbolKind.Class)
                    return GDSemanticType.FromRuntimeTypeName(symbol.Name);

                // For class members with explicit type annotation, return the declared type
                if (symbol != null && symbol.Kind != GDSymbolKind.Method
                    && !string.IsNullOrEmpty(symbol.TypeName))
                {
                    var declaredType = GDSemanticType.FromRuntimeTypeName(symbol.TypeName);
                    if (!declaredType.IsVariant)
                        return declaredType;
                }

                // Fall back to initializer
                if (scope != null)
                {
                    if (symbol?.DeclarationNode is GDVariableDeclarationStatement localVarDecl &&
                        localVarDecl.Initializer != null &&
                        !(GetOrCreateFlowAnalyzer(scope)?.HasReassignment(varName) ?? false))
                    {
                        var initType = GetExpressionType(localVarDecl.Initializer);
                        if (initType != null && !initType.IsVariant)
                            return initType;
                    }
                }
            }
        }

        // For member access
        if (expression is GDMemberOperatorExpression memberExpr)
        {
            var callerType = GetExpressionType(memberExpr.CallerExpression);
            var memberName = memberExpr.Identifier?.Sequence;

            if (callerType != null && !callerType.IsVariant &&
                !string.IsNullOrEmpty(memberName))
            {
                var callerTypeName = callerType.DisplayName;

                // Check if this is a local enum value access (e.g., AIState.IDLE)
                if (_localTypeService != null && _localTypeService.IsLocalEnum(callerTypeName))
                {
                    if (_localTypeService.IsLocalEnumValue(callerTypeName, memberName))
                        return callerType;
                }

                // Fall back to runtime provider for other member access
                if (_runtimeProvider != null)
                {
                    var memberInfo = FindMemberWithInheritance(callerTypeName, memberName);
                    if (memberInfo != null)
                    {
                        if (memberInfo.Kind == GDRuntimeMemberKind.Method)
                        {
                            bool isCalledDirectly = memberExpr.Parent is GDCallExpression call
                                && call.CallerExpression == memberExpr;
                            if (!isCalledDirectly)
                                return GDSemanticType.FromRuntimeTypeName(BuildCallableSignatureString(memberInfo));
                        }
                        return !string.IsNullOrEmpty(memberInfo.Type)
                            ? GDSemanticType.FromRuntimeTypeName(memberInfo.Type)
                            : null;
                    }
                }
            }
        }

        // For call expressions
        if (expression is GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is GDMemberOperatorExpression callMemberExpr)
            {
                var methodName = callMemberExpr.Identifier?.Sequence;
                if (methodName == GDWellKnownFunctions.Constructor)
                {
                    var callerType = GetExpressionType(callMemberExpr.CallerExpression);
                    if (callerType != null)
                        return callerType;
                }
            }

            // Type engine first — handles generics (Array[T].front() -> T), special methods, etc.
            var callResult = _typeEngine?.InferSemanticType(expression);
            if (callResult != null && !callResult.IsVariant)
                return callResult;

            // Flow-aware fallback: when type engine returns Variant (can't resolve receiver),
            // use flow-narrowed receiver type to look up method return type.
            // Handles: result.duplicate() inside `elif result is Dictionary:`
            if (callExpr.CallerExpression is GDMemberOperatorExpression fallbackMemberExpr)
            {
                var methodName = fallbackMemberExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(methodName))
                {
                    var receiverType = GetExpressionType(fallbackMemberExpr.CallerExpression);
                    if (receiverType != null && !receiverType.IsVariant)
                    {
                        var memberInfo = FindMemberWithInheritance(receiverType.DisplayName, methodName);
                        if (memberInfo != null && memberInfo.Kind == GDRuntimeMemberKind.Method
                            && !string.IsNullOrEmpty(memberInfo.Type))
                            return GDSemanticType.FromRuntimeTypeName(memberInfo.Type);
                    }
                }
            }
        }

        // For binary operators
        if (expression is GDDualOperatorExpression dualOp)
        {
            var opType = dualOp.Operator?.OperatorType;

            if (opType == GDDualOperatorType.As)
            {
                return _typeEngine?.InferSemanticType(expression);
            }

            var leftType = GetExpressionType(dualOp.LeftExpression);
            var rightType = GetExpressionType(dualOp.RightExpression);

            if (opType.HasValue)
            {
                var resultType = GDOperatorTypeResolver.ResolveOperatorType(opType.Value, leftType?.DisplayName, rightType?.DisplayName);
                if (!string.IsNullOrEmpty(resultType))
                    return GDSemanticType.FromRuntimeTypeName(resultType);

                if (opType.Value == GDDualOperatorType.Addition)
                {
                    var leftContainer = GetContainerTypeForExpression(dualOp.LeftExpression);
                    var rightContainer = GetContainerTypeForExpression(dualOp.RightExpression);

                    if (leftContainer != null && rightContainer != null &&
                        !leftContainer.IsDictionary && !rightContainer.IsDictionary)
                    {
                        var combined = GDContainerElementType.CombineArrays(leftContainer, rightContainer);
                        if (combined != null)
                            return GDSemanticType.FromRuntimeTypeName(combined.ToString());
                    }
                }
            }
        }

        // For unary operators
        if (expression is GDSingleOperatorExpression singleOp)
        {
            var operandType = GetExpressionType(singleOp.TargetExpression);
            var opType = singleOp.Operator?.OperatorType;

            if (opType.HasValue)
            {
                var resultType = GDOperatorTypeResolver.ResolveSingleOperatorType(opType.Value, operandType?.DisplayName);
                if (!string.IsNullOrEmpty(resultType))
                    return GDSemanticType.FromRuntimeTypeName(resultType);
            }
        }

        // For indexer expressions
        if (expression is GDIndexerExpression indexerExpr)
        {
            var typeNode = GetTypeNodeForExpression(indexerExpr);
            if (typeNode != null)
            {
                var typeName = typeNode.BuildName();
                if (!string.IsNullOrEmpty(typeName))
                {
                    var semType = GDSemanticType.FromRuntimeTypeName(typeName);
                    if (!semType.IsVariant)
                        return semType;
                }
            }

            var varName = GetRootVariableName(indexerExpr.CallerExpression);
            if (!string.IsNullOrEmpty(varName))
            {
                var containerType = _containerTypeService.GetInferredContainerType(varName);
                if (containerType != null && containerType.HasElementTypes)
                {
                    var elementType = containerType.EffectiveElementType;
                    if (!elementType.IsVariant)
                        return elementType;
                }
            }
        }

        // For array initializers
        if (expression is Reader.GDArrayInitializerExpression arrayInit && _typeEngine != null)
        {
            var unionType = ComputeArrayInitializerUnionType(arrayInit);
            if (unionType != null)
                return unionType;
        }

        return _typeEngine?.InferSemanticType(expression);
    }

    /// <summary>
    /// Gets expression type without using flow analysis (to avoid recursion).
    /// </summary>
    public GDSemanticType? GetExpressionTypeWithoutFlow(GDExpression? expression)
    {
        if (expression == null)
            return null;

        if (expression is GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is GDMemberOperatorExpression callMemberExpr)
            {
                var methodName = callMemberExpr.Identifier?.Sequence;
                if (methodName == GDWellKnownFunctions.Constructor)
                {
                    var callerType = GetExpressionTypeWithoutFlow(callMemberExpr.CallerExpression);
                    if (callerType != null)
                        return callerType;
                }
            }

            var callResult = _typeEngine?.InferSemanticType(expression);
            if (callResult != null && !callResult.IsVariant)
                return callResult;
        }

        if (expression is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var symbol = _findSymbol?.Invoke(varName);
                if (symbol?.Kind == GDSymbolKind.Method)
                {
                    return GDSemanticType.FromRuntimeTypeName("Callable");
                }

                // Class name identifier represents the class type itself,
                // not an instance of the base class (TypeName stores extends type)
                if (symbol?.Kind == GDSymbolKind.Class)
                {
                    return GDSemanticType.FromRuntimeTypeName(symbol.Name);
                }

                var unionType = _unionTypeService.GetUnionType(varName, symbol, null);
                if (unionType != null && unionType.IsSingleType)
                {
                    var effectiveType = unionType.EffectiveType;
                    if (!effectiveType.IsVariant && !effectiveType.IsNull)
                    {
                        return effectiveType;
                    }
                }
            }
        }

        return _typeEngine?.InferSemanticType(expression);
    }

    /// <summary>
    /// Resolves the type of a standalone expression (parsed from text, not from file AST).
    /// Use this for completion context and similar scenarios where expression is not part of the file tree.
    /// Unlike GetExpressionType(), this method does not require the expression to be part of the AST.
    /// Internally uses TypeResolver as fallback for complex expressions (member access, Godot API, etc.).
    /// </summary>
    /// <param name="expression">The parsed expression</param>
    /// <returns>Type resolution result with type name and source info</returns>
    public GDTypeResolutionResult ResolveStandaloneExpression(GDExpression expression)
    {
        if (expression == null)
            return GDTypeResolutionResult.Unknown();

        // 1. Try simple identifier lookup first (from file symbols)
        if (expression is GDIdentifierExpression identExpr)
        {
            var name = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name))
            {
                var symbol = _findSymbol?.Invoke(name);
                if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName))
                {
                    return new GDTypeResolutionResult
                    {
                        TypeName = GDSemanticType.FromRuntimeTypeName(symbol.TypeName),
                        IsResolved = true,
                        Source = GDTypeSource.Project
                    };
                }
            }
        }

        // 2. Delegate to TypeEngine for complex expressions (member access, calls, etc.)
        if (_typeEngine != null)
        {
            var semanticType = _typeEngine.InferSemanticType(expression);
            if (semanticType != null && !semanticType.IsVariant)
            {
                return new GDTypeResolutionResult
                {
                    TypeName = semanticType,
                    IsResolved = true,
                    Source = GDTypeSource.Inferred
                };
            }
        }

        // 3. Fallback: create a fresh TypeInferenceEngine without scope
        // This handles expressions that rely purely on runtime provider (Godot API, static types)
        // without needing local variable scope from the file
        if (_runtimeProvider != null)
        {
            var freshEngine = new GDTypeInferenceEngine(_runtimeProvider);
            var semanticType = freshEngine.InferSemanticType(expression);
            if (semanticType != null && !semanticType.IsVariant)
            {
                return new GDTypeResolutionResult
                {
                    TypeName = semanticType,
                    IsResolved = true,
                    Source = GDTypeSource.Inferred
                };
            }
        }

        return GDTypeResolutionResult.Unknown();
    }

    /// <summary>
    /// Gets the type node for an expression (with generics).
    /// </summary>
    public GDTypeNode? GetTypeNodeForExpression(GDExpression? expression)
    {
        if (expression == null)
            return null;

        if (_nodeTypeNodes.TryGetValue(expression, out var typeNode))
            return typeNode;

        // For identifiers, try to resolve through symbol registry
        if (expression is GDIdentifierExpression identExpr)
        {
            var identName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(identName))
            {
                var symbol = _findSymbol?.Invoke(identName);
                if (symbol != null)
                {
                    if (symbol.TypeNode != null)
                        return symbol.TypeNode;
                    if (!string.IsNullOrEmpty(symbol.TypeName))
                        return CreateSimpleType(symbol.TypeName);
                }
            }
        }

        // For indexer expressions, infer from caller type
        if (expression is GDIndexerExpression indexerExpr)
        {
            var callerTypeNode = GetTypeNodeForExpression(indexerExpr.CallerExpression);
            if (callerTypeNode != null)
            {
                if (callerTypeNode is GDArrayTypeNode arrayType)
                    return arrayType.InnerType;
                if (callerTypeNode is GDDictionaryTypeNode dictType)
                    return dictType.ValueType;
            }
        }

        return _typeEngine?.GetTypeNodeForNode(expression);
    }

    /// <summary>
    /// Creates a simple single type node.
    /// </summary>
    private static GDTypeNode CreateSimpleType(string typeName)
    {
        return new GDSingleTypeNode { Type = new GDType { Sequence = typeName } };
    }

    private GDFlowAnalyzer? GetOrCreateFlowAnalyzer(GDNode methodScope)
    {
        if (methodScope == null)
            return null;

        return _flowRegistry.GetOrCreateFlowAnalyzer(
            methodScope,
            this,
            _getOnreadyVariables ?? (() => Enumerable.Empty<string>()),
            _filePath);
    }

    private GDRuntimeMemberInfo? FindMemberWithInheritance(string? typeName, string? memberName)
        => _memberResolver.FindMember(typeName, memberName);

    private static string BuildCallableSignatureString(GDRuntimeMemberInfo memberInfo)
    {
        var paramParts = new List<string>();
        if (memberInfo.Parameters != null)
        {
            foreach (var param in memberInfo.Parameters)
                paramParts.Add(!string.IsNullOrEmpty(param.Type) ? param.Type : "Variant");
        }
        else if (memberInfo.MaxArgs > 0 || memberInfo.IsVarArgs)
        {
            for (int i = 0; i < Math.Max(memberInfo.MinArgs, 1); i++)
                paramParts.Add("Variant");
        }

        var paramsStr = string.Join(", ", paramParts);
        if (memberInfo.IsVarArgs && !string.IsNullOrEmpty(paramsStr))
            paramsStr += "...";
        else if (memberInfo.IsVarArgs)
            paramsStr = "Variant...";

        var returnStr = "";
        if (!string.IsNullOrEmpty(memberInfo.Type)
            && memberInfo.Type != "void"
            && !GDSemanticType.FromRuntimeTypeName(memberInfo.Type).IsVariant)
        {
            returnStr = $" -> {memberInfo.Type}";
        }

        return $"Callable({paramsStr}){returnStr}";
    }

    private GDContainerElementType? GetContainerTypeForExpression(GDExpression? expr)
    {
        if (expr == null)
            return null;

        // 1. Recursive handling of Array Addition
        if (expr is GDDualOperatorExpression dualOp &&
            dualOp.Operator?.OperatorType == GDDualOperatorType.Addition)
        {
            var left = GetContainerTypeForExpression(dualOp.LeftExpression);
            var right = GetContainerTypeForExpression(dualOp.RightExpression);
            if (left != null && right != null &&
                !left.IsDictionary && !right.IsDictionary)
            {
                return GDContainerElementType.CombineArrays(left, right);
            }
        }

        // 2. Identifiers - container profile + local variables
        if (expr is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                // Container profile (for variables with usage-based inference)
                var containerType = _containerTypeService.GetInferredContainerType(varName);
                if (containerType != null && containerType.HasElementTypes)
                    return containerType;

                // Find local variable declaration with type annotation
                var varDecl = GDFlowQueryService.FindLocalVariableDeclaration(identExpr, varName);
                if (varDecl != null)
                {
                    if (varDecl.Type != null)
                    {
                        var typeFromAnnotation = GDContainerElementType.FromTypeNode(varDecl.Type);
                        if (typeFromAnnotation != null)
                            return typeFromAnnotation;
                    }
                    if (varDecl.Initializer != null)
                    {
                        return GetContainerTypeForExpression(varDecl.Initializer);
                    }
                }
            }
        }

        // 3. Fallback to TypeEngine
        var typeNode = _typeEngine?.InferTypeNode(expr);
        return GDContainerElementType.FromTypeNode(typeNode);
    }

    private string? GetRootVariableName(GDExpression? expr)
    {
        while (expr != null)
        {
            if (expr is GDIdentifierExpression identExpr)
                return identExpr.Identifier?.Sequence;

            if (expr is GDMemberOperatorExpression memberOp)
            {
                expr = memberOp.CallerExpression;
                continue;
            }

            if (expr is GDIndexerExpression indexerExpr)
            {
                expr = indexerExpr.CallerExpression;
                continue;
            }

            break;
        }
        return null;
    }

    private GDSemanticType? ComputeArrayInitializerUnionType(Reader.GDArrayInitializerExpression arrayInit)
    {
        if (arrayInit.Values == null || !arrayInit.Values.Any())
            return GDSemanticType.FromRuntimeTypeName("Array");

        var elementTypes = new HashSet<string>();
        foreach (var element in arrayInit.Values)
        {
            var elementType = GetExpressionType(element);
            if (elementType != null && !elementType.IsVariant)
            {
                elementTypes.Add(elementType.DisplayName);
            }
        }

        if (elementTypes.Count == 0)
            return GDSemanticType.FromRuntimeTypeName("Array");

        if (elementTypes.Count == 1)
            return GDSemanticType.FromRuntimeTypeName($"Array[{elementTypes.First()}]");

        var sortedTypes = elementTypes.OrderBy(t => t).ToList();
        return GDSemanticType.FromRuntimeTypeName($"Array[{string.Join("|", sortedTypes)}]");
    }

    #region Semantic Type Resolution

    /// <summary>
    /// Gets a rich GDSemanticType for an expression, bypassing string serialization.
    /// Currently handles global function identifiers to produce generic Callable types.
    /// </summary>
    public GDSemanticType? GetSemanticType(GDExpression? expression)
    {
        if (expression is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var funcInfo = _runtimeProvider?.GetGlobalFunction(varName);
                if (funcInfo != null)
                    return BuildCallableFromFunctionInfo(funcInfo);
            }
        }
        return null;
    }

    /// <summary>
    /// Builds a GDCallableSemanticType from GDRuntimeFunctionInfo.
    /// When overloads exist, analyzes type dependencies across parameter positions
    /// to produce generic type variables with constraints (e.g., Callable&lt;T: int | float&gt;(T) -> T).
    /// </summary>
    private static GDCallableSemanticType BuildCallableFromFunctionInfo(GDRuntimeFunctionInfo funcInfo)
    {
        var overloads = funcInfo.Overloads;

        // No overloads or single overload → simple Callable
        if (overloads == null || overloads.Count <= 1)
        {
            return BuildSimpleCallable(funcInfo);
        }

        // Multiple overloads → analyze for type variable patterns
        return BuildGenericCallable(funcInfo, overloads);
    }

    private static GDCallableSemanticType BuildSimpleCallable(GDRuntimeFunctionInfo funcInfo)
    {
        var paramTypes = BuildParameterSemanticTypes(funcInfo.Parameters);
        var returnType = GDSemanticType.FromRuntimeTypeName(funcInfo.ReturnType ?? "Variant");

        return new GDCallableSemanticType(
            returnType: returnType,
            parameterTypes: paramTypes,
            isVarArgs: funcInfo.IsVarArgs);
    }

    private static GDCallableSemanticType BuildGenericCallable(
        GDRuntimeFunctionInfo funcInfo,
        IReadOnlyList<GDRuntimeFunctionOverload> overloads)
    {
        // Determine max parameter count across overloads
        int maxParams = 0;
        foreach (var ov in overloads)
        {
            var count = ov.Parameters?.Count ?? 0;
            if (count > maxParams) maxParams = count;
        }

        if (maxParams == 0)
            return BuildSimpleCallable(funcInfo);

        // Collect type sets per parameter position and for return type
        var positionTypeSets = new List<HashSet<string>>(maxParams);
        for (int i = 0; i < maxParams; i++)
            positionTypeSets.Add(new HashSet<string>());

        var returnTypeSet = new HashSet<string>();

        foreach (var ov in overloads)
        {
            var ovParams = ov.Parameters;
            if (ovParams != null)
            {
                for (int i = 0; i < ovParams.Count && i < maxParams; i++)
                {
                    positionTypeSets[i].Add(ovParams[i].Type ?? "Variant");
                }
            }
            returnTypeSet.Add(ov.ReturnType ?? "Variant");
        }

        // Group positions by their type set (same set = same type variable)
        // Key: sorted type set string, Value: list of positions
        var groups = new Dictionary<string, List<int>>();
        var positionKeys = new string[maxParams];

        for (int i = 0; i < maxParams; i++)
        {
            var key = string.Join(",", positionTypeSets[i].OrderBy(t => t));
            positionKeys[i] = key;
            if (!groups.ContainsKey(key))
                groups[key] = new List<int>();
            groups[key].Add(i);
        }

        // Check if return type matches any group
        var returnKey = string.Join(",", returnTypeSet.OrderBy(t => t));

        // Only create type variables for positions that vary across overloads
        // (positions with a single type are concrete)
        var typeVarNames = new Dictionary<string, GDTypeVariableSemanticType>();
        int typeVarIndex = 0;

        foreach (var kvp in groups)
        {
            var typeSet = positionTypeSets[kvp.Value[0]];
            if (typeSet.Count <= 1)
                continue; // Concrete type, no type variable needed

            var varName = typeVarIndex == 0 ? "T" : $"T{typeVarIndex + 1}";
            typeVarIndex++;

            var constraintTypes = typeSet
                .Where(t => t != "Variant")
                .OrderBy(t => t)
                .Select(t => GDSemanticType.FromRuntimeTypeName(t))
                .ToList();

            GDSemanticType? constraint = constraintTypes.Count switch
            {
                0 => null,
                1 => constraintTypes[0],
                _ => new GDUnionSemanticType(constraintTypes)
            };

            typeVarNames[kvp.Key] = new GDTypeVariableSemanticType(varName, constraint);
        }

        // Build parameter types
        var parameterTypes = new List<GDSemanticType>(maxParams);
        for (int i = 0; i < maxParams; i++)
        {
            if (typeVarNames.TryGetValue(positionKeys[i], out var typeVar))
            {
                parameterTypes.Add(typeVar);
            }
            else
            {
                // Single concrete type
                var singleType = positionTypeSets[i].FirstOrDefault() ?? "Variant";
                parameterTypes.Add(GDSemanticType.FromRuntimeTypeName(singleType));
            }
        }

        // Build return type
        GDSemanticType returnType;
        if (typeVarNames.TryGetValue(returnKey, out var returnTypeVar))
        {
            returnType = returnTypeVar;
        }
        else if (returnTypeSet.Count == 1)
        {
            returnType = GDSemanticType.FromRuntimeTypeName(returnTypeSet.First());
        }
        else
        {
            var returnSemanticTypes = returnTypeSet
                .Where(t => t != "Variant")
                .OrderBy(t => t)
                .Select(t => GDSemanticType.FromRuntimeTypeName(t))
                .ToList();

            returnType = returnSemanticTypes.Count switch
            {
                0 => GDVariantSemanticType.Instance,
                1 => returnSemanticTypes[0],
                _ => new GDUnionSemanticType(returnSemanticTypes)
            };
        }

        // Collect all unique type variables
        var allTypeVars = typeVarNames.Values
            .GroupBy(tv => tv.VariableName)
            .Select(g => g.First())
            .ToList();

        return new GDCallableSemanticType(
            returnType: returnType,
            parameterTypes: parameterTypes,
            isVarArgs: funcInfo.IsVarArgs,
            typeParameters: allTypeVars.Count > 0 ? allTypeVars : null);
    }

    private static IReadOnlyList<GDSemanticType>? BuildParameterSemanticTypes(
        IReadOnlyList<GDRuntimeParameterInfo>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return null;

        return parameters
            .Select(p => GDSemanticType.FromRuntimeTypeName(p.Type ?? "Variant"))
            .ToList();
    }

    #endregion

    #region Parameter Type Inference

    /// <summary>
    /// Infers the type for a parameter based on its usage within the method.
    /// Returns Variant if cannot infer.
    /// </summary>
    public GDInferredParameterType InferParameterType(GDParameterDeclaration param)
    {
        if (param == null)
            return GDInferredParameterType.Unknown("");

        var paramName = param.Identifier?.Sequence;
        if (string.IsNullOrEmpty(paramName))
            return GDInferredParameterType.Unknown("");

        // If parameter has explicit type annotation, return it
        var typeNode = param.Type;
        var typeName = typeNode?.BuildName();
        if (!string.IsNullOrEmpty(typeName))
            return GDInferredParameterType.Declared(paramName, typeName);

        var method = param.GetContainingMethod();
        if (method == null)
            return GDInferredParameterType.Unknown(paramName);

        var methodName = method.Identifier?.Sequence;

        // Check call-site data first (higher precision than duck-typing)
        if (!string.IsNullOrEmpty(methodName))
        {
            var callSiteTypes = _unionTypeService.GetCallSiteTypes(methodName, paramName);
            if (callSiteTypes != null && !callSiteTypes.IsEmpty)
            {
                var reports = _unionTypeService.GetCallSiteArgumentReports(methodName, paramName);
                var evidence = reports?.Select(GDCallSiteEvidence.FromReport).ToList();

                // Downgrade confidence if all evidence is from literals only
                var callSiteConfidence = GDTypeConfidence.High;
                if (evidence != null && evidence.Count > 0 &&
                    evidence.All(ev => ev.Provenance == GDTypeProvenance.Literal))
                    callSiteConfidence = GDTypeConfidence.Medium;

                if (callSiteTypes.IsSingleType)
                {
                    return GDInferredParameterType.FromCallSite(
                        paramName,
                        callSiteTypes.EffectiveType.DisplayName,
                        "cross-method call sites",
                        callSiteConfidence,
                        evidence);
                }

                var types = callSiteTypes.Types.Select(t => t.DisplayName).ToList();
                var result = GDInferredParameterType.Union(
                    paramName,
                    types,
                    callSiteConfidence,
                    "cross-method call sites",
                    evidence);

                if (evidence != null && evidence.Count > 1 && _runtimeProvider != null)
                {
                    var hint = ComputeNarrowingHint(evidence, types);
                    if (hint != null)
                        result.NarrowingHint = hint;
                }

                return result;
            }
        }

        // Fallback to duck-typing analysis
        var constraints = GDParameterUsageAnalyzer.AnalyzeMethod(method, _runtimeProvider);
        if (!constraints.TryGetValue(paramName, out var paramConstraints) || !paramConstraints.HasConstraints)
            return GDInferredParameterType.Unknown(paramName);

        // Resolve constraints to type
        var resolver = new GDParameterTypeResolver(_runtimeProvider ?? GDGodotTypesProvider.Shared);
        return resolver.ResolveFromConstraints(paramConstraints);
    }

    private GDUnionNarrowingHint? ComputeNarrowingHint(
        List<GDCallSiteEvidence> evidence, List<string> unionTypes)
    {
        if (unionTypes.Count < 2 || _runtimeProvider == null)
            return null;

        var annotated = evidence
            .Where(e => e.Provenance == GDTypeProvenance.ExplicitAnnotation && e.InferredType != null)
            .ToList();

        if (annotated.Count == 0)
            return null;

        foreach (var ann in annotated)
        {
            var widerType = ann.InferredType!;
            var otherTypes = unionTypes.Where(t => t != widerType).ToList();

            if (otherTypes.Count == 0)
                continue;

            // Check if all other types are subtypes of the wider type
            bool allSubtypes = otherTypes.All(t =>
                _runtimeProvider.IsAssignableTo(t, widerType));

            if (allSubtypes && otherTypes.Count == 1)
            {
                return new GDUnionNarrowingHint
                {
                    WiderType = widerType,
                    NarrowType = otherTypes[0],
                    SourceVariable = ann.SourceVariableName ?? ann.ArgumentExpression,
                    SourceFilePath = ann.FilePath,
                    SourceLine = ann.Line
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the duck typing constraints for a parameter.
    /// Returns null if the parameter has no usage constraints.
    /// </summary>
    public GDParameterConstraints? GetParameterConstraints(GDParameterDeclaration param)
    {
        if (param == null)
            return null;

        var paramName = param.Identifier?.Sequence;
        if (string.IsNullOrEmpty(paramName))
            return null;

        var method = param.GetContainingMethod();
        if (method == null)
            return null;

        // Analyze parameter usage
        var constraints = GDParameterUsageAnalyzer.AnalyzeMethod(method, _runtimeProvider);
        return constraints.TryGetValue(paramName, out var paramConstraints) ? paramConstraints : null;
    }

    /// <summary>
    /// Infers parameter types for all parameters of a method.
    /// </summary>
    public IReadOnlyDictionary<string, GDInferredParameterType> InferParameterTypes(GDMethodDeclaration method)
    {
        var result = new Dictionary<string, GDInferredParameterType>();

        if (method?.Parameters == null)
            return result;

        // Analyze all parameter usage at once
        var constraints = GDParameterUsageAnalyzer.AnalyzeMethod(method, _runtimeProvider);
        var resolver = new GDParameterTypeResolver(_runtimeProvider ?? GDGodotTypesProvider.Shared);

        foreach (var param in method.Parameters)
        {
            var paramName = param.Identifier?.Sequence;
            if (string.IsNullOrEmpty(paramName))
                continue;

            // Check for explicit type annotation first
            var typeNode = param.Type;
            var typeName = typeNode?.BuildName();
            if (!string.IsNullOrEmpty(typeName))
            {
                result[paramName] = GDInferredParameterType.Declared(paramName, typeName);
                continue;
            }

            // Try to resolve from constraints
            if (constraints.TryGetValue(paramName, out var paramConstraints) && paramConstraints.HasConstraints)
            {
                result[paramName] = resolver.ResolveFromConstraints(paramConstraints);
            }
            else
            {
                result[paramName] = GDInferredParameterType.Unknown(paramName);
            }
        }

        return result;
    }

    /// <summary>
    /// Finds the containing method for a parameter declaration.
    /// </summary>
    #endregion
}
