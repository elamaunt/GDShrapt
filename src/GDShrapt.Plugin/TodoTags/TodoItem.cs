namespace GDShrapt.Plugin;

/// <summary>
/// Represents a single TODO/FIXME tag found in a comment.
/// </summary>
internal class TodoItem
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
    public TodoPriority Priority { get; set; } = TodoPriority.Normal;

    /// <summary>
    /// The original comment text.
    /// </summary>
    public string RawComment { get; set; } = string.Empty;

    public TodoItem() { }

    public TodoItem(string tag, string description, string filePath, int line, int column, string rawComment)
    {
        Tag = tag;
        Description = description;
        FilePath = filePath;
        Line = line;
        Column = column;
        RawComment = rawComment;
        Priority = DeterminePriority(tag);
    }

    private static TodoPriority DeterminePriority(string tag)
    {
        return tag.ToUpperInvariant() switch
        {
            "FIXME" or "BUG" => TodoPriority.High,
            "TODO" or "HACK" => TodoPriority.Normal,
            "NOTE" or "XXX" => TodoPriority.Low,
            _ => TodoPriority.Normal
        };
    }
}

/// <summary>
/// Priority level for TODO items.
/// </summary>
internal enum TodoPriority
{
    Low = 0,
    Normal = 1,
    High = 2
}
