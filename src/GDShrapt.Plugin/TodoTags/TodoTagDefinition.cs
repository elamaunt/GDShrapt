namespace GDShrapt.Plugin;

/// <summary>
/// Defines a TODO tag type with its display properties.
/// </summary>
internal class TodoTagDefinition
{
    /// <summary>
    /// The tag name (e.g., "TODO").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this tag is enabled for scanning.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Color for display in the dock (hex format like "#FF0000").
    /// </summary>
    public string Color { get; set; } = "#FFFFFF";

    /// <summary>
    /// Default priority for this tag type.
    /// </summary>
    public TodoPriority DefaultPriority { get; set; } = TodoPriority.Normal;

    public TodoTagDefinition() { }

    public TodoTagDefinition(string name, string color, TodoPriority priority = TodoPriority.Normal)
    {
        Name = name;
        Color = color;
        DefaultPriority = priority;
        Enabled = true;
    }
}
