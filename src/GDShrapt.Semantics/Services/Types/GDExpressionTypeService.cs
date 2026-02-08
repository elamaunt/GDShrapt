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
internal class GDExpressionTypeService
{
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly GDTypeInferenceEngine? _typeEngine;
    private readonly GDContainerTypeService _containerTypeService;
    private readonly GDUnionTypeService _unionTypeService;
    private readonly GDDuckTypeService _duckTypeService;
    private readonly GDFlowAnalysisRegistry _flowRegistry;
    private readonly Dictionary<GDNode, string> _nodeTypes;
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

    internal GDExpressionTypeService(
        IGDRuntimeProvider? runtimeProvider,
        GDTypeInferenceEngine? typeEngine,
        GDContainerTypeService containerTypeService,
        GDUnionTypeService unionTypeService,
        GDDuckTypeService duckTypeService,
        GDFlowAnalysisRegistry flowRegistry,
        Dictionary<GDNode, string> nodeTypes,
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

    /// <summary>
    /// Gets the inferred type for an expression.
    /// Uses flow-sensitive analysis when available.
    /// </summary>
    public string? GetExpressionType(GDExpression? expression)
    {
        if (expression == null)
            return null;

        // For identifier expressions, flow analysis takes priority over cache
        if (expression is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                // Check container usage profile FIRST for untyped containers
                var containerType = _containerTypeService.GetInferredContainerType(varName);
                if (containerType != null && containerType.HasElementTypes)
                {
                    return containerType.ToString();
                }

                var method = FindContainingMethodNode(expression);
                if (method != null)
                {
                    var flowAnalyzer = GetOrCreateFlowAnalyzer(method);
                    var flowType = flowAnalyzer?.GetTypeAtLocation(varName, expression);
                    if (flowType != null && !flowType.IsVariant)
                        return flowType.DisplayName;
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
                    return combined.ToString();
            }
        }

        // Check cache
        if (_nodeTypes.TryGetValue(expression, out var cachedType))
            return cachedType;

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
    private string? GetExpressionTypeCore(GDExpression expression)
    {
        // For identifier expressions
        if (expression is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var method = FindContainingMethodNode(expression);
                if (method != null)
                {
                    var flowAnalyzer = GetOrCreateFlowAnalyzer(method);
                    var flowType = flowAnalyzer?.GetTypeAtLocation(varName, expression);
                    if (flowType != null && !flowType.IsVariant)
                        return flowType.DisplayName;
                }

                // Fall back to narrowing
                var narrowed = _duckTypeService.GetNarrowedType(varName, expression);
                if (!string.IsNullOrEmpty(narrowed))
                    return narrowed;

                // Fall back to union type
                var symbol = _findSymbol?.Invoke(varName);
                var unionType = _unionTypeService.GetUnionType(varName, symbol, null);
                if (unionType != null && unionType.IsSingleType)
                {
                    var effectiveType = unionType.EffectiveType;
                    if (!effectiveType.IsVariant && effectiveType.DisplayName != "null")
                        return effectiveType.DisplayName;
                }

                // Check if method reference
                if (symbol?.Kind == GDSymbolKind.Method)
                    return "Callable";

                // Fall back to initializer
                if (method != null)
                {
                    if (symbol?.DeclarationNode is GDVariableDeclarationStatement localVarDecl &&
                        localVarDecl.Initializer != null &&
                        !HasLocalReassignments(method, varName))
                    {
                        var initType = GetExpressionType(localVarDecl.Initializer);
                        if (!string.IsNullOrEmpty(initType) && initType != "Variant")
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

            if (!string.IsNullOrEmpty(callerType) && callerType != "Variant" &&
                !string.IsNullOrEmpty(memberName))
            {
                // Check if this is a local enum value access (e.g., AIState.IDLE)
                if (_localTypeService != null && _localTypeService.IsLocalEnum(callerType))
                {
                    if (_localTypeService.IsLocalEnumValue(callerType, memberName))
                        return "int"; // Enum values are always int in GDScript
                }

                // Fall back to runtime provider for other member access
                if (_runtimeProvider != null)
                {
                    var memberInfo = FindMemberWithInheritance(callerType, memberName);
                    if (memberInfo != null)
                        return memberInfo.Type;
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
                    if (!string.IsNullOrEmpty(callerType))
                        return callerType;
                }
            }

            var callResult = _typeEngine?.InferSemanticType(expression)?.DisplayName;
            if (!string.IsNullOrEmpty(callResult))
                return callResult;
        }

        // For binary operators
        if (expression is GDDualOperatorExpression dualOp)
        {
            var opType = dualOp.Operator?.OperatorType;

            if (opType == GDDualOperatorType.As)
            {
                return _typeEngine?.InferSemanticType(expression)?.DisplayName;
            }

            var leftType = GetExpressionType(dualOp.LeftExpression);
            var rightType = GetExpressionType(dualOp.RightExpression);

            if (opType.HasValue)
            {
                var resultType = GDOperatorTypeResolver.ResolveOperatorType(opType.Value, leftType, rightType);
                if (!string.IsNullOrEmpty(resultType))
                    return resultType;

                if (opType.Value == GDDualOperatorType.Addition)
                {
                    var leftContainer = GetContainerTypeForExpression(dualOp.LeftExpression);
                    var rightContainer = GetContainerTypeForExpression(dualOp.RightExpression);

                    if (leftContainer != null && rightContainer != null &&
                        !leftContainer.IsDictionary && !rightContainer.IsDictionary)
                    {
                        var combined = GDContainerElementType.CombineArrays(leftContainer, rightContainer);
                        if (combined != null)
                            return combined.ToString();
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
                var resultType = GDOperatorTypeResolver.ResolveSingleOperatorType(opType.Value, operandType);
                if (!string.IsNullOrEmpty(resultType))
                    return resultType;
            }
        }

        // For indexer expressions
        if (expression is GDIndexerExpression indexerExpr)
        {
            var typeNode = GetTypeNodeForExpression(indexerExpr);
            if (typeNode != null)
            {
                var typeName = typeNode.BuildName();
                if (!string.IsNullOrEmpty(typeName) && typeName != "Variant")
                    return typeName;
            }

            var varName = GetRootVariableName(indexerExpr.CallerExpression);
            if (!string.IsNullOrEmpty(varName))
            {
                var containerType = _containerTypeService.GetInferredContainerType(varName);
                if (containerType != null && containerType.HasElementTypes)
                {
                    var elementType = containerType.EffectiveElementType;
                    if (!elementType.IsVariant)
                        return elementType.DisplayName;
                }
            }
        }

        // For array initializers
        if (expression is Reader.GDArrayInitializerExpression arrayInit && _typeEngine != null)
        {
            var unionType = ComputeArrayInitializerUnionType(arrayInit);
            if (!string.IsNullOrEmpty(unionType))
                return unionType;
        }

        return _typeEngine?.InferSemanticType(expression)?.DisplayName;
    }

    /// <summary>
    /// Gets expression type without using flow analysis (to avoid recursion).
    /// </summary>
    public string? GetExpressionTypeWithoutFlow(GDExpression? expression)
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
                    if (!string.IsNullOrEmpty(callerType))
                        return callerType;
                }
            }

            var callResult = _typeEngine?.InferSemanticType(expression)?.DisplayName;
            if (!string.IsNullOrEmpty(callResult))
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
                    return "Callable";
                }

                var unionType = _unionTypeService.GetUnionType(varName, symbol, null);
                if (unionType != null && unionType.IsSingleType)
                {
                    var effectiveType = unionType.EffectiveType;
                    if (!effectiveType.IsVariant && effectiveType.DisplayName != "null")
                    {
                        return effectiveType.DisplayName;
                    }
                }
            }
        }

        return _typeEngine?.InferSemanticType(expression)?.DisplayName;
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

    private GDFlowAnalyzer? GetOrCreateFlowAnalyzer(GDMethodDeclaration method)
    {
        if (method == null)
            return null;

        return _flowRegistry.GetOrCreateFlowAnalyzer(
            method,
            _typeEngine,
            GetExpressionTypeWithoutFlow,
            _getOnreadyVariables ?? (() => Enumerable.Empty<string>()));
    }

    private GDRuntimeMemberInfo? FindMemberWithInheritance(string? typeName, string? memberName)
        => _memberResolver.FindMember(typeName, memberName);

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

    private static GDMethodDeclaration? FindContainingMethodNode(GDNode node)
    {
        var current = node?.Parent;
        while (current != null)
        {
            if (current is GDMethodDeclaration method)
                return method;
            current = current.Parent;
        }
        return null;
    }

    private static bool HasLocalReassignments(GDMethodDeclaration? method, string varName)
    {
        if (method?.Statements == null || string.IsNullOrEmpty(varName))
            return false;

        foreach (var node in method.AllNodes)
        {
            if (node is GDVariableDeclarationStatement)
                continue;

            if (node is GDDualOperatorExpression dualOp &&
                dualOp.Operator?.OperatorType == GDDualOperatorType.Assignment &&
                dualOp.LeftExpression is GDIdentifierExpression leftIdent &&
                leftIdent.Identifier?.Sequence == varName)
            {
                return true;
            }
        }
        return false;
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

    private string? ComputeArrayInitializerUnionType(Reader.GDArrayInitializerExpression arrayInit)
    {
        if (arrayInit.Values == null || !arrayInit.Values.Any())
            return "Array";

        var elementTypes = new HashSet<string>();
        foreach (var element in arrayInit.Values)
        {
            var elementType = GetExpressionType(element);
            if (!string.IsNullOrEmpty(elementType) && elementType != "Variant")
            {
                elementTypes.Add(elementType);
            }
        }

        if (elementTypes.Count == 0)
            return "Array";

        if (elementTypes.Count == 1)
            return $"Array[{elementTypes.First()}]";

        var sortedTypes = elementTypes.OrderBy(t => t).ToList();
        return $"Array[{string.Join("|", sortedTypes)}]";
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

        var method = FindContainingMethod(param);
        if (method == null)
            return GDInferredParameterType.Unknown(paramName);

        // Analyze parameter usage
        var constraints = GDParameterUsageAnalyzer.AnalyzeMethod(method, _runtimeProvider);
        if (!constraints.TryGetValue(paramName, out var paramConstraints) || !paramConstraints.HasConstraints)
            return GDInferredParameterType.Unknown(paramName);

        // Resolve constraints to type
        var resolver = new GDParameterTypeResolver(_runtimeProvider ?? new GDGodotTypesProvider());
        return resolver.ResolveFromConstraints(paramConstraints);
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

        var method = FindContainingMethod(param);
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
        var resolver = new GDParameterTypeResolver(_runtimeProvider ?? new GDGodotTypesProvider());

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
    private static GDMethodDeclaration? FindContainingMethod(GDParameterDeclaration param)
    {
        var current = param.Parent;
        while (current != null)
        {
            if (current is GDMethodDeclaration method)
                return method;
            current = current.Parent;
        }
        return null;
    }

    #endregion
}
