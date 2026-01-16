using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a single assignment to a variable.
/// Used for tracking assignments to infer Union types.
/// </summary>
public class GDAssignmentObservation
{
    /// <summary>
    /// The inferred type of the assigned value.
    /// </summary>
    public string? InferredType { get; set; }

    /// <summary>
    /// Whether this type was inferred with high confidence.
    /// </summary>
    public bool IsHighConfidence { get; set; }

    /// <summary>
    /// The AST node of the assignment (for navigation).
    /// </summary>
    public GDNode? Node { get; set; }

    /// <summary>
    /// Line number of the assignment (1-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Column number of the assignment (1-based).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Kind of assignment (initialization, direct assignment, etc.).
    /// </summary>
    public GDAssignmentKind Kind { get; set; }

    /// <summary>
    /// Optional: ID of the branch where this assignment occurs.
    /// Format: "if", "elif_0", "elif_1", "else", or null for linear code.
    /// </summary>
    public string? BranchId { get; set; }

    public override string ToString()
    {
        var confidence = IsHighConfidence ? "High" : "Low";
        return $"[Line {Line}] {Kind}: {InferredType ?? "unknown"} ({confidence})";
    }
}

/// <summary>
/// Kind of assignment observation.
/// </summary>
public enum GDAssignmentKind
{
    /// <summary>
    /// Variable initialization (var x = value).
    /// </summary>
    Initialization,

    /// <summary>
    /// Direct assignment (x = value).
    /// </summary>
    DirectAssignment,

    /// <summary>
    /// Compound assignment (x += value).
    /// </summary>
    CompoundAssignment
}
