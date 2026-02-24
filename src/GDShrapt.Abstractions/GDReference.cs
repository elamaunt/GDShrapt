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
    public GDSemanticType? InferredType { get; set; }

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

    /// <summary>
    /// The specific identifier token that this reference points to.
    /// For member access (e.g., super.take_damage()), this is the "take_damage" identifier,
    /// not the entire expression node. Use this for precise edit positioning.
    /// </summary>
    public GDSyntaxToken? IdentifierToken { get; set; }

    /// <summary>
    /// True when this read reference exists because the variable was the caller
    /// in a property/indexer write (obj.prop = val). The dead code service uses this
    /// to distinguish genuine reads from property-write-on-caller reads.
    /// </summary>
    public bool IsPropertyWriteOnCaller { get; set; }
}
