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
    /// Whether this variable is guaranteed to be non-null at this program point.
    /// Set to true after null checks (x != null) or truthiness checks (if x:).
    /// </summary>
    public bool IsGuaranteedNonNull { get; set; }

    /// <summary>
    /// Whether this variable is potentially null at this program point.
    /// Set to false for types that cannot be null (int, float, bool, etc.).
    /// Default is true for reference types and Variant.
    /// </summary>
    public bool IsPotentiallyNull { get; set; } = true;

    /// <summary>
    /// Duck-type constraints from has_method/has/has_signal checks.
    /// Null if no duck-type constraints have been applied.
    /// </summary>
    public GDDuckType? DuckType { get; set; }

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
    /// For Union types, returns "Type1|Type2" format.
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
                    return string.Join("|", CurrentType.Types.OrderBy(t => t));
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
        LastAssignmentNode = LastAssignmentNode,
        IsGuaranteedNonNull = IsGuaranteedNonNull,
        IsPotentiallyNull = IsPotentiallyNull,
        DuckType = DuckType != null ? CloneDuckType(DuckType) : null
    };

    private static GDDuckType CloneDuckType(GDDuckType original)
    {
        var clone = new GDDuckType();
        foreach (var kv in original.RequiredMethods)
            clone.RequiredMethods[kv.Key] = kv.Value;
        foreach (var kv in original.RequiredProperties)
            clone.RequiredProperties[kv.Key] = kv.Value;
        foreach (var s in original.RequiredSignals)
            clone.RequiredSignals.Add(s);
        foreach (var t in original.PossibleTypes)
            clone.PossibleTypes.Add(t);
        foreach (var t in original.ExcludedTypes)
            clone.ExcludedTypes.Add(t);
        return clone;
    }

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
