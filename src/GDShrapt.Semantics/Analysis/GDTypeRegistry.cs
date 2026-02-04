using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Registry for type information, union types, and container types.
/// Extracted from GDSemanticModel to provide cleaner separation of concerns.
/// </summary>
public class GDTypeRegistry
{
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

    // Call site argument types for parameters
    private readonly Dictionary<string, GDUnionType> _callSiteParameterTypes = new();

    // Container usage profiles
    private readonly Dictionary<string, GDContainerUsageProfile> _containerProfiles = new();
    private readonly Dictionary<string, GDContainerElementType> _containerTypeCache = new();
    private readonly Dictionary<string, GDContainerUsageProfile> _classContainerProfiles = new();

    // ========================================
    // Type Queries
    // ========================================

    /// <summary>
    /// Gets the type name for a node.
    /// </summary>
    public string? GetTypeForNode(GDNode node)
    {
        return _nodeTypes.TryGetValue(node, out var type) ? type : null;
    }

    /// <summary>
    /// Gets the type node for a node.
    /// </summary>
    public GDTypeNode? GetTypeNodeForNode(GDNode node)
    {
        return _nodeTypeNodes.TryGetValue(node, out var typeNode) ? typeNode : null;
    }

    /// <summary>
    /// Gets the duck type for a variable.
    /// </summary>
    public GDDuckType? GetDuckType(string variableName)
    {
        return _duckTypes.TryGetValue(variableName, out var duckType) ? duckType : null;
    }

    /// <summary>
    /// Gets the narrowing context for a node.
    /// </summary>
    public GDTypeNarrowingContext? GetNarrowingContext(GDNode node)
    {
        return _narrowingContexts.TryGetValue(node, out var context) ? context : null;
    }

    /// <summary>
    /// Gets type usages for a type name.
    /// </summary>
    public IReadOnlyList<GDTypeUsage> GetTypeUsages(string typeName)
    {
        return _typeUsages.TryGetValue(typeName, out var usages)
            ? usages
            : Array.Empty<GDTypeUsage>();
    }

    /// <summary>
    /// Gets the variable usage profile.
    /// </summary>
    public GDVariableUsageProfile? GetVariableProfile(string variableName)
    {
        return _variableProfiles.TryGetValue(variableName, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets the union type for a variable.
    /// </summary>
    public GDUnionType? GetUnionType(string variableName)
    {
        return _unionTypeCache.TryGetValue(variableName, out var unionType) ? unionType : null;
    }

    /// <summary>
    /// Gets call site parameter types.
    /// </summary>
    public GDUnionType? GetCallSiteParameterTypes(string paramKey)
    {
        return _callSiteParameterTypes.TryGetValue(paramKey, out var types) ? types : null;
    }

    /// <summary>
    /// Gets container profile for a variable.
    /// </summary>
    public GDContainerUsageProfile? GetContainerProfile(string variableName)
    {
        return _containerProfiles.TryGetValue(variableName, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets class-level container profile.
    /// </summary>
    public GDContainerUsageProfile? GetClassContainerProfile(string variableName)
    {
        return _classContainerProfiles.TryGetValue(variableName, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets container element type.
    /// </summary>
    public GDContainerElementType? GetContainerElementType(string variableName)
    {
        return _containerTypeCache.TryGetValue(variableName, out var elementType) ? elementType : null;
    }

    // ========================================
    // Registration Methods (internal)
    // ========================================

    /// <summary>
    /// Registers a type for a node.
    /// </summary>
    internal void RegisterNodeType(GDNode node, string typeName)
    {
        if (node != null && !string.IsNullOrEmpty(typeName))
        {
            _nodeTypes[node] = typeName;
        }
    }

    /// <summary>
    /// Registers a type node for a node.
    /// </summary>
    internal void RegisterNodeTypeNode(GDNode node, GDTypeNode typeNode)
    {
        if (node != null && typeNode != null)
        {
            _nodeTypeNodes[node] = typeNode;
        }
    }

    /// <summary>
    /// Registers a duck type for a variable.
    /// </summary>
    internal void RegisterDuckType(string variableName, GDDuckType duckType)
    {
        if (!string.IsNullOrEmpty(variableName) && duckType != null)
        {
            _duckTypes[variableName] = duckType;
        }
    }

    /// <summary>
    /// Registers a narrowing context for a node.
    /// </summary>
    internal void RegisterNarrowingContext(GDNode node, GDTypeNarrowingContext context)
    {
        if (node != null && context != null)
        {
            _narrowingContexts[node] = context;
        }
    }

    /// <summary>
    /// Registers a type usage.
    /// </summary>
    internal void RegisterTypeUsage(string typeName, GDTypeUsage usage)
    {
        if (string.IsNullOrEmpty(typeName) || usage == null)
            return;

        if (!_typeUsages.TryGetValue(typeName, out var usages))
        {
            usages = new List<GDTypeUsage>();
            _typeUsages[typeName] = usages;
        }
        usages.Add(usage);
    }

    /// <summary>
    /// Registers a variable profile.
    /// </summary>
    internal void RegisterVariableProfile(string variableName, GDVariableUsageProfile profile)
    {
        if (!string.IsNullOrEmpty(variableName) && profile != null)
        {
            _variableProfiles[variableName] = profile;
        }
    }

    /// <summary>
    /// Registers a union type.
    /// </summary>
    internal void RegisterUnionType(string variableName, GDUnionType unionType)
    {
        if (!string.IsNullOrEmpty(variableName) && unionType != null)
        {
            _unionTypeCache[variableName] = unionType;
        }
    }

    /// <summary>
    /// Registers call site parameter types.
    /// </summary>
    internal void RegisterCallSiteParameterTypes(string paramKey, GDUnionType types)
    {
        if (!string.IsNullOrEmpty(paramKey) && types != null)
        {
            _callSiteParameterTypes[paramKey] = types;
        }
    }

    /// <summary>
    /// Registers a container profile.
    /// </summary>
    internal void RegisterContainerProfile(string variableName, GDContainerUsageProfile profile)
    {
        if (!string.IsNullOrEmpty(variableName) && profile != null)
        {
            _containerProfiles[variableName] = profile;
        }
    }

    /// <summary>
    /// Registers a class-level container profile.
    /// </summary>
    internal void RegisterClassContainerProfile(string variableName, GDContainerUsageProfile profile)
    {
        if (!string.IsNullOrEmpty(variableName) && profile != null)
        {
            _classContainerProfiles[variableName] = profile;
        }
    }

    /// <summary>
    /// Registers a container element type.
    /// </summary>
    internal void RegisterContainerElementType(string variableName, GDContainerElementType elementType)
    {
        if (!string.IsNullOrEmpty(variableName) && elementType != null)
        {
            _containerTypeCache[variableName] = elementType;
        }
    }

    /// <summary>
    /// Gets or creates a duck type for a variable.
    /// </summary>
    internal GDDuckType GetOrCreateDuckType(string variableName)
    {
        if (!_duckTypes.TryGetValue(variableName, out var duckType))
        {
            duckType = new GDDuckType();
            _duckTypes[variableName] = duckType;
        }
        return duckType;
    }

    /// <summary>
    /// Gets or creates a union type for a variable.
    /// </summary>
    internal GDUnionType GetOrCreateUnionType(string variableName)
    {
        if (!_unionTypeCache.TryGetValue(variableName, out var unionType))
        {
            unionType = new GDUnionType();
            _unionTypeCache[variableName] = unionType;
        }
        return unionType;
    }

    /// <summary>
    /// Gets or creates a container profile for a variable.
    /// </summary>
    internal GDContainerUsageProfile GetOrCreateContainerProfile(string variableName)
    {
        if (!_containerProfiles.TryGetValue(variableName, out var profile))
        {
            profile = new GDContainerUsageProfile(variableName);
            _containerProfiles[variableName] = profile;
        }
        return profile;
    }

    /// <summary>
    /// Clears all data in the registry.
    /// </summary>
    internal void Clear()
    {
        _nodeTypes.Clear();
        _nodeTypeNodes.Clear();
        _duckTypes.Clear();
        _narrowingContexts.Clear();
        _typeUsages.Clear();
        _variableProfiles.Clear();
        _unionTypeCache.Clear();
        _callSiteParameterTypes.Clear();
        _containerProfiles.Clear();
        _containerTypeCache.Clear();
        _classContainerProfiles.Clear();
    }
}
