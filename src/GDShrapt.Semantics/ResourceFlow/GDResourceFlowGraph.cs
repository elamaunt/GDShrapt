using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

public class GDResourceFlowGraph
{
    private readonly Dictionary<string, GDResourceNode> _resources = new();
    private readonly Dictionary<string, List<GDResourceFlowEdge>> _consumerToResources = new();
    private readonly Dictionary<string, List<GDResourceFlowEdge>> _resourceToConsumers = new();

    public void AddResource(GDResourceNode resource)
    {
        _resources[resource.ResourcePath] = resource;
    }

    public void AddEdge(GDResourceFlowEdge edge)
    {
        if (!_consumerToResources.TryGetValue(edge.ConsumerPath, out var outList))
        {
            outList = new List<GDResourceFlowEdge>();
            _consumerToResources[edge.ConsumerPath] = outList;
        }
        outList.Add(edge);

        if (!_resourceToConsumers.TryGetValue(edge.ResourcePath, out var inList))
        {
            inList = new List<GDResourceFlowEdge>();
            _resourceToConsumers[edge.ResourcePath] = inList;
        }
        inList.Add(edge);

        // Auto-register resource node if not yet added
        if (!_resources.ContainsKey(edge.ResourcePath))
        {
            _resources[edge.ResourcePath] = new GDResourceNode
            {
                ResourcePath = edge.ResourcePath,
                ResourceType = "Resource",
                Category = GDResourceCategoryResolver.CategoryFromExtension(edge.ResourcePath)
            };
        }
    }

    public GDResourceNode? GetResource(string resourcePath)
    {
        _resources.TryGetValue(resourcePath, out var node);
        return node;
    }

    public IReadOnlyList<GDResourceFlowEdge> GetResourceUsages(string resourcePath)
    {
        if (_resourceToConsumers.TryGetValue(resourcePath, out var edges))
            return edges;
        return Array.Empty<GDResourceFlowEdge>();
    }

    public IReadOnlyList<GDResourceFlowEdge> GetResourcesUsedBy(string consumerPath)
    {
        if (_consumerToResources.TryGetValue(consumerPath, out var edges))
            return edges;
        return Array.Empty<GDResourceFlowEdge>();
    }

    public IEnumerable<GDResourceNode> AllResources => _resources.Values;

    public IEnumerable<string> AllResourcePaths => _resources.Keys;

    public IReadOnlyList<GDResourceFlowEdge> AllEdges =>
        _consumerToResources.Values.SelectMany(list => list).ToList();

    public int ResourceCount => _resources.Count;

    public int EdgeCount => _consumerToResources.Values.Sum(list => list.Count);
}
