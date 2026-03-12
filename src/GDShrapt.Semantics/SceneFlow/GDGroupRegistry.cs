using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Central registry mapping group names to the node types that belong to them.
/// Built from two sources: .tscn scene files and GDScript add_to_group() calls.
/// </summary>
internal class GDGroupRegistry
{
    private readonly Dictionary<string, GDGroupInfo> _groups = new();
    private readonly GDGodotTypesProvider? _godotTypesProvider;
    private readonly IGDScriptProvider? _scriptProvider;

    public GDGroupRegistry(
        GDGodotTypesProvider? godotTypesProvider = null,
        IGDScriptProvider? scriptProvider = null)
    {
        _godotTypesProvider = godotTypesProvider;
        _scriptProvider = scriptProvider;
    }

    public void RegisterMember(string groupName, GDGroupMembership membership)
    {
        if (!_groups.TryGetValue(groupName, out var info))
        {
            info = new GDGroupInfo { GroupName = groupName };
            _groups[groupName] = info;
        }

        // Avoid duplicate type registrations
        if (!info.Members.Any(m => m.TypeName == membership.TypeName))
            info.Members.Add(membership);
    }

    /// <summary>
    /// Gets the narrowed type for a group. If all members share the same type,
    /// returns that type. If multiple types, finds common base type.
    /// Returns null if no members are registered for the group.
    /// </summary>
    public string? GetGroupType(string groupName)
    {
        if (!_groups.TryGetValue(groupName, out var info) || info.Members.Count == 0)
            return null;

        var types = info.Members.Select(m => m.TypeName).Distinct().ToList();

        if (types.Count == 1)
            return types[0];

        return FindCommonBaseType(types);
    }

    public GDGroupInfo? GetGroupInfo(string groupName)
    {
        _groups.TryGetValue(groupName, out var info);
        return info;
    }

    public IEnumerable<string> AllGroupNames => _groups.Keys;

    private string? FindCommonBaseType(List<string> types)
    {
        if (types.Count == 0)
            return null;

        var first = types[0];
        var chain = GetInheritanceChain(first);

        foreach (var baseType in chain)
        {
            if (types.All(t => t == baseType || InheritsFrom(t, baseType)))
                return baseType;
        }

        return null;
    }

    private List<string> GetInheritanceChain(string typeName)
    {
        var chain = new List<string> { typeName };
        var current = typeName;
        var visited = new HashSet<string> { current };

        while (true)
        {
            var baseType = GetBaseType(current);
            if (string.IsNullOrEmpty(baseType) || !visited.Add(baseType))
                break;
            chain.Add(baseType);
            current = baseType;
        }

        return chain;
    }

    private bool InheritsFrom(string typeName, string baseTypeName)
    {
        if (typeName == baseTypeName)
            return true;

        var current = typeName;
        var visited = new HashSet<string> { current };

        while (true)
        {
            var baseType = GetBaseType(current);
            if (string.IsNullOrEmpty(baseType) || !visited.Add(baseType))
                return false;
            if (baseType == baseTypeName)
                return true;
            current = baseType;
        }
    }

    /// <summary>
    /// Resolves base type for both project types (via script extends) and built-in types.
    /// </summary>
    private string? GetBaseType(string typeName)
    {
        // Try project types first (script with class_name)
        if (_scriptProvider != null)
        {
            var script = _scriptProvider.GetScriptByTypeName(typeName);
            if (script?.Class != null)
            {
                var extendsType = script.Class.Extends?.Type?.BuildName();
                if (!string.IsNullOrEmpty(extendsType))
                    return extendsType;
            }
        }

        // Fall back to Godot built-in types
        return _godotTypesProvider?.GetBaseType(typeName);
    }
}
