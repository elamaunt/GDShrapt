using System;
using System.Collections.Generic;
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
    /// Also added to CurrentType as an origin with Kind=Declaration during construction.
    /// </summary>
    public GDSemanticType? DeclaredType { get; set; }

    /// <summary>
    /// True if current type comes from type narrowing (temporary).
    /// </summary>
    public bool IsNarrowed { get; set; }

    /// <summary>
    /// The narrowing type that currently applies (for restoration).
    /// </summary>
    public GDSemanticType? NarrowedFromType { get; set; }

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

    private List<GDNarrowingConstraint>? _activeNarrowings;
    private List<GDEscapePoint>? _escapePoints;

    /// <summary>
    /// Active narrowing constraints at this program point.
    /// </summary>
    public IReadOnlyList<GDNarrowingConstraint> ActiveNarrowings
        => (IReadOnlyList<GDNarrowingConstraint>?)_activeNarrowings ?? Array.Empty<GDNarrowingConstraint>();

    /// <summary>
    /// Points where this variable's data escaped analysis scope.
    /// </summary>
    public IReadOnlyList<GDEscapePoint> EscapePoints
        => (IReadOnlyList<GDEscapePoint>?)_escapePoints ?? Array.Empty<GDEscapePoint>();

    public void AddNarrowing(GDNarrowingConstraint narrowing)
    {
        if (_activeNarrowings == null)
            _activeNarrowings = new List<GDNarrowingConstraint>();
        _activeNarrowings.Add(narrowing);
    }

    public void ClearNarrowings()
    {
        _activeNarrowings?.Clear();
    }

    public void AddEscapePoint(GDEscapePoint escape)
    {
        if (_escapePoints == null)
            _escapePoints = new List<GDEscapePoint>();
        _escapePoints.Add(escape);
    }

    /// <summary>
    /// Gets the effective type for display/inference.
    /// Priority: narrowing > declared (when current is null/generic base) > current inferred > declared > Variant
    /// </summary>
    public GDSemanticType EffectiveType
    {
        get
        {
            if (IsNarrowed && NarrowedFromType != null)
                return NarrowedFromType;

            if (!CurrentType.IsEmpty)
            {
                var currentEffective = CurrentType.EffectiveType;

                if (DeclaredType != null && currentEffective is GDNullSemanticType)
                    return DeclaredType;

                if (DeclaredType != null && IsGenericVersionOf(DeclaredType.DisplayName, currentEffective.DisplayName))
                    return DeclaredType;

                return currentEffective;
            }

            return DeclaredType ?? GDVariantSemanticType.Instance;
        }
    }

    private static bool IsGenericVersionOf(string genericType, string baseType)
    {
        if (string.IsNullOrEmpty(genericType) || string.IsNullOrEmpty(baseType))
            return false;

        var bracketIndex = genericType.IndexOf('[');
        if (bracketIndex <= 0)
            return false;

        var genericBase = genericType.Substring(0, bracketIndex);
        return genericBase.Equals(baseType, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the effective type as a formatted string.
    /// For Union types, returns "Type1|Type2" format.
    /// </summary>
    public string EffectiveTypeFormatted
    {
        get
        {
            if (IsNarrowed && NarrowedFromType != null)
                return NarrowedFromType.DisplayName;

            if (!CurrentType.IsEmpty)
            {
                if (CurrentType.IsSingleType)
                {
                    var singleType = CurrentType.Types.First();
                    if (DeclaredType != null && singleType is GDNullSemanticType)
                        return DeclaredType.DisplayName;
                    if (DeclaredType != null && IsGenericVersionOf(DeclaredType.DisplayName, singleType.DisplayName))
                        return DeclaredType.DisplayName;
                    return singleType.DisplayName;
                }
                if (CurrentType.IsUnion)
                    return string.Join("|", CurrentType.Types.Select(t => t.DisplayName).OrderBy(t => t));
            }

            return DeclaredType?.DisplayName ?? "Variant";
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
        IsGuaranteedNonNull = IsGuaranteedNonNull,
        IsPotentiallyNull = IsPotentiallyNull,
        DuckType = DuckType != null ? CloneDuckType(DuckType) : null,
        _activeNarrowings = _activeNarrowings != null ? new List<GDNarrowingConstraint>(_activeNarrowings) : null,
        _escapePoints = _escapePoints != null ? new List<GDEscapePoint>(_escapePoints) : null
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

        if (original.HasOrigins)
        {
            // Use AddType(type, origin) which adds both type and origin
            foreach (var tracked in original.AllTrackedTypes)
                clone.AddType(tracked.Type, tracked.Origin);

            // Also add any types that have no origins
            foreach (var t in original.Types)
                clone.Types.Add(t); // HashSet.Add is idempotent
        }
        else
        {
            foreach (var t in original.Types)
                clone.Types.Add(t);
        }

        return clone;
    }

    public override string ToString()
    {
        var prefix = IsNarrowed ? "[narrowed] " : "";
        var declared = DeclaredType != null ? $" (declared: {DeclaredType.DisplayName})" : "";
        return $"{prefix}{EffectiveTypeFormatted}{declared}";
    }
}
