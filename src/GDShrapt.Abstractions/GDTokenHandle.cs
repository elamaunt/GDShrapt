using System;

namespace GDShrapt.Abstractions;

/// <summary>
/// Lightweight position handle for a syntax token. No AST dependency.
/// Consumers see position + text. Semantics uses TokenId for internal lookup.
/// </summary>
public readonly struct GDTokenHandle : IEquatable<GDTokenHandle>
{
    /// <summary>0-based start line.</summary>
    public int StartLine { get; }

    /// <summary>0-based start column.</summary>
    public int StartColumn { get; }

    /// <summary>0-based end line.</summary>
    public int EndLine { get; }

    /// <summary>0-based end column.</summary>
    public int EndColumn { get; }

    /// <summary>Text content of the token (e.g., identifier name).</summary>
    public string? Text { get; }

    /// <summary>
    /// Opaque identifier for Semantics-internal token lookup.
    /// Zero means "no associated token."
    /// </summary>
    public int TokenId { get; }

    /// <summary>True if this handle has no associated token.</summary>
    public bool IsEmpty => TokenId == 0;

    /// <summary>Empty handle (no token).</summary>
    public static GDTokenHandle Empty => default;

    public GDTokenHandle(int startLine, int startColumn, int endLine, int endColumn,
                         string? text, int tokenId)
    {
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
        Text = text;
        TokenId = tokenId;
    }

    public bool Equals(GDTokenHandle other) =>
        TokenId == other.TokenId
        && StartLine == other.StartLine
        && StartColumn == other.StartColumn;

    public override bool Equals(object? obj) => obj is GDTokenHandle other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = TokenId * 397;
            hash ^= StartLine * 31;
            hash ^= StartColumn;
            return hash;
        }
    }

    public static bool operator ==(GDTokenHandle a, GDTokenHandle b) => a.Equals(b);
    public static bool operator !=(GDTokenHandle a, GDTokenHandle b) => !a.Equals(b);

    public override string ToString() =>
        IsEmpty ? "(empty)" : $"'{Text ?? "?"}' at {StartLine + 1}:{StartColumn + 1}";
}
