using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Abstractions;

/// <summary>
/// Interface for Pro incremental analysis in plugin.
/// Implemented by GDShrapt.Pro.Plugin for advanced incremental analysis
/// with dependency tracking and caching.
///
/// Base plugin uses simple per-file analysis; Pro plugin can provide
/// this interface for smarter incremental updates.
/// </summary>
public interface IGDPluginIncrementalAnalyzer
{
    /// <summary>
    /// Analyzes only the changed files and their transitive dependents.
    /// Uses cached results for unchanged files.
    /// </summary>
    /// <param name="changedFiles">Files that have been modified.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with statistics about the analysis.</returns>
    Task<GDPluginIncrementalResult> AnalyzeChangedAsync(
        IEnumerable<string> changedFiles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached results for the specified files.
    /// Call this when files are modified externally.
    /// </summary>
    /// <param name="filePaths">Files to invalidate.</param>
    void Invalidate(IEnumerable<string> filePaths);

    /// <summary>
    /// Whether this analyzer is available and properly initialized.
    /// </summary>
    bool IsAvailable { get; }
}

/// <summary>
/// Result of incremental analysis operation.
/// </summary>
public class GDPluginIncrementalResult
{
    /// <summary>
    /// Whether the analysis completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of files that were analyzed (not cached).
    /// </summary>
    public int FilesAnalyzed { get; set; }

    /// <summary>
    /// Number of files that used cached results.
    /// </summary>
    public int FilesCached { get; set; }

    /// <summary>
    /// Total duration of the analysis.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Error message if analysis failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GDPluginIncrementalResult Succeeded(int filesAnalyzed, int filesCached, TimeSpan duration)
    {
        return new GDPluginIncrementalResult
        {
            Success = true,
            FilesAnalyzed = filesAnalyzed,
            FilesCached = filesCached,
            Duration = duration
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static GDPluginIncrementalResult Failed(string errorMessage)
    {
        return new GDPluginIncrementalResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Interface for handling incremental AST changes in plugin.
/// Pro plugin can implement this for sophisticated delta analysis
/// that uses old/new AST comparison for smarter updates.
///
/// The change kind determines what type of change occurred:
/// - Modified: File content changed, both OldTree and NewTree available
/// - Created: New file, only NewTree available
/// - Deleted: File removed, only OldTree available
/// - Renamed: File path changed, OldFilePath contains previous path
/// </summary>
public interface IGDPluginIncrementalChangeHandler
{
    /// <summary>
    /// Handles an incremental change with AST delta information.
    /// </summary>
    /// <param name="filePath">Path to the changed file.</param>
    /// <param name="changeKind">Type of change (Modified, Created, Deleted, Renamed).</param>
    /// <param name="oldTree">AST before the change (null for new files).</param>
    /// <param name="newTree">AST after the change (null for deleted files).</param>
    /// <param name="textChanges">Text changes if available (from editor).</param>
    /// <param name="oldFilePath">Previous file path for rename operations.</param>
    void HandleIncrementalChange(
        string filePath,
        GDIncrementalChangeKind changeKind,
        object? oldTree,
        object? newTree,
        IReadOnlyList<GDTextChangeInfo>? textChanges = null,
        string? oldFilePath = null);
}

/// <summary>
/// The kind of incremental change that occurred.
/// </summary>
public enum GDIncrementalChangeKind
{
    /// <summary>
    /// File content was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// File was created.
    /// </summary>
    Created,

    /// <summary>
    /// File was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// File was renamed.
    /// </summary>
    Renamed
}

/// <summary>
/// Information about a text change within a file.
/// </summary>
public class GDTextChangeInfo
{
    /// <summary>
    /// Start offset in the old text.
    /// </summary>
    public int StartOffset { get; set; }

    /// <summary>
    /// End offset in the old text (exclusive).
    /// </summary>
    public int EndOffset { get; set; }

    /// <summary>
    /// The new text that replaced the range.
    /// </summary>
    public string NewText { get; set; } = string.Empty;
}
