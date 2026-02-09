using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a scene in the SceneFlow graph.
/// </summary>
public class GDSceneFlowNode
{
    public string ScenePath { get; init; } = "";

    public GDSceneInfo? SceneInfo { get; init; }

    /// <summary>
    /// Nodes created at runtime via code (instantiate, add_child, etc.).
    /// </summary>
    public List<GDRuntimeNodeInfo> RuntimeNodes { get; } = new();

    /// <summary>
    /// Sub-scene instances from .tscn (instance=ExtResource).
    /// </summary>
    public List<GDSubSceneReference> SubScenes { get; } = new();
}
