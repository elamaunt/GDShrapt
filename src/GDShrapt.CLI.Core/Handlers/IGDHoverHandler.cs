using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for hover information (symbol info + documentation).
/// </summary>
public interface IGDHoverHandler
{
    /// <summary>
    /// Gets hover information for a symbol at the given position.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="line">Line number (1-based).</param>
    /// <param name="column">Column number (1-based).</param>
    /// <returns>Hover information or null if not found.</returns>
    GDHoverInfo? GetHover(string filePath, int line, int column);
}

/// <summary>
/// Represents hover information for a symbol.
/// </summary>
public class GDHoverInfo
{
    /// <summary>
    /// Markdown-formatted content to display.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Kind of the symbol (Variable, Method, etc.).
    /// </summary>
    public GDSymbolKind? Kind { get; init; }

    /// <summary>
    /// Name of the symbol.
    /// </summary>
    public string? SymbolName { get; init; }

    /// <summary>
    /// Type of the symbol.
    /// </summary>
    public string? TypeName { get; init; }

    /// <summary>
    /// Documentation extracted from doc comments (##).
    /// </summary>
    public string? Documentation { get; init; }

    /// <summary>
    /// Start line of the symbol range (1-based).
    /// </summary>
    public int? StartLine { get; init; }

    /// <summary>
    /// Start column of the symbol range (1-based).
    /// </summary>
    public int? StartColumn { get; init; }

    /// <summary>
    /// End line of the symbol range (1-based).
    /// </summary>
    public int? EndLine { get; init; }

    /// <summary>
    /// End column of the symbol range (1-based).
    /// </summary>
    public int? EndColumn { get; init; }
}
