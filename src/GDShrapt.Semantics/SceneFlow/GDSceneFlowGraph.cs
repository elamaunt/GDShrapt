using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Project-wide scene composition graph.
/// Tracks scene-to-scene dependencies, node hierarchies, and runtime instantiation patterns.
/// </summary>
public class GDSceneFlowGraph
{
    private readonly Dictionary<string, GDSceneFlowNode> _scenes = new();
    private readonly Dictionary<string, List<GDSceneFlowEdge>> _outgoing = new();
    private readonly Dictionary<string, List<GDSceneFlowEdge>> _incoming = new();

    public void AddScene(GDSceneFlowNode scene)
    {
        _scenes[scene.ScenePath] = scene;
    }

    public void AddEdge(GDSceneFlowEdge edge)
    {
        if (!_outgoing.TryGetValue(edge.SourceScene, out var outList))
        {
            outList = new List<GDSceneFlowEdge>();
            _outgoing[edge.SourceScene] = outList;
        }
        outList.Add(edge);

        if (!_incoming.TryGetValue(edge.TargetScene, out var inList))
        {
            inList = new List<GDSceneFlowEdge>();
            _incoming[edge.TargetScene] = inList;
        }
        inList.Add(edge);
    }

    public GDSceneFlowNode? GetScene(string scenePath)
    {
        _scenes.TryGetValue(scenePath, out var node);
        return node;
    }

    public IReadOnlyList<GDSceneFlowEdge> GetOutgoingEdges(string scenePath)
    {
        if (_outgoing.TryGetValue(scenePath, out var edges))
            return edges;
        return Array.Empty<GDSceneFlowEdge>();
    }

    public IReadOnlyList<GDSceneFlowEdge> GetIncomingEdges(string scenePath)
    {
        if (_incoming.TryGetValue(scenePath, out var edges))
            return edges;
        return Array.Empty<GDSceneFlowEdge>();
    }

    /// <summary>
    /// Gets all scenes that instantiate or include the given scene.
    /// </summary>
    public IEnumerable<string> GetScenesThatInstantiate(string scenePath)
    {
        return GetIncomingEdges(scenePath).Select(e => e.SourceScene).Distinct();
    }

    /// <summary>
    /// Gets all scenes that are instantiated or included by the given scene.
    /// </summary>
    public IEnumerable<string> GetInstantiatedScenes(string scenePath)
    {
        return GetOutgoingEdges(scenePath).Select(e => e.TargetScene).Distinct();
    }

    public IEnumerable<string> AllScenePaths => _scenes.Keys;

    public IEnumerable<GDSceneFlowNode> AllScenes => _scenes.Values;

    public int SceneCount => _scenes.Count;

    public int EdgeCount => _outgoing.Values.Sum(list => list.Count);

    public IReadOnlyList<GDSceneFlowEdge> AllEdges =>
        _outgoing.Values.SelectMany(list => list).ToList();

    public void Clear()
    {
        _scenes.Clear();
        _outgoing.Clear();
        _incoming.Clear();
    }
}
