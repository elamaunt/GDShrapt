using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a Union type - a combination of multiple possible types.
/// Used when a variable can be one of several types (e.g., from multiple assignments).
/// </summary>
public class GDUnionType
{
    /// <summary>
    /// All types in the union.
    /// </summary>
    public HashSet<string> Types { get; } = new HashSet<string>();

    /// <summary>
    /// Common base type (if found via inheritance hierarchy).
    /// </summary>
    public string? CommonBaseType { get; set; }

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
    /// Gets the effective type: single type if one, common base if available, or "Variant".
    /// </summary>
    public string EffectiveType =>
        IsSingleType ? Types.First() :
        !string.IsNullOrEmpty(CommonBaseType) ? CommonBaseType :
        "Variant";

    /// <summary>
    /// Adds a type to the union.
    /// </summary>
    /// <param name="typeName">Type name to add</param>
    /// <param name="isHighConfidence">Whether this type was inferred with high confidence</param>
    public void AddType(string typeName, bool isHighConfidence = true)
    {
        if (string.IsNullOrEmpty(typeName) || typeName == "Variant")
            return;

        Types.Add(typeName);

        if (!isHighConfidence)
            AllHighConfidence = false;
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

        // If either is empty, use the other
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

        // Intersect
        foreach (var t in Types)
        {
            if (other.Types.Contains(t))
                result.Types.Add(t);
        }

        return result;
    }

    public override string ToString()
    {
        if (IsEmpty) return "Variant";
        if (IsSingleType) return Types.First();
        return string.Join("|", Types.OrderBy(t => t));
    }
}
