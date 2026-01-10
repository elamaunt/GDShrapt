namespace GDShrapt.Semantics;

/// <summary>
/// Represents a text selection range in a source file (0-based positions).
/// </summary>
public sealed class GDSelectionInfo
{
    /// <summary>
    /// Start position of the selection.
    /// </summary>
    public GDCursorPosition Start { get; }

    /// <summary>
    /// End position of the selection.
    /// </summary>
    public GDCursorPosition End { get; }

    /// <summary>
    /// The selected text content.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Whether there is an actual selection (non-empty text).
    /// </summary>
    public bool HasSelection => !string.IsNullOrEmpty(Text);

    /// <summary>
    /// Whether the selection spans multiple lines.
    /// </summary>
    public bool IsMultiLine => Start.Line != End.Line;

    /// <summary>
    /// Start line (0-based).
    /// </summary>
    public int StartLine => Start.Line;

    /// <summary>
    /// Start column (0-based).
    /// </summary>
    public int StartColumn => Start.Column;

    /// <summary>
    /// End line (0-based).
    /// </summary>
    public int EndLine => End.Line;

    /// <summary>
    /// End column (0-based).
    /// </summary>
    public int EndColumn => End.Column;

    public GDSelectionInfo(int startLine, int startColumn, int endLine, int endColumn, string text)
    {
        Start = new GDCursorPosition(startLine, startColumn);
        End = new GDCursorPosition(endLine, endColumn);
        Text = text ?? string.Empty;
    }

    public GDSelectionInfo(GDCursorPosition start, GDCursorPosition end, string text)
    {
        Start = start;
        End = end;
        Text = text ?? string.Empty;
    }

    /// <summary>
    /// Creates an empty selection at the given position.
    /// </summary>
    public static GDSelectionInfo Empty(GDCursorPosition position) =>
        new(position, position, string.Empty);

    /// <summary>
    /// Creates an empty selection at line 0, column 0.
    /// </summary>
    public static GDSelectionInfo None => Empty(GDCursorPosition.Zero);

    /// <summary>
    /// Checks if a position is within this selection.
    /// </summary>
    public bool Contains(GDCursorPosition position)
    {
        return position.IsAtOrAfter(Start) && position.IsAtOrBefore(End);
    }

    /// <summary>
    /// Checks if a position is within this selection (by line and column).
    /// </summary>
    public bool Contains(int line, int column) => Contains(new GDCursorPosition(line, column));

    public override string ToString() => HasSelection
        ? $"[{Start}-{End}]: \"{(Text.Length > 20 ? Text[..20] + "..." : Text)}\""
        : $"[{Start}] (no selection)";
}
