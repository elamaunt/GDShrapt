using GDShrapt.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Detailed report about parameter type inference for a single parameter.
/// Contains all call sites and inferred types for visualization.
/// </summary>
internal class GDParameterInferenceReport
{
    /// <summary>
    /// Name of the parameter.
    /// </summary>
    public string ParameterName { get; init; } = "";

    /// <summary>
    /// Index of the parameter (0-based).
    /// </summary>
    public int ParameterIndex { get; init; }

    /// <summary>
    /// Explicit type annotation (if any).
    /// </summary>
    public string? ExplicitType { get; init; }

    /// <summary>
    /// Whether the parameter has an explicit type annotation.
    /// </summary>
    public bool HasExplicitType => !string.IsNullOrEmpty(ExplicitType) && ExplicitType != "Variant";

    /// <summary>
    /// The inferred Union type from call sites.
    /// </summary>
    public GDUnionType? InferredUnionType { get; init; }

    /// <summary>
    /// All call site arguments used for inference.
    /// </summary>
    public List<GDCallSiteArgumentReport> CallSiteArguments { get; init; } = new();

    /// <summary>
    /// The effective type (explicit > inferred > Variant).
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
    /// Reason for the confidence level.
    /// </summary>
    public string? ConfidenceReason { get; init; }

    /// <summary>
    /// Number of call sites used for inference.
    /// </summary>
    public int CallSiteCount => CallSiteArguments.Count;

    /// <summary>
    /// Number of high confidence call sites.
    /// </summary>
    public int HighConfidenceCallSiteCount => CallSiteArguments.Count(c => c.IsHighConfidence);

    /// <summary>
    /// Number of duck-typed call sites.
    /// </summary>
    public int DuckTypedCallSiteCount => CallSiteArguments.Count(c => c.IsDuckTyped);

    /// <summary>
    /// Distinct types inferred from all call sites.
    /// </summary>
    public IEnumerable<string> DistinctInferredTypes =>
        CallSiteArguments
            .Where(c => !string.IsNullOrEmpty(c.InferredType))
            .Select(c => c.InferredType!)
            .Distinct();

    public override string ToString()
    {
        var typeStr = EffectiveType;
        var source = HasExplicitType ? "explicit" : $"inferred from {CallSiteCount} call sites";
        return $"{ParameterName}: {typeStr} ({source})";
    }
}
