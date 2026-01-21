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
}
