using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Predicted runtime hierarchy for a scene.
/// </summary>
public class GDPredictedHierarchy
{
    public string ScenePath { get; init; } = "";
    public GDPredictedNode? Root { get; init; }
}

/// <summary>
/// A node that may exist at runtime with confidence level.
/// </summary>
public class GDPredictedNode
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public string NodeType { get; init; } = "Node";
    public string? ScriptTypeName { get; init; }
    public GDNodePresencePrediction Presence { get; init; } = GDNodePresencePrediction.AlwaysPresentFromScene;
    public bool IsSubSceneInstance { get; init; }
    public string? SubScenePath { get; init; }
    public List<GDPredictedNode> Children { get; init; } = new();
}
