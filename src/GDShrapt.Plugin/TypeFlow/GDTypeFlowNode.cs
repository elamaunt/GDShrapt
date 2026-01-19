using GDShrapt.Abstractions;

namespace GDShrapt.Plugin;

/// <summary>
/// Represents a node in the type inference flow graph.
/// Each node represents a symbol, expression, or type source that contributes to type inference.
/// </summary>
internal class GDTypeFlowNode
{
    /// <summary>
    /// Unique identifier for this node within the graph.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Display label for the node (e.g., "event", "is_action_pressed()").
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// The inferred or declared type (e.g., "InputEvent", "bool", "Variant").
    /// </summary>
    public string Type { get; set; }

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
    public string Description { get; set; }

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
    public GDSourceLocation Location { get; set; }

    /// <summary>
    /// Reference to the script file containing this node.
    /// </summary>
    public GDScriptFile SourceScript { get; set; }

    /// <summary>
    /// Reference to the AST node (if available).
    /// </summary>
    public GDShrapt.Reader.GDNode AstNode { get; set; }

    // ========== Source type support (for method calls, indexers, property access) ==========

    /// <summary>
    /// For member access, method call, or indexer - the type of the source object.
    /// e.g., for "result.get()" on Dictionary, this would be "Dictionary".
    /// For "array[i]" on Array, this would be "Array".
    /// </summary>
    public string SourceType { get; set; }

    /// <summary>
    /// For member access, method call, or indexer - the name of the source object.
    /// e.g., for "result.get()", this would be "result".
    /// </summary>
    public string SourceObjectName { get; set; }

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

    // ========== Canvas positioning (computed by layout engine) ==========

    /// <summary>
    /// Position of this node on the canvas (computed by layout engine).
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Size of this node's visual block (computed by layout engine).
    /// </summary>
    public Vector2 Size { get; set; }

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
    public GDUnionType UnionTypeInfo { get; set; }

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
    public GDDuckType DuckTypeInfo { get; set; }

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
    /// Gets the color associated with this node's confidence level.
    /// </summary>
    public Color GetConfidenceColor()
    {
        return Confidence switch
        {
            >= 0.8f => new Color(0.3f, 0.8f, 0.3f),   // Green - High
            >= 0.5f => new Color(1.0f, 0.7f, 0.3f),   // Orange - Medium
            _ => new Color(0.8f, 0.3f, 0.3f)          // Red - Low
        };
    }

    /// <summary>
    /// Gets the icon name for this node kind.
    /// </summary>
    public string GetIconName()
    {
        return Kind switch
        {
            GDTypeFlowNodeKind.Parameter => "MemberSignal",
            GDTypeFlowNodeKind.LocalVariable => "MemberProperty",
            GDTypeFlowNodeKind.MemberVariable => "MemberProperty",
            GDTypeFlowNodeKind.MethodCall => "MemberMethod",
            GDTypeFlowNodeKind.ReturnValue => "ArrowRight",
            GDTypeFlowNodeKind.Assignment => "Edit",
            GDTypeFlowNodeKind.TypeAnnotation => "ClassList",
            GDTypeFlowNodeKind.InheritedMember => "Node",
            GDTypeFlowNodeKind.BuiltinType => "Object",
            GDTypeFlowNodeKind.Literal => "String",
            GDTypeFlowNodeKind.IndexerAccess => "ArrayMesh",
            GDTypeFlowNodeKind.PropertyAccess => "MemberProperty",
            GDTypeFlowNodeKind.TypeCheck => "ClassList",
            GDTypeFlowNodeKind.NullCheck => "GuiRadioUnchecked",
            GDTypeFlowNodeKind.Comparison => "Compare",
            _ => "StatusWarning"
        };
    }

    public override string ToString()
    {
        return $"{Label}: {Type} ({Kind}, {Confidence:P0})";
    }
}

/// <summary>
/// Types of nodes in the type flow graph.
/// </summary>
internal enum GDTypeFlowNodeKind
{
    /// <summary>
    /// A function parameter.
    /// </summary>
    Parameter,

    /// <summary>
    /// A local variable within a function.
    /// </summary>
    LocalVariable,

    /// <summary>
    /// A class member variable (field).
    /// </summary>
    MemberVariable,

    /// <summary>
    /// A method or function call expression.
    /// </summary>
    MethodCall,

    /// <summary>
    /// A return value from a method.
    /// </summary>
    ReturnValue,

    /// <summary>
    /// An assignment expression.
    /// </summary>
    Assignment,

    /// <summary>
    /// An explicit type annotation.
    /// </summary>
    TypeAnnotation,

    /// <summary>
    /// A member inherited from a base class.
    /// </summary>
    InheritedMember,

    /// <summary>
    /// A built-in Godot type.
    /// </summary>
    BuiltinType,

    /// <summary>
    /// A literal value (string, int, etc.).
    /// </summary>
    Literal,

    /// <summary>
    /// An indexer access expression (e.g., result["key"], array[i]).
    /// </summary>
    IndexerAccess,

    /// <summary>
    /// A property access expression (e.g., result.property, not a method call).
    /// </summary>
    PropertyAccess,

    /// <summary>
    /// A type check expression (e.g., x is Dictionary).
    /// </summary>
    TypeCheck,

    /// <summary>
    /// A null check expression (e.g., x == null, x != null).
    /// </summary>
    NullCheck,

    /// <summary>
    /// A comparison expression (e.g., x == y, x != y, x &lt; y) that is not a null check.
    /// </summary>
    Comparison,

    /// <summary>
    /// Unknown or generic source.
    /// </summary>
    Unknown
}
