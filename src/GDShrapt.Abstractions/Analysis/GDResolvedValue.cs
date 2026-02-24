using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

/// <summary>
/// A resolved compile-time value with source location.
/// </summary>
public sealed class GDResolvedValue
{
    /// <summary>
    /// The resolved value (string, int, etc. — consumer casts as needed).
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// AST node where the value physically resides (for edits/renames).
    /// Null if value is computed (concatenation, arithmetic).
    /// </summary>
    public GDExpression? SourceNode { get; }

    /// <summary>
    /// Confidence of this resolution.
    /// </summary>
    public GDReferenceConfidence Confidence { get; }

    public GDResolvedValue(object value, GDExpression? sourceNode,
        GDReferenceConfidence confidence = GDReferenceConfidence.Strict)
    {
        Value = value;
        SourceNode = sourceNode;
        Confidence = confidence;
    }
}
