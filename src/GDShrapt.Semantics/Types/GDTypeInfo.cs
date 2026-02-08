using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Complete type information for a variable/expression at a specific program point.
/// Combines declared (from code), inferred (from analysis), and narrowed (from control flow).
/// </summary>
public class GDTypeInfo
{
    // ========================================
    // DECLARED TYPE — explicit type annotation from source code
    // ========================================

    /// <summary>
    /// AST node for type annotation (var x: int, func f() -> String).
    /// Null if type is not explicitly specified (Variant).
    /// </summary>
    public GDTypeNode? DeclaredTypeNode { get; init; }

    /// <summary>
    /// String representation of declared type.
    /// </summary>
    public string? DeclaredTypeName => DeclaredTypeNode?.BuildName();

    /// <summary>
    /// Declared type as GDSemanticType (for unification with inferred).
    /// </summary>
    public GDSemanticType? DeclaredSemanticType { get; init; }

    // ========================================
    // INFERRED TYPE — type deduced from usage analysis
    // MAY BE GDUnionSemanticType!
    // ========================================

    /// <summary>
    /// Inferred type. May be:
    /// - GDSimpleSemanticType ("int", "Node")
    /// - GDUnionSemanticType ("int|String") — when multiple assignments of different types
    /// - GDCallableSemanticType ("Callable(int, String) -> void")
    /// - GDVariantSemanticType (when inference failed)
    /// </summary>
    public GDSemanticType InferredType { get; set; } = GDVariantSemanticType.Instance;

    /// <summary>
    /// Whether the inferred type is a union of multiple types.
    /// </summary>
    public bool IsUnionType => InferredType is GDUnionSemanticType;

    /// <summary>
    /// Get all types in the union (or single type if not union).
    /// </summary>
    public IReadOnlyList<GDSemanticType> UnionMembers =>
        InferredType is GDUnionSemanticType union
            ? union.Types
            : new[] { InferredType };

    // ========================================
    // NARROWED TYPE — temporarily narrowed after if x is Type:
    // ========================================

    /// <summary>
    /// Narrowed type in current scope (after `if x is Node:`).
    /// Null if narrowing is not applied.
    /// </summary>
    public GDSemanticType? NarrowedType { get; set; }

    /// <summary>
    /// Whether the type is currently narrowed.
    /// </summary>
    public bool IsNarrowed => NarrowedType != null;

    // ========================================
    // NULLABILITY — can be null
    // ========================================

    /// <summary>
    /// Whether the type can be null by its nature (Object, Variant).
    /// False for value types (int, float, Vector2, etc.)
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Whether guaranteed non-null at current point (after null check).
    /// </summary>
    public bool IsGuaranteedNonNull { get; set; }

    /// <summary>
    /// Whether potentially null (before null check or after null assignment).
    /// </summary>
    public bool IsPotentiallyNull { get; set; }

    // ========================================
    // DUCK-TYPE CONSTRAINTS — from has_method/has checks
    // ========================================

    /// <summary>
    /// Duck-type constraints from has_method(), has(), has_signal() checks.
    /// </summary>
    public GDDuckType? DuckTypeConstraints { get; set; }

    // ========================================
    // CONTAINER INFO — for Array[T], Dictionary[K,V]
    // ========================================

    /// <summary>
    /// Container element info (if applicable).
    /// Populated from declared type or inferred from usage (append, []=).
    /// </summary>
    public GDContainerElementType? ContainerInfo { get; set; }

    // ========================================
    // METADATA
    // ========================================

    /// <summary>
    /// Last assignment node (for tracing type source).
    /// </summary>
    public GDNode? LastAssignmentNode { get; set; }

    /// <summary>
    /// Confidence level in the inferred type.
    /// </summary>
    public GDTypeConfidence Confidence { get; set; } = GDTypeConfidence.Unknown;

    // ========================================
    // COMPUTED PROPERTIES
    // ========================================

    /// <summary>
    /// Effective type to use for checks.
    /// Priority: Narrowed > Inferred (if not Variant) > Declared > Variant
    /// </summary>
    public GDSemanticType EffectiveType
    {
        get
        {
            if (NarrowedType != null)
                return NarrowedType;

            if (InferredType != null && !InferredType.IsVariant)
                return InferredType;

            return DeclaredSemanticType ?? GDVariantSemanticType.Instance;
        }
    }

    /// <summary>
    /// Effective type as string.
    /// </summary>
    public string EffectiveTypeName => EffectiveType.DisplayName;

    /// <summary>
    /// Add an observed type (from a new assignment).
    /// If InferredType is not a union — converts to union.
    /// </summary>
    public void AddObservedType(GDSemanticType observedType)
    {
        if (InferredType.IsVariant)
        {
            InferredType = observedType;
            Confidence = GDTypeConfidence.High;
            return;
        }

        if (InferredType.DisplayName == observedType.DisplayName)
            return;

        // Create/expand union
        InferredType = GDSemanticType.CreateUnion(InferredType, observedType);
        Confidence = GDTypeConfidence.High;
    }

    /// <summary>
    /// Creates GDTypeInfo from a GDFlowVariableType.
    /// </summary>
    public static GDTypeInfo FromFlowType(GDFlowVariableType? flowType)
    {
        if (flowType == null)
            return new GDTypeInfo();

        var info = new GDTypeInfo
        {
            DeclaredSemanticType = flowType.DeclaredType,
            InferredType = flowType.EffectiveType,
            NarrowedType = flowType.IsNarrowed ? flowType.NarrowedFromType : null,
            IsNullable = !flowType.IsGuaranteedNonNull,
            IsGuaranteedNonNull = flowType.IsGuaranteedNonNull,
            IsPotentiallyNull = flowType.IsPotentiallyNull,
            DuckTypeConstraints = flowType.DuckType,
            LastAssignmentNode = flowType.LastAssignmentNode
        };

        // Determine confidence
        if (info.NarrowedType != null)
            info.Confidence = GDTypeConfidence.Certain; // Narrowed type is certain from control flow
        else if (info.DeclaredSemanticType != null)
            info.Confidence = GDTypeConfidence.Certain; // Declared type is certain
        else if (!info.InferredType.IsVariant)
            info.Confidence = GDTypeConfidence.High; // Inferred type is high confidence
        else
            info.Confidence = GDTypeConfidence.Unknown;

        return info;
    }

    public override string ToString()
    {
        var parts = new List<string>();

        if (DeclaredSemanticType != null)
            parts.Add($"declared:{DeclaredSemanticType.DisplayName}");

        if (!InferredType.IsVariant)
            parts.Add($"inferred:{InferredType.DisplayName}");

        if (NarrowedType != null)
            parts.Add($"narrowed:{NarrowedType.DisplayName}");

        if (IsPotentiallyNull)
            parts.Add("nullable");

        if (IsGuaranteedNonNull)
            parts.Add("non-null");

        return $"GDTypeInfo[{string.Join(", ", parts)}]";
    }
}
