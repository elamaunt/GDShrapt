using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for go-to-definition navigation.
/// </summary>
public interface IGDGoToDefHandler
{
    /// <summary>
    /// Finds the definition of a symbol at the given position.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="line">Line number (1-based).</param>
    /// <param name="column">Column number (1-based).</param>
    /// <returns>Definition location or null if not found.</returns>
    GDDefinitionLocation? FindDefinition(string filePath, int line, int column);

    /// <summary>
    /// Finds the definition of a symbol by name.
    /// </summary>
    /// <param name="symbolName">Name of the symbol.</param>
    /// <param name="fromFilePath">File to search from (for scope).</param>
    /// <returns>Definition location or null if not found.</returns>
    GDDefinitionLocation? FindDefinitionByName(string symbolName, string? fromFilePath = null);
}

/// <summary>
/// Represents a definition location.
/// </summary>
public class GDDefinitionLocation
{
    /// <summary>
    /// Full path to the file containing the definition.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Column number (1-based).
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Name of the defined symbol.
    /// </summary>
    public string? SymbolName { get; init; }

    /// <summary>
    /// Kind of the symbol.
    /// </summary>
    public GDSymbolKind? Kind { get; init; }
}
