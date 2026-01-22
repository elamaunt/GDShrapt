using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Represents a node in the type inference flow graph.
/// Each node represents a symbol, expression, or type source that contributes to type inference.
/// This is the data model - UI rendering is handled by consumers (Plugin, CLI, etc.).
/// </summary>
public class GDTypeFlowNode
{
    /// <summary>
    /// Unique identifier for this node within the graph.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display label for the node (e.g., "event", "is_action_pressed()").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The inferred or declared type (e.g., "InputEvent", "bool", "Variant").
    /// </summary>
    public string Type { get; set; } = "Variant";

    /// <summary>
    /// Confidence level of the type inference (0.0 - 1.0).
    /// 1.0 = explicit annotation or known built-in type.
    /// 0.5 = inferred from usage patterns.
    /// 0.0 = unknown/Variant.
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// The kind of this node (what it represents in the code).
    /// </summary>
    public GDTypeFlowNodeKind Kind { get; set; }

    /// <summary>
    /// A short description of how/why this type was inferred.
    /// Shown as secondary information in the UI.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Nodes that flow INTO this node (type sources).
    /// These are nodes that contribute to this node's type inference.
    /// </summary>
    public List<GDTypeFlowNode> Inflows { get; set; } = new();

    /// <summary>
    /// Nodes that flow OUT from this node (type consumers).
    /// These are nodes that use this node's type.
    /// </summary>
    public List<GDTypeFlowNode> Outflows { get; set; } = new();

    /// <summary>
    /// Source location for editor navigation.
    /// </summary>
    public GDSourceLocation? Location { get; set; }

    /// <summary>
    /// Reference to the script file containing this node.
    /// </summary>
    public GDScriptFile? SourceScript { get; set; }

    /// <summary>
    /// Reference to the AST node (if available).
    /// </summary>
    public GDNode? AstNode { get; set; }

    // ========== Source type support (for method calls, indexers, property access) ==========

    /// <summary>
    /// For member access, method call, or indexer - the type of the source object.
    /// e.g., for "result.get()" on Dictionary, this would be "Dictionary".
    /// For "array[i]" on Array, this would be "Array".
    /// </summary>
    public string? SourceType { get; set; }

    /// <summary>
    /// For member access, method call, or indexer - the name of the source object.
    /// e.g., for "result.get()", this would be "result".
    /// </summary>
    public string? SourceObjectName { get; set; }

    // ========== Lazy loading support ==========

    /// <summary>
    /// IDs of inflow nodes (for lazy loading).
    /// Populated during graph building; actual nodes loaded on demand.
    /// </summary>
    public List<string> InflowNodeIds { get; set; } = new();

    /// <summary>
    /// IDs of outflow nodes (for lazy loading).
    /// </summary>
    public List<string> OutflowNodeIds { get; set; } = new();

    /// <summary>
    /// Whether the inflows have been fully loaded.
    /// </summary>
    public bool AreInflowsLoaded { get; set; }

    /// <summary>
    /// Whether the outflows have been fully loaded.
    /// </summary>
    public bool AreOutflowsLoaded { get; set; }

    // ========== Layout positioning (computed by layout engine, UI-agnostic) ==========

    /// <summary>
    /// X position of this node on the canvas (computed by layout engine).
    /// </summary>
    public float PositionX { get; set; }

    /// <summary>
    /// Y position of this node on the canvas (computed by layout engine).
    /// </summary>
    public float PositionY { get; set; }

    /// <summary>
    /// Width of this node's visual block (computed by layout engine).
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Height of this node's visual block (computed by layout engine).
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Level in the graph hierarchy.
    /// 0 = focus node, negative = inflows, positive = outflows.
    /// </summary>
    public int Level { get; set; }

    // ========== Union type support ==========

    /// <summary>
    /// Whether this node represents a union type (multiple possible types).
    /// </summary>
    public bool IsUnionType { get; set; }

    /// <summary>
    /// Union type information (if IsUnionType is true).
    /// </summary>
    public GDUnionType? UnionTypeInfo { get; set; }

    /// <summary>
    /// Source nodes for each type in the union (traced back to their origin).
    /// </summary>
    public List<GDTypeFlowNode> UnionSources { get; set; } = new();

    // ========== Duck type support ==========

    /// <summary>
    /// Whether this node has duck typing constraints.
    /// </summary>
    public bool HasDuckConstraints { get; set; }

    /// <summary>
    /// Duck type information (constraints from usage).
    /// </summary>
    public GDDuckType? DuckTypeInfo { get; set; }

    // ========== Edge references (computed by layout engine) ==========

    /// <summary>
    /// Edges coming into this node.
    /// </summary>
    public List<GDTypeFlowEdge> IncomingEdges { get; set; } = new();

    /// <summary>
    /// Edges going out from this node.
    /// </summary>
    public List<GDTypeFlowEdge> OutgoingEdges { get; set; } = new();

    /// <summary>
    /// Creates a simple node with minimal information.
    /// </summary>
    public static GDTypeFlowNode Create(string id, string label, string type, GDTypeFlowNodeKind kind)
    {
        return new GDTypeFlowNode
        {
            Id = id,
            Label = label,
            Type = type,
            Kind = kind,
            Confidence = kind == GDTypeFlowNodeKind.TypeAnnotation ? 1.0f : 0.5f
        };
    }

    /// <summary>
    /// Gets the confidence level category.
    /// </summary>
    public GDTypeFlowConfidenceLevel GetConfidenceLevel()
    {
        return Confidence switch
        {
            >= 0.8f => GDTypeFlowConfidenceLevel.High,
            >= 0.5f => GDTypeFlowConfidenceLevel.Medium,
            _ => GDTypeFlowConfidenceLevel.Low
        };
    }

    public override string ToString()
    {
        return $"{Label}: {Type} ({Kind}, {Confidence:P0})";
    }
}

/// <summary>
/// Confidence level categories for UI display.
/// </summary>
public enum GDTypeFlowConfidenceLevel
{
    /// <summary>
    /// High confidence (>= 0.8) - typically explicit annotations.
    /// </summary>
    High,

    /// <summary>
    /// Medium confidence (>= 0.5) - typically inferred from context.
    /// </summary>
    Medium,

    /// <summary>
    /// Low confidence (&lt; 0.5) - uncertain or Variant.
    /// </summary>
    Low
}
