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
    /// Priority: narrowing > current inferred > declared > Variant
    /// </summary>
    public string EffectiveType =>
        IsNarrowed && !string.IsNullOrEmpty(NarrowedFromType) ? NarrowedFromType :
        !CurrentType.IsEmpty ? CurrentType.EffectiveType :
        DeclaredType ?? "Variant";

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
                    return CurrentType.Types.First();
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
