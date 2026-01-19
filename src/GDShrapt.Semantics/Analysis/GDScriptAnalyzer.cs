using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Script analyzer using GDShrapt.Validator for type inference and reference collection.
/// Godot-independent version for semantic analysis.
/// Uses GDSemanticModel as the unified API for symbol resolution and references.
/// </summary>
public class GDScriptAnalyzer
{
    private readonly GDScriptFile _scriptFile;
    private GDTypeInferenceEngine? _typeEngine;
    private GDValidationContext? _validationContext;
    private GDSemanticModel? _semanticModel;
    private readonly IGDSemanticLogger _logger;

    /// <summary>
    /// The type inference engine. Internal - use SemanticModel API instead.
    /// </summary>
    internal GDTypeInferenceEngine? TypeEngine => _typeEngine;

    /// <summary>
    /// The validation context with scope and symbol information. Internal - use SemanticModel API instead.
    /// </summary>
    internal GDValidationContext? Context => _validationContext;

    /// <summary>
    /// The semantic model for this script. Provides unified access to symbol resolution,
    /// references, type queries, and confidence analysis with full inheritance support.
    /// </summary>
    public GDSemanticModel? SemanticModel => _semanticModel;

    /// <summary>
    /// All declared symbol infos in the script (full semantic information).
    /// </summary>
    public IEnumerable<GDSymbolInfo> Symbols => _semanticModel?.Symbols ?? Enumerable.Empty<GDSymbolInfo>();

    public GDScriptAnalyzer(GDScriptFile scriptFile, IGDSemanticLogger? logger = null)
    {
        _scriptFile = scriptFile;
        _logger = logger ?? GDNullLogger.Instance;
    }

    /// <summary>
    /// Analyzes the script and builds reference/type information.
    /// </summary>
    /// <param name="runtimeProvider">Optional runtime provider for type resolution.</param>
    public void Analyze(IGDRuntimeProvider? runtimeProvider = null)
    {
        var classDecl = _scriptFile.Class;
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

        // Build semantic model (unified API with inheritance support)
        var semanticCollector = new GDSemanticReferenceCollector(_scriptFile, runtimeProvider);
        _semanticModel = semanticCollector.BuildSemanticModel();

        _logger.Debug($"Analysis complete: {_semanticModel.Symbols.Count()} symbols found");
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
}
