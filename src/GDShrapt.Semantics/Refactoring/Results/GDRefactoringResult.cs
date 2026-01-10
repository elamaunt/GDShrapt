using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Base class for refactoring operation results.
/// </summary>
public class GDRefactoringResult
{
    /// <summary>
    /// Whether the refactoring was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Error message if the refactoring failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// The edits to apply (may be empty on failure).
    /// </summary>
    public IReadOnlyList<GDTextEdit> Edits { get; }

    /// <summary>
    /// Edits grouped by file path.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<GDTextEdit>> EditsByFile { get; }

    protected GDRefactoringResult(bool success, string? errorMessage, IReadOnlyList<GDTextEdit>? edits)
    {
        Success = success;
        ErrorMessage = errorMessage;
        Edits = edits ?? Array.Empty<GDTextEdit>();
        EditsByFile = GroupEditsByFile(Edits);
    }

    /// <summary>
    /// Creates a successful result with the given edits.
    /// </summary>
    public static GDRefactoringResult Succeeded(IReadOnlyList<GDTextEdit> edits) =>
        new(true, null, edits);

    /// <summary>
    /// Creates a successful result with a single edit.
    /// </summary>
    public static GDRefactoringResult Succeeded(GDTextEdit edit) =>
        new(true, null, new[] { edit });

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static GDRefactoringResult Failed(string errorMessage) =>
        new(false, errorMessage, null);

    /// <summary>
    /// Creates an empty successful result (no edits needed).
    /// </summary>
    public static GDRefactoringResult Empty =>
        new(true, null, Array.Empty<GDTextEdit>());

    private static IReadOnlyDictionary<string, IReadOnlyList<GDTextEdit>> GroupEditsByFile(IReadOnlyList<GDTextEdit> edits)
    {
        return edits
            .GroupBy(e => e.FilePath)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<GDTextEdit>)g.ToList());
    }

    /// <summary>
    /// Gets the number of files affected by this refactoring.
    /// </summary>
    public int AffectedFilesCount => EditsByFile.Count;

    /// <summary>
    /// Gets the total number of edits.
    /// </summary>
    public int TotalEditsCount => Edits.Count;

    public override string ToString()
    {
        if (!Success)
            return $"Failed: {ErrorMessage}";
        return $"Success: {TotalEditsCount} edit(s) in {AffectedFilesCount} file(s)";
    }
}
