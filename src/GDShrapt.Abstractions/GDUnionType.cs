using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a Union type - a mutable accumulator of multiple possible types.
/// Used during analysis when a variable can be one of several types (e.g., from multiple assignments).
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
    /// Gets the union type name: single type if one, or "A|B|C" format if multiple.
    /// Unlike EffectiveType, this preserves union information instead of falling back to Variant.
    /// </summary>
    public string UnionTypeName =>
        IsSingleType ? Types.First().DisplayName :
        IsEmpty ? "Variant" :
        string.Join("|", Types.Select(t => t.DisplayName).OrderBy(n => n, StringComparer.Ordinal));

    /// <summary>
    /// Adds a type to the union.
    /// </summary>
    public void AddType(GDSemanticType type, bool isHighConfidence = true)
    {
        if (type == null || type.IsVariant)
            return;

        Types.Add(type);

        if (!isHighConfidence)
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
    /// Merges another union type into this one.
    /// </summary>
    public void MergeWith(GDUnionType? other)
    {
        if (other == null)
            return;

        foreach (var type in other.Types)
            Types.Add(type);

        if (!other.AllHighConfidence)
            AllHighConfidence = false;
    }

    /// <summary>
    /// Creates intersection of possible types (for type narrowing).
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
            return result;
        }
        if (other.IsEmpty)
        {
            foreach (var t in Types)
                result.Types.Add(t);
            return result;
        }

        foreach (var t in Types)
        {
            if (other.Types.Contains(t))
                result.Types.Add(t);
        }

        return result;
    }

    /// <summary>
    /// Computes type-safe intersection with a single target type, considering inheritance and numeric compatibility.
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

            if (type.Equals(targetType))
            {
                result.Types.Add(type);
                continue;
            }

            if (IsNumericType(typeName) && IsNumericType(targetName))
            {
                result.Types.Add(targetType);
                continue;
            }

            if (runtimeProvider != null)
            {
                if (runtimeProvider.IsAssignableTo(typeName, targetName))
                {
                    result.Types.Add(type);
                    continue;
                }

                if (runtimeProvider.IsAssignableTo(targetName, typeName))
                {
                    result.Types.Add(targetType);
                    continue;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Converts to an immutable GDSemanticType.
    /// </summary>
    public GDSemanticType ToSemanticType()
    {
        if (IsEmpty) return GDVariantSemanticType.Instance;
        if (IsSingleType) return Types.First();
        return new GDUnionSemanticType(Types);
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
