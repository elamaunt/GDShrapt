namespace GDShrapt.Semantics;

/// <summary>
/// Information about a node created at runtime via code.
/// </summary>
public class GDRuntimeNodeInfo
{
    /// <summary>
    /// Node path where add_child is called (the parent).
    /// </summary>
    public string? ParentNodePath { get; init; }

    /// <summary>
    /// Base node type (e.g., Node2D).
    /// </summary>
    public string? NodeType { get; init; }

    /// <summary>
    /// Scene resource path if created from scene instantiation.
    /// </summary>
    public string? ScenePath { get; init; }

    /// <summary>
    /// Script resource path if set_script() was detected.
    /// </summary>
    public string? ScriptPath { get; init; }

    /// <summary>
    /// Resolved script type name (class_name or inferred).
    /// </summary>
    public string? ScriptTypeName { get; init; }

    public GDTypeConfidence Confidence { get; init; }

    /// <summary>
    /// Source .gd file where this runtime node is created.
    /// </summary>
    public string SourceFile { get; init; } = "";

    public int LineNumber { get; init; }

    /// <summary>
    /// Whether the creation is inside a conditional (if/match/for).
    /// </summary>
    public bool IsConditional { get; init; }
}
