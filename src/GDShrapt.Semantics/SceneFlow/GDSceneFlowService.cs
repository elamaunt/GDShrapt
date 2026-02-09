using GDShrapt.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GDShrapt.Semantics;

/// <summary>
/// Project-wide SceneFlow analysis service.
/// Exposed via GDProjectSemanticModel.SceneFlow.
/// </summary>
public class GDSceneFlowService
{
    private readonly GDProjectSemanticModel _projectModel;
    private readonly GDScriptProject _project;
    private readonly Lazy<GDSceneFlowGraph> _graph;

    internal GDSceneFlowService(GDProjectSemanticModel projectModel)
    {
        _projectModel = projectModel;
        _project = projectModel.Project;
        _graph = new Lazy<GDSceneFlowGraph>(BuildGraph, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private GDSceneFlowGraph BuildGraph()
    {
        var builder = new GDSceneFlowBuilder(_project, _project.SceneTypesProvider);
        return builder.Build();
    }

    /// <summary>
    /// Full scene flow report for the project.
    /// </summary>
    public GDSceneFlowReport AnalyzeProject(GDSceneFlowOptions? options = null)
    {
        var graph = _graph.Value;
        var allEdges = graph.AllEdges;
        var warnings = DetectWarnings(graph);

        return new GDSceneFlowReport
        {
            AllEdges = allEdges,
            Scenes = graph.AllScenes.ToDictionary(s => s.ScenePath),
            Warnings = warnings,
            TotalScenes = graph.SceneCount,
            TotalEdges = graph.EdgeCount,
            StaticSubSceneCount = allEdges.Count(e => e.EdgeType == GDSceneFlowEdgeType.StaticSubScene),
            CodeInstantiationCount = allEdges.Count(e => e.EdgeType is GDSceneFlowEdgeType.PreloadInstantiate or GDSceneFlowEdgeType.LoadInstantiate)
        };
    }

    /// <summary>
    /// Predicted runtime hierarchy for a scene with confidence.
    /// </summary>
    public GDPredictedHierarchy PredictHierarchy(string scenePath, GDTypeConfidence minConfidence = GDTypeConfidence.Low)
    {
        var predictor = new GDSceneHierarchyPredictor(_graph.Value, _project.SceneTypesProvider,
            new GDSceneFlowOptions { MinimumConfidence = minConfidence });
        return predictor.Predict(scenePath);
    }

    /// <summary>
    /// Check if a node path is valid in a scene.
    /// </summary>
    public GDNodePresencePrediction CheckNodePath(string scenePath, string nodePath)
    {
        var predictor = new GDSceneHierarchyPredictor(_graph.Value, _project.SceneTypesProvider);
        return predictor.CheckNodePath(scenePath, nodePath);
    }

    /// <summary>
    /// Get possible children of a node in a scene.
    /// </summary>
    public IReadOnlyList<GDPredictedNode> GetPossibleChildren(string scenePath, string parentNodePath)
    {
        var predictor = new GDSceneHierarchyPredictor(_graph.Value, _project.SceneTypesProvider);
        return predictor.GetPossibleChildren(scenePath, parentNodePath);
    }

    /// <summary>
    /// Which scenes contain this scene as a sub-scene or instantiate it?
    /// </summary>
    public IReadOnlyList<GDSceneFlowEdge> GetScenesThatInstantiate(string scenePath)
    {
        return _graph.Value.GetIncomingEdges(scenePath);
    }

    /// <summary>
    /// Which scenes does this scene reference (sub-scenes + code instantiation)?
    /// </summary>
    public IReadOnlyList<GDSceneFlowEdge> GetInstantiatedScenes(string scenePath)
    {
        return _graph.Value.GetOutgoingEdges(scenePath);
    }

    /// <summary>
    /// Full scene composition graph.
    /// </summary>
    public GDSceneFlowGraph GetGraph() => _graph.Value;

    private List<GDSceneFlowWarning> DetectWarnings(GDSceneFlowGraph graph)
    {
        var warnings = new List<GDSceneFlowWarning>();

        // Detect cycles
        var visited = new HashSet<string>();
        var stack = new HashSet<string>();

        foreach (var scenePath in graph.AllScenePaths)
        {
            DetectCycles(graph, scenePath, visited, stack, warnings);
        }

        // Detect missing target scenes
        foreach (var edge in graph.AllEdges)
        {
            if (graph.GetScene(edge.TargetScene) == null &&
                edge.EdgeType == GDSceneFlowEdgeType.StaticSubScene)
            {
                warnings.Add(new GDSceneFlowWarning
                {
                    Message = $"Sub-scene '{edge.TargetScene}' referenced but not loaded",
                    ScenePath = edge.SourceScene,
                    LineNumber = edge.LineNumber
                });
            }
        }

        return warnings;
    }

    private void DetectCycles(GDSceneFlowGraph graph, string scenePath, HashSet<string> visited, HashSet<string> stack, List<GDSceneFlowWarning> warnings)
    {
        if (stack.Contains(scenePath))
        {
            warnings.Add(new GDSceneFlowWarning
            {
                Message = $"Circular scene dependency detected involving '{scenePath}'",
                ScenePath = scenePath
            });
            return;
        }

        if (visited.Contains(scenePath))
            return;

        visited.Add(scenePath);
        stack.Add(scenePath);

        foreach (var edge in graph.GetOutgoingEdges(scenePath))
        {
            if (edge.EdgeType == GDSceneFlowEdgeType.StaticSubScene)
                DetectCycles(graph, edge.TargetScene, visited, stack, warnings);
        }

        stack.Remove(scenePath);
    }
}
