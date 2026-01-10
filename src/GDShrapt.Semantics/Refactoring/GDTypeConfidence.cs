namespace GDShrapt.Semantics;

/// <summary>
/// Confidence level for inferred types.
/// </summary>
public enum GDTypeConfidence
{
    /// <summary>
    /// Type is explicitly annotated or from known literal.
    /// </summary>
    Certain,

    /// <summary>
    /// Type inferred from expression with full type info.
    /// </summary>
    High,

    /// <summary>
    /// Type inferred but some uncertainty (method return, generics).
    /// </summary>
    Medium,

    /// <summary>
    /// Type guessed from heuristics or naming conventions.
    /// </summary>
    Low,

    /// <summary>
    /// Type completely unknown (Variant fallback).
    /// </summary>
    Unknown
}

/// <summary>
/// Result of type inference with confidence level.
/// </summary>
public sealed class GDInferredType
{
    /// <summary>
    /// The inferred type name.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Confidence level of the inference.
    /// </summary>
    public GDTypeConfidence Confidence { get; }

    /// <summary>
    /// Human-readable reason for the confidence level.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Whether the type is unknown (Variant fallback).
    /// </summary>
    public bool IsUnknown => Confidence == GDTypeConfidence.Unknown;

    /// <summary>
    /// Whether the type is certain (explicit annotation or literal).
    /// </summary>
    public bool IsCertain => Confidence == GDTypeConfidence.Certain;

    /// <summary>
    /// Whether the type has high confidence or better.
    /// </summary>
    public bool IsHighOrBetter => Confidence <= GDTypeConfidence.High;

    private GDInferredType(string typeName, GDTypeConfidence confidence, string? reason)
    {
        TypeName = typeName ?? "Variant";
        Confidence = confidence;
        Reason = reason;
    }

    /// <summary>
    /// Creates a certain type inference (explicit annotation or literal).
    /// </summary>
    public static GDInferredType Certain(string type) =>
        new(type, GDTypeConfidence.Certain, "Explicit type annotation or literal");

    /// <summary>
    /// Creates a certain type inference with custom reason.
    /// </summary>
    public static GDInferredType Certain(string type, string reason) =>
        new(type, GDTypeConfidence.Certain, reason);

    /// <summary>
    /// Creates a high-confidence type inference.
    /// </summary>
    public static GDInferredType High(string type, string reason) =>
        new(type, GDTypeConfidence.High, reason);

    /// <summary>
    /// Creates a medium-confidence type inference.
    /// </summary>
    public static GDInferredType Medium(string type, string reason) =>
        new(type, GDTypeConfidence.Medium, reason);

    /// <summary>
    /// Creates a low-confidence type inference.
    /// </summary>
    public static GDInferredType Low(string type, string reason) =>
        new(type, GDTypeConfidence.Low, reason);

    /// <summary>
    /// Creates an unknown type inference (Variant fallback).
    /// </summary>
    public static GDInferredType Unknown() =>
        new("Variant", GDTypeConfidence.Unknown, "Type cannot be determined");

    /// <summary>
    /// Creates an unknown type inference with custom reason.
    /// </summary>
    public static GDInferredType Unknown(string reason) =>
        new("Variant", GDTypeConfidence.Unknown, reason);

    /// <summary>
    /// Creates a type inference from a type name with automatic confidence detection.
    /// Returns Unknown if type is null, empty, or "Variant".
    /// </summary>
    public static GDInferredType FromType(string? type, GDTypeConfidence confidence, string? reason = null)
    {
        if (string.IsNullOrEmpty(type) || type == "Variant")
            return Unknown(reason ?? "Type cannot be determined");

        return new GDInferredType(type, confidence, reason);
    }

    public override string ToString() =>
        $"{TypeName} ({Confidence}{(Reason != null ? $": {Reason}" : "")})";
}
