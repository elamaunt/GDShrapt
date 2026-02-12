using System.Collections.Generic;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for symbol rename operations.
/// Base implementation uses GDRenameService with Strict confidence.
/// Pro implementation adds Potential and NameMatch confidence modes.
/// </summary>
public interface IGDRenameHandler
{
    /// <summary>
    /// Resolves the symbol name at a given file position.
    /// </summary>
    /// <param name="filePath">Full path to the file.</param>
    /// <param name="line">Line number (1-based).</param>
    /// <param name="column">Column number (1-based).</param>
    /// <returns>Symbol name or null if not found.</returns>
    string? ResolveSymbolAtPosition(string filePath, int line, int column);

    /// <summary>
    /// Validates that a name is a valid GDScript identifier.
    /// </summary>
    /// <param name="name">The identifier to validate.</param>
    /// <param name="error">Error message if invalid.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool ValidateIdentifier(string name, out string? error);

    /// <summary>
    /// Plans a rename operation without applying changes.
    /// </summary>
    /// <param name="oldName">Current symbol name.</param>
    /// <param name="newName">New symbol name.</param>
    /// <param name="filePath">Optional file path to limit scope.</param>
    /// <returns>Rename result with planned edits.</returns>
    GDRenameResult Plan(string oldName, string newName, string? filePath = null);

    /// <summary>
    /// Applies edits to a file.
    /// </summary>
    /// <param name="filePath">Path to the file to modify.</param>
    /// <param name="edits">Edits to apply.</param>
    void ApplyEdits(string filePath, IEnumerable<GDTextEdit> edits);

    /// <summary>
    /// Applies a list of edits across multiple files.
    /// Groups edits by file internally.
    /// </summary>
    /// <param name="edits">Edits to apply.</param>
    /// <returns>Number of modified files.</returns>
    int ApplyEdits(IReadOnlyList<GDTextEdit> edits);
}
