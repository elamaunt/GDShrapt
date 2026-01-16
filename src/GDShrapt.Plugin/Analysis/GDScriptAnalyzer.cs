namespace GDShrapt.Plugin;

/// <summary>
/// Script analyzer using GDSemanticModel for type inference and reference collection.
/// Uses GDShrapt.Abstractions types for unified symbol management.
/// </summary>
internal class GDScriptAnalyzer
{
    private readonly GDScriptFile _map;
    private GDTypeInferenceEngine? _typeEngine;
    private GDValidationContext? _validationContext;
    private GDSemanticModel? _semanticModel;

    /// <summary>
    /// The type inference engine.
    /// </summary>
    public GDTypeInferenceEngine? TypeEngine => _typeEngine;

    /// <summary>
    /// The validation context with scope and symbol information.
    /// </summary>
    public GDValidationContext? Context => _validationContext;

    /// <summary>
    /// The semantic model for this script. Provides unified access to symbol resolution,
    /// references, type queries, and confidence analysis.
    /// </summary>
    public GDSemanticModel? SemanticModel => _semanticModel;

    /// <summary>
    /// All declared symbols in the script.
    /// </summary>
    public IEnumerable<GDSymbolInfo> Symbols => _semanticModel?.Symbols ?? Enumerable.Empty<GDSymbolInfo>();

    public GDScriptAnalyzer(GDScriptFile map)
    {
        _map = map;
    }

    /// <summary>
    /// Analyzes the script and builds reference/type information.
    /// </summary>
    /// <param name="runtimeProvider">Optional runtime provider for type resolution.</param>
    public void Analyze(IGDRuntimeProvider? runtimeProvider = null)
    {
        var classDecl = _map.Class;
        if (classDecl == null)
            return;

        // Create validation context with runtime provider
        _validationContext = new GDValidationContext(runtimeProvider);

        // Collect declarations first (enables forward references)
        var declarationCollector = new GDDeclarationCollector();
        declarationCollector.Collect(classDecl, _validationContext);

        // Create type inference engine
        _typeEngine = new GDTypeInferenceEngine(
            _validationContext.RuntimeProvider,
            _validationContext.Scopes);

        // Build semantic model (unified API)
        var semanticCollector = new GDSemanticReferenceCollector(_map, runtimeProvider);
        _semanticModel = semanticCollector.BuildSemanticModel();

        Logger.Debug($"Analysis complete: {_semanticModel.Symbols.Count()} symbols found");
    }

    /// <summary>
    /// Gets the type for a node.
    /// </summary>
    public string? GetTypeForNode(GDNode node)
    {
        if (node == null)
            return null;

        // First try semantic model
        var type = _semanticModel?.GetTypeForNode(node);
        if (type != null)
            return type;

        // Fall back to type inference engine
        return _typeEngine?.GetTypeForNode(node);
    }

    /// <summary>
    /// Gets the full type node for a node (with generic type info).
    /// </summary>
    public GDTypeNode? GetTypeNodeForNode(GDNode node)
    {
        if (node == null)
            return null;

        // First try semantic model
        var typeNode = _semanticModel?.GetTypeNodeForNode(node);
        if (typeNode != null)
            return typeNode;

        // Fall back to type inference engine
        return _typeEngine?.GetTypeNodeForNode(node);
    }

    /// <summary>
    /// Gets the symbol info that a node references.
    /// </summary>
    public GDSymbolInfo? GetSymbolForNode(GDNode node)
    {
        return _semanticModel?.GetSymbolForNode(node);
    }

    /// <summary>
    /// Gets all references to a symbol.
    /// </summary>
    public IReadOnlyList<GDReference> GetReferencesTo(GDSymbolInfo symbol)
    {
        return _semanticModel?.GetReferencesTo(symbol) ?? Array.Empty<GDReference>();
    }

    /// <summary>
    /// Gets all references to a symbol by name.
    /// </summary>
    public IReadOnlyList<GDReference> GetReferencesTo(string symbolName)
    {
        var symbol = _semanticModel?.FindSymbol(symbolName);
        if (symbol == null)
            return Array.Empty<GDReference>();
        return GetReferencesTo(symbol);
    }

    /// <summary>
    /// Finds a symbol by name. Returns the first match.
    /// </summary>
    public GDSymbolInfo? FindSymbol(string name)
    {
        return _semanticModel?.FindSymbol(name);
    }

    /// <summary>
    /// Finds all symbols with the given name (handles same-named symbols in different scopes).
    /// </summary>
    public IEnumerable<GDSymbolInfo> FindSymbols(string name)
    {
        return Symbols.Where(s => s.Name == name);
    }

    /// <summary>
    /// Gets symbols of a specific kind.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetSymbolsOfKind(GDSymbolKind kind)
    {
        return Symbols.Where(s => s.Kind == kind);
    }

    /// <summary>
    /// Looks up a symbol in the current scope chain.
    /// </summary>
    public GDSymbol? LookupSymbol(string name)
    {
        return _validationContext?.Lookup(name);
    }

    /// <summary>
    /// Gets the duck type for a variable (what methods/properties it must have).
    /// </summary>
    public GDDuckType? GetDuckType(string variableName)
    {
        return _semanticModel?.GetDuckType(variableName);
    }

    /// <summary>
    /// Gets the effective type for a variable at a specific location.
    /// Considers declared type, duck type, and type narrowing.
    /// </summary>
    public string? GetEffectiveType(string variableName, GDNode? atNode = null)
    {
        return _semanticModel?.GetEffectiveType(variableName, atNode);
    }

    /// <summary>
    /// Gets the narrowed type for a variable at a specific location (from if checks).
    /// </summary>
    public string? GetNarrowedType(string variableName, GDNode atNode)
    {
        return _semanticModel?.GetNarrowedType(variableName, atNode);
    }

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

    /// <summary>
    /// Gets the declaration node for a symbol.
    /// </summary>
    public GDNode? GetDeclaration(string symbolName)
    {
        var symbol = FindSymbol(symbolName);
        return symbol?.DeclarationNode;
    }

    /// <summary>
    /// Gets all method symbols.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetMethods()
    {
        return GetSymbolsOfKind(GDSymbolKind.Method);
    }

    /// <summary>
    /// Gets all variable symbols (class-level).
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetVariables()
    {
        return GetSymbolsOfKind(GDSymbolKind.Variable);
    }

    /// <summary>
    /// Gets all signal symbols.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetSignals()
    {
        return GetSymbolsOfKind(GDSymbolKind.Signal);
    }

    /// <summary>
    /// Gets all constant symbols.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetConstants()
    {
        return GetSymbolsOfKind(GDSymbolKind.Constant);
    }

    /// <summary>
    /// Gets all enum symbols.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetEnums()
    {
        return GetSymbolsOfKind(GDSymbolKind.Enum);
    }

    /// <summary>
    /// Gets all inner class symbols.
    /// </summary>
    public IEnumerable<GDSymbolInfo> GetInnerClasses()
    {
        return GetSymbolsOfKind(GDSymbolKind.Class);
    }
}
