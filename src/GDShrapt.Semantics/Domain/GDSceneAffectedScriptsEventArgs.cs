using System;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Event arguments for when scene changes affect scripts that need reanalysis.
/// </summary>
public class GDSceneAffectedScriptsEventArgs : EventArgs
{
    /// <summary>
    /// Resource path to the scene that changed (res://...).
    /// </summary>
    public string ScenePath { get; }

    /// <summary>
    /// Scripts that are affected by the scene change and need reanalysis.
    /// </summary>
    public IReadOnlyList<GDScriptFile> AffectedScripts { get; }

    public GDSceneAffectedScriptsEventArgs(string scenePath, IReadOnlyList<GDScriptFile> affectedScripts)
    {
        ScenePath = scenePath;
        AffectedScripts = affectedScripts;
    }
}
