namespace GDShrapt.Semantics;

/// <summary>
/// Options for SceneFlow analysis queries.
/// </summary>
public class GDSceneFlowOptions
{
    public GDTypeConfidence MinimumConfidence { get; init; } = GDTypeConfidence.Low;
    public bool IncludeRuntimeNodes { get; init; } = true;
    public bool ExpandSubScenes { get; init; } = true;
    public int MaxSubSceneDepth { get; init; } = 10;
}
