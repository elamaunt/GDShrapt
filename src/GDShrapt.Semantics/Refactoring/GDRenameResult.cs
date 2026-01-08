using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Result of a rename operation planning.
/// </summary>
public sealed class GDRenameResult
{
    /// <summary>
    /// Whether the rename can be performed.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// The list of text edits required to perform the rename.
    /// </summary>
    public IReadOnlyList<GDTextEdit> Edits { get; }

    /// <summary>
    /// Conflicts detected during rename planning.
    /// </summary>
    public IReadOnlyList<GDRenameConflict> Conflicts { get; }

    /// <summary>
    /// Error message if the rename cannot be performed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Number of files that would be modified.
    /// </summary>
    public int FileCount { get; }

    private GDRenameResult(
        bool success,
        IReadOnlyList<GDTextEdit> edits,
        IReadOnlyList<GDRenameConflict> conflicts,
        string? errorMessage,
        int fileCount)
    {
        Success = success;
        Edits = edits;
        Conflicts = conflicts;
        ErrorMessage = errorMessage;
        FileCount = fileCount;
    }

    /// <summary>
    /// Creates a successful rename result.
    /// </summary>
    public static GDRenameResult Successful(IReadOnlyList<GDTextEdit> edits, int fileCount)
    {
        return new GDRenameResult(true, edits, System.Array.Empty<GDRenameConflict>(), null, fileCount);
    }

    /// <summary>
    /// Creates a failed rename result with an error message.
    /// </summary>
    public static GDRenameResult Failed(string errorMessage)
    {
        return new GDRenameResult(false, System.Array.Empty<GDTextEdit>(), System.Array.Empty<GDRenameConflict>(), errorMessage, 0);
    }

    /// <summary>
    /// Creates a result with conflicts.
    /// </summary>
    public static GDRenameResult WithConflicts(IReadOnlyList<GDRenameConflict> conflicts)
    {
        return new GDRenameResult(false, System.Array.Empty<GDTextEdit>(), conflicts, "Rename would cause conflicts", 0);
    }

    /// <summary>
    /// Creates a result when no occurrences were found.
    /// </summary>
    public static GDRenameResult NoOccurrences(string symbolName)
    {
        return new GDRenameResult(true, System.Array.Empty<GDTextEdit>(), System.Array.Empty<GDRenameConflict>(), $"No occurrences of '{symbolName}' found", 0);
    }
}
