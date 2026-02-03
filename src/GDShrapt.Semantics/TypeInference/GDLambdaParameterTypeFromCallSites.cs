using GDShrapt.Abstractions;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Result of inferring a lambda parameter type from call sites.
/// </summary>
public class GDLambdaParameterTypeFromCallSites
{
    /// <summary>
    /// Name of the parameter.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Index of the parameter (0-based).
    /// </summary>
    public int ParameterIndex { get; }

    /// <summary>
    /// Union of argument types from all call sites.
    /// </summary>
    public GDUnionType ArgumentTypes { get; }

    /// <summary>
    /// Call sites that contributed to this inference.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> Sources { get; }

    /// <summary>
    /// Confidence level of the inference.
    /// </summary>
    public GDTypeConfidence Confidence { get; }

    /// <summary>
    /// The effective inferred type (single type, common base, or Variant).
    /// </summary>
    public string EffectiveType => ArgumentTypes.EffectiveType;

    /// <summary>
    /// Whether this is a high-confidence inference.
    /// </summary>
    public bool IsHighConfidence => Confidence <= GDTypeConfidence.High;

    /// <summary>
    /// Whether all call sites agree on the type.
    /// </summary>
    public bool IsHomogeneous => ArgumentTypes.IsSingleType;

    /// <summary>
    /// Number of call sites that contributed to this inference.
    /// </summary>
    public int CallSiteCount => Sources.Count;

    public GDLambdaParameterTypeFromCallSites(
        string parameterName,
        int parameterIndex,
        GDUnionType argumentTypes,
        IReadOnlyList<GDCallableCallSiteInfo> sources,
        GDTypeConfidence confidence)
    {
        ParameterName = parameterName;
        ParameterIndex = parameterIndex;
        ArgumentTypes = argumentTypes;
        Sources = sources;
        Confidence = confidence;
    }

    /// <summary>
    /// Creates an inference result with no call sites (unknown type).
    /// </summary>
    public static GDLambdaParameterTypeFromCallSites Unknown(string parameterName, int parameterIndex)
    {
        return new GDLambdaParameterTypeFromCallSites(
            parameterName,
            parameterIndex,
            new GDUnionType(),
            System.Array.Empty<GDCallableCallSiteInfo>(),
            GDTypeConfidence.Unknown);
    }

    /// <summary>
    /// Creates an inference result from a union type and sources.
    /// </summary>
    public static GDLambdaParameterTypeFromCallSites FromCallSites(
        string parameterName,
        int parameterIndex,
        GDUnionType argumentTypes,
        IReadOnlyList<GDCallableCallSiteInfo> sources)
    {
        var confidence = DetermineConfidence(argumentTypes, sources);

        return new GDLambdaParameterTypeFromCallSites(
            parameterName,
            parameterIndex,
            argumentTypes,
            sources,
            confidence);
    }

    private static GDTypeConfidence DetermineConfidence(
        GDUnionType argumentTypes,
        IReadOnlyList<GDCallableCallSiteInfo> sources)
    {
        if (sources.Count == 0 || argumentTypes.IsEmpty)
            return GDTypeConfidence.Unknown;

        // Single type from multiple call sites -> high confidence
        if (argumentTypes.IsSingleType && sources.Count >= 2)
            return GDTypeConfidence.High;

        // Single type from single call site -> medium confidence
        if (argumentTypes.IsSingleType)
            return GDTypeConfidence.Medium;

        // Multiple types -> low confidence (union)
        if (argumentTypes.AllHighConfidence)
            return GDTypeConfidence.Medium;

        return GDTypeConfidence.Low;
    }

    public override string ToString()
    {
        return $"{ParameterName}[{ParameterIndex}]: {EffectiveType} ({Confidence}, {CallSiteCount} call sites)";
    }
}
