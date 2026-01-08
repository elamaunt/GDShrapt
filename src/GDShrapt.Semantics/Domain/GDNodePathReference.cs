using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a reference to a node path in either a GDScript or scene file.
/// Used for tracking all occurrences of a node path when renaming.
/// </summary>
public class GDNodePathReference
{
    /// <summary>
    /// Type of reference.
    /// </summary>
    public enum RefType
    {
        /// <summary>
        /// Reference in a GDScript file ($Node, get_node("Node")).
        /// </summary>
        GDScript,

        /// <summary>
        /// Node name definition in a scene file ([node name="Node"]).
        /// </summary>
        SceneNodeName,

        /// <summary>
        /// Parent path reference in a scene file ([node parent="Node"]).
        /// </summary>
        SceneParentPath
    }

    /// <summary>
    /// Type of this reference.
    /// </summary>
    public RefType Type { get; set; }

    /// <summary>
    /// Full path to the file containing this reference.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number (1-based) where this reference occurs.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// The node path being referenced (e.g., "Player" or "Enemy/Spawner").
    /// </summary>
    public string NodePath { get; set; } = string.Empty;

    /// <summary>
    /// Index of the segment being renamed (0-based) for nested paths.
    /// </summary>
    public int SegmentIndex { get; set; }

    /// <summary>
    /// For GDScript references: the GDPathSpecifier token to modify.
    /// </summary>
    public GDPathSpecifier? PathSpecifier { get; set; }

    /// <summary>
    /// Reference to the script file (for GDScript references).
    /// </summary>
    public GDScriptReference? ScriptReference { get; set; }

    /// <summary>
    /// Display name for the file (just the filename).
    /// </summary>
    public string DisplayName => System.IO.Path.GetFileName(FilePath);

    /// <summary>
    /// Context string showing the line content for display in dialogs.
    /// </summary>
    public string? DisplayContext { get; set; }

    /// <summary>
    /// Resource path for the file (res://...).
    /// </summary>
    public string? ResourcePath { get; set; }

    public override string ToString()
    {
        return $"{Type}: {NodePath} in {DisplayName}:{LineNumber}";
    }
}
