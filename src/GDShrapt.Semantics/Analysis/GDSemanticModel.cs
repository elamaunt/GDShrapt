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
/// </summary>
public class GDSemanticModel
{
    private readonly GDScriptFile _scriptFile;
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly GDTypeInferenceEngine? _typeEngine;
    private readonly GDValidationContext? _validationContext;

    // Symbol tracking
    private readonly Dictionary<GDNode, GDSymbolInfo> _nodeToSymbol = new();
    private readonly Dictionary<string, List<GDSymbolInfo>> _nameToSymbols = new();
    private readonly Dictionary<GDSymbolInfo, List<GDReference>> _symbolReferences = new();

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

    // Container usage profiles
    private readonly Dictionary<string, GDContainerUsageProfile> _containerProfiles = new();
    private readonly Dictionary<string, GDContainerElementType> _containerTypeCache = new();

    /// <summary>
    /// The script file this model represents.
    /// </summary>
    public GDScriptFile ScriptFile => _scriptFile;

    /// <summary>
    /// The runtime provider for type resolution.
    /// </summary>
    public IGDRuntimeProvider? RuntimeProvider => _runtimeProvider;

    /// <summary>
    /// All symbols in this script.
    /// </summary>
    public IEnumerable<GDSymbolInfo> Symbols => _nameToSymbols.Values.SelectMany(x => x);

    /// <summary>
    /// Creates a semantic model for a script file.
    /// </summary>
    public GDSemanticModel(
        GDScriptFile scriptFile,
        IGDRuntimeProvider? runtimeProvider = null,
        GDValidationContext? validationContext = null,
        GDTypeInferenceEngine? typeEngine = null)
    {
        _scriptFile = scriptFile ?? throw new ArgumentNullException(nameof(scriptFile));
        _runtimeProvider = runtimeProvider;
        _validationContext = validationContext;
        _typeEngine = typeEngine;
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
        if (parent != null && _nodeToSymbol.TryGetValue(parent, out var symbolInfo))
            return symbolInfo;

        // Try resolving by name
        var name = identifier.Sequence;
        if (!string.IsNullOrEmpty(name))
            return FindSymbol(name);

        return null;
    }

    /// <summary>
    /// Gets the symbol for a specific AST node.
    /// </summary>
    public GDSymbolInfo? GetSymbolForNode(GDNode node)
    {
        if (node == null)
            return null;

        return _nodeToSymbol.TryGetValue(node, out var symbol) ? symbol : null;
    }

    /// <summary>
    /// Finds a symbol by name. Returns the first match.
    /// </summary>
    public GDSymbolInfo? FindSymbol(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        return _nameToSymbols.TryGetValue(name, out var symbols) && symbols.Count > 0
            ? symbols[0]
            : null;
    }

    /// <summary>
    /// Finds all symbols with the given name (handles same-named symbols in different scopes).
    /// </summary>
    public IEnumerable<GDSymbolInfo> FindSymbols(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Enumerable.Empty<GDSymbolInfo>();

        return _nameToSymbols.TryGetValue(name, out var symbols)
            ? symbols
            : Enumerable.Empty<GDSymbolInfo>();
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

        // Find the declaring type (may be different from typeName if inherited)
        var declaringType = FindDeclaringType(typeName, memberName) ?? typeName;

        return GDSymbolInfo.BuiltIn(memberInfo, declaringType);
    }

    /// <summary>
    /// Finds the type that actually declares a member (for inherited members).
    /// </summary>
    private string? FindDeclaringType(string typeName, string memberName)
    {
        if (_runtimeProvider == null)
            return typeName;

        var current = typeName;
        while (!string.IsNullOrEmpty(current))
        {
            var typeInfo = _runtimeProvider.GetTypeInfo(current);
            if (typeInfo?.Members != null)
            {
                if (typeInfo.Members.Any(m => m.Name == memberName))
                    return current;
            }

            current = _runtimeProvider.GetBaseType(current);
        }

        return typeName;
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

        return _symbolReferences.TryGetValue(symbol, out var refs)
            ? refs
            : Array.Empty<GDReference>();
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

        return null;
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

        return null;
    }

    /// <summary>
    /// Gets the inferred type for an expression.
    /// </summary>
    public string? GetExpressionType(GDExpression expression)
    {
        if (expression == null)
            return null;

        // Check cache first
        if (_nodeTypes.TryGetValue(expression, out var cachedType))
            return cachedType;

        // Use type engine for type inference
        // Note: Do NOT delegate to Analyzer to avoid circular dependency
        return _typeEngine?.InferType(expression);
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
        if (symbol?.TypeName != null)
            return symbol.TypeName;

        // Duck type as string representation
        var duckType = GetDuckType(variableName);
        return duckType?.ToString();
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
    /// Gets the Union type for a Variant variable, computed from all assignments.
    /// Returns null if the variable is not a tracked Variant variable.
    /// </summary>
    public GDUnionType? GetUnionType(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        // Check cache first
        if (_unionTypeCache.TryGetValue(variableName, out var cached))
            return cached;

        // Compute from profile
        var profile = GetVariableProfile(variableName);
        if (profile == null)
            return null;

        var union = profile.ComputeUnionType();

        // Enrich with common base type if we have a runtime provider
        if (_runtimeProvider != null && union.IsUnion)
        {
            var resolver = new GDUnionTypeResolver(_runtimeProvider);
            resolver.EnrichUnionType(union);
        }

        _unionTypeCache[variableName] = union;
        return union;
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

        // Enrich with common base type if we have a runtime provider
        if (_runtimeProvider != null && containerType.ElementUnionType.IsUnion)
        {
            var resolver = new GDUnionTypeResolver(_runtimeProvider);
            resolver.EnrichUnionType(containerType.ElementUnionType);
        }
        if (_runtimeProvider != null && containerType.KeyUnionType?.IsUnion == true)
        {
            var resolver = new GDUnionTypeResolver(_runtimeProvider);
            resolver.EnrichUnionType(containerType.KeyUnionType);
        }

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
        if (!string.IsNullOrEmpty(callerType) && callerType != "Variant" && !callerType.StartsWith("Unknown"))
            return GDReferenceConfidence.Strict;

        // Check for type narrowing and Union types
        var varName = GetRootVariableName(memberAccess.CallerExpression);
        if (!string.IsNullOrEmpty(varName))
        {
            var narrowed = GetNarrowedType(varName, memberAccess);
            if (!string.IsNullOrEmpty(narrowed))
                return GDReferenceConfidence.Strict;

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

            // Check duck type
            var duckType = GetDuckType(varName);
            if (duckType != null)
                return GDReferenceConfidence.Potential;
        }

        // Type unknown
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

    #region Internal Modification Methods (for collector)

    /// <summary>
    /// Registers a symbol in the model.
    /// </summary>
    internal void RegisterSymbol(GDSymbolInfo symbol)
    {
        if (symbol == null)
            return;

        if (!_nameToSymbols.TryGetValue(symbol.Name, out var list))
        {
            list = new List<GDSymbolInfo>();
            _nameToSymbols[symbol.Name] = list;
        }
        list.Add(symbol);

        if (symbol.DeclarationNode != null)
            _nodeToSymbol[symbol.DeclarationNode] = symbol;
    }

    /// <summary>
    /// Registers a node-to-symbol mapping.
    /// </summary>
    internal void SetNodeSymbol(GDNode node, GDSymbolInfo symbol)
    {
        if (node != null && symbol != null)
            _nodeToSymbol[node] = symbol;
    }

    /// <summary>
    /// Adds a reference to a symbol.
    /// </summary>
    internal void AddReference(GDSymbolInfo symbol, GDReference reference)
    {
        if (symbol == null || reference == null)
            return;

        if (!_symbolReferences.TryGetValue(symbol, out var refs))
        {
            refs = new List<GDReference>();
            _symbolReferences[symbol] = refs;
        }
        refs.Add(reference);
    }

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

    #endregion
}
