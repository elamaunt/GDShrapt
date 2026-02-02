using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a reference from one location to a symbol.
/// </summary>
public class GDReference
{
    /// <summary>
    /// The AST node that contains this reference.
    /// </summary>
    public GDNode? ReferenceNode { get; set; }

    /// <summary>
    /// The scope in which this reference occurs.
    /// </summary>
    public GDScope? Scope { get; set; }

    /// <summary>
    /// The inferred type at this reference location.
    /// </summary>
    public string? InferredType { get; set; }

    /// <summary>
    /// The full type node at this reference location.
    /// </summary>
    public GDTypeNode? InferredTypeNode { get; set; }

    /// <summary>
    /// True if this reference is a write (assignment target).
    /// </summary>
    public bool IsWrite { get; set; }

    /// <summary>
    /// True if this reference is a read.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// The confidence level of this reference.
    /// </summary>
    public GDReferenceConfidence Confidence { get; set; } = GDReferenceConfidence.NameMatch;

    /// <summary>
    /// Human-readable reason for the confidence level.
    /// </summary>
    public string? ConfidenceReason { get; set; }

    /// <summary>
    /// The type name of the caller for member access (e.g., "OS" for OS.execute()).
    /// Null for local function calls or variable references.
    /// </summary>
    public string? CallerTypeName { get; set; }
}
