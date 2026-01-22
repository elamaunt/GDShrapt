using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

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

    /// <summary>
    /// Finds a symbol by name in a file.
    /// </summary>
    /// <param name="symbolName">The name of the symbol to find.</param>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>Symbol information if found, null otherwise.</returns>
    Semantics.GDSymbolInfo? FindSymbolByName(string symbolName, string filePath);

    /// <summary>
    /// Gets all symbols of a specific kind in a file.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="kind">The kind of symbols to retrieve.</param>
    /// <returns>List of symbols matching the specified kind.</returns>
    IReadOnlyList<Semantics.GDSymbolInfo> GetSymbolsOfKind(string filePath, GDSymbolKind kind);

    /// <summary>
    /// Gets all references to a symbol within a file.
    /// </summary>
    /// <param name="symbol">The symbol to find references for.</param>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>List of references to the symbol.</returns>
    IReadOnlyList<GDReference> GetReferencesToSymbol(Semantics.GDSymbolInfo symbol, string filePath);

    /// <summary>
    /// Gets the type of an AST node.
    /// </summary>
    /// <param name="node">The AST node.</param>
    /// <param name="filePath">Path to the file containing the node.</param>
    /// <returns>The type name if resolved, null otherwise.</returns>
    string? GetTypeForNode(Reader.GDNode node, string filePath);
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
