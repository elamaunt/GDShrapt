namespace GDShrapt.Semantics;

/// <summary>
/// Position information for an unreachable code statement.
/// </summary>
public class GDUnreachableCodeInfo
{
    public int Line { get; }
    public int Column { get; }
    public int EndLine { get; }
    public int EndColumn { get; }

    internal GDUnreachableCodeInfo(int line, int column, int endLine, int endColumn)
    {
        Line = line;
        Column = column;
        EndLine = endLine;
        EndColumn = endColumn;
    }
}
