using System;

namespace GDShrapt.Semantics;

/// <summary>
/// Event arguments for script file rename events.
/// </summary>
public class GDScriptRenamedEventArgs : EventArgs
{
    /// <summary>
    /// Old full path to the script file.
    /// </summary>
    public string OldFullPath { get; }

    /// <summary>
    /// New full path to the script file.
    /// </summary>
    public string NewFullPath { get; }

    /// <summary>
    /// The script file at the new location.
    /// </summary>
    public GDScriptFile? Script { get; }

    public GDScriptRenamedEventArgs(string oldFullPath, string newFullPath, GDScriptFile? script = null)
    {
        OldFullPath = oldFullPath;
        NewFullPath = newFullPath;
        Script = script;
    }
}
