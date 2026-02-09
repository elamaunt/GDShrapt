namespace GDShrapt.Semantics;

public enum GDSceneFlowEdgeType
{
    /// <summary>
    /// Static sub-scene in .tscn: [node instance=ExtResource(...)].
    /// </summary>
    StaticSubScene,

    /// <summary>
    /// preload("scene.tscn").instantiate() in code.
    /// </summary>
    PreloadInstantiate,

    /// <summary>
    /// load("scene.tscn").instantiate() in code.
    /// </summary>
    LoadInstantiate,

    /// <summary>
    /// load(variable).instantiate() — dynamic path.
    /// </summary>
    DynamicLoad,

    /// <summary>
    /// node.set_script(preload("script.gd")) — runtime script attachment.
    /// </summary>
    SetScript
}

/// <summary>
/// Connection between scenes in the SceneFlow graph.
/// </summary>
public class GDSceneFlowEdge
{
    /// <summary>
    /// Scene or script that references another scene.
    /// </summary>
    public string SourceScene { get; init; } = "";

    /// <summary>
    /// Referenced scene or script resource path.
    /// </summary>
    public string TargetScene { get; init; } = "";

    public GDSceneFlowEdgeType EdgeType { get; init; }

    public GDTypeConfidence Confidence { get; init; }

    /// <summary>
    /// Source .gd file for code-based edges.
    /// </summary>
    public string? SourceFile { get; init; }

    public int? LineNumber { get; init; }

    /// <summary>
    /// Node path within the parent scene (for sub-scene instances).
    /// </summary>
    public string? NodePathInParent { get; init; }
}
