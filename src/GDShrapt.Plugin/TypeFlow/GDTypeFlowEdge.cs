using GDShrapt.Abstractions;

namespace GDShrapt.Plugin;

/// <summary>
/// Represents an edge in the type flow graph.
/// Edges connect nodes and carry information about the relationship (type flow, assignment, constraint).
/// </summary>
internal class GDTypeFlowEdge
{
    /// <summary>
    /// Unique identifier for this edge.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Source node (where the type flows from).
    /// </summary>
    public GDTypeFlowNode Source { get; set; }

    /// <summary>
    /// Target node (where the type flows to).
    /// </summary>
    public GDTypeFlowNode Target { get; set; }

    /// <summary>
    /// The kind of edge (type flow, assignment, union member, duck constraint).
    /// </summary>
    public GDTypeFlowEdgeKind Kind { get; set; }

    /// <summary>
    /// Optional label for the edge.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Confidence level of this edge (0.0 - 1.0).
    /// </summary>
    public float Confidence { get; set; } = 1.0f;

    /// <summary>
    /// Duck typing constraints for this edge (if Kind == DuckConstraint).
    /// </summary>
    public GDEdgeConstraints Constraints { get; set; }

    /// <summary>
    /// Gets the color for this edge based on its kind.
    /// </summary>
    public Color GetEdgeColor()
    {
        return Kind switch
        {
            GDTypeFlowEdgeKind.TypeFlow => new Color(0.5f, 0.7f, 0.5f),        // Green
            GDTypeFlowEdgeKind.Assignment => new Color(0.5f, 0.7f, 0.9f),      // Blue
            GDTypeFlowEdgeKind.UnionMember => new Color(0.7f, 0.5f, 0.9f),     // Purple
            GDTypeFlowEdgeKind.DuckConstraint => new Color(1.0f, 0.85f, 0.3f), // Yellow
            GDTypeFlowEdgeKind.Return => new Color(1.0f, 0.7f, 0.4f),          // Orange
            _ => new Color(0.5f, 0.5f, 0.5f)                                    // Gray
        };
    }

    /// <summary>
    /// Gets the line width for this edge based on confidence.
    /// </summary>
    public float GetLineWidth()
    {
        return Kind switch
        {
            GDTypeFlowEdgeKind.DuckConstraint => 1.5f,
            _ => Confidence >= 0.8f ? 2.0f : 1.5f
        };
    }

    /// <summary>
    /// Whether this edge should be drawn with dashed lines.
    /// </summary>
    public bool IsDashed => Kind == GDTypeFlowEdgeKind.DuckConstraint || Confidence < 0.5f;

    public override string ToString()
    {
        return $"{Source?.Label ?? "?"} --[{Kind}]--> {Target?.Label ?? "?"}";
    }
}

/// <summary>
/// Types of edges in the type flow graph.
/// </summary>
internal enum GDTypeFlowEdgeKind
{
    /// <summary>
    /// Normal type flow (type inference propagation).
    /// </summary>
    TypeFlow,

    /// <summary>
    /// Assignment edge (value assigned to variable).
    /// </summary>
    Assignment,

    /// <summary>
    /// Union member edge (one type in a union).
    /// </summary>
    UnionMember,

    /// <summary>
    /// Duck type constraint edge (method/property requirement).
    /// </summary>
    DuckConstraint,

    /// <summary>
    /// Return value edge (method return type).
    /// </summary>
    Return
}

/// <summary>
/// Represents duck typing constraints attached to an edge.
/// Used to show what methods/properties are required.
/// </summary>
internal class GDEdgeConstraints
{
    /// <summary>
    /// Methods required by this constraint.
    /// Key = method name, Value = parameter count.
    /// </summary>
    public Dictionary<string, int> RequiredMethods { get; } = new();

    /// <summary>
    /// Properties required by this constraint.
    /// Key = property name, Value = expected type (or null).
    /// </summary>
    public Dictionary<string, string> RequiredProperties { get; } = new();

    /// <summary>
    /// Signals required by this constraint.
    /// </summary>
    public HashSet<string> RequiredSignals { get; } = new();

    /// <summary>
    /// Creates constraints from a GDDuckType.
    /// </summary>
    public static GDEdgeConstraints FromDuckType(GDDuckType duckType)
    {
        if (duckType == null)
            return null;

        var constraints = new GDEdgeConstraints();

        foreach (var kv in duckType.RequiredMethods)
            constraints.RequiredMethods[kv.Key] = kv.Value;

        foreach (var kv in duckType.RequiredProperties)
        {
            if (kv.Value != null)
                constraints.RequiredProperties[kv.Key] = kv.Value;
        }

        foreach (var signal in duckType.RequiredSignals)
            constraints.RequiredSignals.Add(signal);

        return constraints;
    }

    /// <summary>
    /// Whether this constraint has any requirements.
    /// </summary>
    public bool HasRequirements =>
        RequiredMethods.Count > 0 ||
        RequiredProperties.Count > 0 ||
        RequiredSignals.Count > 0;

    /// <summary>
    /// Gets a summary string for tooltip display.
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string>();

        if (RequiredMethods.Count > 0)
        {
            var methods = string.Join(", ", RequiredMethods.Keys.Select(m => m + "()"));
            parts.Add($"Methods: {methods}");
        }

        if (RequiredProperties.Count > 0)
        {
            var props = string.Join(", ", RequiredProperties.Keys);
            parts.Add($"Properties: {props}");
        }

        if (RequiredSignals.Count > 0)
        {
            var signals = string.Join(", ", RequiredSignals);
            parts.Add($"Signals: {signals}");
        }

        return string.Join("\n", parts);
    }
}
