using System.Collections.Generic;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for finding symbol references.
/// </summary>
public interface IGDFindRefsHandler
{
    /// <summary>
    /// Finds all references to a symbol across the project.
    /// </summary>
    /// <param name="symbolName">Name of the symbol to find.</param>
    /// <param name="filePath">Optional file path to limit scope.</param>
    /// <returns>List of references found.</returns>
    IReadOnlyList<GDReferenceLocation> FindReferences(string symbolName, string? filePath = null);
}

/// <summary>
/// Represents a reference location in the codebase.
/// </summary>
public class GDReferenceLocation
{
    /// <summary>
    /// Full path to the file containing the reference.
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
    /// Whether this is the declaration of the symbol.
    /// </summary>
    public bool IsDeclaration { get; init; }

    /// <summary>
    /// Whether this is a write reference (assignment).
    /// </summary>
    public bool IsWrite { get; init; }

    /// <summary>
    /// Optional context text around the reference.
    /// </summary>
    public string? Context { get; init; }
}
