using GDShrapt.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Report about return type inference for a method.
/// Contains all return statements and the computed Union type.
/// </summary>
internal class GDReturnInferenceReport
{
    /// <summary>
    /// Explicit return type annotation (if any).
    /// </summary>
    public string? ExplicitType { get; init; }

    /// <summary>
    /// Whether the method has an explicit return type annotation.
    /// </summary>
    public bool HasExplicitType => !string.IsNullOrEmpty(ExplicitType) && ExplicitType != "Variant";

    /// <summary>
    /// The inferred Union type from return statements.
    /// </summary>
    public GDUnionType? InferredUnionType { get; init; }

    /// <summary>
    /// All return statements in the method.
    /// </summary>
    public List<GDReturnStatementReport> ReturnStatements { get; init; } = new();

    /// <summary>
    /// The effective return type (explicit > inferred > Variant).
    /// </summary>
    public string EffectiveType
    {
        get
        {
            if (HasExplicitType)
                return ExplicitType!;

            if (InferredUnionType != null && !InferredUnionType.IsEmpty)
                return InferredUnionType.ToString();

            return "Variant";
        }
    }

    /// <summary>
    /// Overall confidence of the inference.
    /// </summary>
    public GDReferenceConfidence Confidence { get; init; }

    /// <summary>
    /// Whether the method has any implicit returns.
    /// </summary>
    public bool HasImplicitReturn => ReturnStatements.Any(r => r.IsImplicit);

    /// <summary>
    /// Number of explicit return statements.
    /// </summary>
    public int ExplicitReturnCount => ReturnStatements.Count(r => !r.IsImplicit);

    /// <summary>
    /// Number of high confidence return statements.
    /// </summary>
    public int HighConfidenceReturnCount => ReturnStatements.Count(r => r.IsHighConfidence);

    /// <summary>
    /// Distinct types from all return statements.
    /// </summary>
    public IEnumerable<string> DistinctReturnTypes =>
        ReturnStatements
            .Select(r => r.InferredType ?? "null")
            .Distinct();

    /// <summary>
    /// Whether the return type is void (only implicit/null returns).
    /// </summary>
    public bool IsVoid => ReturnStatements.All(r => r.InferredType == null || r.InferredType == "null");

    public override string ToString()
    {
        var typeStr = EffectiveType;
        var source = HasExplicitType ? "explicit" : $"inferred from {ReturnStatements.Count} returns";
        return $"-> {typeStr} ({source})";
    }
}
