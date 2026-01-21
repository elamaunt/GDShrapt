using System.Collections.Generic;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for inlay hint operations.
/// Provides type hints for variables without explicit type annotations.
/// </summary>
public interface IGDInlayHintHandler
{
    /// <summary>
    /// Gets inlay hints for a range of lines in a file.
    /// Positions are 1-based.
    /// </summary>
    /// <param name="filePath">Path to the GDScript file.</param>
    /// <param name="startLine">Start line (1-based).</param>
    /// <param name="endLine">End line (1-based).</param>
    /// <returns>List of inlay hints, or empty list if no hints available.</returns>
    IReadOnlyList<GDInlayHint> GetInlayHints(string filePath, int startLine, int endLine);
}

/// <summary>
/// Represents an inlay hint (inline type annotation suggestion).
/// </summary>
public class GDInlayHint
{
    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Column number (1-based).
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// The label text to display (e.g., ": int").
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Kind of inlay hint.
    /// </summary>
    public GDInlayHintKind Kind { get; init; }

    /// <summary>
    /// Optional tooltip text.
    /// </summary>
    public string? Tooltip { get; init; }

    /// <summary>
    /// Whether to add padding on the left side.
    /// </summary>
    public bool PaddingLeft { get; init; }

    /// <summary>
    /// Whether to add padding on the right side.
    /// </summary>
    public bool PaddingRight { get; init; }
}

/// <summary>
/// Kind of inlay hint.
/// </summary>
public enum GDInlayHintKind
{
    /// <summary>
    /// Type annotation hint.
    /// </summary>
    Type = 1,

    /// <summary>
    /// Parameter name hint.
    /// </summary>
    Parameter = 2
}
