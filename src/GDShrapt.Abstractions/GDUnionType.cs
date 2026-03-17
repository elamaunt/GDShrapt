using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a Union type - a mutable accumulator of multiple possible types.
/// Used during analysis when a variable can be one of several types (e.g., from multiple assignments).
/// Supports per-type origin tracking and immutable snapshots via Freeze().
/// </summary>
public class GDUnionType
{
    /// <summary>
    /// All types in the union.
    /// </summary>
    public HashSet<GDSemanticType> Types { get; } = new HashSet<GDSemanticType>();

    /// <summary>
    /// Common base type (if found via inheritance hierarchy).
    /// </summary>
    public GDSemanticType? CommonBaseType { get; set; }

    /// <summary>
    /// Whether all observed types have high confidence.
    /// </summary>
    public bool AllHighConfidence { get; set; } = true;

    /// <summary>
    /// Reason for the current confidence level.
    /// </summary>
    public string? ConfidenceReason { get; set; }

    /// <summary>
    /// Whether this union has been frozen (immutable snapshot).
    /// </summary>
    public bool IsFrozen { get; private set; }

    private Dictionary<GDSemanticType, List<GDTypeOrigin>>? _origins;

    /// <summary>
    /// Whether any origin tracking data is present.
    /// </summary>
    public bool HasOrigins => _origins != null && _origins.Count > 0;

    /// <summary>
    /// Whether this is a single type (not really a union).
    /// </summary>
    public bool IsSingleType => Types.Count == 1;

    /// <summary>
    /// Whether the union is empty (no observed types).
    /// </summary>
    public bool IsEmpty => Types.Count == 0;

    /// <summary>
    /// Whether this represents a true union of multiple types.
    /// </summary>
    public bool IsUnion => Types.Count > 1;

    /// <summary>
    /// Gets the effective type: single type if one, common base if available, or Variant.
    /// </summary>
    public GDSemanticType EffectiveType =>
        IsSingleType ? Types.First() :
        CommonBaseType != null ? CommonBaseType :
        GDVariantSemanticType.Instance;

    /// <summary>
    /// Gets the union type name: single type if one, or "A | B | C" format if multiple.
    /// Unlike EffectiveType, this preserves union information instead of falling back to Variant.
    /// Uses ToSemanticType() to apply container simplification (e.g., merging dictionaries with same key type).
    /// </summary>
    public string UnionTypeName =>
        IsEmpty ? "Variant" :
        ToSemanticType().DisplayName;

    /// <summary>
    /// Adds a type to the union.
    /// </summary>
    public void AddType(GDSemanticType type, bool isHighConfidence = true)
    {
        ThrowIfFrozen();

        if (type == null || type.IsVariant)
            return;

        Types.Add(type);

        if (!isHighConfidence)
            AllHighConfidence = false;
    }

    /// <summary>
    /// Adds a type with origin tracking.
    /// </summary>
    public void AddType(GDSemanticType type, GDTypeOrigin origin)
    {
        ThrowIfFrozen();

        if (type == null || type.IsVariant)
            return;

        Types.Add(type);

        if (_origins == null)
            _origins = new Dictionary<GDSemanticType, List<GDTypeOrigin>>();

        if (!_origins.TryGetValue(type, out var list))
        {
            list = new List<GDTypeOrigin>();
            _origins[type] = list;
        }

        list.Add(origin);

        if (origin.Confidence == GDTypeOriginConfidence.DuckTyped || origin.Confidence == GDTypeOriginConfidence.Heuristic)
            AllHighConfidence = false;
    }

    /// <summary>
    /// Adds a type by name (convenience, creates GDSemanticType from runtime type name).
    /// </summary>
    public void AddTypeName(string typeName, bool isHighConfidence = true)
    {
        if (string.IsNullOrEmpty(typeName) || typeName == "Variant")
            return;

        AddType(GDSemanticType.FromRuntimeTypeName(typeName), isHighConfidence);
    }

    /// <summary>
    /// Merges another union type into this one, preserving origins.
    /// </summary>
    public void MergeWith(GDUnionType? other)
    {
        ThrowIfFrozen();

        if (other == null)
            return;

        foreach (var type in other.Types)
            Types.Add(type);

        if (!other.AllHighConfidence)
            AllHighConfidence = false;

        if (other._origins != null)
        {
            if (_origins == null)
                _origins = new Dictionary<GDSemanticType, List<GDTypeOrigin>>();

            foreach (var kv in other._origins)
            {
                if (!_origins.TryGetValue(kv.Key, out var list))
                {
                    list = new List<GDTypeOrigin>();
                    _origins[kv.Key] = list;
                }

                list.AddRange(kv.Value);
            }
        }
    }

    /// <summary>
    /// Creates intersection of possible types (for type narrowing).
    /// Preserves origins for surviving types.
    /// </summary>
    public GDUnionType IntersectWith(GDUnionType? other)
    {
        if (other == null)
            return this;

        var result = new GDUnionType
        {
            AllHighConfidence = AllHighConfidence && other.AllHighConfidence
        };

        if (IsEmpty)
        {
            foreach (var t in other.Types)
                result.Types.Add(t);
            CopyOrigins(other, result);
            return result;
        }
        if (other.IsEmpty)
        {
            foreach (var t in Types)
                result.Types.Add(t);
            CopyOrigins(this, result);
            return result;
        }

        foreach (var t in Types)
        {
            if (other.Types.Contains(t))
            {
                result.Types.Add(t);
                CopyOriginsForType(this, t, result);
                CopyOriginsForType(other, t, result);
            }
        }

        return result;
    }

    /// <summary>
    /// Computes type-safe intersection with a single target type, considering inheritance and numeric compatibility.
    /// Preserves origins for surviving types.
    /// </summary>
    public GDUnionType IntersectWithType(GDSemanticType targetType, IGDRuntimeProvider? runtimeProvider)
    {
        var result = new GDUnionType { AllHighConfidence = AllHighConfidence };

        if (IsEmpty)
        {
            result.Types.Add(targetType);
            return result;
        }

        var targetName = targetType.DisplayName;

        foreach (var type in Types)
        {
            var typeName = type.DisplayName;

            if (type is GDNullSemanticType && targetType is not GDNullSemanticType)
                continue;

            // Variant is compatible with any target type - narrow to target
            if (type.IsVariant)
            {
                result.Types.Add(targetType);
                CopyOriginsForType(this, type, result, targetType);
                continue;
            }

            if (type.Equals(targetType))
            {
                result.Types.Add(type);
                CopyOriginsForType(this, type, result);
                continue;
            }

            if (IsNumericType(typeName) && IsNumericType(targetName))
            {
                result.Types.Add(targetType);
                CopyOriginsForType(this, type, result, targetType);
                continue;
            }

            if (runtimeProvider != null)
            {
                if (runtimeProvider.IsAssignableTo(typeName, targetName))
                {
                    result.Types.Add(type);
                    CopyOriginsForType(this, type, result);
                    continue;
                }

                if (runtimeProvider.IsAssignableTo(targetName, typeName))
                {
                    result.Types.Add(targetType);
                    CopyOriginsForType(this, type, result, targetType);
                    continue;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all origins for a specific type.
    /// </summary>
    public IReadOnlyList<GDTypeOrigin> GetOrigins(GDSemanticType type)
    {
        if (_origins != null && _origins.TryGetValue(type, out var list))
            return list;
        return Array.Empty<GDTypeOrigin>();
    }

    /// <summary>
    /// Enumerates all tracked (type, origin) pairs.
    /// </summary>
    public IEnumerable<(GDSemanticType Type, GDTypeOrigin Origin)> AllTrackedTypes
    {
        get
        {
            if (_origins == null)
                yield break;

            foreach (var kv in _origins)
                foreach (var origin in kv.Value)
                    yield return (kv.Key, origin);
        }
    }

    /// <summary>
    /// Gets all origins grouped by type.
    /// </summary>
    public IEnumerable<(GDSemanticType Type, IReadOnlyList<GDTypeOrigin> Origins)> GetAllOrigins()
    {
        if (_origins == null)
            yield break;

        foreach (var kv in _origins)
            yield return (kv.Key, kv.Value);
    }

    /// <summary>
    /// Replaces a specific origin with an updated one for a given type.
    /// Used for mutation tracking where an origin's object state needs updating.
    /// </summary>
    public void ReplaceOrigin(GDSemanticType type, GDTypeOrigin oldOrigin, GDTypeOrigin newOrigin)
    {
        ThrowIfFrozen();

        if (_origins == null || !_origins.TryGetValue(type, out var list))
            return;

        var idx = list.IndexOf(oldOrigin);
        if (idx >= 0)
            list[idx] = newOrigin;
    }

    /// <summary>
    /// Creates an immutable snapshot. After freezing, mutation methods throw.
    /// </summary>
    public GDUnionType Freeze()
    {
        IsFrozen = true;
        return this;
    }

    /// <summary>
    /// Converts to an immutable GDSemanticType.
    /// Merges container types with matching keys/structure (e.g., Dictionary[String, int] | Dictionary[String, Array] → Dictionary[String, Array | int]).
    /// </summary>
    public GDSemanticType ToSemanticType()
    {
        if (IsEmpty) return GDVariantSemanticType.Instance;
        if (IsSingleType) return Types.First();

        var simplified = SimplifyContainerTypes(Types);
        if (simplified.Count == 1) return simplified[0];
        return new GDUnionSemanticType(simplified);
    }

    private static List<GDSemanticType> SimplifyContainerTypes(HashSet<GDSemanticType> types)
    {
        var result = new List<GDSemanticType>();
        var dictGroups = new Dictionary<string, List<GDContainerSemanticType>>();
        var arrayTypes = new List<GDContainerSemanticType>();

        foreach (var type in types)
        {
            if (type is GDContainerSemanticType container)
            {
                if (container.IsDictionary)
                {
                    var keyName = container.KeyType?.DisplayName ?? "Variant";
                    if (!dictGroups.TryGetValue(keyName, out var group))
                    {
                        group = new List<GDContainerSemanticType>();
                        dictGroups[keyName] = group;
                    }
                    group.Add(container);
                }
                else if (container.IsArray)
                {
                    arrayTypes.Add(container);
                }
                else
                {
                    result.Add(type);
                }
            }
            else
            {
                result.Add(type);
            }
        }

        foreach (var kv in dictGroups)
        {
            var dicts = kv.Value;
            if (dicts.Count == 1)
            {
                result.Add(dicts[0]);
            }
            else
            {
                var mergedValues = MergeElementTypes(dicts.Select(d => d.ElementType));
                var keyType = dicts[0].KeyType ?? GDVariantSemanticType.Instance;
                result.Add(new GDContainerSemanticType(true, mergedValues, keyType));
            }
        }

        if (arrayTypes.Count == 1)
        {
            result.Add(arrayTypes[0]);
        }
        else if (arrayTypes.Count > 1)
        {
            var mergedElements = MergeElementTypes(arrayTypes.Select(a => a.ElementType));
            result.Add(new GDContainerSemanticType(false, mergedElements));
        }

        return result;
    }

    private static GDSemanticType MergeElementTypes(IEnumerable<GDSemanticType> elementTypes)
    {
        var allTypes = new HashSet<GDSemanticType>();
        foreach (var et in elementTypes)
        {
            if (et is GDUnionSemanticType union)
            {
                foreach (var ut in union.Types)
                    allTypes.Add(ut);
            }
            else
            {
                allTypes.Add(et);
            }
        }

        if (allTypes.Count == 1) return allTypes.First();
        return new GDUnionSemanticType(allTypes);
    }

    private void ThrowIfFrozen()
    {
        if (IsFrozen)
            throw new InvalidOperationException("Cannot modify a frozen GDUnionType.");
    }

    private static void CopyOrigins(GDUnionType source, GDUnionType target)
    {
        if (source._origins == null)
            return;

        if (target._origins == null)
            target._origins = new Dictionary<GDSemanticType, List<GDTypeOrigin>>();

        foreach (var kv in source._origins)
        {
            if (!target._origins.TryGetValue(kv.Key, out var list))
            {
                list = new List<GDTypeOrigin>();
                target._origins[kv.Key] = list;
            }
            list.AddRange(kv.Value);
        }
    }

    private static void CopyOriginsForType(GDUnionType source, GDSemanticType sourceType, GDUnionType target, GDSemanticType? targetType = null)
    {
        if (source._origins == null || !source._origins.TryGetValue(sourceType, out var sourceOrigins))
            return;

        var key = targetType ?? sourceType;

        if (target._origins == null)
            target._origins = new Dictionary<GDSemanticType, List<GDTypeOrigin>>();

        if (!target._origins.TryGetValue(key, out var list))
        {
            list = new List<GDTypeOrigin>();
            target._origins[key] = list;
        }
        list.AddRange(sourceOrigins);
    }

    private static bool IsNumericType(string type) =>
        type == "int" || type == "float";

    public override string ToString()
    {
        if (IsEmpty) return "Variant";
        if (IsSingleType) return Types.First().DisplayName;
        return string.Join("|", Types.Select(t => t.DisplayName).OrderBy(n => n));
    }
}
