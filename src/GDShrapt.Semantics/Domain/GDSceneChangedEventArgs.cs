using System;

namespace GDShrapt.Semantics;

/// <summary>
/// Event arguments for scene file change events.
/// </summary>
public class GDSceneChangedEventArgs : EventArgs
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
    /// The scene info after the change.
    /// </summary>
    public GDSceneInfo? SceneInfo { get; }

    public GDSceneChangedEventArgs(string scenePath, string fullPath, GDSceneInfo? sceneInfo = null)
    {
        ScenePath = scenePath;
        FullPath = fullPath;
        SceneInfo = sceneInfo;
    }
}
