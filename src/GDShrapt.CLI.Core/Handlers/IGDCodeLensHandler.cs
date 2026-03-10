using System.Collections.Generic;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for CodeLens operations.
/// Provides reference counts above type members.
/// </summary>
public interface IGDCodeLensHandler
{
    /// <summary>
    /// Gets code lenses for a file.
    /// </summary>
    /// <param name="filePath">Path to the GDScript file.</param>
    /// <returns>List of code lenses, or empty list if none available.</returns>
    IReadOnlyList<GDCodeLens> GetCodeLenses(string filePath);

    /// <summary>
    /// Returns cached reference locations for a symbol from the last CodeLens computation.
    /// Returns null if no cached data is available.
    /// </summary>
    IReadOnlyList<GDCodeLensReference>? GetCachedReferences(string symbolName, string filePath);
}

/// <summary>
/// Represents a CodeLens item (reference count above a declaration).
/// </summary>
public class GDCodeLens
{
    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Start column (1-based).
    /// </summary>
    public int StartColumn { get; init; }

    /// <summary>
    /// End column (1-based).
    /// </summary>
    public int EndColumn { get; init; }

    /// <summary>
    /// The display label (e.g., "5 references").
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// The command to execute when clicked.
    /// </summary>
    public string? CommandName { get; init; }

    /// <summary>
    /// The argument for the command (e.g., symbol name).
    /// </summary>
    public string? CommandArgument { get; init; }
}

/// <summary>
/// A cached reference location from CodeLens computation.
/// All coordinates are 1-based.
/// </summary>
public class GDCodeLensReference
{
    public required string FilePath { get; init; }
    public int Line { get; init; }
    public int Column { get; init; }
    public int EndColumn { get; init; }
    public bool IsDeclaration { get; init; }
}
