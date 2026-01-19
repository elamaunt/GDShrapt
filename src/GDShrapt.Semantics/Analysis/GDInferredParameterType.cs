using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Result of parameter type inference.
/// Extends GDInferredType with parameter-specific information like union types
/// and inference sources.
/// </summary>
public class GDInferredParameterType
{
    /// <summary>
    /// The parameter name.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// The inferred type name. For union types, this is the combined type (e.g., "int|String").
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Confidence level of the inference.
    /// </summary>
    public GDTypeConfidence Confidence { get; }

    /// <summary>
    /// Human-readable reason for the inference.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Individual types if this is a union type, null otherwise.
    /// </summary>
    public IReadOnlyList<string>? UnionTypes { get; private init; }

    /// <summary>
    /// Whether the inferred type is a union of multiple types.
    /// </summary>
    public bool IsUnion => UnionTypes != null && UnionTypes.Count > 1;

    /// <summary>
    /// Whether the type is completely unknown (Variant).
    /// </summary>
    public bool IsUnknown => Confidence == GDTypeConfidence.Unknown;

    /// <summary>
    /// Whether the type inference was derived from duck typing (usage analysis).
    /// </summary>
    public bool IsDuckTyped { get; private init; }

    /// <summary>
    /// The constraints used to infer this type (if duck typed).
    /// </summary>
    public GDParameterConstraints? SourceConstraints { get; private init; }

    /// <summary>
    /// Internal constructor for factory methods.
    /// </summary>
    protected GDInferredParameterType(
        string paramName,
        string typeName,
        GDTypeConfidence confidence,
        string? reason = null)
    {
        ParameterName = paramName;
        TypeName = typeName;
        Confidence = confidence;
        Reason = reason;
    }

    #region Factory Methods

    /// <summary>
    /// Creates a parameter type with specified confidence and reason.
    /// </summary>
    internal static GDInferredParameterType Create(
        string paramName,
        string typeName,
        GDTypeConfidence confidence,
        string? reason = null)
        => new(paramName, typeName, confidence, reason);

    /// <summary>
    /// Creates an unknown parameter type (Variant fallback).
    /// </summary>
    public static GDInferredParameterType Unknown(string paramName)
        => new(paramName, "Variant", GDTypeConfidence.Unknown);

    /// <summary>
    /// Creates a parameter type from an explicit type annotation.
    /// </summary>
    public static GDInferredParameterType Declared(string paramName, string typeName)
        => new(paramName, typeName, GDTypeConfidence.Certain, "explicit type annotation");

    /// <summary>
    /// Creates a parameter type from duck typing analysis.
    /// </summary>
    public static GDInferredParameterType FromDuckTyping(
        string paramName,
        string typeName,
        GDParameterConstraints? constraints = null)
        => new(paramName, typeName, GDTypeConfidence.Medium, "inferred from usage (duck typing)")
        {
            IsDuckTyped = true,
            SourceConstraints = constraints
        };

    /// <summary>
    /// Creates a parameter type from call site data flow.
    /// </summary>
    public static GDInferredParameterType FromCallSite(
        string paramName,
        string typeName,
        string sourceLocation)
        => new(paramName, typeName, GDTypeConfidence.High, $"passed from {sourceLocation}");

    /// <summary>
    /// Creates a parameter type from a type check (is operator).
    /// </summary>
    public static GDInferredParameterType FromTypeCheck(
        string paramName,
        string typeName)
        => new(paramName, typeName, GDTypeConfidence.High, "narrowed by type check");

    /// <summary>
    /// Creates a union type from multiple candidate types.
    /// </summary>
    public static GDInferredParameterType Union(
        string paramName,
        List<string> types,
        GDTypeConfidence confidence,
        string? reason = null)
    {
        if (types == null || types.Count == 0)
            return Unknown(paramName);

        if (types.Count == 1)
            return new GDInferredParameterType(paramName, types[0], confidence, reason);

        var effectiveType = string.Join("|", types);
        return new GDInferredParameterType(paramName, effectiveType, confidence, reason ?? "multiple types detected")
        {
            UnionTypes = types
        };
    }

    #endregion

    /// <summary>
    /// Converts to the general GDInferredType.
    /// </summary>
    public GDInferredType ToInferredType()
        => GDInferredType.FromType(TypeName, Confidence, Reason);

    /// <summary>
    /// Returns a compact string representation for debugging/display.
    /// </summary>
    public override string ToString()
    {
        var conf = Confidence switch
        {
            GDTypeConfidence.Unknown => "?",
            GDTypeConfidence.Low => "~",
            GDTypeConfidence.Medium => "→",
            GDTypeConfidence.High => "::",
            GDTypeConfidence.Certain => ":",
            _ => ""
        };
        return $"{ParameterName}{conf}{TypeName}";
    }
}
