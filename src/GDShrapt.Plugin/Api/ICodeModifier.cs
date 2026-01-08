using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Plugin.Api;

/// <summary>
/// Provides code modification functionality (rename, extract method, etc.).
/// </summary>
public interface ICodeModifier
{
    /// <summary>
    /// Renames a symbol across the project.
    /// </summary>
    /// <param name="filePath">File containing the symbol to rename.</param>
    /// <param name="line">Line of the symbol (0-based).</param>
    /// <param name="column">Column of the symbol (0-based).</param>
    /// <param name="newName">New name for the symbol.</param>
    /// <param name="options">Rename options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the rename operation.</returns>
    Task<IRenameResult> RenameAsync(
        string filePath,
        int line,
        int column,
        string newName,
        RenameOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts selected code into a new method.
    /// </summary>
    /// <param name="filePath">File containing the code to extract.</param>
    /// <param name="startLine">Start line of selection (0-based).</param>
    /// <param name="startColumn">Start column of selection (0-based).</param>
    /// <param name="endLine">End line of selection (0-based).</param>
    /// <param name="endColumn">End column of selection (0-based).</param>
    /// <param name="methodName">Name for the new method.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the extract operation.</returns>
    Task<IExtractMethodResult> ExtractMethodAsync(
        string filePath,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn,
        string methodName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Previews changes without applying them.
    /// </summary>
    Task<IReadOnlyList<ITextChange>> PreviewRenameAsync(
        string filePath,
        int line,
        int column,
        string newName,
        RenameOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for rename operations.
/// </summary>
public class RenameOptions
{
    /// <summary>
    /// Only rename strongly-typed references.
    /// </summary>
    public bool RenameOnlyStrongTyped { get; set; }

    /// <summary>
    /// Include references in comments and strings.
    /// </summary>
    public bool IncludeCommentsAndStrings { get; set; }

    /// <summary>
    /// Specific references to include (null = all).
    /// </summary>
    public IReadOnlyList<IReferenceInfo>? SelectedReferences { get; set; }
}

/// <summary>
/// Result of a rename operation.
/// </summary>
public interface IRenameResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Number of files modified.
    /// </summary>
    int FilesModified { get; }

    /// <summary>
    /// Number of references renamed.
    /// </summary>
    int ReferencesRenamed { get; }

    /// <summary>
    /// List of changes made.
    /// </summary>
    IReadOnlyList<ITextChange> Changes { get; }
}

/// <summary>
/// Result of an extract method operation.
/// </summary>
public interface IExtractMethodResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// The generated method code.
    /// </summary>
    string? GeneratedMethod { get; }

    /// <summary>
    /// Line where the new method was inserted.
    /// </summary>
    int MethodInsertedAtLine { get; }

    /// <summary>
    /// List of changes made.
    /// </summary>
    IReadOnlyList<ITextChange> Changes { get; }
}

/// <summary>
/// Represents a text change in a file.
/// </summary>
public interface ITextChange
{
    /// <summary>
    /// File path.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Start line (0-based).
    /// </summary>
    int StartLine { get; }

    /// <summary>
    /// Start column (0-based).
    /// </summary>
    int StartColumn { get; }

    /// <summary>
    /// End line (0-based).
    /// </summary>
    int EndLine { get; }

    /// <summary>
    /// End column (0-based).
    /// </summary>
    int EndColumn { get; }

    /// <summary>
    /// Original text being replaced.
    /// </summary>
    string OldText { get; }

    /// <summary>
    /// New text to insert.
    /// </summary>
    string NewText { get; }
}
