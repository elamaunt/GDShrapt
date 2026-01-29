using GDShrapt.Reader;
using System;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Result of an incremental reload operation.
/// </summary>
public class GDIncrementalReloadResult
{
    /// <summary>
    /// The AST before the reload (null for new files).
    /// </summary>
    public GDClassDeclaration? OldTree { get; }

    /// <summary>
    /// The AST after the reload (null if parsing failed).
    /// </summary>
    public GDClassDeclaration? NewTree { get; }

    /// <summary>
    /// The text changes that were applied.
    /// </summary>
    public IReadOnlyList<GDTextChange> Changes { get; }

    /// <summary>
    /// Whether incremental parsing was used (vs full reparse).
    /// </summary>
    public bool WasIncremental { get; }

    /// <summary>
    /// Whether the reload was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Error that occurred during reload (if any).
    /// </summary>
    public Exception? Error { get; }

    public GDIncrementalReloadResult(
        GDClassDeclaration? oldTree,
        GDClassDeclaration? newTree,
        IReadOnlyList<GDTextChange>? changes,
        bool wasIncremental,
        bool success = true,
        Exception? error = null)
    {
        OldTree = oldTree;
        NewTree = newTree;
        Changes = changes ?? Array.Empty<GDTextChange>();
        WasIncremental = wasIncremental;
        Success = success;
        Error = error;
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static GDIncrementalReloadResult Failed(GDClassDeclaration? oldTree, Exception ex)
        => new(oldTree, null, null, false, success: false, error: ex);
}
