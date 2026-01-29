using GDShrapt.Reader;
using System;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

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
/// Event arguments for incremental script changes.
/// Contains both old and new AST for delta analysis.
/// </summary>
public class GDScriptIncrementalChangeEventArgs : EventArgs
{
    /// <summary>
    /// Full path to the script file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The script file (may be null for deleted files).
    /// </summary>
    public GDScriptFile? Script { get; }

    /// <summary>
    /// The old AST before the change (null for new files).
    /// </summary>
    public GDClassDeclaration? OldTree { get; }

    /// <summary>
    /// The new AST after the change (null for deleted files).
    /// </summary>
    public GDClassDeclaration? NewTree { get; }

    /// <summary>
    /// The kind of change that occurred.
    /// </summary>
    public GDIncrementalChangeKind ChangeKind { get; }

    /// <summary>
    /// Text changes (if available from editor).
    /// Empty for file system events.
    /// </summary>
    public IReadOnlyList<GDTextChange> TextChanges { get; }

    /// <summary>
    /// Old file path (for renames).
    /// </summary>
    public string? OldFilePath { get; }

    public GDScriptIncrementalChangeEventArgs(
        string filePath,
        GDScriptFile? script,
        GDClassDeclaration? oldTree,
        GDClassDeclaration? newTree,
        GDIncrementalChangeKind changeKind,
        IReadOnlyList<GDTextChange>? textChanges = null,
        string? oldFilePath = null)
    {
        FilePath = filePath;
        Script = script;
        OldTree = oldTree;
        NewTree = newTree;
        ChangeKind = changeKind;
        TextChanges = textChanges ?? Array.Empty<GDTextChange>();
        OldFilePath = oldFilePath;
    }
}
