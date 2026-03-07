using System;

namespace GDShrapt.Abstractions;

/// <summary>
/// Lightweight position handle for an AST node. No AST dependency.
/// Consumers see position info only. Semantics uses NodeId for internal lookup.
/// </summary>
public readonly struct GDNodeHandle : IEquatable<GDNodeHandle>
{
    /// <summary>0-based start line.</summary>
    public int StartLine { get; }

    /// <summary>0-based start column.</summary>
    public int StartColumn { get; }

    /// <summary>0-based end line.</summary>
    public int EndLine { get; }

    /// <summary>0-based end column.</summary>
    public int EndColumn { get; }

    /// <summary>File path (may be null for in-memory scripts).</summary>
    public string? FilePath { get; }

    /// <summary>
    /// Opaque identifier for Semantics-internal node lookup.
    /// Zero means "no associated AST node."
    /// </summary>
    public int NodeId { get; }

    /// <summary>True if this handle has no meaningful data (default struct).</summary>
    public bool IsEmpty => NodeId == 0 && StartLine == 0 && StartColumn == 0 && EndLine == 0 && EndColumn == 0;

    /// <summary>Empty handle (no node).</summary>
    public static GDNodeHandle Empty => default;

    public GDNodeHandle(int startLine, int startColumn, int endLine, int endColumn,
                        string? filePath, int nodeId)
    {
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
        FilePath = filePath;
        NodeId = nodeId;
    }

    public GDNodeHandle(int startLine, int startColumn, int endLine, int endColumn, int nodeId)
    {
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
        FilePath = null;
        NodeId = nodeId;
    }

    public bool Equals(GDNodeHandle other) =>
        NodeId == other.NodeId
        && StartLine == other.StartLine
        && StartColumn == other.StartColumn;

    public override bool Equals(object? obj) => obj is GDNodeHandle other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = NodeId * 397;
            hash ^= StartLine * 31;
            hash ^= StartColumn;
            return hash;
        }
    }

    public static bool operator ==(GDNodeHandle a, GDNodeHandle b) => a.Equals(b);
    public static bool operator !=(GDNodeHandle a, GDNodeHandle b) => !a.Equals(b);

    public override string ToString() =>
        IsEmpty ? "(empty)" : $"{FilePath ?? "?"}:{StartLine + 1}:{StartColumn + 1}";
}
