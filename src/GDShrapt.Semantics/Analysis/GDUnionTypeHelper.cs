using GDShrapt.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Helper methods for working with Union types.
/// </summary>
internal static class GDUnionTypeHelper
{
    /// <summary>
    /// Finds the common base type for a set of types by traversing inheritance chains.
    /// Returns null if no common base found (other than Object/Variant).
    /// </summary>
    public static string? FindCommonBaseType(IEnumerable<string> types, IGDRuntimeProvider provider)
    {
        if (provider == null)
            return null;

        var typeList = types.ToList();
        if (typeList.Count == 0)
            return null;

        if (typeList.Count == 1)
            return typeList[0];

        // Get ancestor chains for all types
        var ancestorChains = typeList
            .Select(t => GetAncestorChain(t, provider))
            .ToList();

        // Find the most specific common ancestor
        var firstChain = ancestorChains[0];

        foreach (var ancestor in firstChain)
        {
            // Skip Object and Variant as they're too generic
            if (ancestor == "Object" || ancestor == "Variant" || ancestor == "RefCounted")
                continue;

            // Check if this ancestor is in all chains
            var inAllChains = ancestorChains.All(chain => chain.Contains(ancestor));
            if (inAllChains)
                return ancestor;
        }

        return null;
    }

    /// <summary>
    /// Gets the inheritance chain for a type (type itself + all ancestors).
    /// </summary>
    public static List<string> GetAncestorChain(string typeName, IGDRuntimeProvider provider)
    {
        var chain = new List<string> { typeName };
        var current = typeName;
        var visited = new HashSet<string> { typeName };

        while (true)
        {
            var baseType = provider.GetBaseType(current);
            if (string.IsNullOrEmpty(baseType) || visited.Contains(baseType))
                break;

            chain.Add(baseType);
            visited.Add(baseType);
            current = baseType;
        }

        return chain;
    }

    /// <summary>
    /// Checks if typeA is assignable to typeB (typeA is same as or derives from typeB).
    /// </summary>
    public static bool IsAssignableTo(string typeA, string typeB, IGDRuntimeProvider provider)
    {
        if (string.IsNullOrEmpty(typeA) || string.IsNullOrEmpty(typeB))
            return false;

        if (typeA == typeB)
            return true;

        // Check inheritance chain
        var chain = GetAncestorChain(typeA, provider);
        return chain.Contains(typeB);
    }

    /// <summary>
    /// Computes union of two union types.
    /// </summary>
    public static GDUnionType ComputeUnion(GDUnionType? a, GDUnionType? b)
    {
        var result = new GDUnionType();

        if (a != null)
        {
            foreach (var t in a.Types)
                result.Types.Add(t);
            if (!a.AllHighConfidence)
                result.AllHighConfidence = false;
        }

        if (b != null)
        {
            foreach (var t in b.Types)
                result.Types.Add(t);
            if (!b.AllHighConfidence)
                result.AllHighConfidence = false;
        }

        return result;
    }

    /// <summary>
    /// Computes intersection of two union types.
    /// </summary>
    public static GDUnionType ComputeIntersection(GDUnionType? a, GDUnionType? b)
    {
        if (a == null || a.IsEmpty)
            return b ?? new GDUnionType();
        if (b == null || b.IsEmpty)
            return a;

        var result = new GDUnionType
        {
            AllHighConfidence = a.AllHighConfidence && b.AllHighConfidence
        };

        foreach (var t in a.Types)
        {
            if (b.Types.Contains(t))
                result.Types.Add(t);
        }

        return result;
    }
}
