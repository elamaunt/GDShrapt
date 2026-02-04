using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Kind of type usage in code.
/// </summary>
public enum GDTypeUsageKind
{
    /// <summary>
    /// Type annotation (var x: ClassName, func f(x: ClassName)).
    /// </summary>
    TypeAnnotation,

    /// <summary>
    /// Type check (if obj is ClassName).
    /// </summary>
    TypeCheck,

    /// <summary>
    /// Extends declaration (extends ClassName).
    /// </summary>
    Extends
}

/// <summary>
/// Represents a usage of a type in code.
/// </summary>
public class GDTypeUsage
{
    /// <summary>
    /// The type name being used.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// The AST node where the type is used.
    /// </summary>
    public GDNode Node { get; }

    /// <summary>
    /// The kind of usage.
    /// </summary>
    public GDTypeUsageKind Kind { get; }

    /// <summary>
    /// Line number of the usage.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column number of the usage.
    /// </summary>
    public int Column { get; }

    public GDTypeUsage(string typeName, GDNode node, GDTypeUsageKind kind)
    {
        TypeName = typeName;
        Node = node;
        Kind = kind;

        // Extract position from first token
        var token = node.AllTokens.FirstOrDefault();
        Line = token?.StartLine ?? 0;
        Column = token?.StartColumn ?? 0;
    }
}

/// <summary>
/// Unified facade for semantic queries on a single script file.
/// Provides symbol resolution, reference tracking, type inference, and confidence analysis.
/// Implements IGDMemberAccessAnalyzer and IGDArgumentTypeAnalyzer for use with GDValidator.
/// </summary>
public class GDSemanticModel : IGDMemberAccessAnalyzer, IGDArgumentTypeAnalyzer
{
    private readonly GDScriptFile _scriptFile;
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly GDTypeInferenceEngine? _typeEngine;
    private readonly GDValidationContext? _validationContext;

    // Component registries
    private readonly GDSymbolRegistry _symbolRegistry = new();
    private readonly GDFlowAnalysisRegistry _flowRegistry = new();

    // Type tracking
    private readonly Dictionary<GDNode, string> _nodeTypes = new();
    private readonly Dictionary<GDNode, GDTypeNode> _nodeTypeNodes = new();

    // Duck typing
    private readonly Dictionary<string, GDDuckType> _duckTypes = new();
    private readonly Dictionary<GDNode, GDTypeNarrowingContext> _narrowingContexts = new();

    // Type usages (type annotations, is checks, extends)
    private readonly Dictionary<string, List<GDTypeUsage>> _typeUsages = new();

    // Union types for Variant variables
    private readonly Dictionary<string, GDVariableUsageProfile> _variableProfiles = new();
    private readonly Dictionary<string, GDUnionType> _unionTypeCache = new();

    // Call site argument types for parameters (method.paramName -> union of argument types)
    private readonly Dictionary<string, GDUnionType> _callSiteParameterTypes = new();

    // Container usage profiles (local variables within methods)
    private readonly Dictionary<string, GDContainerUsageProfile> _containerProfiles = new();
    private readonly Dictionary<string, GDContainerElementType> _containerTypeCache = new();

    // Class-level container usage profiles (class member variables)
    private readonly Dictionary<string, GDContainerUsageProfile> _classContainerProfiles = new();

    // Callable call site registry for lambda parameter inference
    private GDCallableCallSiteRegistry? _callSiteRegistry;

    // Recursion guard for GetExpressionType to prevent infinite loops
    private readonly HashSet<GDExpression> _expressionTypeInProgress = new();
    private const int MaxExpressionTypeRecursionDepth = 50;

    /// <summary>
    /// The script file this model represents.
    /// </summary>
    public GDScriptFile ScriptFile => _scriptFile;

    /// <summary>
    /// The runtime provider for type resolution.
    /// </summary>
    public IGDRuntimeProvider? RuntimeProvider => _runtimeProvider;

    /// <summary>
    /// The Callable call site registry for lambda parameter inference.
    /// </summary>
    public GDCallableCallSiteRegistry? CallSiteRegistry => _callSiteRegistry;

    /// <summary>
    /// All symbols in this script.
    /// </summary>
    public IEnumerable<GDSymbolInfo> Symbols => _symbolRegistry.Symbols;

    /// <summary>
    /// Gets the symbol registry.
    /// </summary>
    public GDSymbolRegistry SymbolRegistry => _symbolRegistry;

    /// <summary>
    /// Creates a semantic model for a script file.
    /// Internal - use GDSemanticModel.Create() for external access.
    /// </summary>
    internal GDSemanticModel(
        GDScriptFile scriptFile,
        IGDRuntimeProvider? runtimeProvider,
        GDValidationContext? validationContext,
        GDTypeInferenceEngine? typeEngine)
    {
        _scriptFile = scriptFile ?? throw new ArgumentNullException(nameof(scriptFile));
        _runtimeProvider = runtimeProvider;
        _validationContext = validationContext;
        _typeEngine = typeEngine;
    }

    /// <summary>
    /// Creates and builds a semantic model for a script file.
    /// This is the recommended factory method for external use.
    /// </summary>
    /// <param name="scriptFile">The script file to analyze.</param>
    /// <param name="runtimeProvider">Optional runtime provider for type resolution.</param>
    /// <param name="typeInjector">Optional type injector for scene-based node type inference.</param>
    /// <returns>A fully built semantic model.</returns>
    public static GDSemanticModel Create(
        GDScriptFile scriptFile,
        IGDRuntimeProvider? runtimeProvider = null,
        IGDRuntimeTypeInjector? typeInjector = null)
    {
        if (scriptFile == null)
            throw new ArgumentNullException(nameof(scriptFile));

        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider, typeInjector);
        return collector.BuildSemanticModel();
    }

    #region Symbol Resolution

    /// <summary>
    /// Gets the symbol at a specific position in the file.
    /// </summary>
    public GDSymbolInfo? GetSymbolAt(int line, int column)
    {
        if (_scriptFile.Class == null)
            return null;

        var finder = new GDPositionFinder(_scriptFile.Class);
        var identifier = finder.FindIdentifierAtPosition(line, column);

        if (identifier == null)
            return null;

        // Try to find the node that contains this identifier
        var parent = identifier.Parent as GDNode;
        if (parent != null)
        {
            var symbolInfo = _symbolRegistry.GetSymbolForNode(parent);
            if (symbolInfo != null)
                return symbolInfo;
        }

        // Try resolving by name
        var name = identifier.Sequence;
        if (!string.IsNullOrEmpty(name))
            return FindSymbol(name);

        return null;
    }

    /// <summary>
    /// Gets the symbol for a specific AST node.
    /// Uses scope-aware lookup for identifier expressions.
    /// </summary>
    public GDSymbolInfo? GetSymbolForNode(GDNode node)
    {
        if (node == null)
            return null;

        // First check direct node mapping
        var symbol = _symbolRegistry.GetSymbolForNode(node);
        if (symbol != null)
            return symbol;

        // For identifier expressions, use scope-aware lookup
        if (node is GDIdentifierExpression identExpr)
        {
            var name = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name))
                return FindSymbolInScope(name, node);
        }

        return null;
    }

    /// <summary>
    /// Finds a symbol by name. Returns the first match.
    /// </summary>
    public GDSymbolInfo? FindSymbol(string name) => _symbolRegistry.FindSymbol(name);

    /// <summary>
    /// Finds all symbols with the given name (handles same-named symbols in different scopes).
    /// </summary>
    public IEnumerable<GDSymbolInfo> FindSymbols(string name) => _symbolRegistry.FindSymbols(name);

    /// <summary>
    /// Finds a symbol by name, considering the scope context.
    /// For local variables, only returns symbols declared in the same method/lambda.
    /// This prevents same-named variables in different methods from being confused.
    /// </summary>
    /// <param name="name">The symbol name to find</param>
    /// <param name="contextNode">The AST node providing scope context (e.g., identifier expression)</param>
    /// <returns>The symbol in the appropriate scope, or null if not found</returns>
    public GDSymbolInfo? FindSymbolInScope(string name, GDNode? contextNode)
    {
        var symbols = _symbolRegistry.FindSymbols(name).ToList();
        if (symbols.Count == 0)
            return null;

        // If no context or only one symbol, return first
        if (contextNode == null || symbols.Count == 1)
            return symbols[0];

        var contextMethod = FindEnclosingMethod(contextNode);

        // First, try to find a local symbol in the same method
        foreach (var symbol in symbols)
        {
            if (symbol.DeclaringScopeNode != null)
            {
                // Local symbol - check if in same method
                if (symbol.DeclaringScopeNode == contextMethod)
                    return symbol;
            }
        }

        // Fall back to class-level symbols (DeclaringScopeNode == null)
        foreach (var symbol in symbols)
        {
            if (symbol.DeclaringScopeNode == null)
                return symbol;
        }

        // Last resort: return first symbol
        return symbols[0];
    }

    /// <summary>
    /// Finds the enclosing method or lambda for an AST node.
    /// </summary>
    private GDNode? FindEnclosingMethod(GDNode node)
    {
        var current = node;
        while (current != null)
        {
            if (current is GDMethodDeclaration || current is GDMethodExpression)
                return current;
            current = current.Parent as GDNode;
        }
        return null;
    }

    /// <summary>
    /// Resolves a member on a type, including inherited members.
    /// </summary>
    public GDSymbolInfo? ResolveMember(string typeName, string memberName)
    {
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(memberName))
            return null;

        if (_runtimeProvider == null)
            return null;

        var memberInfo = _runtimeProvider.GetMember(typeName, memberName);
        if (memberInfo == null)
            return null;

        var declaringType = FindDeclaringType(typeName, memberName) ?? typeName;

        return GDSymbolInfo.BuiltIn(memberInfo, declaringType);
    }

    /// <summary>
    /// Finds the type that actually declares a member (for inherited members).
    /// </summary>
    private string? FindDeclaringType(string typeName, string memberName)
    {
        return TraverseInheritanceChain(typeName, current =>
        {
            var typeInfo = _runtimeProvider!.GetTypeInfo(current);
            if (typeInfo?.Members?.Any(m => m.Name == memberName) == true)
                return current;
            return null;
        }) ?? typeName;
    }

    #endregion

    #region Reference Queries

    /// <summary>
    /// Gets all references to a symbol within this file.
    /// </summary>
    public IReadOnlyList<GDReference> GetReferencesTo(GDSymbolInfo symbol)
    {
        if (symbol == null)
            return Array.Empty<GDReference>();

        return _symbolRegistry.GetReferences(symbol);
    }

    /// <summary>
    /// Gets all references to a symbol by name.
    /// </summary>
    public IReadOnlyList<GDReference> GetReferencesTo(string symbolName)
    {
        var symbol = FindSymbol(symbolName);
        if (symbol == null)
            return Array.Empty<GDReference>();

        return GetReferencesTo(symbol);
    }

    /// <summary>
    /// Gets all accesses to a specific member on a type (e.g., OS.execute, Node.add_child).
    /// Works for both built-in types and user-defined types.
    /// </summary>
    /// <param name="typeName">The type name (e.g., "OS", "@GDScript", "Node")</param>
    /// <param name="memberName">The member name (e.g., "execute", "str2var")</param>
    /// <returns>All references to that member in this file.</returns>
    public IReadOnlyList<GDReference> GetMemberAccesses(string typeName, string memberName)
    {
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(memberName))
            return Array.Empty<GDReference>();

        return _symbolRegistry.GetMemberAccessReferences(typeName, memberName);
    }

    /// <summary>
    /// Gets all accesses to a global function (e.g., str2var, load, preload).
    /// Global functions in GDScript belong to "@GDScript" pseudo-type.
    /// </summary>
    public IReadOnlyList<GDReference> GetGlobalFunctionAccesses(string functionName)
    {
        return GetMemberAccesses("@GDScript", functionName);
    }

    /// <summary>
    /// Checks if there are any accesses to a specific member on a type.
    /// </summary>
    public bool HasMemberAccesses(string typeName, string memberName)
    {
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(memberName))
            return false;

        return _symbolRegistry.GetMemberAccessReferences(typeName, memberName).Count > 0;
    }

    #endregion

    #region Type Queries

    /// <summary>
    /// Gets the type for any AST node.
    /// </summary>
    public string? GetTypeForNode(GDNode node)
    {
        if (node == null)
            return null;

        // Check cache first
        if (_nodeTypes.TryGetValue(node, out var cachedType))
            return cachedType;

        // For expressions, use expression type inference
        if (node is GDExpression expr)
            return GetExpressionType(expr);

        // For parameter declarations, use parameter type inference
        if (node is GDParameterDeclaration paramDecl)
        {
            var inferred = InferParameterType(paramDecl);
            if (inferred.Confidence != GDTypeConfidence.Unknown)
                return inferred.TypeName;
        }

        // Fallback to type inference engine for declarations (variables, methods, etc.)
        return _typeEngine?.GetTypeForNode(node);
    }

    /// <summary>
    /// Gets the full type node for any AST node (with generics).
    /// </summary>
    public GDTypeNode? GetTypeNodeForNode(GDNode node)
    {
        if (node == null)
            return null;

        // Check cache first
        if (_nodeTypeNodes.TryGetValue(node, out var typeNode))
            return typeNode;

        // For expressions, use expression type node inference
        if (node is GDExpression expr)
            return GetTypeNodeForExpression(expr);

        // Fallback to type inference engine for declarations (variables, methods, etc.)
        return _typeEngine?.GetTypeNodeForNode(node);
    }

    /// <summary>
    /// Gets the inferred type for an expression.
    /// Uses flow-sensitive analysis when available.
    /// </summary>
    public string? GetExpressionType(GDExpression expression)
    {
        if (expression == null)
            return null;

        // For identifier expressions, flow analysis takes priority over cache
        // because cache may contain stale types from initialization (before reassignments)
        if (expression is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                // Check container usage profile FIRST for untyped containers
                // This handles: var a = []; a.append(1) → a should be Array[int]
                // Container profile has element type info from usage analysis
                var containerType = GetInferredContainerType(varName);
                if (containerType != null && containerType.HasElementTypes)
                {
                    return containerType.ToString();
                }

                var method = FindContainingMethodNode(expression);
                if (method != null)
                {
                    var flowAnalyzer = GetOrCreateFlowAnalyzer(method);
                    var flowType = flowAnalyzer?.GetTypeAtLocation(varName, expression);
                    // Return flow type if it's concrete (not Variant)
                    // If flow returns Variant, fall through to TypeEngine which may have
                    // explicit type annotation from class-level variable declaration
                    if (!string.IsNullOrEmpty(flowType) && flowType != "Variant")
                        return flowType;
                }
            }
        }

        // For array addition, skip cache and compute union type
        // This handles: var c = a + b where a is Array[int] and b is Array[String]
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

        // Check cache for non-identifier expressions or when flow analysis didn't provide result
        if (_nodeTypes.TryGetValue(expression, out var cachedType))
            return cachedType;

        // Recursion guard: prevent infinite loops when resolving expression types
        if (_expressionTypeInProgress.Contains(expression))
            return null; // Already computing this expression's type - return null to break cycle

        if (_expressionTypeInProgress.Count >= MaxExpressionTypeRecursionDepth)
            return null; // Too deep - likely infinite recursion

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
        // For identifier expressions, use flow-sensitive type analysis
        // Priority: flow-sensitive > narrowing > class variable union > type engine
        // Note: flow analysis is now checked in GetExpressionType() BEFORE cache lookup
        if (expression is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                // Flow analysis is already checked in GetExpressionType(), but we keep this
                // as fallback for cases where it wasn't available
                var method = FindContainingMethodNode(expression);
                if (method != null)
                {
                    var flowAnalyzer = GetOrCreateFlowAnalyzer(method);
                    var flowType = flowAnalyzer?.GetTypeAtLocation(varName, expression);
                    if (!string.IsNullOrEmpty(flowType) && flowType != "Variant")
                        return flowType;
                }

                // Fall back to narrowing (for backward compatibility)
                var narrowed = GetNarrowedType(varName, expression);
                if (!string.IsNullOrEmpty(narrowed))
                    return narrowed;

                // Fall back to class variable union type (for untyped class-level variables)
                // This handles: var entity_manager assigned in _ready() with ECSLikeSystem.new()
                var unionType = GetUnionType(varName);
                if (unionType != null && unionType.IsSingleType)
                {
                    var effectiveType = unionType.EffectiveType;
                    if (!string.IsNullOrEmpty(effectiveType) && effectiveType != "Variant")
                        return effectiveType;
                }

                // Check if identifier references a method - method reference is Callable
                // This handles: var cb = _on_timeout; cb.bind(timer)
                // Method references (without calling them) have type Callable
                var methodSymbol = FindSymbol(varName);
                if (methodSymbol?.Kind == GDSymbolKind.Method)
                    return "Callable";

                // Fall back to local variable initializer type inference
                // This handles: var entity = entity_manager.create_entity(...)
                // where entity_manager type is already known through union types
                // Only use this when there are NO reassignments (otherwise, flow analysis should handle it)
                if (method != null)
                {
                    var symbol = FindSymbol(varName);
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

        // For member access, check if caller has narrowed type
        // This handles current.get where current is narrowed to Dictionary
        if (expression is GDMemberOperatorExpression memberExpr)
        {
            var callerType = GetExpressionType(memberExpr.CallerExpression);
            var memberName = memberExpr.Identifier?.Sequence;

            if (!string.IsNullOrEmpty(callerType) && callerType != "Variant" &&
                !string.IsNullOrEmpty(memberName) && _runtimeProvider != null)
            {
                var memberInfo = FindMemberWithInheritanceInternal(callerType, memberName);
                if (memberInfo != null)
                    return memberInfo.Type;
            }
        }

        // For call expressions, delegate to TypeEngine which properly applies ReturnTypeRole
        // for container methods like front(), back(), get(), etc.
        if (expression is GDCallExpression callExpr)
        {
            // Handle .new() constructor specially
            if (callExpr.CallerExpression is GDMemberOperatorExpression callMemberExpr)
            {
                var methodName = callMemberExpr.Identifier?.Sequence;
                if (methodName == GDTypeInferenceConstants.ConstructorMethodName)
                {
                    var callerType = GetExpressionType(callMemberExpr.CallerExpression);
                    if (!string.IsNullOrEmpty(callerType))
                        return callerType;
                }

            }

            // Delegate to TypeEngine for all other call expressions - it applies ReturnTypeRole
            var callResult = _typeEngine?.InferType(expression);
            if (!string.IsNullOrEmpty(callResult))
                return callResult;
        }

        // For binary operators, recursively resolve operand types using flow-sensitive analysis
        // This ensures narrowed types (e.g., after 'if x is int:') are used for expressions like 'x * 2'
        if (expression is GDDualOperatorExpression dualOp)
        {
            var opType = dualOp.Operator?.OperatorType;

            // For 'as' operator, delegate to TypeEngine which handles it correctly
            // (right side is a TYPE NAME, not an expression value)
            if (opType == GDDualOperatorType.As)
            {
                return _typeEngine?.InferType(expression);
            }

            var leftType = GetExpressionType(dualOp.LeftExpression);
            var rightType = GetExpressionType(dualOp.RightExpression);

            if (opType.HasValue)
            {
                var resultType = GDOperatorTypeResolver.ResolveOperatorType(opType.Value, leftType, rightType);
                if (!string.IsNullOrEmpty(resultType))
                    return resultType;

                // For array addition with incompatible types, compute union type
                // (Also handled before cache check in GetExpressionType, this is fallback)
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

        // For unary operators, recursively resolve operand type using flow-sensitive analysis
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

        // For indexer expressions (dict[key], array[index]), infer element type from container
        if (expression is GDIndexerExpression indexerExpr)
        {
            // First, try typed containers (Array[T], Dictionary[K,V])
            var typeNode = GetTypeNodeForExpression(indexerExpr);
            if (typeNode != null)
            {
                var typeName = typeNode.BuildName();
                if (!string.IsNullOrEmpty(typeName) && typeName != "Variant")
                    return typeName;
            }

            // Fallback: check container usage analysis for untyped containers
            var varName = GetRootVariableName(indexerExpr.CallerExpression);
            if (!string.IsNullOrEmpty(varName))
            {
                var containerType = GetInferredContainerType(varName);
                if (containerType != null && containerType.HasElementTypes)
                {
                    var elementType = containerType.EffectiveElementType;
                    if (!string.IsNullOrEmpty(elementType) && elementType != "Variant")
                        return elementType;
                }
            }
        }

        // For array initializers, compute union type from elements directly
        // This handles: var arr = [1, "hello", true] → Array[String|bool|int]
        if (expression is Reader.GDArrayInitializerExpression arrayInit && _typeEngine != null)
        {
            var unionType = ComputeArrayInitializerUnionType(arrayInit);
            if (!string.IsNullOrEmpty(unionType))
                return unionType;
        }

        // Use type engine for type inference
        // Note: Do NOT delegate to Analyzer to avoid circular dependency
        return _typeEngine?.InferType(expression);
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
                var symbol = FindSymbol(name);
                if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName))
                {
                    return new GDTypeResolutionResult
                    {
                        TypeName = symbol.TypeName,
                        IsResolved = true,
                        Source = GDTypeSource.Project
                    };
                }
            }
        }

        // 2. Delegate to TypeEngine for complex expressions (member access, calls, etc.)
        if (_typeEngine != null)
        {
            var typeName = _typeEngine.InferType(expression);
            if (!string.IsNullOrEmpty(typeName))
            {
                return new GDTypeResolutionResult
                {
                    TypeName = typeName,
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
            var typeName = freshEngine.InferType(expression);
            if (!string.IsNullOrEmpty(typeName))
            {
                return new GDTypeResolutionResult
                {
                    TypeName = typeName,
                    IsResolved = true,
                    Source = GDTypeSource.Inferred
                };
            }
        }

        return GDTypeResolutionResult.Unknown();
    }

    /// <summary>
    /// Finds a member in a type, traversing the inheritance chain if necessary.
    /// Internal method to avoid name collision with public API.
    /// </summary>
    private GDRuntimeMemberInfo? FindMemberWithInheritanceInternal(string typeName, string memberName)
    {
        return TraverseInheritanceChain(typeName, current =>
            _runtimeProvider!.GetMember(current, memberName));
    }

    /// <summary>
    /// Gets or creates a flow analyzer for a method.
    /// Flow analyzers are cached per method.
    /// </summary>
    private GDFlowAnalyzer? GetOrCreateFlowAnalyzer(GDMethodDeclaration method)
    {
        if (method == null)
            return null;

        return _flowRegistry.GetOrCreateFlowAnalyzer(method, _typeEngine, GetExpressionTypeWithoutFlow, () => GetOnreadyVariables());
    }

    /// <summary>
    /// Gets expression type without using flow analysis (to avoid recursion when called from flow analyzer).
    /// Uses union types for class variables and type engine for other expressions.
    /// </summary>
    private string? GetExpressionTypeWithoutFlow(GDExpression expression)
    {
        if (expression == null)
            return null;

        // For call expressions, use TypeEngine which properly handles ReturnTypeRole metadata
        // for container methods like front(), back(), get(), etc.
        if (expression is GDCallExpression callExpr)
        {
            // Handle .new() constructor specially
            if (callExpr.CallerExpression is GDMemberOperatorExpression callMemberExpr)
            {
                var methodName = callMemberExpr.Identifier?.Sequence;
                if (methodName == GDTypeInferenceConstants.ConstructorMethodName)
                {
                    var callerType = GetExpressionTypeWithoutFlow(callMemberExpr.CallerExpression);
                    if (!string.IsNullOrEmpty(callerType))
                        return callerType;
                }
            }

            // Delegate to TypeEngine for all other call expressions - it applies ReturnTypeRole
            var callResult = _typeEngine?.InferType(expression);
            if (!string.IsNullOrEmpty(callResult))
                return callResult;
        }

        // For identifiers, try union types (class variables assigned in different methods)
        if (expression is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                // IMPORTANT: Check for method reference FIRST, before union types.
                // A method name without () should be typed as Callable, not by its return type.
                // Example: var cb = _handler  =>  cb should be Callable (method reference)
                // This enables .bind(), .call(), .is_valid() methods on the reference.
                var symbol = FindSymbol(varName);
                if (symbol?.Kind == GDSymbolKind.Method)
                {
                    return "Callable";
                }

                // Check union types (handles class variables like entity_manager)
                var unionType = GetUnionType(varName);
                if (unionType != null && unionType.IsSingleType)
                {
                    var effectiveType = unionType.EffectiveType;
                    if (!string.IsNullOrEmpty(effectiveType) && effectiveType != "Variant" && effectiveType != "null")
                    {
                        return effectiveType;
                    }
                }
            }
        }

        // Fall back to type engine
        return _typeEngine?.InferType(expression);
    }

    /// <summary>
    /// Finds the containing method declaration for an AST node.
    /// </summary>
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

    /// <summary>
    /// Finds a local variable declaration in the containing method that appears before the given expression.
    /// </summary>
    private static GDVariableDeclarationStatement? FindLocalVariableDeclaration(GDExpression expr, string varName)
    {
        var method = FindContainingMethodNode(expr);
        if (method?.Statements == null) return null;

        var beforeLine = expr.StartLine;
        return method.AllNodes.OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == varName && v.StartLine < beforeLine);
    }

    /// <summary>
    /// Checks if a local variable has any reassignments within the method.
    /// Used to determine if we should fall back to initializer type inference.
    /// </summary>
    private static bool HasLocalReassignments(GDMethodDeclaration? method, string varName)
    {
        if (method?.Statements == null || string.IsNullOrEmpty(varName))
            return false;

        // Look for assignment expressions targeting this variable
        foreach (var node in method.AllNodes)
        {
            // Skip the initial declaration
            if (node is GDVariableDeclarationStatement)
                continue;

            // Check for reassignment: x = something
            // Can be in GDExpressionStatement or directly as GDDualOperatorExpression
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

    /// <summary>
    /// Gets the flow-sensitive type for a variable at a specific location.
    /// Returns null if flow analysis is not available.
    /// </summary>
    public string? GetFlowSensitiveType(string variableName, GDNode atLocation)
    {
        if (string.IsNullOrEmpty(variableName) || atLocation == null)
            return null;

        var method = FindContainingMethodNode(atLocation);
        if (method == null)
            return null;

        var flowAnalyzer = GetOrCreateFlowAnalyzer(method);
        return flowAnalyzer?.GetTypeAtLocation(variableName, atLocation);
    }

    /// <summary>
    /// Gets the full flow variable type info at a specific location.
    /// </summary>
    public GDFlowVariableType? GetFlowVariableType(string variableName, GDNode atLocation)
    {
        if (string.IsNullOrEmpty(variableName) || atLocation == null)
            return null;

        var method = FindContainingMethodNode(atLocation);
        if (method == null)
            return null;

        var flowAnalyzer = GetOrCreateFlowAnalyzer(method);
        return flowAnalyzer?.GetVariableTypeAtLocation(variableName, atLocation);
    }

    /// <summary>
    /// Gets the flow state at a specific location in the code.
    /// Returns null if flow analysis is not available.
    /// </summary>
    public GDFlowState? GetFlowStateAtLocation(GDNode atLocation)
    {
        if (atLocation == null)
            return null;

        var method = FindContainingMethodNode(atLocation);
        if (method == null)
            return null;

        var flowAnalyzer = GetOrCreateFlowAnalyzer(method);
        return flowAnalyzer?.GetStateAtLocation(atLocation);
    }

    /// <summary>
    /// Checks if a variable is potentially null at a given location.
    /// </summary>
    public bool IsVariablePotentiallyNull(string variableName, GDNode atLocation)
    {
        if (string.IsNullOrEmpty(variableName) || atLocation == null)
            return true; // Assume potentially null if unknown

        // Check if this is an enum type access (enums are never null)
        if (IsEnumType(variableName))
            return false;

        // Check if this is a class/type name (for static method calls like ClassName.new())
        // Class names are not variables and cannot be null
        if (IsClassName(variableName))
            return false;

        // Check if this is a built-in value type (Vector2, Vector3, etc.) - they have static members like ZERO
        if (IsBuiltInValueType(variableName))
            return false;

        // Check if inherited property from base class (always exists) - check even if no local symbol
        // This handles cases like "position.distance_to()" where position is from Node2D
        if (IsInheritedProperty(variableName))
            return false;

        // Check if this is a signal (signals are never null)
        var symbol = FindSymbol(variableName);
        if (symbol != null)
        {
            // Signals are never null
            if (symbol.Kind == GDSymbolKind.Signal)
                return false;

            // Check if class-level variable has non-null initializer
            if (symbol.Kind == GDSymbolKind.Variable || symbol.Kind == GDSymbolKind.Property)
            {
                if (HasNonNullInitializer(symbol))
                    return false;
            }
        }

        var method = FindContainingMethodNode(atLocation);
        if (method == null)
            return true;

        var flowAnalyzer = GetOrCreateFlowAnalyzer(method);
        var state = flowAnalyzer?.GetStateAtLocation(atLocation);
        if (state == null)
            return true;

        return state.IsVariablePotentiallyNull(variableName, _runtimeProvider);
    }

    /// <summary>
    /// Checks if a type name represents an enum type (local or cross-file).
    /// </summary>
    private bool IsEnumType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        // Check local enums (same logic as IGDMemberAccessAnalyzer.IsLocalEnum)
        var symbols = FindSymbols(typeName);
        if (symbols.Any(s => s.Kind == GDSymbolKind.Enum))
            return true;

        // Cross-file enums are handled via GDProjectTypesProvider which registers them
        // as constants with the enum name as type. No additional check needed here
        // since FindSymbols already covers project-level symbols.

        return false;
    }

    /// <summary>
    /// Public method to check if a type is an enum (for external access).
    /// </summary>
    public bool IsLocalEnumType(string typeName)
    {
        return IsEnumType(typeName);
    }

    /// <summary>
    /// Checks if a name represents a class/type name.
    /// Class names are used for static method calls (ClassName.new()) and cannot be null.
    /// </summary>
    private bool IsClassName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        // Check if it's a local class (class_name declaration)
        var symbols = FindSymbols(name);
        if (symbols.Any(s => s.Kind == GDSymbolKind.Class))
            return true;

        // Check if it's a known Godot type via runtime provider
        if (_runtimeProvider?.IsKnownType(name) == true)
            return true;

        // Check global classes (singletons like Input, Engine, OS)
        if (_runtimeProvider?.GetGlobalClass(name) != null)
            return true;

        return false;
    }

    /// <summary>
    /// Checks if a name represents a built-in value type (Vector2, Vector3, etc.).
    /// These types have static members like ZERO, ONE, UP, etc. that are never null.
    /// </summary>
    private static bool IsBuiltInValueType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        return typeName is "Vector2" or "Vector2i" or "Vector3" or "Vector3i" or "Vector4" or "Vector4i"
            or "Color" or "Rect2" or "Rect2i" or "Transform2D" or "Transform3D"
            or "Basis" or "Quaternion" or "Plane" or "AABB" or "Projection"
            or "int" or "float" or "bool" or "String" or "StringName" or "RID" or "Callable" or "Signal";
    }

    /// <summary>
    /// Checks if a symbol has a non-null initializer.
    /// </summary>
    private static bool HasNonNullInitializer(GDSymbolInfo symbol)
    {
        if (symbol.DeclarationNode is GDVariableDeclaration varDecl)
        {
            var initializer = varDecl.Initializer;
            if (initializer == null)
                return false;

            // Null literal means explicitly null (in GDScript, null is GDIdentifierExpression with "null")
            if (initializer is GDIdentifierExpression nullIdent && nullIdent.Identifier?.Sequence == "null")
                return false;

            // Any other initializer ([], {}, new(), literals, etc.) is non-null
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a variable has the @onready attribute.
    /// </summary>
    public bool IsOnreadyVariable(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return false;

        var symbol = FindSymbol(variableName);
        if (symbol?.DeclarationNode is not GDVariableDeclaration varDecl)
            return false;

        return varDecl.AttributesDeclaredBefore.Any(attr => attr.Attribute?.IsOnready() == true);
    }

    /// <summary>
    /// Checks if a variable is initialized in _ready() method (not @onready, but assigned there).
    /// </summary>
    public bool IsReadyInitializedVariable(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return false;

        var symbol = FindSymbol(variableName);
        if (symbol?.DeclarationNode is not GDVariableDeclaration varDecl)
            return false;

        // Has initializer at class level — not a _ready() initialized variable
        if (varDecl.Initializer != null)
            return false;

        // Check if there's an assignment in _ready()
        return HasAssignmentInReadyMethod(variableName);
    }

    /// <summary>
    /// Checks if a variable is either @onready or initialized in _ready().
    /// </summary>
    public bool IsOnreadyOrReadyInitializedVariable(string variableName)
    {
        return IsOnreadyVariable(variableName) || IsReadyInitializedVariable(variableName);
    }

    /// <summary>
    /// Gets all @onready variable names in the current class.
    /// </summary>
    public IEnumerable<string> GetOnreadyVariables()
    {
        var classDecl = _scriptFile?.Class;
        if (classDecl == null)
            yield break;

        foreach (var member in classDecl.Members)
        {
            if (member is GDVariableDeclaration varDecl)
            {
                if (varDecl.AttributesDeclaredBefore.Any(attr => attr.Attribute?.IsOnready() == true))
                {
                    var name = varDecl.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(name))
                        yield return name;
                }
            }
        }
    }

    /// <summary>
    /// Checks if there's an assignment to a variable in the _ready() method.
    /// </summary>
    private bool HasAssignmentInReadyMethod(string variableName)
    {
        var readyMethod = GetReadyMethod();
        if (readyMethod == null)
            return false;

        // Walk the _ready() method looking for assignments to this variable
        var visitor = new AssignmentFinder(variableName);
        readyMethod.WalkIn(visitor);
        return visitor.Found;
    }

    /// <summary>
    /// Gets the _ready() method declaration if it exists.
    /// </summary>
    public GDMethodDeclaration? GetReadyMethod()
    {
        var classDecl = _scriptFile?.Class;
        if (classDecl == null)
            return null;

        foreach (var member in classDecl.Members)
        {
            if (member is GDMethodDeclaration method && method.IsReady())
                return method;
        }

        return null;
    }

    /// <summary>
    /// Helper visitor to find assignments to a specific variable.
    /// </summary>
    private class AssignmentFinder : GDVisitor
    {
        private readonly string _targetVariable;
        public bool Found { get; private set; }

        public AssignmentFinder(string targetVariable)
        {
            _targetVariable = targetVariable;
        }

        public override void Visit(GDExpressionStatement statement)
        {
            if (Found)
                return;

            base.Visit(statement);

            // Check for assignment expression
            if (statement.Expression is GDDualOperatorExpression dualOp)
            {
                if (dualOp.OperatorType == GDDualOperatorType.Assignment ||
                    dualOp.OperatorType == GDDualOperatorType.AddAndAssign ||
                    dualOp.OperatorType == GDDualOperatorType.SubtractAndAssign ||
                    dualOp.OperatorType == GDDualOperatorType.MultiplyAndAssign ||
                    dualOp.OperatorType == GDDualOperatorType.DivideAndAssign)
                {
                    if (dualOp.LeftExpression is GDIdentifierExpression leftIdent &&
                        leftIdent.Identifier?.Sequence == _targetVariable)
                    {
                        Found = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if a variable is an inherited property from the extends clause.
    /// </summary>
    private bool IsInheritedProperty(string variableName)
    {
        if (string.IsNullOrEmpty(variableName) || _runtimeProvider == null)
            return false;

        // Get the extends type for this script
        var extendsType = GetExtendsType();
        if (string.IsNullOrEmpty(extendsType))
            return false;

        // Check if this is a property on the base type
        var member = TraverseInheritanceChain(extendsType, current =>
            _runtimeProvider.GetMember(current, variableName));

        return member != null && member.Kind == GDRuntimeMemberKind.Property;
    }

    /// <summary>
    /// Gets the extends type for the current script.
    /// </summary>
    private string? GetExtendsType()
    {
        // Find the class declaration in the script
        var classDecl = _scriptFile?.Class;
        if (classDecl == null)
            return "RefCounted"; // Default GDScript base

        var extendsType = classDecl.Extends?.Type?.BuildName();
        return string.IsNullOrEmpty(extendsType) ? "RefCounted" : extendsType;
    }

    /// <summary>
    /// Gets the full type node (with generics) for an expression.
    /// </summary>
    public GDTypeNode? GetTypeNodeForExpression(GDExpression expression)
    {
        if (expression == null)
            return null;

        // Check cache first
        if (_nodeTypeNodes.TryGetValue(expression, out var typeNode))
            return typeNode;

        // For identifiers, try to resolve through our symbol registry
        // This handles local variables that aren't in the type engine's scope
        if (expression is GDIdentifierExpression identExpr)
        {
            var identName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(identName))
            {
                var symbol = FindSymbol(identName);
                if (symbol != null)
                {
                    // Prefer TypeNode if available
                    if (symbol.TypeNode != null)
                        return symbol.TypeNode;
                    // Fall back to TypeName
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
                // Array[T][index] -> T
                if (callerTypeNode is GDArrayTypeNode arrayType)
                    return arrayType.InnerType;

                // Dictionary[K,V][key] -> V
                if (callerTypeNode is GDDictionaryTypeNode dictType)
                    return dictType.ValueType;
            }
        }

        // Use type engine for other expressions
        // Note: Do NOT delegate to Analyzer to avoid circular dependency
        return _typeEngine?.InferTypeNode(expression);
    }

    /// <summary>
    /// Creates a simple single type node.
    /// </summary>
    private static GDTypeNode CreateSimpleType(string typeName)
    {
        return new GDSingleTypeNode { Type = new GDType { Sequence = typeName } };
    }

    /// <summary>
    /// Gets the duck type for a variable (required methods/properties).
    /// </summary>
    public GDDuckType? GetDuckType(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        return _duckTypes.TryGetValue(variableName, out var duckType) ? duckType : null;
    }

    /// <summary>
    /// Gets the narrowed type for a variable at a specific location (from if checks).
    /// Walks up the AST to find the nearest branch with narrowing info.
    /// </summary>
    public string? GetNarrowedType(string variableName, GDNode atLocation)
    {
        if (string.IsNullOrEmpty(variableName) || atLocation == null)
            return null;

        var narrowingContext = FindNarrowingContextForNode(atLocation);
        return narrowingContext?.GetConcreteType(variableName);
    }

    /// <summary>
    /// Finds the narrowing context that applies to a given node location.
    /// Walks up the AST to find the nearest branch with narrowing info.
    /// </summary>
    private GDTypeNarrowingContext? FindNarrowingContextForNode(GDNode node)
    {
        var current = node;
        while (current != null)
        {
            if (_narrowingContexts.TryGetValue(current, out var context))
                return context;

            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// Gets the effective type for a variable at a location.
    /// Considers narrowing, declared type, and duck type.
    /// </summary>
    public string? GetEffectiveType(string variableName, GDNode? atLocation = null)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        // Check narrowing first
        if (atLocation != null)
        {
            var narrowed = GetNarrowedType(variableName, atLocation);
            if (narrowed != null)
                return narrowed;
        }

        // Check symbol type
        var symbol = FindSymbol(variableName);
        if (symbol?.TypeName != null && symbol.TypeName != "Variant")
            return symbol.TypeName;

        // Duck type as string representation (for Variant or untyped)
        var duckType = GetDuckType(variableName);
        if (duckType != null)
            return duckType.ToString();

        // Fallback to symbol type (including Variant)
        return symbol?.TypeName;
    }

    /// <summary>
    /// Checks if duck type constraints should be suppressed for a symbol.
    /// Suppresses when:
    /// - The symbol has a known concrete type (not Variant/Unknown)
    /// - All member access usages are within narrowed contexts (type guards)
    /// </summary>
    /// <param name="symbolName">The symbol name to check.</param>
    /// <returns>True if duck constraints should be suppressed.</returns>
    public bool ShouldSuppressDuckConstraints(string symbolName)
    {
        if (string.IsNullOrEmpty(symbolName))
            return true;

        // If symbol has known concrete type, suppress duck constraints
        var unionType = GetUnionType(symbolName);
        if (unionType?.IsSingleType == true)
        {
            var type = unionType.EffectiveType;
            if (IsConcreteType(type))
                return true;
        }

        var symbol = FindSymbol(symbolName);
        if (symbol?.TypeName != null && IsConcreteType(symbol.TypeName))
            return true;

        // If no duck type requirements, nothing to suppress
        var duckType = GetDuckType(symbolName);
        if (duckType == null || !duckType.HasRequirements)
            return true;

        if (symbol != null)
        {
            var refs = GetReferencesTo(symbol);
            foreach (var reference in refs)
            {
                if (reference.ReferenceNode?.Parent is GDMemberOperatorExpression memberOp)
                {
                    var narrowedType = GetNarrowedType(symbolName, memberOp);
                    if (string.IsNullOrEmpty(narrowedType))
                    {
                        // This usage is not in a type guard - duck type is needed
                        return false;
                    }
                }
            }
        }

        // All usages are narrowed, suppress duck constraints
        return true;
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
    private GDMethodDeclaration? FindContainingMethod(GDParameterDeclaration param)
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

    #region Type Usage Queries

    /// <summary>
    /// Gets all usages of a type in this script.
    /// </summary>
    public IReadOnlyList<GDTypeUsage> GetTypeUsages(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return Array.Empty<GDTypeUsage>();

        return _typeUsages.TryGetValue(typeName, out var usages)
            ? usages
            : Array.Empty<GDTypeUsage>();
    }

    /// <summary>
    /// Gets all type usages in this script.
    /// </summary>
    public IEnumerable<GDTypeUsage> AllTypeUsages => _typeUsages.Values.SelectMany(x => x);

    /// <summary>
    /// Gets all type names that are used in this script.
    /// </summary>
    public IEnumerable<string> UsedTypeNames => _typeUsages.Keys;

    #endregion

    #region Union Type Queries

    /// <summary>
    /// Gets the variable usage profile for a Variant variable.
    /// </summary>
    public GDVariableUsageProfile? GetVariableProfile(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        return _variableProfiles.TryGetValue(variableName, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets the Union type for a Variant variable or method return type.
    /// For variables, computes from all assignments.
    /// For methods, computes from all return statements.
    /// Returns null if the symbol is not found.
    /// </summary>
    public GDUnionType? GetUnionType(string symbolName)
    {
        if (string.IsNullOrEmpty(symbolName))
            return null;

        // Check cache first
        if (_unionTypeCache.TryGetValue(symbolName, out var cached))
            return cached;

        var symbol = FindSymbol(symbolName);
        if (symbol?.DeclarationNode is GDMethodDeclaration method)
        {
            var union = ComputeMethodReturnUnion(method);
            if (union != null)
            {
                EnrichUnionTypeIfNeeded(union);
                _unionTypeCache[symbolName] = union;
                return union;
            }
        }

        if (symbol?.Kind == GDSymbolKind.Parameter && symbol.DeclarationNode is GDParameterDeclaration param)
        {
            var union = ComputeParameterUnion(param, symbol);
            if (union != null)
            {
                EnrichUnionTypeIfNeeded(union);
                _unionTypeCache[symbolName] = union;
                return union;
            }
        }

        // Compute from variable profile (for local variables)
        var profile = GetVariableProfile(symbolName);
        if (profile == null)
            return null;

        var varUnion = profile.ComputeUnionType();
        EnrichUnionTypeIfNeeded(varUnion);
        _unionTypeCache[symbolName] = varUnion;
        return varUnion;
    }

    /// <summary>
    /// Computes the union type for a method's return statements.
    /// </summary>
    private GDUnionType? ComputeMethodReturnUnion(GDMethodDeclaration method)
    {
        var collector = new GDReturnTypeCollector(method, _runtimeProvider);
        collector.Collect();
        return collector.ComputeReturnUnionType();
    }

    /// <summary>
    /// Computes the union type for a parameter based on type guards, null checks, and call site arguments.
    /// </summary>
    private GDUnionType? ComputeParameterUnion(GDParameterDeclaration param, GDSymbolInfo symbol)
    {
        var paramName = param.Identifier?.Sequence;
        if (string.IsNullOrEmpty(paramName))
            return null;

        var method = param.Parent?.Parent as GDMethodDeclaration;
        if (method?.Statements == null)
            return null;

        // Use analyzer to compute expected types from code analysis
        var analyzer = new GDParameterTypeAnalyzer(_runtimeProvider, _typeEngine);
        var union = analyzer.ComputeExpectedTypes(param, method);

        // Add call site argument types if available
        var methodName = method.Identifier?.Sequence;
        if (!string.IsNullOrEmpty(methodName))
        {
            var key = BuildParameterKey(methodName, paramName);
            if (_callSiteParameterTypes.TryGetValue(key, out var callSiteUnion) && callSiteUnion != null)
            {
                foreach (var type in callSiteUnion.Types)
                {
                    union.AddType(type, isHighConfidence: false);
                }
            }
        }

        return union.IsEmpty ? null : union;
    }

    /// <summary>
    /// Gets all variable usage profiles (for UI display).
    /// </summary>
    public IEnumerable<GDVariableUsageProfile> GetAllVariableProfiles()
    {
        return _variableProfiles.Values;
    }

    /// <summary>
    /// Gets the member access confidence for a Union type.
    /// </summary>
    public GDReferenceConfidence GetUnionMemberConfidence(GDUnionType unionType, string memberName)
    {
        if (unionType == null || string.IsNullOrEmpty(memberName) || _runtimeProvider == null)
            return GDReferenceConfidence.NameMatch;

        var resolver = new GDUnionTypeResolver(_runtimeProvider);
        return resolver.GetMemberConfidence(unionType, memberName);
    }

    /// <summary>
    /// Gets the confidence level for member access on an indexer result.
    /// Attempts to infer element type from class-level container profiles.
    /// </summary>
    private GDReferenceConfidence GetIndexerMemberAccessConfidence(
        GDIndexerExpression indexerExpr,
        GDMemberOperatorExpression memberAccess)
    {
        var memberName = memberAccess.Identifier?.Sequence;
        if (string.IsNullOrEmpty(memberName))
            return GDReferenceConfidence.Potential;

        // Try to get the container variable name
        var containerVarName = GetRootVariableName(indexerExpr.CallerExpression);
        if (string.IsNullOrEmpty(containerVarName))
            return GDReferenceConfidence.Potential;

        // Try to get element type from local container profile first
        var localProfile = GetContainerProfile(containerVarName);
        if (localProfile != null)
        {
            var inferredType = localProfile.ComputeInferredType();
            var elementType = inferredType.EffectiveElementType;

            if (!string.IsNullOrEmpty(elementType) && elementType != "Variant")
            {
                return GetMemberConfidenceOnType(elementType, memberName);
            }
        }

        // Try class-level container profile
        var classProfile = GetClassContainerProfile(_scriptFile?.TypeName ?? "", containerVarName);
        if (classProfile != null)
        {
            var inferredType = classProfile.ComputeInferredType();
            var elementType = inferredType.EffectiveElementType;

            if (!string.IsNullOrEmpty(elementType) && elementType != "Variant")
            {
                return GetMemberConfidenceOnType(elementType, memberName);
            }

            // If we have a union of element types, check if ALL types have the member
            if (inferredType.ElementUnionType != null && inferredType.ElementUnionType.IsUnion)
            {
                return GetUnionMemberConfidence(inferredType.ElementUnionType, memberName);
            }
        }

        // Default: duck-typing - treat as Potential since programmer expects element to have the member
        return GDReferenceConfidence.Potential;
    }

    /// <summary>
    /// Gets member confidence for a known type.
    /// </summary>
    private GDReferenceConfidence GetMemberConfidenceOnType(string typeName, string memberName)
    {
        if (_runtimeProvider == null)
            return GDReferenceConfidence.Potential;

        var memberInfo = _runtimeProvider.GetMember(typeName, memberName);
        if (memberInfo != null)
            return GDReferenceConfidence.Potential; // Known member on inferred type

        // Member not found on the inferred type - still Potential for duck-typing
        return GDReferenceConfidence.Potential;
    }

    /// <summary>
    /// Gets the type diff for a parameter, comparing expected types (from usage/type guards)
    /// vs actual types (from call site arguments).
    /// </summary>
    /// <param name="methodName">The method name.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>Type diff result, or null if parameter is not found.</returns>
    public GDParameterTypeDiff? GetParameterTypeDiff(string methodName, string paramName)
    {
        if (string.IsNullOrEmpty(methodName) || string.IsNullOrEmpty(paramName))
            return null;

        var methodSymbol = FindSymbol(methodName);
        if (methodSymbol?.DeclarationNode is not GDMethodDeclaration method)
            return null;

        var param = method.Parameters?.FirstOrDefault(p => p.Identifier?.Sequence == paramName);
        if (param == null)
            return null;

        // Use analyzer to compute expected types (including usage constraints)
        var analyzer = new GDParameterTypeAnalyzer(_runtimeProvider, _typeEngine);
        var expectedUnion = analyzer.ComputeExpectedTypes(param, method, includeUsageConstraints: true);

        var actualUnion = new GDUnionType();
        var key = BuildParameterKey(methodName, paramName);
        if (_callSiteParameterTypes.TryGetValue(key, out var callSiteUnion) && callSiteUnion != null)
        {
            foreach (var type in callSiteUnion.Types)
            {
                actualUnion.AddType(type, isHighConfidence: false);
            }
        }

        // Compute the diff
        return GDParameterTypeDiff.Create(paramName, expectedUnion, actualUnion, _runtimeProvider);
    }

    /// <summary>
    /// Gets the call site argument types for a parameter (from external callers).
    /// </summary>
    /// <param name="methodName">The method name.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>Union of types passed at call sites, or null if none.</returns>
    public GDUnionType? GetCallSiteTypes(string methodName, string paramName)
    {
        if (string.IsNullOrEmpty(methodName) || string.IsNullOrEmpty(paramName))
            return null;

        var key = BuildParameterKey(methodName, paramName);
        return _callSiteParameterTypes.TryGetValue(key, out var union) ? union : null;
    }

    /// <summary>
    /// Gets the unified type diff for ANY AST node.
    /// This is the primary API for comparing expected vs actual types.
    ///
    /// The diff includes:
    /// - Expected types: from annotations, type guards, typeof checks, match patterns, asserts
    /// - Actual types: from assignments, call site arguments, initializers, flow analysis
    /// - Duck constraints: inferred from method calls, property accesses on the value
    /// - Narrowed type: flow-sensitive type at this specific location
    ///
    /// Works for:
    /// - Parameters (type guards, call site arguments)
    /// - Variables (annotations, assignments)
    /// - Expressions (inferred types)
    /// - Method declarations (return type analysis)
    /// - Identifiers (resolved to their declaration)
    /// </summary>
    /// <param name="node">Any AST node to analyze.</param>
    /// <returns>Type diff with expected vs actual comparison.</returns>
    public GDTypeDiff GetTypeDiffForNode(GDNode node)
    {
        if (node == null)
            return GDTypeDiff.Empty(node);

        var analyzer = new GDNodeTypeAnalyzer(this, _runtimeProvider, _typeEngine);
        return analyzer.Analyze(node);
    }

    #endregion

    #region Container Queries

    /// <summary>
    /// Gets the container usage profile for a variable.
    /// </summary>
    public GDContainerUsageProfile? GetContainerProfile(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        return _containerProfiles.TryGetValue(variableName, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets the inferred container element type.
    /// </summary>
    public GDContainerElementType? GetInferredContainerType(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        // Check cache first
        if (_containerTypeCache.TryGetValue(variableName, out var cached))
            return cached;

        // Compute from profile
        var profile = GetContainerProfile(variableName);
        if (profile == null)
            return null;

        var containerType = profile.ComputeInferredType();
        EnrichUnionTypeIfNeeded(containerType.ElementUnionType);
        EnrichUnionTypeIfNeeded(containerType.KeyUnionType);
        _containerTypeCache[variableName] = containerType;
        return containerType;
    }

    /// <summary>
    /// Gets all container usage profiles (for UI display).
    /// </summary>
    public IEnumerable<GDContainerUsageProfile> GetAllContainerProfiles()
    {
        return _containerProfiles.Values;
    }

    /// <summary>
    /// Gets GDContainerElementType for an expression (works with objects, not strings).
    /// Used for array addition and other container operations.
    /// </summary>
    private GDContainerElementType? GetContainerTypeForExpression(GDExpression? expr)
    {
        if (expr == null) return null;

        // For array addition - recursively combine
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

        // For identifiers - check container profile first, then look up variable declaration
        if (expr is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                // First try container profile (for variables with direct array usage)
                var containerType = GetInferredContainerType(varName);
                if (containerType != null)
                    return containerType;

                // For variables, look up their declaration
                var varDecl = FindLocalVariableDeclaration(identExpr, varName);
                if (varDecl != null)
                {
                    // If variable has explicit type annotation, use it
                    if (varDecl.Type != null)
                    {
                        var typeFromAnnotation = GDContainerElementType.FromTypeNode(varDecl.Type);
                        if (typeFromAnnotation != null)
                            return typeFromAnnotation;
                    }

                    // Otherwise try to infer from initializer
                    if (varDecl.Initializer != null)
                    {
                        return GetContainerTypeForExpression(varDecl.Initializer);
                    }
                }
            }
        }

        // Fallback to TypeEngine → GDTypeNode → GDContainerElementType
        var typeNode = _typeEngine?.InferTypeNode(expr);
        return GDContainerElementType.FromTypeNode(typeNode);
    }

    #endregion

    #region Confidence Analysis

    /// <summary>
    /// Gets the confidence level for a member access expression.
    /// </summary>
    public GDReferenceConfidence GetMemberAccessConfidence(GDMemberOperatorExpression memberAccess)
    {
        if (memberAccess?.CallerExpression == null)
            return GDReferenceConfidence.Potential;

        var callerType = GetExpressionType(memberAccess.CallerExpression);

        // Type is known and concrete
        if (IsConcreteType(callerType))
            return GDReferenceConfidence.Strict;

        // For indexer-based member access (e.g., dict[key].property, dict[key].method()),
        // try to get element type from class-level container profiles first.
        // If we know the element type, verify the member exists on that type.
        if (memberAccess.CallerExpression is GDIndexerExpression indexerExpr)
        {
            return GetIndexerMemberAccessConfidence(indexerExpr, memberAccess);
        }

        // Check for type narrowing and Union types
        var varName = GetRootVariableName(memberAccess.CallerExpression);
        if (!string.IsNullOrEmpty(varName))
        {
            var narrowed = GetNarrowedType(varName, memberAccess);
            if (!string.IsNullOrEmpty(narrowed))
                return GDReferenceConfidence.Strict;

            // Check narrowing context for duck type constraints (P1, P8, P10)
            // If there's a narrowing context with any requirements or excluded types, consider it Potential
            var narrowingContext = FindNarrowingContextForNode(memberAccess);
            if (narrowingContext != null)
            {
                var narrowedInfo = narrowingContext.GetNarrowedType(varName);
                if (narrowedInfo != null)
                {
                    // P1: "method" in obj - has RequiredMethods
                    // If the required method matches what we're calling, it's safe
                    var memberName = memberAccess.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(memberName) && narrowedInfo.RequiredMethods.ContainsKey(memberName))
                        return GDReferenceConfidence.Potential;

                    // P8/P10: null excluded - still should allow method calls
                    // If null is excluded, the variable is known to be non-null
                    if (narrowedInfo.ExcludedTypes.Contains("null"))
                        return GDReferenceConfidence.Potential;
                }
            }

            // Check Union type (for Variant variables with tracked assignments)
            var unionType = GetUnionType(varName);
            if (unionType != null && !unionType.IsEmpty)
            {
                var memberName = memberAccess.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(memberName))
                {
                    return GetUnionMemberConfidence(unionType, memberName);
                }
            }

            // Check duck-type constraints for parameters
            // If a parameter has duck-type constraints collected from usage, and the member
            // being accessed is in those constraints AND exists in TypesMap, it's a Potential reference
            var duckType = GetDuckType(varName);
            if (duckType != null && duckType.HasRequirements)
            {
                var memberName = memberAccess.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(memberName))
                {
                    // Only return Potential if the method/property exists in known types
                    // This ensures unknown members still get NameMatch (warning)
                    if (_runtimeProvider is GDGodotTypesProvider typesProvider)
                    {
                        // If the called method is in required methods AND exists in TypesMap - it's duck-typed
                        if (duckType.RequiredMethods.ContainsKey(memberName))
                        {
                            var typesWithMethod = typesProvider.FindTypesWithMethod(memberName);
                            if (typesWithMethod.Count > 0)
                                return GDReferenceConfidence.Potential;
                        }

                        // If the accessed property is in required properties AND exists in TypesMap - it's duck-typed
                        if (duckType.RequiredProperties.ContainsKey(memberName))
                        {
                            var typesWithProperty = typesProvider.FindTypesWithProperty(memberName);
                            if (typesWithProperty.Count > 0)
                                return GDReferenceConfidence.Potential;
                        }
                    }
                    else if (_runtimeProvider is GDCompositeRuntimeProvider compositeProvider)
                    {
                        bool foundInTypes = false;

                        // Check Godot types
                        var godotProvider = compositeProvider.GodotTypesProvider;
                        if (godotProvider != null && !foundInTypes)
                        {
                            if (duckType.RequiredMethods.ContainsKey(memberName))
                            {
                                var typesWithMethod = godotProvider.FindTypesWithMethod(memberName);
                                if (typesWithMethod.Count > 0)
                                    foundInTypes = true;
                            }

                            if (!foundInTypes && duckType.RequiredProperties.ContainsKey(memberName))
                            {
                                var typesWithProperty = godotProvider.FindTypesWithProperty(memberName);
                                if (typesWithProperty.Count > 0)
                                    foundInTypes = true;
                            }
                        }

                        // Check project types
                        var projectProvider = compositeProvider.ProjectTypesProvider;
                        if (projectProvider != null && !foundInTypes)
                        {
                            if (duckType.RequiredMethods.ContainsKey(memberName))
                            {
                                var typesWithMethod = projectProvider.FindTypesWithMethod(memberName);
                                if (typesWithMethod.Count > 0)
                                    foundInTypes = true;
                            }

                            if (!foundInTypes && duckType.RequiredProperties.ContainsKey(memberName))
                            {
                                var typesWithProperty = projectProvider.FindTypesWithProperty(memberName);
                                if (typesWithProperty.Count > 0)
                                    foundInTypes = true;
                            }
                        }

                        if (foundInTypes)
                            return GDReferenceConfidence.Potential;
                    }
                }
            }
        }

        // Type is Variant or unknown without type guard - this is unguarded access
        return GDReferenceConfidence.NameMatch;
    }

    /// <summary>
    /// Gets the confidence level for any identifier.
    /// For simple identifiers, always Strict. For member access, delegates to GetMemberAccessConfidence.
    /// </summary>
    public GDReferenceConfidence GetIdentifierConfidence(GDIdentifier identifier)
    {
        if (identifier == null)
            return GDReferenceConfidence.NameMatch;

        var parent = identifier.Parent;

        // Member access - check caller type
        if (parent is GDMemberOperatorExpression memberOp && memberOp.Identifier == identifier)
            return GetMemberAccessConfidence(memberOp);

        // Simple identifier - always strict (scope is statically known)
        // This includes:
        // - Local variables
        // - Parameters
        // - Class members (implicit self)
        // - Inherited members (inheritance is static)
        // - Globals (autoloads)
        return GDReferenceConfidence.Strict;
    }

    /// <summary>
    /// Builds a human-readable reason for confidence determination.
    /// </summary>
    public string? GetConfidenceReason(GDIdentifier identifier)
    {
        if (identifier == null)
            return null;

        var parent = identifier.Parent;

        if (parent is GDMemberOperatorExpression memberOp && memberOp.Identifier == identifier)
        {
            var callerType = memberOp.CallerExpression != null
                ? GetExpressionType(memberOp.CallerExpression)
                : null;

            if (!string.IsNullOrEmpty(callerType) && callerType != "Variant")
                return $"Caller type is '{callerType}'";

            var varName = memberOp.CallerExpression != null
                ? GetRootVariableName(memberOp.CallerExpression)
                : null;

            if (!string.IsNullOrEmpty(varName))
            {
                var narrowed = GetNarrowedType(varName, memberOp);
                if (narrowed != null)
                    return $"Variable '{varName}' narrowed to '{narrowed}' by control flow";

                var duckType = GetDuckType(varName);
                if (duckType != null)
                    return $"Variable '{varName}' is duck-typed";

                return $"Variable '{varName}' type is unknown";
            }

            return "Caller expression type unknown";
        }

        // Simple identifier
        var symbol = FindSymbol(identifier.Sequence ?? "");
        if (symbol != null)
        {
            if (symbol.IsInherited)
                return $"Inherited member from {symbol.DeclaringTypeName}";
            if (symbol.Kind == GDSymbolKind.Parameter)
                return "Method parameter";
            if (symbol.Kind == GDSymbolKind.Variable && symbol.DeclaringTypeName == null)
                return "Local variable";
            if (symbol.DeclaringTypeName != null)
                return $"Class member in {symbol.DeclaringTypeName}";
        }

        return "Symbol in scope";
    }

    #endregion

    #region Scope Filtering APIs

    /// <summary>
    /// Gets the scope type where the symbol was declared.
    /// Inferred from the declaration node type.
    /// </summary>
    public GDScopeType? GetDeclarationScopeType(GDSymbolInfo symbol)
    {
        if (symbol?.DeclarationNode == null)
            return null;

        return symbol.DeclarationNode switch
        {
            // Class-level declarations
            GDMethodDeclaration => GDScopeType.Class,
            GDVariableDeclaration => GDScopeType.Class,
            GDSignalDeclaration => GDScopeType.Class,
            GDEnumDeclaration => GDScopeType.Class,
            GDEnumValueDeclaration => GDScopeType.Class,
            GDInnerClassDeclaration => GDScopeType.Class,

            // Method-level declarations
            GDVariableDeclarationStatement => GDScopeType.Method,
            GDParameterDeclaration => GDScopeType.Method,

            // Loop-level declarations
            GDForStatement => GDScopeType.ForLoop,

            // Match case declarations
            GDMatchCaseVariableExpression => GDScopeType.Match,

            // Lambda declarations (parameters)
            GDMethodExpression => GDScopeType.Lambda,

            _ => null
        };
    }

    /// <summary>
    /// Gets references to a symbol filtered by scope type.
    /// </summary>
    public IEnumerable<GDReference> GetReferencesInScope(GDSymbolInfo symbol, GDScopeType scopeType)
    {
        var refs = GetReferencesTo(symbol);
        return refs.Where(r => r.Scope?.Type == scopeType);
    }

    /// <summary>
    /// Gets references to a symbol filtered by multiple scope types.
    /// </summary>
    public IEnumerable<GDReference> GetReferencesInScopes(GDSymbolInfo symbol, params GDScopeType[] scopeTypes)
    {
        if (scopeTypes == null || scopeTypes.Length == 0)
            return GetReferencesTo(symbol);

        var scopeSet = new HashSet<GDScopeType>(scopeTypes);
        var refs = GetReferencesTo(symbol);
        return refs.Where(r => r.Scope != null && scopeSet.Contains(r.Scope.Type));
    }

    /// <summary>
    /// Gets references only within method/lambda scope (local references).
    /// This includes Method, Lambda, ForLoop, WhileLoop, Conditional, Match, and Block scopes.
    /// </summary>
    public IEnumerable<GDReference> GetLocalReferences(GDSymbolInfo symbol)
    {
        var refs = GetReferencesTo(symbol);
        return refs.Where(r => IsLocalScope(r.Scope?.Type));
    }

    /// <summary>
    /// Determines if a symbol is a local variable (declared in method/lambda scope).
    /// Local symbols include: local variables, parameters, for-loop iterators, match case variables.
    /// </summary>
    public bool IsLocalSymbol(GDSymbolInfo symbol)
    {
        if (symbol == null)
            return false;

        // Check by symbol kind first
        switch (symbol.Kind)
        {
            case GDSymbolKind.Parameter:
            case GDSymbolKind.Iterator:
                return true;
            case GDSymbolKind.Method:
            case GDSymbolKind.Signal:
            case GDSymbolKind.Enum:
            case GDSymbolKind.EnumValue:
            case GDSymbolKind.Class:
            case GDSymbolKind.Constant when IsClassLevelDeclaration(symbol.DeclarationNode):
                return false;
        }

        // For variables, check declaration type
        var scopeType = GetDeclarationScopeType(symbol);
        return scopeType != null && IsLocalScope(scopeType.Value);
    }

    /// <summary>
    /// Determines if a symbol is a class member (declared in class scope).
    /// Class members include: methods, signals, class-level variables, constants, enums, inner classes.
    /// </summary>
    public bool IsClassMember(GDSymbolInfo symbol)
    {
        if (symbol == null)
            return false;

        // Check by symbol kind first
        switch (symbol.Kind)
        {
            case GDSymbolKind.Method:
            case GDSymbolKind.Signal:
            case GDSymbolKind.Enum:
            case GDSymbolKind.EnumValue:
            case GDSymbolKind.Class:
                return true;
            case GDSymbolKind.Parameter:
            case GDSymbolKind.Iterator:
                return false;
        }

        // For variables/constants, check declaration type
        var scopeType = GetDeclarationScopeType(symbol);
        return scopeType == GDScopeType.Class || scopeType == GDScopeType.Global;
    }

    /// <summary>
    /// Checks if the scope type is a local (non-class) scope.
    /// </summary>
    private static bool IsLocalScope(GDScopeType? scopeType)
    {
        if (scopeType == null)
            return false;

        return scopeType.Value switch
        {
            GDScopeType.Method => true,
            GDScopeType.Lambda => true,
            GDScopeType.ForLoop => true,
            GDScopeType.WhileLoop => true,
            GDScopeType.Conditional => true,
            GDScopeType.Match => true,
            GDScopeType.Block => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if the declaration is at class level.
    /// </summary>
    private static bool IsClassLevelDeclaration(GDNode? declaration)
    {
        return declaration is GDVariableDeclaration or
            GDMethodDeclaration or
            GDSignalDeclaration or
            GDEnumDeclaration or
            GDInnerClassDeclaration;
    }

    /// <summary>
    /// Gets the enclosing method/lambda scope for a reference.
    /// </summary>
    public GDScope? GetEnclosingMethodScope(GDReference reference)
    {
        var scope = reference?.Scope;
        while (scope != null)
        {
            if (scope.Type == GDScopeType.Method || scope.Type == GDScopeType.Lambda)
                return scope;
            scope = scope.Parent;
        }
        return null;
    }

    /// <summary>
    /// Gets references within the same method/lambda as the symbol's declaration.
    /// For local symbols, this returns references in the declaring method only.
    /// For class members, this returns all references.
    /// </summary>
    public IEnumerable<GDReference> GetReferencesInDeclaringScope(GDSymbolInfo symbol)
    {
        if (!IsLocalSymbol(symbol))
        {
            // Class members can be referenced from any method
            return GetReferencesTo(symbol);
        }

        // For local symbols, find the declaring method node
        var declaringMethodNode = GetDeclaringMethodNode(symbol);
        if (declaringMethodNode == null)
            return GetReferencesTo(symbol);

        // Filter references to those in the same method
        return GetReferencesTo(symbol).Where(r =>
        {
            var enclosingMethod = GetEnclosingMethodScope(r);
            return enclosingMethod?.Node == declaringMethodNode;
        });
    }

    /// <summary>
    /// Gets the method/lambda node that contains the symbol declaration.
    /// </summary>
    private GDNode? GetDeclaringMethodNode(GDSymbolInfo symbol)
    {
        if (symbol?.DeclarationNode == null)
            return null;

        // Walk up the AST to find the enclosing method
        var node = symbol.DeclarationNode;
        while (node != null)
        {
            if (node is GDMethodDeclaration or GDMethodExpression)
                return node;
            node = node.Parent as GDNode;
        }
        return null;
    }

    #endregion

    #region Type Engine Delegation

    /// <summary>
    /// Gets the expected type at a position (reverse type inference).
    /// </summary>
    public string? GetExpectedType(GDNode node)
    {
        return _typeEngine?.InferExpectedType(node);
    }

    /// <summary>
    /// Checks if two types are compatible.
    /// </summary>
    public bool AreTypesCompatible(string sourceType, string targetType)
    {
        return _typeEngine?.AreTypesCompatible(sourceType, targetType) ?? true;
    }

    #endregion

    #region Convenience Methods

    /// <summary>
    /// Gets symbols of a specific kind.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetSymbolsOfKind(GDSymbolKind kind)
    {
        return Symbols.Where(s => s.Kind == kind);
    }

    /// <summary>
    /// Gets all method symbols.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetMethods() => GetSymbolsOfKind(GDSymbolKind.Method);

    /// <summary>
    /// Gets all variable symbols (class-level).
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetVariables() => GetSymbolsOfKind(GDSymbolKind.Variable);

    /// <summary>
    /// Gets all signal symbols.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetSignals() => GetSymbolsOfKind(GDSymbolKind.Signal);

    /// <summary>
    /// Gets all constant symbols.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetConstants() => GetSymbolsOfKind(GDSymbolKind.Constant);

    /// <summary>
    /// Gets all enum symbols.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetEnums() => GetSymbolsOfKind(GDSymbolKind.Enum);

    /// <summary>
    /// Gets all inner class symbols.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetInnerClasses() => GetSymbolsOfKind(GDSymbolKind.Class);

    /// <summary>
    /// Gets the declaration node for a symbol.
    /// </summary>
    public GDNode? GetDeclaration(string symbolName)
    {
        var symbol = FindSymbol(symbolName);
        return symbol?.DeclarationNode;
    }

    #endregion

    #region Internal Modification Methods (for collector)

    /// <summary>
    /// Sets the Callable call site registry.
    /// </summary>
    internal void SetCallSiteRegistry(GDCallableCallSiteRegistry registry)
    {
        _callSiteRegistry = registry;
    }

    /// <summary>
    /// Gets or creates the Callable call site registry.
    /// </summary>
    internal GDCallableCallSiteRegistry GetOrCreateCallSiteRegistry()
    {
        _callSiteRegistry ??= new GDCallableCallSiteRegistry();
        return _callSiteRegistry;
    }

    /// <summary>
    /// Infers lambda parameter types from call sites.
    /// </summary>
    public IReadOnlyDictionary<int, GDUnionType> InferLambdaParameterTypesFromCallSites(GDMethodExpression lambda)
    {
        if (_callSiteRegistry == null || lambda == null)
            return new Dictionary<int, GDUnionType>();

        return _callSiteRegistry.InferParameterTypes(lambda, _scriptFile);
    }

    /// <summary>
    /// Infers a specific lambda parameter type from call sites.
    /// </summary>
    public string? InferLambdaParameterTypeFromCallSites(GDMethodExpression lambda, int parameterIndex)
    {
        if (_callSiteRegistry == null || lambda == null)
            return null;

        return _callSiteRegistry.InferParameterType(lambda, _scriptFile, parameterIndex);
    }

    /// <summary>
    /// Infers lambda parameter types including inter-procedural analysis.
    /// This includes call sites from method parameters when the lambda is passed as argument.
    /// </summary>
    public IReadOnlyDictionary<int, GDUnionType> InferLambdaParameterTypesWithFlow(GDMethodExpression lambda)
    {
        if (_callSiteRegistry == null || lambda == null)
            return new Dictionary<int, GDUnionType>();

        return _callSiteRegistry.InferParameterTypesWithFlow(lambda, _scriptFile);
    }

    /// <summary>
    /// Infers a specific lambda parameter type with inter-procedural analysis.
    /// </summary>
    public string? InferLambdaParameterTypeWithFlow(GDMethodExpression lambda, int parameterIndex)
    {
        if (_callSiteRegistry == null || lambda == null)
            return null;

        return _callSiteRegistry.InferParameterTypeWithFlow(lambda, _scriptFile, parameterIndex);
    }

    /// <summary>
    /// Gets the method Callable profile for a method.
    /// </summary>
    public GDMethodCallableProfile? GetMethodCallableProfile(string methodName)
    {
        if (_callSiteRegistry == null || string.IsNullOrEmpty(methodName))
            return null;

        var className = _scriptFile?.Class?.ClassName?.Identifier?.Sequence;
        var methodKey = GDMethodCallableProfile.CreateMethodKey(className, methodName);
        return _callSiteRegistry.GetMethodProfile(methodKey);
    }

    /// <summary>
    /// Gets argument bindings for a lambda (where it's passed to method parameters).
    /// </summary>
    public IReadOnlyList<GDCallableArgumentBinding> GetLambdaArgumentBindings(GDMethodExpression lambda)
    {
        if (_callSiteRegistry == null || lambda == null)
            return Array.Empty<GDCallableArgumentBinding>();

        return _callSiteRegistry.GetBindingsForLambda(lambda, _scriptFile);
    }

    /// <summary>
    /// Registers a symbol in the model.
    /// </summary>
    internal void RegisterSymbol(GDSymbolInfo symbol) => _symbolRegistry.RegisterSymbol(symbol);

    /// <summary>
    /// Registers a node-to-symbol mapping.
    /// </summary>
    internal void SetNodeSymbol(GDNode node, GDSymbolInfo symbol) => _symbolRegistry.RegisterNodeSymbol(node, symbol);

    /// <summary>
    /// Adds a reference to a symbol.
    /// </summary>
    internal void AddReference(GDSymbolInfo symbol, GDReference reference) => _symbolRegistry.RegisterReference(symbol, reference);

    /// <summary>
    /// Adds a member access reference indexed by caller type and member name.
    /// Used for querying built-in method calls like OS.execute() or global functions like str2var().
    /// </summary>
    internal void AddMemberAccess(string callerType, string memberName, GDReference reference) => _symbolRegistry.RegisterMemberAccess(callerType, memberName, reference);

    /// <summary>
    /// Sets the inferred type for a node.
    /// </summary>
    internal void SetNodeType(GDNode node, string type, GDTypeNode? typeNode = null)
    {
        if (node == null)
            return;

        if (!string.IsNullOrEmpty(type))
            _nodeTypes[node] = type;

        if (typeNode != null)
            _nodeTypeNodes[node] = typeNode;
    }

    /// <summary>
    /// Sets duck type information for a variable.
    /// </summary>
    internal void SetDuckType(string variableName, GDDuckType duckType)
    {
        if (!string.IsNullOrEmpty(variableName) && duckType != null)
            _duckTypes[variableName] = duckType;
    }

    /// <summary>
    /// Sets narrowing context for a node.
    /// </summary>
    internal void SetNarrowingContext(GDNode node, GDTypeNarrowingContext context)
    {
        if (node != null && context != null)
            _narrowingContexts[node] = context;
    }

    /// <summary>
    /// Sets narrowing context for statements following an if-statement with early return.
    /// The context applies to sibling statements that come after the if-statement.
    /// </summary>
    internal void SetPostIfNarrowing(GDIfStatement ifStatement, GDTypeNarrowingContext context)
    {
        if (ifStatement == null || context == null)
            return;

        // Get parent statements list and find statements after this if-statement
        if (ifStatement.Parent is GDStatementsList statementsList)
        {
            bool foundIf = false;
            foreach (var statement in statementsList)
            {
                if (foundIf)
                {
                    // Apply narrowing context to all statements after the if
                    _narrowingContexts[statement] = context;
                }
                if (ReferenceEquals(statement, ifStatement))
                    foundIf = true;
            }
        }
    }

    /// <summary>
    /// Adds a type usage to the model.
    /// </summary>
    internal void AddTypeUsage(string typeName, GDNode node, GDTypeUsageKind kind)
    {
        if (string.IsNullOrEmpty(typeName) || node == null)
            return;

        if (!_typeUsages.TryGetValue(typeName, out var usages))
        {
            usages = new List<GDTypeUsage>();
            _typeUsages[typeName] = usages;
        }

        usages.Add(new GDTypeUsage(typeName, node, kind));
    }

    /// <summary>
    /// Sets a variable usage profile (for Union type inference).
    /// </summary>
    internal void SetVariableProfile(string variableName, GDVariableUsageProfile profile)
    {
        if (!string.IsNullOrEmpty(variableName) && profile != null)
        {
            _variableProfiles[variableName] = profile;
            // Clear cache when profile is updated
            _unionTypeCache.Remove(variableName);
        }
    }

    /// <summary>
    /// Sets a container usage profile (for container element type inference).
    /// </summary>
    internal void SetContainerProfile(string variableName, GDContainerUsageProfile profile)
    {
        if (!string.IsNullOrEmpty(variableName) && profile != null)
        {
            _containerProfiles[variableName] = profile;
            // Clear cache when profile is updated
            _containerTypeCache.Remove(variableName);
        }
    }

    /// <summary>
    /// Sets a class-level container usage profile.
    /// </summary>
    internal void SetClassContainerProfile(string className, string variableName, GDContainerUsageProfile profile)
    {
        if (!string.IsNullOrEmpty(className) && !string.IsNullOrEmpty(variableName) && profile != null)
        {
            var key = $"{className}.{variableName}";
            _classContainerProfiles[key] = profile;
        }
    }

    /// <summary>
    /// Gets a class-level container usage profile.
    /// </summary>
    public GDContainerUsageProfile? GetClassContainerProfile(string className, string variableName)
    {
        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(variableName))
            return null;

        var key = $"{className}.{variableName}";
        return _classContainerProfiles.TryGetValue(key, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets all class-level container profiles.
    /// </summary>
    public IReadOnlyDictionary<string, GDContainerUsageProfile> ClassContainerProfiles => _classContainerProfiles;

    /// <summary>
    /// Gets a class-level container profile merged with cross-file usages.
    /// This method combines local profile with usages collected from external files.
    /// </summary>
    /// <param name="className">The class name containing the container.</param>
    /// <param name="variableName">The container variable name.</param>
    /// <param name="project">Optional project for cross-file collection.</param>
    /// <returns>Merged container profile, or null if not found.</returns>
    public GDContainerUsageProfile? GetMergedContainerProfile(
        string className,
        string variableName,
        GDScriptProject? project)
    {
        // Get local profile first
        var localProfile = GetClassContainerProfile(className, variableName);
        if (localProfile == null)
            return null;

        // If no project provided, return local profile only
        if (project == null)
            return localProfile;

        // Collect cross-file usages and merge
        var crossCollector = new GDCrossFileContainerUsageCollector(project);
        var crossUsages = crossCollector.CollectUsages(className, variableName);

        if (crossUsages.Count == 0)
            return localProfile;

        // Merge profiles
        return GDCrossFileContainerUsageCollector.MergeProfiles(localProfile, crossUsages);
    }

    /// <summary>
    /// Sets call site argument types for a parameter.
    /// This is used to inject project-level call site data into the file-level semantic model.
    /// </summary>
    /// <param name="methodName">The method name.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="callSiteTypes">The union of argument types from call sites.</param>
    internal void SetCallSiteParameterTypes(string methodName, string paramName, GDUnionType callSiteTypes)
    {
        if (string.IsNullOrEmpty(methodName) || string.IsNullOrEmpty(paramName) || callSiteTypes == null)
            return;

        var key = BuildParameterKey(methodName, paramName);
        _callSiteParameterTypes[key] = callSiteTypes;

        // Clear union type cache for this parameter
        _unionTypeCache.Remove(paramName);
    }

    /// <summary>
    /// Sets call site argument types from a method inference report.
    /// </summary>
    /// <param name="report">The method inference report containing call site data.</param>
    internal void SetCallSiteTypesFromReport(GDMethodInferenceReport report)
    {
        if (report == null)
            return;

        foreach (var (paramName, paramReport) in report.Parameters)
        {
            if (paramReport.InferredUnionType != null && !paramReport.InferredUnionType.IsEmpty)
            {
                SetCallSiteParameterTypes(report.MethodName, paramName, paramReport.InferredUnionType);
            }
        }
    }

    #endregion

    #region Helper Methods

    private string? GetRootVariableName(GDExpression? expr)
    {
        while (expr is GDMemberOperatorExpression member)
            expr = member.CallerExpression;
        while (expr is GDIndexerExpression indexer)
            expr = indexer.CallerExpression;

        return (expr as GDIdentifierExpression)?.Identifier?.Sequence;
    }

    /// <summary>
    /// Enriches a union type with common base type if available.
    /// Centralizes the repeated enrichment pattern.
    /// </summary>
    private void EnrichUnionTypeIfNeeded(GDUnionType? union)
    {
        if (_runtimeProvider == null || union == null || !union.IsUnion)
            return;

        var resolver = new GDUnionTypeResolver(_runtimeProvider);
        resolver.EnrichUnionType(union);
    }

    /// <summary>
    /// Builds a parameter key for call site type lookup.
    /// Format: "methodName.paramName"
    /// </summary>
    private static string BuildParameterKey(string methodName, string paramName)
    {
        return $"{methodName}.{paramName}";
    }

    /// <summary>
    /// Checks if a type name represents a concrete (non-Variant, non-Unknown) type.
    /// </summary>
    private static bool IsConcreteType(string? typeName)
    {
        return !string.IsNullOrEmpty(typeName)
            && typeName != "Variant"
            && !typeName.StartsWith("Unknown");
    }

    /// <summary>
    /// Traverses the inheritance chain looking for a matching result.
    /// Handles cycle detection automatically.
    /// </summary>
    private T? TraverseInheritanceChain<T>(string typeName, Func<string, T?> finder) where T : class
    {
        if (_runtimeProvider == null || string.IsNullOrEmpty(typeName))
            return default;

        var visited = new HashSet<string>();
        var current = typeName;

        while (!string.IsNullOrEmpty(current))
        {
            if (!visited.Add(current))
                break; // Cycle detection

            var result = finder(current);
            if (result != null)
                return result;

            current = _runtimeProvider.GetBaseType(current);
        }

        return default;
    }

    #endregion

    #region IGDMemberAccessAnalyzer Implementation

    /// <summary>
    /// Explicit interface implementation for IGDMemberAccessAnalyzer.GetMemberAccessConfidence.
    /// </summary>
    GDReferenceConfidence IGDMemberAccessAnalyzer.GetMemberAccessConfidence(object memberAccess)
    {
        if (memberAccess is GDMemberOperatorExpression memberExpr)
            return GetMemberAccessConfidence(memberExpr);
        return GDReferenceConfidence.NameMatch;
    }

    /// <summary>
    /// Explicit interface implementation for IGDMemberAccessAnalyzer.GetExpressionType.
    /// </summary>
    string? IGDMemberAccessAnalyzer.GetExpressionType(object expression)
    {
        if (expression is GDExpression expr)
            return GetExpressionType(expr);
        return null;
    }

    /// <summary>
    /// Checks if the type name refers to a local enum declaration.
    /// </summary>
    bool IGDMemberAccessAnalyzer.IsLocalEnum(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        var symbols = FindSymbols(typeName);
        return symbols.Any(s => s.Kind == GDSymbolKind.Enum);
    }

    /// <summary>
    /// Checks if a member name is a valid value for a local enum.
    /// </summary>
    bool IGDMemberAccessAnalyzer.IsLocalEnumValue(string enumTypeName, string memberName)
    {
        if (string.IsNullOrEmpty(enumTypeName) || string.IsNullOrEmpty(memberName))
            return false;

        var enumSymbol = FindSymbols(enumTypeName)
            .FirstOrDefault(s => s.Kind == GDSymbolKind.Enum);

        if (enumSymbol?.DeclarationNode is not GDEnumDeclaration enumDecl)
            return false;

        return enumDecl.Values?.Any(v => v.Identifier?.Sequence == memberName) ?? false;
    }

    /// <summary>
    /// Checks if the type name refers to a local inner class declaration.
    /// </summary>
    bool IGDMemberAccessAnalyzer.IsLocalInnerClass(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        var symbols = FindSymbols(typeName);
        return symbols.Any(s => s.Kind == GDSymbolKind.Class);
    }

    /// <summary>
    /// Gets a member from a local inner class.
    /// </summary>
    GDRuntimeMemberInfo? IGDMemberAccessAnalyzer.GetInnerClassMember(string innerClassName, string memberName)
    {
        if (string.IsNullOrEmpty(innerClassName) || string.IsNullOrEmpty(memberName))
            return null;

        var classSymbol = FindSymbols(innerClassName)
            .FirstOrDefault(s => s.Kind == GDSymbolKind.Class);

        if (classSymbol?.DeclarationNode is not GDInnerClassDeclaration innerClass)
            return null;

        // Check members for property or method
        foreach (var member in innerClass.Members)
        {
            if (member is GDVariableDeclaration varDecl &&
                varDecl.Identifier?.Sequence == memberName)
            {
                var varType = varDecl.Type?.BuildName() ?? "Variant";
                return GDRuntimeMemberInfo.Property(memberName, varType, false);
            }

            if (member is GDMethodDeclaration methodDecl &&
                methodDecl.Identifier?.Sequence == memberName)
            {
                var returnType = methodDecl.ReturnType?.BuildName() ?? "Variant";
                var paramCount = methodDecl.Parameters?.Count ?? 0;
                return GDRuntimeMemberInfo.Method(memberName, returnType, paramCount, paramCount, isVarArgs: false, isStatic: methodDecl.IsStatic);
            }
        }

        // Check base type inheritance chain
        var baseTypeName = innerClass.BaseType?.BuildName();
        if (!string.IsNullOrEmpty(baseTypeName))
        {
            return FindMemberWithInheritanceInternal(baseTypeName, memberName);
        }

        return null;
    }

    #endregion

    #region IGDArgumentTypeAnalyzer Implementation

    /// <summary>
    /// Gets the type diff for a call expression argument at the given index.
    /// </summary>
    GDArgumentTypeDiff? IGDArgumentTypeAnalyzer.GetArgumentTypeDiff(object callExpression, int argumentIndex)
    {
        if (!(callExpression is GDCallExpression call))
            return null;

        return GetArgumentTypeDiffInternal(call, argumentIndex);
    }

    /// <summary>
    /// Gets all argument type diffs for a call expression.
    /// </summary>
    IEnumerable<GDArgumentTypeDiff> IGDArgumentTypeAnalyzer.GetAllArgumentTypeDiffs(object callExpression)
    {
        if (!(callExpression is GDCallExpression call))
            return Enumerable.Empty<GDArgumentTypeDiff>();

        return GetAllArgumentTypeDiffsInternal(call);
    }

    /// <summary>
    /// Gets the inferred type of an expression.
    /// </summary>
    string? IGDArgumentTypeAnalyzer.GetExpressionType(object expression)
    {
        if (expression is GDExpression expr)
            return GetExpressionType(expr);
        return null;
    }

    /// <summary>
    /// Gets the source description for an expression type.
    /// </summary>
    string? IGDArgumentTypeAnalyzer.GetExpressionTypeSource(object expression)
    {
        if (expression is GDExpression expr)
            return GetExpressionTypeSource(expr);
        return null;
    }

    /// <summary>
    /// Internal implementation of GetArgumentTypeDiff.
    /// </summary>
    private GDArgumentTypeDiff? GetArgumentTypeDiffInternal(GDCallExpression call, int argumentIndex)
    {
        var args = call.Parameters?.ToList();
        if (args == null || argumentIndex >= args.Count)
            return null;

        var arg = args[argumentIndex];

        var (methodDecl, parameterInfo) = ResolveCalledMethod(call, argumentIndex);
        var actualType = GetExpressionType(arg);
        var actualSource = GetExpressionTypeSource(arg);

        // If we have a method declaration with parameter info
        if (methodDecl != null)
        {
            var parameters = methodDecl.Parameters?.ToList();
            if (parameters != null && argumentIndex < parameters.Count)
            {
                var param = parameters[argumentIndex];
                var paramName = param.Identifier?.Sequence;

                // Get expected type directly from parameter annotation
                // Avoid GetTypeDiffForNode to prevent potential infinite loops
                var explicitType = param.Type?.BuildName();
                if (string.IsNullOrEmpty(explicitType))
                {
                    // No explicit type - skip validation (Variant parameter)
                    return GDArgumentTypeDiff.Skip(argumentIndex, paramName);
                }

                var expectedTypes = new List<string> { explicitType };
                var expectedSource = "type annotation";

                // Check compatibility
                var isCompatible = CheckTypeCompatibility(actualType, expectedTypes, null);
                var reason = isCompatible ? null : FormatIncompatibilityReason(actualType, expectedTypes, null);

                if (isCompatible)
                {
                    return GDArgumentTypeDiff.Compatible(
                        argumentIndex, paramName, actualType, actualSource,
                        expectedTypes, expectedSource,
                        GDReferenceConfidence.Strict);
                }
                else
                {
                    return GDArgumentTypeDiff.Incompatible(
                        argumentIndex, paramName, actualType, actualSource,
                        expectedTypes, expectedSource, reason!,
                        GDReferenceConfidence.Strict);
                }
            }
        }

        // If we have runtime parameter info (for built-in functions/methods)
        if (parameterInfo != null)
        {
            var expectedType = parameterInfo.Type;

            // Skip type checking for varargs parameters - they accept any arguments
            if (parameterInfo.IsParams)
            {
                return GDArgumentTypeDiff.Skip(argumentIndex, parameterInfo.Name);
            }

            if (string.IsNullOrEmpty(expectedType) || expectedType == "Variant")
            {
                return GDArgumentTypeDiff.Skip(argumentIndex, parameterInfo.Name);
            }

            var expectedTypes = new List<string> { expectedType };
            var isCompatible = CheckTypeCompatibility(actualType, expectedTypes, null);
            var reason = isCompatible ? null : FormatIncompatibilityReason(actualType, expectedTypes, null);

            if (isCompatible)
            {
                return GDArgumentTypeDiff.Compatible(
                    argumentIndex, parameterInfo.Name, actualType, actualSource,
                    expectedTypes, "function signature",
                    GDReferenceConfidence.Strict);
            }
            else
            {
                return GDArgumentTypeDiff.Incompatible(
                    argumentIndex, parameterInfo.Name, actualType, actualSource,
                    expectedTypes, "function signature", reason!,
                    GDReferenceConfidence.Strict);
            }
        }

        // Cannot determine parameter info - skip
        return null;
    }

    /// <summary>
    /// Internal implementation of GetAllArgumentTypeDiffs.
    /// </summary>
    private IEnumerable<GDArgumentTypeDiff> GetAllArgumentTypeDiffsInternal(GDCallExpression call)
    {
        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            yield break;

        for (int i = 0; i < args.Count; i++)
        {
            var diff = GetArgumentTypeDiffInternal(call, i);
            if (diff != null)
                yield return diff;
        }
    }

    /// <summary>
    /// Resolves the called method/function and returns parameter info.
    /// </summary>
    private (GDMethodDeclaration? method, GDRuntimeParameterInfo? paramInfo) ResolveCalledMethod(GDCallExpression call, int argIndex)
    {
        var caller = call.CallerExpression;

        // Direct function call
        if (caller is GDIdentifierExpression idExpr)
        {
            var funcName = idExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(funcName))
            {
                // Check user-defined functions first
                var symbol = FindSymbol(funcName);
                if (symbol?.DeclarationNode is GDMethodDeclaration method)
                {
                    return (method, null);
                }

                // Check built-in global functions
                if (_runtimeProvider != null)
                {
                    var funcInfo = _runtimeProvider.GetGlobalFunction(funcName);
                    if (funcInfo?.Parameters != null && argIndex < funcInfo.Parameters.Count)
                    {
                        return (null, funcInfo.Parameters[argIndex]);
                    }
                }

                // Fallback: Check methods of the base class (implicit self)
                // This handles calls like add_child() without explicit self. prefix
                if (_runtimeProvider != null)
                {
                    var baseType = _scriptFile?.Class?.Extends?.Type?.BuildName();
                    if (!string.IsNullOrEmpty(baseType))
                    {
                        var memberInfo = FindMemberWithInheritanceInternal(baseType, funcName);
                        if (memberInfo?.Parameters != null && argIndex < memberInfo.Parameters.Count)
                        {
                            return (null, memberInfo.Parameters[argIndex]);
                        }
                    }
                }
            }
        }
        // Method call: obj.method() or self.method()
        else if (caller is GDMemberOperatorExpression memberExpr)
        {
            var methodName = memberExpr.Identifier?.Sequence;
            var callerExprType = GetExpressionType(memberExpr.CallerExpression);

            if (!string.IsNullOrEmpty(methodName))
            {
                // self.method()
                if (memberExpr.CallerExpression is GDIdentifierExpression selfExpr &&
                    selfExpr.Identifier?.Sequence == "self")
                {
                    var symbol = FindSymbol(methodName);
                    if (symbol?.DeclarationNode is GDMethodDeclaration method)
                    {
                        return (method, null);
                    }
                }

                // Type.method() - check RuntimeProvider
                if (!string.IsNullOrEmpty(callerExprType) && _runtimeProvider != null)
                {
                    var memberInfo = FindMemberWithInheritanceInternal(callerExprType, methodName);
                    if (memberInfo?.Parameters != null && argIndex < memberInfo.Parameters.Count)
                    {
                        return (null, memberInfo.Parameters[argIndex]);
                    }
                }
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Gets the source description for an expression type.
    /// </summary>
    private string? GetExpressionTypeSource(GDExpression expr)
    {
        return expr switch
        {
            GDStringExpression => "string literal",
            GDNumberExpression num => num.Number?.Sequence?.Contains('.') == true ? "float literal" : "integer literal",
            GDBoolExpression => "boolean literal",
            GDArrayInitializerExpression => "array literal",
            GDDictionaryInitializerExpression => "dictionary literal",
            GDIdentifierExpression id when id.Identifier?.Sequence == "null" => "null literal",
            GDIdentifierExpression id => $"variable '{id.Identifier?.Sequence}'",
            GDMemberOperatorExpression mem => "property access",
            GDCallExpression => "function call result",
            GDIndexerExpression => "indexer access",
            _ => null
        };
    }

    /// <summary>
    /// Gets the source description for expected types.
    /// </summary>
    private string? GetExpectedTypeSource(GDTypeDiff paramDiff)
    {
        if (paramDiff.HasExpectedTypes && paramDiff.ExpectedTypes.Types.Any())
        {
            // Try to determine source from confidence
            if (paramDiff.Confidence == GDTypeConfidence.High || paramDiff.Confidence == GDTypeConfidence.Certain)
                return "type annotation";

            if (paramDiff.HasDuckConstraints)
                return "usage analysis";

            return "type guard";
        }

        return null;
    }

    /// <summary>
    /// Checks if actual type is compatible with any of the expected types.
    /// </summary>
    private bool CheckTypeCompatibility(string? actualType, IReadOnlyList<string> expectedTypes, GDDuckType? duckConstraints)
    {
        if (string.IsNullOrEmpty(actualType))
            return true; // Unknown type - can't validate

        if (expectedTypes.Count == 0 && (duckConstraints == null || !duckConstraints.HasRequirements))
            return true; // No constraints

        foreach (var expected in expectedTypes)
        {
            if (string.IsNullOrEmpty(expected) || expected == "Variant")
                return true;

            if (actualType == expected)
                return true;

            if (actualType == "null")
                return true; // null is assignable to any reference type

            if (_runtimeProvider?.IsAssignableTo(actualType, expected) == true)
                return true;
        }

        // Check duck typing constraints
        if (duckConstraints != null && duckConstraints.HasRequirements)
        {
            return CheckDuckTypeCompatibility(actualType, duckConstraints);
        }

        return expectedTypes.Count == 0; // If no expected types but has duck constraints that failed
    }

    /// <summary>
    /// Checks if a type satisfies duck typing constraints.
    /// </summary>
    private bool CheckDuckTypeCompatibility(string actualType, GDDuckType duckConstraints)
    {
        if (_runtimeProvider == null)
            return true; // Can't validate without runtime info

        // Check required methods
        foreach (var method in duckConstraints.RequiredMethods.Keys)
        {
            var memberInfo = FindMemberWithInheritanceInternal(actualType, method);
            if (memberInfo == null || memberInfo.Kind != GDRuntimeMemberKind.Method)
                return false;
        }

        // Check required properties
        foreach (var prop in duckConstraints.RequiredProperties.Keys)
        {
            var memberInfo = FindMemberWithInheritanceInternal(actualType, prop);
            if (memberInfo == null)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Formats the incompatibility reason message.
    /// </summary>
    private string FormatIncompatibilityReason(string? actualType, IReadOnlyList<string> expectedTypes, GDDuckType? duckConstraints)
    {
        actualType ??= "unknown";

        if (expectedTypes.Count == 1)
        {
            var expected = expectedTypes[0];

            if (_runtimeProvider != null)
            {
                var actualBase = _runtimeProvider.GetBaseType(actualType);
                var expectedBase = _runtimeProvider.GetBaseType(expected);

                if (_runtimeProvider.IsAssignableTo(expected, actualType))
                {
                    return $"'{actualType}' is not a subtype of '{expected}'. Hint: '{expected}' extends '{actualType}', but not vice versa";
                }
            }

            return $"'{actualType}' is not assignable to '{expected}'";
        }
        else if (expectedTypes.Count > 1)
        {
            return $"'{actualType}' is not among expected types [{string.Join(", ", expectedTypes)}]";
        }
        else if (duckConstraints != null && duckConstraints.HasRequirements)
        {
            var missing = new List<string>();

            foreach (var method in duckConstraints.RequiredMethods.Keys)
            {
                var memberInfo = _runtimeProvider != null
                    ? FindMemberWithInheritanceInternal(actualType, method)
                    : null;

                if (memberInfo == null || memberInfo.Kind != GDRuntimeMemberKind.Method)
                    missing.Add($"{method}()");
            }

            foreach (var prop in duckConstraints.RequiredProperties.Keys)
            {
                var memberInfo = _runtimeProvider != null
                    ? FindMemberWithInheritanceInternal(actualType, prop)
                    : null;

                if (memberInfo == null)
                    missing.Add(prop);
            }

            if (missing.Count > 0)
            {
                return $"Type '{actualType}' does not have: {string.Join(", ", missing)}";
            }
        }

        return $"'{actualType}' is not compatible";
    }

    /// <summary>
    /// Maps GDTypeConfidence to GDReferenceConfidence.
    /// </summary>
    private static GDReferenceConfidence MapConfidence(GDTypeConfidence confidence)
    {
        return confidence switch
        {
            GDTypeConfidence.Certain or GDTypeConfidence.High => GDReferenceConfidence.Strict,
            GDTypeConfidence.Medium => GDReferenceConfidence.Potential,
            _ => GDReferenceConfidence.NameMatch
        };
    }

    #endregion

    #region Cross-Method Flow Analysis

    private GDCrossMethodFlowState? _crossMethodState;
    private GDMethodFlowSummaryRegistry? _flowSummaryRegistry;

    /// <summary>
    /// Checks if a variable is safe to access at a given method, considering cross-method analysis.
    /// </summary>
    public bool IsVariableSafeAtMethod(string varName, string methodName)
    {
        EnsureCrossMethodAnalysis();

        if (_crossMethodState == null)
            return false;

        // If variable is not @onready or _ready() initialized, use regular flow analysis
        if (!IsOnreadyOrReadyInitializedVariable(varName))
            return false;

        // Check method safety
        var safety = GetMethodOnreadySafety(methodName);
        if (safety != GDMethodOnreadySafety.Safe)
            return false;

        // Check if variable is guaranteed after ready and not conditionally initialized
        return _crossMethodState.GuaranteedAfterReady.Contains(varName) &&
               !_crossMethodState.MayBeNullAfterReady.Contains(varName);
    }

    /// <summary>
    /// Gets the @onready safety level for a method.
    /// </summary>
    public GDMethodOnreadySafety GetMethodOnreadySafety(string methodName)
    {
        EnsureCrossMethodAnalysis();

        if (_crossMethodState?.MethodSafetyCache.TryGetValue(methodName, out var safety) == true)
            return safety;

        return GDMethodOnreadySafety.Unknown;
    }

    /// <summary>
    /// Checks if a variable has conditional initialization in _ready().
    /// </summary>
    public bool HasConditionalReadyInitialization(string varName)
    {
        EnsureCrossMethodAnalysis();
        return _crossMethodState?.MayBeNullAfterReady.Contains(varName) ?? false;
    }

    /// <summary>
    /// Gets the flow summary for a method.
    /// </summary>
    public GDMethodFlowSummary? GetMethodFlowSummary(string methodName)
    {
        EnsureCrossMethodAnalysis();
        return _flowSummaryRegistry?.GetSummary(_scriptFile?.TypeName ?? "", methodName);
    }

    /// <summary>
    /// Gets the cross-method flow state.
    /// </summary>
    public GDCrossMethodFlowState? GetCrossMethodFlowState()
    {
        EnsureCrossMethodAnalysis();
        return _crossMethodState;
    }

    /// <summary>
    /// Ensures cross-method analysis has been performed.
    /// </summary>
    private void EnsureCrossMethodAnalysis()
    {
        if (_crossMethodState != null)
            return;

        _flowSummaryRegistry = new GDMethodFlowSummaryRegistry();
        var analyzer = new GDCrossMethodFlowAnalyzer(this, _flowSummaryRegistry);
        _crossMethodState = analyzer.Analyze();
    }

    #endregion

    #region Member Access Key Comparer

    /// <summary>
    /// Case-insensitive comparer for member access lookup keys.
    /// </summary>
    private class MemberAccessKeyComparer : IEqualityComparer<(string, string)>
    {
        public static readonly MemberAccessKeyComparer Instance = new();

        public bool Equals((string, string) x, (string, string) y) =>
            string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string, string) obj)
        {
            unchecked
            {
                var h1 = obj.Item1?.ToUpperInvariant().GetHashCode() ?? 0;
                var h2 = obj.Item2?.ToUpperInvariant().GetHashCode() ?? 0;
                return h1 * 397 ^ h2;
            }
        }
    }

    #endregion

    #region Array Type Helpers

    /// <summary>
    /// Computes the union type for an array initializer expression.
    /// </summary>
    private string? ComputeArrayInitializerUnionType(Reader.GDArrayInitializerExpression arrayInit)
    {
        if (arrayInit?.Values == null || !arrayInit.Values.Any())
            return null;

        var elementTypes = new HashSet<string>();
        foreach (var element in arrayInit.Values)
        {
            if (element != null)
            {
                var type = _typeEngine?.InferType(element);
                if (!string.IsNullOrEmpty(type) && type != "Variant")
                    elementTypes.Add(type);
            }
        }

        if (elementTypes.Count == 0)
            return "Array";

        var unionType = string.Join("|", elementTypes.OrderBy(t => t, System.StringComparer.Ordinal));
        return $"Array[{unionType}]";
    }

    #endregion
}
