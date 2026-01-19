using GDShrapt.Semantics;

namespace GDShrapt.Plugin;

/// <summary>
/// Represents a single TODO/FIXME tag found in a comment.
/// </summary>
internal class GDTodoItem
{
    /// <summary>
    /// The tag type (e.g., "TODO", "FIXME", "HACK").
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Description text after the tag.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the file containing this item.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Resource path (res://...) for navigation.
    /// </summary>
    public string ResourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Line number (0-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Column number (0-based).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Priority level (derived from tag type or explicit priority markers).
    /// </summary>
    public GDTodoPriority Priority { get; set; } = GDTodoPriority.Normal;

    /// <summary>
    /// The original comment text.
    /// </summary>
    public string RawComment { get; set; } = string.Empty;

    public GDTodoItem() { }

    public GDTodoItem(string tag, string description, string filePath, int line, int column, string rawComment)
    {
        Tag = tag;
        Description = description;
        FilePath = filePath;
        Line = line;
        Column = column;
        RawComment = rawComment;
        Priority = DeterminePriority(tag);
    }

    private static GDTodoPriority DeterminePriority(string tag)
    {
        return tag.ToUpperInvariant() switch
        {
            "FIXME" or "BUG" => GDTodoPriority.High,
            "TODO" or "HACK" => GDTodoPriority.Normal,
            "NOTE" or "XXX" => GDTodoPriority.Low,
            _ => GDTodoPriority.Normal
        };
    }
}
