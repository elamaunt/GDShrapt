using System;
using System.Collections.Generic;

namespace GDShrapt.Plugin.Api;

/// <summary>
/// Provides access to project-wide GDScript analysis.
/// </summary>
public interface IProjectAnalyzer
{
    /// <summary>
    /// Gets all scripts in the project.
    /// </summary>
    IReadOnlyList<IScriptInfo> Scripts { get; }

    /// <summary>
    /// Gets a script by its resource path (e.g., "res://scripts/player.gd").
    /// </summary>
    IScriptInfo? GetScriptByResourcePath(string resourcePath);

    /// <summary>
    /// Gets a script by its full file system path.
    /// </summary>
    IScriptInfo? GetScriptByFullPath(string fullPath);

    /// <summary>
    /// Gets a script by its class/type name (e.g., "Player").
    /// </summary>
    IScriptInfo? GetScriptByTypeName(string typeName);

    /// <summary>
    /// Event fired when a script is added to the project.
    /// </summary>
    event Action<IScriptInfo>? ScriptAdded;

    /// <summary>
    /// Event fired when a script is removed from the project.
    /// </summary>
    event Action<string>? ScriptRemoved;

    /// <summary>
    /// Event fired when a script is modified.
    /// </summary>
    event Action<IScriptInfo>? ScriptChanged;
}
