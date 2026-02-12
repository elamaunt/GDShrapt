using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

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
    /// Strict edits - confirmed type-matched references.
    /// </summary>
    public IReadOnlyList<GDTextEdit> StrictEdits { get; }

    /// <summary>
    /// Potential edits - may be references but type unknown.
    /// </summary>
    public IReadOnlyList<GDTextEdit> PotentialEdits { get; }

    /// <summary>
    /// The list of all text edits required to perform the rename.
    /// Combines StrictEdits and PotentialEdits for backward compatibility.
    /// </summary>
    public IReadOnlyList<GDTextEdit> Edits { get; }

    /// <summary>
    /// Conflicts detected during rename planning.
    /// </summary>
    public IReadOnlyList<GDRenameConflict> Conflicts { get; }

    /// <summary>
    /// Warnings about references that could not be auto-edited (e.g. concatenated strings).
    /// </summary>
    public IReadOnlyList<GDRenameWarning> Warnings { get; }

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
        IReadOnlyList<GDTextEdit> strictEdits,
        IReadOnlyList<GDTextEdit> potentialEdits,
        IReadOnlyList<GDRenameConflict> conflicts,
        string? errorMessage,
        int fileCount,
        IReadOnlyList<GDRenameWarning>? warnings = null)
    {
        Success = success;
        StrictEdits = strictEdits;
        PotentialEdits = potentialEdits;
        Edits = strictEdits.Concat(potentialEdits).ToList();
        Conflicts = conflicts;
        Warnings = warnings ?? System.Array.Empty<GDRenameWarning>();
        ErrorMessage = errorMessage;
        FileCount = fileCount;
    }

    /// <summary>
    /// Creates a successful rename result (backward compatible - all edits as Strict).
    /// </summary>
    public static GDRenameResult Successful(IReadOnlyList<GDTextEdit> edits, int fileCount)
    {
        return new GDRenameResult(true, edits, System.Array.Empty<GDTextEdit>(), System.Array.Empty<GDRenameConflict>(), null, fileCount);
    }

    /// <summary>
    /// Creates a successful rename result with confidence separation.
    /// </summary>
    public static GDRenameResult SuccessfulWithConfidence(
        IReadOnlyList<GDTextEdit> strictEdits,
        IReadOnlyList<GDTextEdit> potentialEdits,
        int fileCount,
        IReadOnlyList<GDRenameWarning>? warnings = null)
    {
        return new GDRenameResult(true, strictEdits, potentialEdits, System.Array.Empty<GDRenameConflict>(), null, fileCount, warnings);
    }

    /// <summary>
    /// Creates a failed rename result with an error message.
    /// </summary>
    public static GDRenameResult Failed(string errorMessage)
    {
        return new GDRenameResult(false, System.Array.Empty<GDTextEdit>(), System.Array.Empty<GDTextEdit>(), System.Array.Empty<GDRenameConflict>(), errorMessage, 0);
    }

    /// <summary>
    /// Creates a result with conflicts.
    /// </summary>
    public static GDRenameResult WithConflicts(IReadOnlyList<GDRenameConflict> conflicts)
    {
        return new GDRenameResult(false, System.Array.Empty<GDTextEdit>(), System.Array.Empty<GDTextEdit>(), conflicts, "Rename would cause conflicts", 0);
    }

    /// <summary>
    /// Creates a result when no occurrences were found.
    /// </summary>
    public static GDRenameResult NoOccurrences(string symbolName)
    {
        return new GDRenameResult(true, System.Array.Empty<GDTextEdit>(), System.Array.Empty<GDTextEdit>(), System.Array.Empty<GDRenameConflict>(), $"No occurrences of '{symbolName}' found", 0);
    }

    #region Grouping Helpers

    /// <summary>
    /// Gets strict edits grouped by file path.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<GDTextEdit>> GetStrictEditsByFile()
    {
        return GroupEditsByFile(StrictEdits);
    }

    /// <summary>
    /// Gets potential edits grouped by file path.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<GDTextEdit>> GetPotentialEditsByFile()
    {
        return GroupEditsByFile(PotentialEdits);
    }

    /// <summary>
    /// Gets all edits grouped by file path.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<GDTextEdit>> GetEditsByFile()
    {
        return GroupEditsByFile(Edits);
    }

    /// <summary>
    /// Gets file paths that have potential edits.
    /// </summary>
    public IEnumerable<string> GetFilesWithPotentialEdits()
    {
        return PotentialEdits.Select(e => e.FilePath).Distinct();
    }

    /// <summary>
    /// Gets file paths that have strict edits.
    /// </summary>
    public IEnumerable<string> GetFilesWithStrictEdits()
    {
        return StrictEdits.Select(e => e.FilePath).Distinct();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<GDTextEdit>> GroupEditsByFile(IReadOnlyList<GDTextEdit> edits)
    {
        return edits
            .GroupBy(e => e.FilePath)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<GDTextEdit>)g.OrderBy(e => e.Line).ThenBy(e => e.Column).ToList());
    }

    #endregion
}
