using System;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Event arguments for detected node renames in a scene file.
/// </summary>
public class GDNodeRenameDetectedEventArgs : EventArgs
{
    /// <summary>
    /// Resource path to the scene (res://...).
    /// </summary>
    public string ScenePath { get; }

    /// <summary>
    /// Full filesystem path to the scene.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// List of detected node renames.
    /// </summary>
    public IReadOnlyList<GDDetectedNodeRename> Renames { get; }

    public GDNodeRenameDetectedEventArgs(string scenePath, string fullPath, IReadOnlyList<GDDetectedNodeRename> renames)
    {
        ScenePath = scenePath;
        FullPath = fullPath;
        Renames = renames;
    }
}
