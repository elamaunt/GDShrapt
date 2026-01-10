namespace GDShrapt.Semantics;

/// <summary>
/// Represents a cursor position in a source file (0-based line and column).
/// </summary>
/// <param name="Line">0-based line number</param>
/// <param name="Column">0-based column number</param>
public readonly record struct GDCursorPosition(int Line, int Column)
{
    /// <summary>
    /// Creates a position at the start of the file.
    /// </summary>
    public static GDCursorPosition Zero => new(0, 0);

    /// <summary>
    /// Checks if this position is before another position.
    /// </summary>
    public bool IsBefore(GDCursorPosition other)
    {
        if (Line < other.Line) return true;
        if (Line > other.Line) return false;
        return Column < other.Column;
    }

    /// <summary>
    /// Checks if this position is after another position.
    /// </summary>
    public bool IsAfter(GDCursorPosition other)
    {
        if (Line > other.Line) return true;
        if (Line < other.Line) return false;
        return Column > other.Column;
    }

    /// <summary>
    /// Checks if this position is at or before another position.
    /// </summary>
    public bool IsAtOrBefore(GDCursorPosition other) => !IsAfter(other);

    /// <summary>
    /// Checks if this position is at or after another position.
    /// </summary>
    public bool IsAtOrAfter(GDCursorPosition other) => !IsBefore(other);

    public override string ToString() => $"L{Line}:C{Column}";
}
