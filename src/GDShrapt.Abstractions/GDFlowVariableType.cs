using GDShrapt.Reader;
using System.Linq;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents the type state of a variable at a specific point in the program.
/// Used for flow-sensitive type analysis (SSA-style).
/// </summary>
public class GDFlowVariableType
{
    /// <summary>
    /// The current inferred type at this point (can be Union).
    /// </summary>
    public GDUnionType CurrentType { get; set; } = new();

    /// <summary>
    /// The explicitly declared type (if any). Null for Variant variables.
    /// </summary>
    public string? DeclaredType { get; set; }

    /// <summary>
    /// True if current type comes from type narrowing (temporary).
    /// </summary>
    public bool IsNarrowed { get; set; }

    /// <summary>
    /// The narrowing type that currently applies (for restoration).
    /// </summary>
    public string? NarrowedFromType { get; set; }

    /// <summary>
    /// Last assignment AST node (for source tracking).
    /// </summary>
    public GDNode? LastAssignmentNode { get; set; }

    /// <summary>
    /// Gets the effective type for display/inference.
    /// Priority: narrowing > declared (when current is null/generic base) > current inferred > declared > Variant
    /// </summary>
    public string EffectiveType
    {
        get
        {
            if (IsNarrowed && !string.IsNullOrEmpty(NarrowedFromType))
                return NarrowedFromType;

            if (!CurrentType.IsEmpty)
            {
                var currentEffective = CurrentType.EffectiveType;

                // If DeclaredType exists and CurrentType is only "null", prefer DeclaredType
                // This ensures "var x: Node = null" returns "Node", not "null"
                if (!string.IsNullOrEmpty(DeclaredType) && currentEffective == "null")
                    return DeclaredType;

                // If DeclaredType is a generic version of CurrentType, prefer DeclaredType
                // e.g., DeclaredType = "Dictionary[String,int]", CurrentType = "Dictionary"
                if (!string.IsNullOrEmpty(DeclaredType) && IsGenericVersionOf(DeclaredType, currentEffective))
                    return DeclaredType;

                return currentEffective;
            }

            return DeclaredType ?? "Variant";
        }
    }

    /// <summary>
    /// Checks if genericType is a generic version of baseType.
    /// e.g., "Dictionary[String,int]" is generic version of "Dictionary".
    /// </summary>
    private static bool IsGenericVersionOf(string genericType, string baseType)
    {
        if (string.IsNullOrEmpty(genericType) || string.IsNullOrEmpty(baseType))
            return false;

        // Check if genericType starts with baseType followed by '['
        var bracketIndex = genericType.IndexOf('[');
        if (bracketIndex <= 0)
            return false;

        var genericBase = genericType.Substring(0, bracketIndex);
        return genericBase.Equals(baseType, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the effective type as a formatted string.
    /// For Union types, returns "Type1 | Type2" format.
    /// </summary>
    public string EffectiveTypeFormatted
    {
        get
        {
            if (IsNarrowed && !string.IsNullOrEmpty(NarrowedFromType))
                return NarrowedFromType;

            if (!CurrentType.IsEmpty)
            {
                if (CurrentType.IsSingleType)
                {
                    var singleType = CurrentType.Types.First();
                    // Prefer DeclaredType when current is null
                    if (!string.IsNullOrEmpty(DeclaredType) && singleType == "null")
                        return DeclaredType;
                    // Prefer DeclaredType if it's a generic version
                    if (!string.IsNullOrEmpty(DeclaredType) && IsGenericVersionOf(DeclaredType, singleType))
                        return DeclaredType;
                    return singleType;
                }
                if (CurrentType.IsUnion)
                    return string.Join(" | ", CurrentType.Types.OrderBy(t => t));
            }

            return DeclaredType ?? "Variant";
        }
    }

    /// <summary>
    /// Creates a copy of this flow type.
    /// </summary>
    public GDFlowVariableType Clone() => new()
    {
        CurrentType = CloneUnion(CurrentType),
        DeclaredType = DeclaredType,
        IsNarrowed = IsNarrowed,
        NarrowedFromType = NarrowedFromType,
        LastAssignmentNode = LastAssignmentNode
    };

    private static GDUnionType CloneUnion(GDUnionType original)
    {
        var clone = new GDUnionType
        {
            AllHighConfidence = original.AllHighConfidence,
            CommonBaseType = original.CommonBaseType,
            ConfidenceReason = original.ConfidenceReason
        };
        foreach (var t in original.Types)
            clone.Types.Add(t);
        return clone;
    }

    public override string ToString()
    {
        var prefix = IsNarrowed ? "[narrowed] " : "";
        var declared = DeclaredType != null ? $" (declared: {DeclaredType})" : "";
        return $"{prefix}{EffectiveTypeFormatted}{declared}";
    }
}
