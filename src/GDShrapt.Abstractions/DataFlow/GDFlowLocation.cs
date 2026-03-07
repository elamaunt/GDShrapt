using System;

namespace GDShrapt.Abstractions;

/// <summary>
/// Value-type for source code position. No AST node reference.
/// </summary>
public readonly struct GDFlowLocation : IEquatable<GDFlowLocation>
{
    public string? FilePath { get; }
    public int Line { get; }
    public int Column { get; }

    public GDFlowLocation(string? filePath, int line, int column)
    {
        FilePath = filePath;
        Line = line;
        Column = column;
    }

    public bool Equals(GDFlowLocation other) =>
        Line == other.Line && Column == other.Column
        && string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is GDFlowLocation other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (FilePath?.ToLowerInvariant().GetHashCode() ?? 0);
            hash = hash * 31 + Line;
            hash = hash * 31 + Column;
            return hash;
        }
    }

    public static bool operator ==(GDFlowLocation a, GDFlowLocation b) => a.Equals(b);
    public static bool operator !=(GDFlowLocation a, GDFlowLocation b) => !a.Equals(b);

    public override string ToString() => $"{FilePath}:{Line + 1}:{Column + 1}";
}
