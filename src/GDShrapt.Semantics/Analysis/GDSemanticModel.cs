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

    // Type services
    private readonly GDContainerTypeService _containerTypeService;
    private readonly GDUnionTypeService _unionTypeService;
    private readonly GDDuckTypeService _duckTypeService;
    private readonly GDExpressionTypeService _expressionTypeService;
    private readonly GDConfidenceService _confidenceService;
    private readonly GDArgumentTypeService _argumentTypeService;
    private readonly GDScopeService _scopeService;
    private readonly GDCrossMethodFlowService _crossMethodFlowService;
    private readonly GDLocalTypeService _localTypeService;
    private readonly GDOnreadyService _onreadyService;
    private readonly GDNullabilityService _nullabilityService;
    private readonly GDFlowQueryService _flowQueryService;
    private readonly GDLambdaTypeService _lambdaTypeService;

    // Type tracking (node-level, shared with expression type service)
    private readonly Dictionary<GDNode, string> _nodeTypes = new();
    private readonly Dictionary<GDNode, GDTypeNode> _nodeTypeNodes = new();

    // Type usages (type annotations, is checks, extends)
    private readonly Dictionary<string, List<GDTypeUsage>> _typeUsages = new();

    // Callable call site registry for lambda parameter inference
    private GDCallableCallSiteRegistry? _callSiteRegistry;

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
    internal GDCallableCallSiteRegistry? CallSiteRegistry => _callSiteRegistry;

    /// <summary>
    /// All symbols in this script.
    /// </summary>
    public IEnumerable<GDSymbolInfo> Symbols => _symbolRegistry.Symbols;

    /// <summary>
    /// Gets the symbol registry.
    /// </summary>
    internal GDSymbolRegistry SymbolRegistry => _symbolRegistry;

    /// <summary>
    /// Gets the container type service.
    /// </summary>
    internal GDContainerTypeService ContainerTypeService => _containerTypeService;

    /// <summary>
    /// Gets the union type service.
    /// </summary>
    internal GDUnionTypeService UnionTypeService => _unionTypeService;

    /// <summary>
    /// Gets the duck type service.
    /// </summary>
    internal GDDuckTypeService DuckTypeService => _duckTypeService;

    /// <summary>
    /// Gets the expression type service.
    /// </summary>
    internal GDExpressionTypeService ExpressionTypeService => _expressionTypeService;

    // Lazy-initialized unified type query interface
    private IGDUnifiedTypeQuery? _typeQuery;

    /// <summary>
    /// Gets the unified type query interface.
    /// Provides a single entry point for all type-related queries.
    /// </summary>
    public IGDUnifiedTypeQuery TypeQuery => _typeQuery ??= new GDTypeQueryFacade(this);

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

        // Initialize type services
        _containerTypeService = new GDContainerTypeService(runtimeProvider);
        _unionTypeService = new GDUnionTypeService(runtimeProvider);
        _unionTypeService.SetTypeEngine(typeEngine);
        _duckTypeService = new GDDuckTypeService();
        _expressionTypeService = new GDExpressionTypeService(
            runtimeProvider,
            typeEngine,
            _containerTypeService,
            _unionTypeService,
            _duckTypeService,
            _flowRegistry,
            _nodeTypes,
            _nodeTypeNodes);
        _expressionTypeService.SetFindSymbolDelegate(FindSymbol);
        _expressionTypeService.SetGetOnreadyVariablesDelegate(GetOnreadyVariables);

        // Initialize confidence service
        _confidenceService = new GDConfidenceService(
            runtimeProvider,
            _duckTypeService,
            _unionTypeService,
            _containerTypeService,
            GetExpressionType,
            FindSymbol,
            GetRootVariableName);

        // Initialize argument type service
        _argumentTypeService = new GDArgumentTypeService(runtimeProvider);
        _argumentTypeService.SetFindSymbolDelegate(FindSymbol);
        _argumentTypeService.SetGetExpressionTypeDelegate(GetExpressionType);
        _argumentTypeService.SetFindMemberWithInheritanceDelegate(FindMemberWithInheritanceInternal);
        _argumentTypeService.SetBaseTypeName(scriptFile?.Class?.Extends?.Type?.BuildName());

        // Initialize scope service
        _scopeService = new GDScopeService(GetReferencesTo);

        // Initialize cross-method flow service
        _crossMethodFlowService = new GDCrossMethodFlowService(
            () =>
            {
                var registry = new GDMethodFlowSummaryRegistry();
                var analyzer = new GDCrossMethodFlowAnalyzer(this, registry);
                return (analyzer.Analyze(), registry);
            },
            IsOnreadyOrReadyInitializedVariable,
            () => _scriptFile?.TypeName ?? "");

        // Initialize local type service
        _localTypeService = new GDLocalTypeService(FindSymbols, FindMemberWithInheritanceInternal);

        // Initialize onready service
        _onreadyService = new GDOnreadyService(FindSymbol, () => _scriptFile?.Class);

        // Initialize flow query service
        _flowQueryService = new GDFlowQueryService(
            (method, varName, loc) => GetOrCreateFlowAnalyzer(method)?.GetTypeAtLocation(varName, loc),
            (method, varName, loc) => GetOrCreateFlowAnalyzer(method)?.GetVariableTypeAtLocation(varName, loc),
            (method, loc) => GetOrCreateFlowAnalyzer(method)?.GetStateAtLocation(loc));

        // Initialize nullability service
        _nullabilityService = new GDNullabilityService(
            runtimeProvider,
            FindSymbol,
            FindSymbols,
            _flowQueryService.GetFlowStateAtLocation,
            IsInheritedProperty);

        // Initialize lambda type service
        _lambdaTypeService = new GDLambdaTypeService(
            () => _callSiteRegistry,
            () => _scriptFile,
            () => _scriptFile?.Class?.ClassName?.Identifier?.Sequence);
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
    public string? GetExpressionType(GDExpression? expression)
    {
        return _expressionTypeService.GetExpressionType(expression);
    }

    /// <summary>
    /// Resolves the type of a standalone expression (parsed from text, not from file AST).
    /// Use this for completion context and similar scenarios where expression is not part of the file tree.
    /// </summary>
    public GDTypeResolutionResult ResolveStandaloneExpression(GDExpression expression)
        => _expressionTypeService.ResolveStandaloneExpression(expression);

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
    /// </summary>
    private string? GetExpressionTypeWithoutFlow(GDExpression expression)
    {
        return _expressionTypeService.GetExpressionTypeWithoutFlow(expression);
    }

    /// <summary>
    /// Gets the flow-sensitive type for a variable at a specific location.
    /// Returns null if flow analysis is not available.
    /// </summary>
    public string? GetFlowSensitiveType(string variableName, GDNode atLocation)
        => _flowQueryService.GetFlowSensitiveType(variableName, atLocation);

    /// <summary>
    /// Gets the full flow variable type info at a specific location.
    /// </summary>
    public GDFlowVariableType? GetFlowVariableType(string variableName, GDNode atLocation)
        => _flowQueryService.GetFlowVariableType(variableName, atLocation);

    /// <summary>
    /// Gets the flow state at a specific location in the code.
    /// Returns null if flow analysis is not available.
    /// </summary>
    public GDFlowState? GetFlowStateAtLocation(GDNode atLocation)
        => _flowQueryService.GetFlowStateAtLocation(atLocation);

    /// <summary>
    /// Checks if a variable is potentially null at a given location.
    /// </summary>
    public bool IsVariablePotentiallyNull(string variableName, GDNode atLocation)
        => _nullabilityService.IsVariablePotentiallyNull(variableName, atLocation);

    /// <summary>
    /// Public method to check if a type is an enum (for external access).
    /// </summary>
    public bool IsLocalEnumType(string typeName)
        => _nullabilityService.IsEnumType(typeName);

    /// <summary>
    /// Checks if a variable has the @onready attribute.
    /// </summary>
    public bool IsOnreadyVariable(string variableName)
        => _onreadyService.IsOnreadyVariable(variableName);

    /// <summary>
    /// Checks if a variable is initialized in _ready() method.
    /// </summary>
    public bool IsReadyInitializedVariable(string variableName)
        => _onreadyService.IsReadyInitializedVariable(variableName);

    /// <summary>
    /// Checks if a variable is either @onready or initialized in _ready().
    /// </summary>
    public bool IsOnreadyOrReadyInitializedVariable(string variableName)
        => _onreadyService.IsOnreadyOrReadyInitializedVariable(variableName);

    /// <summary>
    /// Gets all @onready variable names in the current class.
    /// </summary>
    public IEnumerable<string> GetOnreadyVariables()
        => _onreadyService.GetOnreadyVariables();

    /// <summary>
    /// Gets the _ready() method declaration if it exists.
    /// </summary>
    public GDMethodDeclaration? GetReadyMethod()
        => _onreadyService.GetReadyMethod();

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
        return _expressionTypeService.GetTypeNodeForExpression(expression);
    }

    /// <summary>
    /// Gets the duck type for a variable (required methods/properties).
    /// </summary>
    public GDDuckType? GetDuckType(string variableName)
    {
        // Delegate to service (Phase 5 migration)
        return _duckTypeService.GetDuckType(variableName);
    }

    /// <summary>
    /// Gets the narrowed type for a variable at a specific location (from if checks).
    /// Walks up the AST to find the nearest branch with narrowing info.
    /// </summary>
    public string? GetNarrowedType(string variableName, GDNode atLocation)
    {
        // Delegate to service (Phase 5 migration)
        return _duckTypeService.GetNarrowedType(variableName, atLocation);
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
    /// </summary>
    public bool ShouldSuppressDuckConstraints(string symbolName)
    {
        if (string.IsNullOrEmpty(symbolName))
            return true;

        var symbol = FindSymbol(symbolName);
        var unionType = GetUnionType(symbolName);
        var refs = symbol != null ? GetReferencesTo(symbol) : null;

        return _duckTypeService.ShouldSuppressDuckConstraints(
            symbolName,
            symbol?.TypeName,
            unionType,
            refs);
    }

    #endregion

    #region Parameter Type Inference

    /// <summary>
    /// Infers the type for a parameter based on its usage within the method.
    /// Returns Variant if cannot infer.
    /// </summary>
    public GDInferredParameterType InferParameterType(GDParameterDeclaration param)
    {
        return _expressionTypeService.InferParameterType(param);
    }

    /// <summary>
    /// Gets the duck typing constraints for a parameter.
    /// Returns null if the parameter has no usage constraints.
    /// </summary>
    public GDParameterConstraints? GetParameterConstraints(GDParameterDeclaration param)
    {
        return _expressionTypeService.GetParameterConstraints(param);
    }

    /// <summary>
    /// Infers parameter types for all parameters of a method.
    /// </summary>
    public IReadOnlyDictionary<string, GDInferredParameterType> InferParameterTypes(GDMethodDeclaration method)
    {
        return _expressionTypeService.InferParameterTypes(method);
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
        // Delegate to service (Phase 5 migration)
        return _unionTypeService.GetVariableProfile(variableName);
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

        var symbol = FindSymbol(symbolName);
        return _unionTypeService.GetUnionType(symbolName, symbol, _scriptFile);
    }

    /// <summary>
    /// Gets all variable usage profiles (for UI display).
    /// </summary>
    public IEnumerable<GDVariableUsageProfile> GetAllVariableProfiles()
    {
        // Delegate to service (Phase 6 migration)
        return _unionTypeService.GetAllVariableProfiles();
    }

    /// <summary>
    /// Gets the member access confidence for a Union type.
    /// </summary>
    public GDReferenceConfidence GetUnionMemberConfidence(GDUnionType unionType, string memberName)
    {
        return _unionTypeService.GetUnionMemberConfidence(unionType, memberName);
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
        var callSiteUnion = GetCallSiteTypes(methodName, paramName);
        if (callSiteUnion != null)
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
        // Delegate to service (Phase 6 migration)
        return _unionTypeService.GetCallSiteTypes(methodName, paramName);
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
        // Delegate to service (Phase 5 migration)
        return _containerTypeService.GetContainerProfile(variableName);
    }

    /// <summary>
    /// Gets the inferred container element type.
    /// </summary>
    public GDContainerElementType? GetInferredContainerType(string variableName)
    {
        // Delegate to service (Phase 5 migration)
        return _containerTypeService.GetInferredContainerType(variableName);
    }

    /// <summary>
    /// Gets all container usage profiles (for UI display).
    /// </summary>
    public IEnumerable<GDContainerUsageProfile> GetAllContainerProfiles()
    {
        return _containerTypeService.GetAllContainerProfiles();
    }

    #endregion

    #region Confidence Analysis

    /// <summary>
    /// Gets the confidence level for a member access expression.
    /// </summary>
    public GDReferenceConfidence GetMemberAccessConfidence(GDMemberOperatorExpression memberAccess)
    {
        var className = _scriptFile?.Class?.ClassName?.Identifier?.Sequence;
        return _confidenceService.GetMemberAccessConfidence(memberAccess, className);
    }

    /// <summary>
    /// Gets the confidence level for any identifier.
    /// For simple identifiers, always Strict. For member access, delegates to GetMemberAccessConfidence.
    /// </summary>
    public GDReferenceConfidence GetIdentifierConfidence(GDIdentifier identifier)
    {
        var className = _scriptFile?.Class?.ClassName?.Identifier?.Sequence;
        return _confidenceService.GetIdentifierConfidence(identifier, className);
    }

    /// <summary>
    /// Builds a human-readable reason for confidence determination.
    /// </summary>
    public string? GetConfidenceReason(GDIdentifier identifier)
    {
        return _confidenceService.GetConfidenceReason(identifier);
    }

    #endregion

    #region Scope Filtering APIs (delegated to GDScopeService)

    /// <summary>
    /// Gets the scope type where the symbol was declared.
    /// </summary>
    public GDScopeType? GetDeclarationScopeType(GDSymbolInfo symbol)
        => _scopeService.GetDeclarationScopeType(symbol);

    /// <summary>
    /// Gets references to a symbol filtered by scope type.
    /// </summary>
    public IEnumerable<GDReference> GetReferencesInScope(GDSymbolInfo symbol, GDScopeType scopeType)
        => _scopeService.GetReferencesInScope(symbol, scopeType);

    /// <summary>
    /// Gets references to a symbol filtered by multiple scope types.
    /// </summary>
    public IEnumerable<GDReference> GetReferencesInScopes(GDSymbolInfo symbol, params GDScopeType[] scopeTypes)
        => _scopeService.GetReferencesInScopes(symbol, scopeTypes);

    /// <summary>
    /// Gets references only within method/lambda scope (local references).
    /// </summary>
    public IEnumerable<GDReference> GetLocalReferences(GDSymbolInfo symbol)
        => _scopeService.GetLocalReferences(symbol);

    /// <summary>
    /// Determines if a symbol is a local variable (declared in method/lambda scope).
    /// </summary>
    public bool IsLocalSymbol(GDSymbolInfo symbol)
        => _scopeService.IsLocalSymbol(symbol);

    /// <summary>
    /// Determines if a symbol is a class member (declared in class scope).
    /// </summary>
    public bool IsClassMember(GDSymbolInfo symbol)
        => _scopeService.IsClassMember(symbol);

    /// <summary>
    /// Gets the enclosing method/lambda scope for a reference.
    /// </summary>
    public GDScope? GetEnclosingMethodScope(GDReference reference)
        => _scopeService.GetEnclosingMethodScope(reference);

    /// <summary>
    /// Gets references within the same method/lambda as the symbol's declaration.
    /// </summary>
    public IEnumerable<GDReference> GetReferencesInDeclaringScope(GDSymbolInfo symbol)
        => _scopeService.GetReferencesInDeclaringScope(symbol);

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
        => _lambdaTypeService.InferLambdaParameterTypesFromCallSites(lambda);

    /// <summary>
    /// Infers a specific lambda parameter type from call sites.
    /// </summary>
    public string? InferLambdaParameterTypeFromCallSites(GDMethodExpression lambda, int parameterIndex)
        => _lambdaTypeService.InferLambdaParameterTypeFromCallSites(lambda, parameterIndex);

    /// <summary>
    /// Infers lambda parameter types including inter-procedural analysis.
    /// This includes call sites from method parameters when the lambda is passed as argument.
    /// </summary>
    public IReadOnlyDictionary<int, GDUnionType> InferLambdaParameterTypesWithFlow(GDMethodExpression lambda)
        => _lambdaTypeService.InferLambdaParameterTypesWithFlow(lambda);

    /// <summary>
    /// Infers a specific lambda parameter type with inter-procedural analysis.
    /// </summary>
    public string? InferLambdaParameterTypeWithFlow(GDMethodExpression lambda, int parameterIndex)
        => _lambdaTypeService.InferLambdaParameterTypeWithFlow(lambda, parameterIndex);

    /// <summary>
    /// Gets the method Callable profile for a method.
    /// </summary>
    public GDMethodCallableProfile? GetMethodCallableProfile(string methodName)
        => _lambdaTypeService.GetMethodCallableProfile(methodName);

    /// <summary>
    /// Gets argument bindings for a lambda (where it's passed to method parameters).
    /// </summary>
    public IReadOnlyList<GDCallableArgumentBinding> GetLambdaArgumentBindings(GDMethodExpression lambda)
        => _lambdaTypeService.GetLambdaArgumentBindings(lambda);

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
        {
            _duckTypeService.SetDuckType(variableName, duckType);
        }
    }

    /// <summary>
    /// Sets narrowing context for a node.
    /// </summary>
    internal void SetNarrowingContext(GDNode node, GDTypeNarrowingContext context)
    {
        if (node != null && context != null)
        {
            _duckTypeService.SetNarrowingContext(node, context);
        }
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
                    // Apply narrowing context to all statements after the if (via service)
                    _duckTypeService.SetNarrowingContext(statement, context);
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
            _unionTypeService.SetVariableProfile(variableName, profile);
        }
    }

    /// <summary>
    /// Sets a container usage profile (for container element type inference).
    /// </summary>
    internal void SetContainerProfile(string variableName, GDContainerUsageProfile profile)
    {
        if (!string.IsNullOrEmpty(variableName) && profile != null)
        {
            _containerTypeService.SetContainerProfile(variableName, profile);
        }
    }

    /// <summary>
    /// Sets a class-level container usage profile.
    /// </summary>
    internal void SetClassContainerProfile(string className, string variableName, GDContainerUsageProfile profile)
    {
        if (!string.IsNullOrEmpty(className) && !string.IsNullOrEmpty(variableName) && profile != null)
        {
            _containerTypeService.SetClassContainerProfile(className, variableName, profile);
        }
    }

    /// <summary>
    /// Gets a class-level container usage profile.
    /// </summary>
    public GDContainerUsageProfile? GetClassContainerProfile(string className, string variableName)
    {
        // Delegate to service (Phase 5 migration)
        return _containerTypeService.GetClassContainerProfile(className, variableName);
    }

    /// <summary>
    /// Gets all class-level container profiles.
    /// </summary>
    public IReadOnlyDictionary<string, GDContainerUsageProfile> ClassContainerProfiles => _containerTypeService.ClassContainerProfiles;

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

        _unionTypeService.SetCallSiteParameterTypes(methodName, paramName, callSiteTypes);
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

    #region IGDMemberAccessAnalyzer Implementation (delegated to services)

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

    bool IGDMemberAccessAnalyzer.IsLocalEnum(string typeName)
        => _localTypeService.IsLocalEnum(typeName);

    bool IGDMemberAccessAnalyzer.IsLocalEnumValue(string enumTypeName, string memberName)
        => _localTypeService.IsLocalEnumValue(enumTypeName, memberName);

    bool IGDMemberAccessAnalyzer.IsLocalInnerClass(string typeName)
        => _localTypeService.IsLocalInnerClass(typeName);

    GDRuntimeMemberInfo? IGDMemberAccessAnalyzer.GetInnerClassMember(string innerClassName, string memberName)
        => _localTypeService.GetInnerClassMember(innerClassName, memberName);

    #endregion

    #region IGDArgumentTypeAnalyzer Implementation

    /// <summary>
    /// Gets the type diff for a call expression argument at the given index.
    /// </summary>
    GDArgumentTypeDiff? IGDArgumentTypeAnalyzer.GetArgumentTypeDiff(object callExpression, int argumentIndex)
    {
        if (callExpression is GDCallExpression call)
            return _argumentTypeService.GetArgumentTypeDiff(call, argumentIndex);
        return null;
    }

    /// <summary>
    /// Gets all argument type diffs for a call expression.
    /// </summary>
    IEnumerable<GDArgumentTypeDiff> IGDArgumentTypeAnalyzer.GetAllArgumentTypeDiffs(object callExpression)
    {
        if (callExpression is GDCallExpression call)
            return _argumentTypeService.GetAllArgumentTypeDiffs(call);
        return Enumerable.Empty<GDArgumentTypeDiff>();
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
            return _argumentTypeService.GetExpressionTypeSource(expr);
        return null;
    }

    #endregion

    #region Cross-Method Flow Analysis (delegated to GDCrossMethodFlowService)

    /// <summary>
    /// Checks if a variable is safe to access at a given method, considering cross-method analysis.
    /// </summary>
    public bool IsVariableSafeAtMethod(string varName, string methodName)
        => _crossMethodFlowService.IsVariableSafeAtMethod(varName, methodName);

    /// <summary>
    /// Gets the @onready safety level for a method.
    /// </summary>
    public GDMethodOnreadySafety GetMethodOnreadySafety(string methodName)
        => _crossMethodFlowService.GetMethodOnreadySafety(methodName);

    /// <summary>
    /// Checks if a variable has conditional initialization in _ready().
    /// </summary>
    public bool HasConditionalReadyInitialization(string varName)
        => _crossMethodFlowService.HasConditionalReadyInitialization(varName);

    /// <summary>
    /// Gets the flow summary for a method.
    /// </summary>
    public GDMethodFlowSummary? GetMethodFlowSummary(string methodName)
        => _crossMethodFlowService.GetMethodFlowSummary(methodName);

    /// <summary>
    /// Gets the cross-method flow state.
    /// </summary>
    public GDCrossMethodFlowState? GetCrossMethodFlowState()
        => _crossMethodFlowService.GetCrossMethodFlowState();

    #endregion

}
