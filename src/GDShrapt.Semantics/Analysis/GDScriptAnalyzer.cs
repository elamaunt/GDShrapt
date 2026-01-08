using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Script analyzer using GDShrapt.Validator for type inference and reference collection.
/// Godot-independent version for semantic analysis.
/// </summary>
public class GDScriptAnalyzer
{
    private readonly GDScriptFile _scriptFile;
    private GDReferenceResult? _references;
    private GDTypeInferenceEngine? _typeEngine;
    private GDValidationContext? _validationContext;
    private readonly IGDSemanticLogger _logger;

    /// <summary>
    /// The collected references (forward and back references, types, duck types).
    /// </summary>
    public GDReferenceResult? References => _references;

    /// <summary>
    /// The type inference engine.
    /// </summary>
    public GDTypeInferenceEngine? TypeEngine => _typeEngine;

    /// <summary>
    /// The validation context with scope and symbol information.
    /// </summary>
    public GDValidationContext? Context => _validationContext;

    /// <summary>
    /// All declared symbols in the script.
    /// </summary>
    public IEnumerable<GDSymbol> Symbols => _references?.Symbols ?? Enumerable.Empty<GDSymbol>();

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

        // Collect references
        var referenceCollector = new GDReferenceCollector();
        _references = referenceCollector.Collect(classDecl, _validationContext, runtimeProvider);

        // Create type inference engine
        _typeEngine = new GDTypeInferenceEngine(
            _validationContext.RuntimeProvider,
            _validationContext.Scopes);

        _logger.Debug($"Analysis complete: {_references.Symbols.Count()} symbols found");
    }

    /// <summary>
    /// Gets the type for a node.
    /// </summary>
    public string? GetTypeForNode(GDNode node)
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
    public GDTypeNode? GetTypeNodeForNode(GDNode node)
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
    public GDSymbol? GetSymbolForNode(GDNode node)
    {
        return _references?.GetSymbolForNode(node);
    }

    /// <summary>
    /// Gets all references to a symbol.
    /// </summary>
    public IReadOnlyList<GDReference> GetReferencesTo(GDSymbol symbol)
    {
        return _references?.GetReferencesTo(symbol) ?? Array.Empty<GDReference>();
    }

    /// <summary>
    /// Gets all references to a symbol by name.
    /// </summary>
    public IReadOnlyList<GDReference> GetReferencesTo(string symbolName)
    {
        var symbol = _references?.FindSymbol(symbolName);
        if (symbol == null)
            return Array.Empty<GDReference>();
        return GetReferencesTo(symbol);
    }

    /// <summary>
    /// Finds a symbol by name. Returns the first match.
    /// </summary>
    public GDSymbol? FindSymbol(string name)
    {
        return _references?.FindSymbol(name);
    }

    /// <summary>
    /// Finds all symbols with the given name (handles same-named symbols in different scopes).
    /// </summary>
    public IEnumerable<GDSymbol> FindSymbols(string name)
    {
        return Symbols.Where(s => s.Name == name);
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
    public GDSymbol? LookupSymbol(string name)
    {
        return _validationContext?.Lookup(name);
    }

    /// <summary>
    /// Gets the duck type for a variable (what methods/properties it must have).
    /// </summary>
    public GDDuckType? GetDuckType(string variableName)
    {
        return _references?.GetDuckType(variableName);
    }

    /// <summary>
    /// Gets the effective type for a variable at a specific location.
    /// Considers declared type, duck type, and type narrowing.
    /// </summary>
    public string? GetEffectiveType(string variableName, GDNode? atNode = null)
    {
        return _references?.GetEffectiveType(variableName, atNode);
    }

    /// <summary>
    /// Gets the narrowed type for a variable at a specific location (from if checks).
    /// </summary>
    public string? GetNarrowedType(string variableName, GDNode atNode)
    {
        return _references?.GetNarrowedType(variableName, atNode);
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

    #region Scope Filtering APIs

    /// <summary>
    /// Gets the scope type where the symbol was declared.
    /// Inferred from the declaration node type.
    /// </summary>
    public GDScopeType? GetDeclarationScopeType(GDSymbol symbol)
    {
        if (symbol?.Declaration == null)
            return null;

        return symbol.Declaration switch
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
    public IEnumerable<GDReference> GetReferencesInScope(GDSymbol symbol, GDScopeType scopeType)
    {
        var refs = GetReferencesTo(symbol);
        return refs.Where(r => r.Scope?.Type == scopeType);
    }

    /// <summary>
    /// Gets references to a symbol filtered by multiple scope types.
    /// </summary>
    public IEnumerable<GDReference> GetReferencesInScopes(GDSymbol symbol, params GDScopeType[] scopeTypes)
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
    public IEnumerable<GDReference> GetLocalReferences(GDSymbol symbol)
    {
        var refs = GetReferencesTo(symbol);
        return refs.Where(r => IsLocalScope(r.Scope?.Type));
    }

    /// <summary>
    /// Determines if a symbol is a local variable (declared in method/lambda scope).
    /// Local symbols include: local variables, parameters, for-loop iterators, match case variables.
    /// </summary>
    public bool IsLocalSymbol(GDSymbol symbol)
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
            case GDSymbolKind.Constant when IsClassLevelDeclaration(symbol.Declaration):
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
    public bool IsClassMember(GDSymbol symbol)
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
    private static bool IsClassLevelDeclaration(GDNode declaration)
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
    public IEnumerable<GDReference> GetReferencesInDeclaringScope(GDSymbol symbol)
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
    private GDNode? GetDeclaringMethodNode(GDSymbol symbol)
    {
        if (symbol?.Declaration == null)
            return null;

        // Walk up the AST to find the enclosing method
        var node = symbol.Declaration;
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
