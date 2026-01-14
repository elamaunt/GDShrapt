namespace GDShrapt.Plugin;

/// <summary>
/// Script analyzer using GDShrapt.Validator for type inference and reference collection.
/// Replaces the old ReferencingVisitor with a cleaner API.
/// </summary>
internal class GDScriptAnalyzer
{
    private readonly GDScriptFile _map;
    private GDReferenceResult _references;
    private GDTypeInferenceEngine _typeEngine;
    private GDValidationContext _validationContext;

    /// <summary>
    /// The collected references (forward and back references, types, duck types).
    /// </summary>
    public GDReferenceResult References => _references;

    /// <summary>
    /// The type inference engine.
    /// </summary>
    public GDTypeInferenceEngine TypeEngine => _typeEngine;

    /// <summary>
    /// The validation context with scope and symbol information.
    /// </summary>
    public GDValidationContext Context => _validationContext;

    /// <summary>
    /// All declared symbols in the script.
    /// </summary>
    public IEnumerable<GDSymbol> Symbols => _references?.Symbols ?? Enumerable.Empty<GDSymbol>();

    public GDScriptAnalyzer(GDScriptFile map)
    {
        _map = map;
    }

    /// <summary>
    /// Analyzes the script and builds reference/type information.
    /// </summary>
    /// <param name="runtimeProvider">Optional runtime provider for type resolution.</param>
    public void Analyze(IGDRuntimeProvider runtimeProvider = null)
    {
        var classDecl = _map.Class;
        if (classDecl == null)
            return;

        // Create validation context with runtime provider
        _validationContext = new GDValidationContext(runtimeProvider);

        // Collect declarations first (enables forward references)
        var declarationCollector = new GDDeclarationCollector();
        declarationCollector.Collect(classDecl, _validationContext);

        // Collect references
        var referenceCollector = new GDReferenceCollector();
        _references = referenceCollector.Collect(classDecl, _validationContext, runtimeProvider);

        // Create type inference engine
        _typeEngine = new GDTypeInferenceEngine(
            _validationContext.RuntimeProvider,
            _validationContext.Scopes);

        Logger.Debug($"Analysis complete: {_references.Symbols.Count()} symbols found");
    }

    /// <summary>
    /// Gets the type for a node.
    /// </summary>
    public string GetTypeForNode(GDNode node)
    {
        if (node == null)
            return null;

        // First try references cache
        var type = _references?.GetTypeForNode(node);
        if (type != null)
            return type;

        // Fall back to type inference engine
        return _typeEngine?.GetTypeForNode(node);
    }

    /// <summary>
    /// Gets the full type node for a node (with generic type info).
    /// </summary>
    public GDTypeNode GetTypeNodeForNode(GDNode node)
    {
        if (node == null)
            return null;

        // First try references cache
        var typeNode = _references?.GetTypeNodeForNode(node);
        if (typeNode != null)
            return typeNode;

        // Fall back to type inference engine
        return _typeEngine?.GetTypeNodeForNode(node);
    }

    /// <summary>
    /// Gets the symbol that a node references.
    /// </summary>
    public GDSymbol GetSymbolForNode(GDNode node)
    {
        return _references?.GetSymbolForNode(node);
    }

    /// <summary>
    /// Gets all references to a symbol.
    /// </summary>
    public IReadOnlyList<GDReference> GetReferencesTo(GDSymbol symbol)
    {
        return _references?.GetReferencesTo(symbol) ?? (IReadOnlyList<GDReference>)System.Array.Empty<GDReference>();
    }

    /// <summary>
    /// Gets all references to a symbol by name.
    /// </summary>
    public IReadOnlyList<GDReference> GetReferencesTo(string symbolName)
    {
        var symbol = _references?.FindSymbol(symbolName);
        if (symbol == null)
            return System.Array.Empty<GDReference>();
        return GetReferencesTo(symbol);
    }

    /// <summary>
    /// Finds a symbol by name.
    /// </summary>
    public GDSymbol FindSymbol(string name)
    {
        return _references?.FindSymbol(name);
    }

    /// <summary>
    /// Gets symbols of a specific kind.
    /// </summary>
    public IEnumerable<GDSymbol> GetSymbolsOfKind(GDSymbolKind kind)
    {
        return _references?.GetSymbolsOfKind(kind) ?? Enumerable.Empty<GDSymbol>();
    }

    /// <summary>
    /// Looks up a symbol in the current scope chain.
    /// </summary>
    public GDSymbol LookupSymbol(string name)
    {
        return _validationContext?.Lookup(name);
    }

    /// <summary>
    /// Gets the duck type for a variable (what methods/properties it must have).
    /// </summary>
    public GDDuckType GetDuckType(string variableName)
    {
        return _references?.GetDuckType(variableName);
    }

    /// <summary>
    /// Gets the effective type for a variable at a specific location.
    /// Considers declared type, duck type, and type narrowing.
    /// </summary>
    public string GetEffectiveType(string variableName, GDNode atNode = null)
    {
        return _references?.GetEffectiveType(variableName, atNode);
    }

    /// <summary>
    /// Gets the narrowed type for a variable at a specific location (from if checks).
    /// </summary>
    public string GetNarrowedType(string variableName, GDNode atNode)
    {
        return _references?.GetNarrowedType(variableName, atNode);
    }

    /// <summary>
    /// Gets the expected type at a position (reverse type inference).
    /// </summary>
    public string GetExpectedType(GDNode node)
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
    public GDNode GetDeclaration(string symbolName)
    {
        var symbol = FindSymbol(symbolName);
        return symbol?.Declaration;
    }

    /// <summary>
    /// Gets all method symbols.
    /// </summary>
    public IEnumerable<GDSymbol> GetMethods()
    {
        return GetSymbolsOfKind(GDSymbolKind.Method);
    }

    /// <summary>
    /// Gets all variable symbols (class-level).
    /// </summary>
    public IEnumerable<GDSymbol> GetVariables()
    {
        return GetSymbolsOfKind(GDSymbolKind.Variable);
    }

    /// <summary>
    /// Gets all signal symbols.
    /// </summary>
    public IEnumerable<GDSymbol> GetSignals()
    {
        return GetSymbolsOfKind(GDSymbolKind.Signal);
    }

    /// <summary>
    /// Gets all constant symbols.
    /// </summary>
    public IEnumerable<GDSymbol> GetConstants()
    {
        return GetSymbolsOfKind(GDSymbolKind.Constant);
    }

    /// <summary>
    /// Gets all enum symbols.
    /// </summary>
    public IEnumerable<GDSymbol> GetEnums()
    {
        return GetSymbolsOfKind(GDSymbolKind.Enum);
    }

    /// <summary>
    /// Gets all inner class symbols.
    /// </summary>
    public IEnumerable<GDSymbol> GetInnerClasses()
    {
        return GetSymbolsOfKind(GDSymbolKind.Class);
    }
}
