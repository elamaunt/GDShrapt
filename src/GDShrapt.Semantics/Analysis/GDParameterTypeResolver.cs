using GDShrapt.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Resolves parameter types by merging:
/// 1. Duck typing constraints (from usage analysis)
/// 2. Type narrowing (from 'is' checks)
/// </summary>
internal class GDParameterTypeResolver
{
    private readonly IGDRuntimeProvider _runtimeProvider;

    // Types that are iterable in GDScript
    private static readonly HashSet<string> IterableTypes = new()
    {
        "Array", "Dictionary", "String",
        "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
        "PackedFloat32Array", "PackedFloat64Array", "PackedStringArray",
        "PackedVector2Array", "PackedVector3Array", "PackedColorArray"
    };

    // Types that support indexing
    private static readonly HashSet<string> IndexableTypes = new()
    {
        "Array", "Dictionary", "String",
        "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
        "PackedFloat32Array", "PackedFloat64Array", "PackedStringArray",
        "PackedVector2Array", "PackedVector3Array", "PackedColorArray",
        "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
        "Color", "Basis", "Transform2D", "Transform3D", "Projection"
    };

    /// <summary>
    /// Creates a new parameter type resolver.
    /// </summary>
    public GDParameterTypeResolver(IGDRuntimeProvider runtimeProvider)
    {
        _runtimeProvider = runtimeProvider;
    }

    /// <summary>
    /// Resolves parameter type from constraints.
    /// </summary>
    public GDInferredParameterType ResolveFromConstraints(GDParameterConstraints constraints)
    {
        if (!constraints.HasConstraints)
            return GDInferredParameterType.Unknown(constraints.ParameterName);

        var candidates = new List<(string Type, GDTypeConfidence Confidence, string Reason)>();

        // Duck typing → find matching types
        var duckMatches = FindDuckTypeMatches(constraints);
        foreach (var match in duckMatches)
        {
            candidates.Add((match, GDTypeConfidence.Medium, "duck typing from usage"));
        }

        // Type checks - these are high confidence
        foreach (var possibleType in constraints.PossibleTypes)
        {
            candidates.Add((possibleType, GDTypeConfidence.High, "type check"));
        }

        return MergeCandidates(constraints.ParameterName, candidates, constraints.ExcludedTypes, constraints);
    }

    /// <summary>
    /// Finds types that match the duck typing constraints.
    /// </summary>
    private List<string> FindDuckTypeMatches(GDParameterConstraints constraints)
    {
        var matches = new List<string>();

        // If we have structural constraints only, return generic types
        if (constraints.RequiredMethods.Count == 0 && constraints.RequiredProperties.Count == 0)
        {
            if (constraints.IsIterable && constraints.IsIndexable)
                matches.Add("Array");
            else if (constraints.IsIterable)
                matches.Add("Array");
            else if (constraints.IsIndexable)
                matches.Add("Array");
            return matches;
        }

        // Check if "get" method is required - likely Dictionary
        if (constraints.RequiredMethods.Contains("get"))
            matches.Add("Dictionary");

        // Check if common container methods are used
        if (constraints.RequiredMethods.Contains("append") ||
            constraints.RequiredMethods.Contains("push_back") ||
            constraints.RequiredMethods.Contains("pop_back"))
            matches.Add("Array");

        // Check if common string methods are used
        if (constraints.RequiredMethods.Contains("substr") ||
            constraints.RequiredMethods.Contains("find") ||
            constraints.RequiredMethods.Contains("split"))
            matches.Add("String");

        // Check for Node methods
        if (constraints.RequiredMethods.Contains("get_node") ||
            constraints.RequiredMethods.Contains("add_child") ||
            constraints.RequiredMethods.Contains("get_children"))
            matches.Add("Node");

        // Check for common properties
        if (constraints.RequiredProperties.Contains("x") &&
            constraints.RequiredProperties.Contains("y"))
        {
            if (constraints.RequiredProperties.Contains("z"))
                matches.Add("Vector3");
            else
                matches.Add("Vector2");
        }

        if (constraints.RequiredProperties.Contains("position") ||
            constraints.RequiredProperties.Contains("rotation"))
            matches.Add("Node2D");

        // If still no matches but has structural constraints, fall back
        if (matches.Count == 0)
        {
            if (constraints.IsIterable)
                matches.Add("Array");
            else if (constraints.IsIndexable)
                matches.Add("Array");
        }

        return matches;
    }

    /// <summary>
    /// Merges candidate types into a final result.
    /// </summary>
    private GDInferredParameterType MergeCandidates(
        string paramName,
        List<(string Type, GDTypeConfidence Confidence, string Reason)> candidates,
        HashSet<string> excluded,
        GDParameterConstraints? sourceConstraints)
    {
        // Filter out excluded types
        candidates = candidates
            .Where(c => !excluded.Contains(c.Type))
            .ToList();

        if (candidates.Count == 0)
            return GDInferredParameterType.Unknown(paramName);

        // Group by type
        var grouped = candidates.GroupBy(c => c.Type).ToList();

        if (grouped.Count == 1)
        {
            // Single type - use highest confidence
            var best = candidates.OrderBy(c => c.Confidence).First();
            return sourceConstraints != null
                ? GDInferredParameterType.FromDuckTyping(paramName, best.Type, sourceConstraints)
                : GDInferredParameterType.Create(paramName, best.Type, best.Confidence, best.Reason);
        }

        // Multiple types - return Union
        var types = grouped.Select(g => g.Key).ToList();
        var minConfidence = candidates.Min(c => c.Confidence);
        return GDInferredParameterType.Union(paramName, types, minConfidence);
    }
}
