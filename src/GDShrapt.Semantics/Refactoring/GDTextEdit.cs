namespace GDShrapt.Semantics;

/// <summary>
/// Represents a text edit operation at a specific location in a file.
/// </summary>
public sealed class GDTextEdit
{
    /// <summary>
    /// The file path where the edit should be applied.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The line number (1-based).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// The column number (1-based).
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// The text to be replaced.
    /// </summary>
    public string OldText { get; }

    /// <summary>
    /// The replacement text.
    /// </summary>
    public string NewText { get; }

    public GDTextEdit(string filePath, int line, int column, string oldText, string newText)
    {
        FilePath = filePath;
        Line = line;
        Column = column;
        OldText = oldText;
        NewText = newText;
    }

    public override string ToString() => $"{FilePath}:{Line}:{Column}: {OldText} -> {NewText}";
}
