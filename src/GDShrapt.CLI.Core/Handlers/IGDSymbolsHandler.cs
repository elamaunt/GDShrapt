using System.Collections.Generic;
using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for extracting document symbols.
/// </summary>
public interface IGDSymbolsHandler
{
    /// <summary>
    /// Gets all symbols defined in a file.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>List of symbols in the file.</returns>
    IReadOnlyList<GDDocumentSymbol> GetSymbols(string filePath);
}

/// <summary>
/// Represents a symbol in a document.
/// </summary>
public class GDDocumentSymbol
{
    /// <summary>
    /// Symbol name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Symbol kind (class, method, variable, signal, constant, enum, etc.)
    /// </summary>
    public required GDSymbolKind Kind { get; init; }

    /// <summary>
    /// Type annotation if available.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Column number (1-based).
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Child symbols (e.g., methods inside a class).
    /// </summary>
    public IReadOnlyList<GDDocumentSymbol>? Children { get; init; }
}
