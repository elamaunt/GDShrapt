namespace GDShrapt.Semantics;

/// <summary>
/// Represents a detected node rename in a scene file.
/// </summary>
public class GDDetectedNodeRename
{
    /// <summary>
    /// The old name of the node.
    /// </summary>
    public string OldName { get; set; } = string.Empty;

    /// <summary>
    /// The new name of the node.
    /// </summary>
    public string NewName { get; set; } = string.Empty;

    /// <summary>
    /// Line number in the scene file where the node is defined.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Number of GDScript references to the old node name.
    /// </summary>
    public int GDScriptReferenceCount { get; set; }
}
