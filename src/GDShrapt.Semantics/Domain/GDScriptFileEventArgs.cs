using System;

namespace GDShrapt.Semantics;

/// <summary>
/// Event arguments for script file events (changed, created, deleted).
/// </summary>
public class GDScriptFileEventArgs : EventArgs
{
    /// <summary>
    /// Full path to the script file.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// The script file (may be null for deleted files).
    /// </summary>
    public GDScriptFile? Script { get; }

    public GDScriptFileEventArgs(string fullPath, GDScriptFile? script = null)
    {
        FullPath = fullPath;
        Script = script;
    }
}
