using GDShrapt.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Resolves duck types to possible concrete types using runtime provider.
/// Moved from Abstractions to keep logic in Semantics layer.
/// </summary>
public class GDDuckTypeResolver
{
    private readonly IGDRuntimeProvider _runtimeProvider;

    public GDDuckTypeResolver(IGDRuntimeProvider runtimeProvider)
    {
        _runtimeProvider = runtimeProvider;
    }

    /// <summary>
    /// Finds all known types that satisfy the duck type requirements.
    /// </summary>
    public IEnumerable<string> FindCompatibleTypes(GDDuckType duckType)
    {
        foreach (var typeName in _runtimeProvider.GetAllTypes())
        {
            if (IsCompatibleWith(duckType, typeName))
                yield return typeName;
        }
    }

    /// <summary>
    /// Checks if a concrete type satisfies duck type requirements.
    /// </summary>
    public bool IsCompatibleWith(GDDuckType duckType, string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return true; // Unknown type is potentially compatible

        if (duckType == null)
            return true;

        // Check excluded types
        if (duckType.ExcludedTypes.Contains(typeName))
            return false;

        // Check if type is in possible types (if any defined)
        if (duckType.PossibleTypes.Count > 0)
        {
            var matchesPossible = duckType.PossibleTypes.Any(pt =>
                pt == typeName || _runtimeProvider.IsAssignableTo(typeName, pt));
            if (!matchesPossible)
                return false;
        }

        // Check required methods
        foreach (var method in duckType.RequiredMethods.Keys)
        {
            var member = _runtimeProvider.GetMember(typeName, method);
            if (member == null || member.Kind != GDRuntimeMemberKind.Method)
                return false;
        }

        // Check required properties
        foreach (var prop in duckType.RequiredProperties.Keys)
        {
            var member = _runtimeProvider.GetMember(typeName, prop);
            if (member == null || member.Kind != GDRuntimeMemberKind.Property)
                return false;
        }

        // Check required signals
        foreach (var signal in duckType.RequiredSignals)
        {
            var member = _runtimeProvider.GetMember(typeName, signal);
            if (member == null || member.Kind != GDRuntimeMemberKind.Signal)
                return false;
        }

        return true;
    }
}
