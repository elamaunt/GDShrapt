using System;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Project-level SceneFlow analysis report.
/// </summary>
public class GDSceneFlowReport
{
    public IReadOnlyList<GDSceneFlowEdge> AllEdges { get; init; } = Array.Empty<GDSceneFlowEdge>();
    public IReadOnlyDictionary<string, GDSceneFlowNode> Scenes { get; init; } = new Dictionary<string, GDSceneFlowNode>();
    public IReadOnlyList<GDSceneFlowWarning> Warnings { get; init; } = Array.Empty<GDSceneFlowWarning>();
    public int TotalScenes { get; init; }
    public int TotalEdges { get; init; }
    public int StaticSubSceneCount { get; init; }
    public int CodeInstantiationCount { get; init; }
}

/// <summary>
/// Warning produced during SceneFlow analysis (cycles, missing scenes, etc.).
/// </summary>
public class GDSceneFlowWarning
{
    public string Message { get; init; } = "";
    public string? ScenePath { get; init; }
    public string? SourceFile { get; init; }
    public int? LineNumber { get; init; }
}
