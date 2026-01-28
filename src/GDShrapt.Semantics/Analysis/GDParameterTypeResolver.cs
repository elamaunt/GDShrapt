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
    /// Finds types that match the duck typing constraints using TypesMap data.
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

        // Try to use TypesMap for method-based duck typing
        GDGodotTypesProvider? typesProvider = null;
        GDProjectTypesProvider? projectProvider = null;

        if (_runtimeProvider is GDGodotTypesProvider godotProvider)
        {
            typesProvider = godotProvider;
        }
        else if (_runtimeProvider is GDCompositeRuntimeProvider compositeProvider)
        {
            typesProvider = compositeProvider.GodotTypesProvider;
            projectProvider = compositeProvider.ProjectTypesProvider;
        }

        if (typesProvider != null && constraints.RequiredMethods.Count > 0)
        {
            var methodMatches = FindTypesMatchingAllMethods(typesProvider, projectProvider, constraints.RequiredMethods);
            if (methodMatches.Count > 0)
            {
                matches.AddRange(methodMatches);
                return matches.Distinct().ToList();
            }
        }

        // Fallback to hardcoded patterns if TypesMap lookup fails or is unavailable
        if (constraints.RequiredMethods.Contains("get"))
            matches.Add("Dictionary");

        if (constraints.RequiredMethods.Contains("append") ||
            constraints.RequiredMethods.Contains("push_back") ||
            constraints.RequiredMethods.Contains("pop_back"))
            matches.Add("Array");

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
    /// Finds types that have ALL required methods using TypesMap data.
    /// Intersects the sets of types for each method to find common types.
    /// Checks both Godot types and project types.
    /// </summary>
    private static List<string> FindTypesMatchingAllMethods(
        GDGodotTypesProvider? godotProvider,
        GDProjectTypesProvider? projectProvider,
        HashSet<string> requiredMethods)
    {
        if (requiredMethods.Count == 0)
            return new List<string>();

        HashSet<string>? intersection = null;

        foreach (var methodName in requiredMethods)
        {
            var typesWithMethod = new HashSet<string>();

            // Check Godot types
            if (godotProvider != null)
            {
                foreach (var t in godotProvider.FindTypesWithMethod(methodName))
                    typesWithMethod.Add(t);
            }

            // Check project types
            if (projectProvider != null)
            {
                foreach (var t in projectProvider.FindTypesWithMethod(methodName))
                    typesWithMethod.Add(t);
            }

            if (typesWithMethod.Count == 0)
            {
                // If any method has no types, intersection is empty
                return new List<string>();
            }

            if (intersection == null)
            {
                intersection = typesWithMethod;
            }
            else
            {
                intersection.IntersectWith(typesWithMethod);
                if (intersection.Count == 0)
                    return new List<string>();
            }
        }

        return intersection?.ToList() ?? new List<string>();
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

        // Build union members with detailed info
        var unionMembers = grouped.Select(g => BuildUnionMember(g.Key, g.First().Confidence, sourceConstraints)).ToList();

        // Format types with per-type constraints if available
        var formattedTypes = unionMembers.Select(m => m.FormattedType).ToList();

        if (formattedTypes.Count == 1)
        {
            // Single type - use highest confidence
            var best = candidates.OrderBy(c => c.Confidence).First();

            // Still use UnionWithMembers to preserve derivability info
            return GDInferredParameterType.UnionWithMembers(paramName, unionMembers, best.Confidence, sourceConstraints);
        }

        // Multiple types - return Union with members
        var minConfidence = candidates.Min(c => c.Confidence);
        return GDInferredParameterType.UnionWithMembers(paramName, unionMembers, minConfidence, sourceConstraints);
    }

    /// <summary>
    /// Builds a union member with detailed type info, sources, and derivability markers.
    /// </summary>
    private GDUnionTypeMember BuildUnionMember(string baseType, GDTypeConfidence confidence, GDParameterConstraints? constraints)
    {
        if (constraints == null)
        {
            return new GDUnionTypeMember
            {
                BaseType = baseType,
                FormattedType = baseType,
                Confidence = confidence
            };
        }

        // Try to get per-type constraints
        GDTypeSpecificConstraints? typeSpecific = null;
        constraints.TypeConstraints.TryGetValue(baseType, out typeSpecific);

        var source = typeSpecific?.InferenceSources.FirstOrDefault();

        // Build key and value slots
        GDGenericTypeSlot? keySlot = null;
        GDGenericTypeSlot? valueSlot = null;

        if (baseType == "Dictionary")
        {
            keySlot = BuildKeySlot(typeSpecific, constraints);
            valueSlot = BuildValueSlot(typeSpecific, constraints);
        }
        else if (baseType == "Array")
        {
            valueSlot = BuildValueSlot(typeSpecific, constraints);
        }

        // Format the type
        var formattedType = typeSpecific?.FormatFullType() ?? FormatTypeWithElements(baseType, constraints);

        return new GDUnionTypeMember
        {
            BaseType = baseType,
            FormattedType = formattedType,
            Source = source,
            KeyType = keySlot,
            ValueType = valueSlot,
            Confidence = confidence
        };
    }

    /// <summary>
    /// Builds a key type slot with derivability info.
    /// </summary>
    private GDGenericTypeSlot BuildKeySlot(GDTypeSpecificConstraints? typeSpecific, GDParameterConstraints constraints)
    {
        var keyTypes = typeSpecific?.KeyTypes ?? constraints.KeyTypes;
        var sources = typeSpecific?.KeyTypeSources ?? new List<GDTypeInferenceSource>();

        if (keyTypes.Count == 0)
        {
            if (typeSpecific?.KeyIsDerivable == true)
            {
                return GDGenericTypeSlot.Derivable(
                    typeSpecific.KeyDerivableNode,
                    typeSpecific.KeyDerivableReason);
            }
            return GDGenericTypeSlot.Variant();
        }

        var typeStr = keyTypes.Count == 1
            ? keyTypes.First()
            : string.Join(" | ", keyTypes.OrderBy(t => t));

        return new GDGenericTypeSlot
        {
            TypeName = typeStr,
            IsDerivable = typeSpecific?.KeyIsDerivable ?? false,
            DerivableSourceNode = typeSpecific?.KeyDerivableNode,
            DerivableReason = typeSpecific?.KeyDerivableReason,
            Sources = sources.ToList(),
            Confidence = GDTypeConfidence.Medium
        };
    }

    /// <summary>
    /// Builds a value type slot with derivability info.
    /// </summary>
    private GDGenericTypeSlot BuildValueSlot(GDTypeSpecificConstraints? typeSpecific, GDParameterConstraints constraints)
    {
        var elemTypes = typeSpecific?.ElementTypes ?? constraints.ElementTypes;
        var sources = typeSpecific?.ElementTypeSources ?? new List<GDTypeInferenceSource>();

        if (elemTypes.Count == 0)
        {
            if (typeSpecific?.ValueIsDerivable == true)
            {
                return GDGenericTypeSlot.Derivable(
                    typeSpecific.ValueDerivableNode,
                    typeSpecific.ValueDerivableReason);
            }
            return GDGenericTypeSlot.Variant();
        }

        var typeStr = elemTypes.Count == 1
            ? elemTypes.First()
            : string.Join(" | ", elemTypes.OrderBy(t => t));

        return new GDGenericTypeSlot
        {
            TypeName = typeStr,
            IsDerivable = typeSpecific?.ValueIsDerivable ?? false,
            DerivableSourceNode = typeSpecific?.ValueDerivableNode,
            DerivableReason = typeSpecific?.ValueDerivableReason,
            Sources = sources.ToList(),
            Confidence = GDTypeConfidence.Medium
        };
    }

    /// <summary>
    /// Formats a type using per-type constraints if available, falling back to global constraints.
    /// This ensures Dictionary gets its specific key types and Array gets its specific element types.
    /// </summary>
    private string FormatTypeWithPerTypeConstraints(string baseType, GDParameterConstraints? constraints)
    {
        if (constraints == null)
            return baseType;

        // Try to use per-type constraints first
        if (constraints.TypeConstraints.TryGetValue(baseType, out var typeSpecific))
        {
            return typeSpecific.FormatFullType();
        }

        // Fall back to global constraints (legacy behavior)
        return FormatTypeWithElements(baseType, constraints);
    }

    /// <summary>
    /// Formats a container type with element/key types if available from constraints.
    /// E.g., "Array" → "Array[int | String]", "Dictionary" → "Dictionary[String, Variant]"
    /// </summary>
    private string FormatTypeWithElements(string baseType, GDParameterConstraints? constraints)
    {
        if (constraints == null)
            return baseType;

        // Build element type string
        string? elementTypeString = null;
        if (constraints.ElementTypes.Count > 0)
        {
            elementTypeString = constraints.ElementTypes.Count == 1
                ? constraints.ElementTypes.First()
                : string.Join(" | ", constraints.ElementTypes.OrderBy(t => t));
        }

        // Build key type string
        string? keyTypeString = null;
        if (constraints.KeyTypes.Count > 0)
        {
            keyTypeString = constraints.KeyTypes.Count == 1
                ? constraints.KeyTypes.First()
                : string.Join(" | ", constraints.KeyTypes.OrderBy(t => t));
        }

        // Format based on type
        if (baseType == "Array")
        {
            if (!string.IsNullOrEmpty(elementTypeString))
                return $"Array[{elementTypeString}]";
            return baseType;
        }

        if (baseType == "Dictionary")
        {
            var key = keyTypeString ?? "Variant";
            var val = elementTypeString ?? "Variant";
            if (keyTypeString != null || elementTypeString != null)
                return $"Dictionary[{key}, {val}]";
            return baseType;
        }

        // Other types - no element formatting
        return baseType;
    }
}
