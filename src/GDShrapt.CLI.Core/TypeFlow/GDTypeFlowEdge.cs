namespace GDShrapt.CLI.Core;

/// <summary>
/// Represents an edge in the type flow graph.
/// Edges connect nodes and carry information about the relationship (type flow, assignment, constraint).
/// </summary>
public class GDTypeFlowEdge
{
    /// <summary>
    /// Unique identifier for this edge.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Source node (where the type flows from).
    /// </summary>
    public GDTypeFlowNode? Source { get; set; }

    /// <summary>
    /// Target node (where the type flows to).
    /// </summary>
    public GDTypeFlowNode? Target { get; set; }

    /// <summary>
    /// The kind of edge (type flow, assignment, union member, duck constraint).
    /// </summary>
    public GDTypeFlowEdgeKind Kind { get; set; }

    /// <summary>
    /// Optional label for the edge.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Confidence level of this edge (0.0 - 1.0).
    /// </summary>
    public float Confidence { get; set; } = 1.0f;

    /// <summary>
    /// Duck typing constraints for this edge (if Kind == DuckConstraint).
    /// </summary>
    public GDEdgeConstraints? Constraints { get; set; }

    /// <summary>
    /// Whether this edge should be drawn with dashed lines.
    /// </summary>
    public bool IsDashed => Kind == GDTypeFlowEdgeKind.DuckConstraint || Confidence < 0.5f;

    /// <summary>
    /// Gets the line width based on confidence.
    /// </summary>
    public float GetLineWidth()
    {
        return Kind switch
        {
            GDTypeFlowEdgeKind.DuckConstraint => 1.5f,
            _ => Confidence >= 0.8f ? 2.0f : 1.5f
        };
    }

    public override string ToString()
    {
        return $"{Source?.Label ?? "?"} --[{Kind}]--> {Target?.Label ?? "?"}";
    }
}
