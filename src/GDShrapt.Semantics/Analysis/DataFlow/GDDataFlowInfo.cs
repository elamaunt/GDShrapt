using System;
using System.Collections.Generic;
using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

/// <summary>
/// Complete data flow information for a symbol at a specific program point.
/// </summary>
public sealed class GDDataFlowInfo
{
    /// <summary>
    /// The union type with per-type origins showing what flows through this point.
    /// </summary>
    public GDUnionType FlowType { get; }

    /// <summary>
    /// The single effective type (best guess: narrowed > declared > inferred > Variant).
    /// </summary>
    public GDSemanticType EffectiveType { get; }

    /// <summary>
    /// Active type narrowing constraints at this point.
    /// </summary>
    public IReadOnlyList<GDNarrowingConstraint> ActiveNarrowings { get; }

    /// <summary>
    /// Duck type constraints from capability checks.
    /// </summary>
    public GDDuckType? DuckType { get; }

    /// <summary>
    /// Whether this is guaranteed non-null at this point.
    /// </summary>
    public bool IsGuaranteedNonNull { get; }

    /// <summary>
    /// Whether this is potentially null at this point.
    /// </summary>
    public bool IsPotentiallyNull { get; }

    /// <summary>
    /// Points where this variable's data escaped analysis scope.
    /// </summary>
    public IReadOnlyList<GDEscapePoint> EscapePoints { get; }

    /// <summary>
    /// Object state snapshot at this point (scene hierarchy, collision layers, properties).
    /// </summary>
    public GDObjectState? ObjectState { get; }

    public GDDataFlowInfo(
        GDUnionType flowType,
        GDSemanticType effectiveType,
        IReadOnlyList<GDNarrowingConstraint> activeNarrowings,
        GDDuckType? duckType,
        bool isGuaranteedNonNull,
        bool isPotentiallyNull,
        IReadOnlyList<GDEscapePoint> escapePoints,
        GDObjectState? objectState)
    {
        FlowType = flowType ?? throw new ArgumentNullException(nameof(flowType));
        EffectiveType = effectiveType ?? throw new ArgumentNullException(nameof(effectiveType));
        ActiveNarrowings = activeNarrowings ?? Array.Empty<GDNarrowingConstraint>();
        DuckType = duckType;
        IsGuaranteedNonNull = isGuaranteedNonNull;
        IsPotentiallyNull = isPotentiallyNull;
        EscapePoints = escapePoints ?? Array.Empty<GDEscapePoint>();
        ObjectState = objectState;
    }

    public override string ToString()
    {
        var escape = EscapePoints.Count > 0 ? $", {EscapePoints.Count} escape(s)" : "";
        var state = ObjectState != null ? ", has object state" : "";
        return $"{EffectiveType.DisplayName} (flow: {FlowType}{escape}{state})";
    }
}
