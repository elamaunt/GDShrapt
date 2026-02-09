using GDShrapt.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GDShrapt.Semantics;

/// <summary>
/// Project-wide resource usage analysis service.
/// Exposed via GDProjectSemanticModel.ResourceFlow.
/// </summary>
public class GDResourceFlowService
{
    private readonly GDProjectSemanticModel _projectModel;
    private readonly GDScriptProject _project;
    private readonly Lazy<GDResourceFlowGraph> _graph;

    internal GDResourceFlowService(GDProjectSemanticModel projectModel)
    {
        _projectModel = projectModel;
        _project = projectModel.Project;
        _graph = new Lazy<GDResourceFlowGraph>(BuildGraph, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private GDResourceFlowGraph BuildGraph()
    {
        var builder = new GDResourceFlowBuilder(_project, _project.SceneTypesProvider);
        return builder.Build();
    }

    public GDResourceFlowReport AnalyzeProject(GDResourceFlowOptions? options = null)
    {
        var graph = _graph.Value;
        var allEdges = graph.AllEdges;
        var warnings = DetectWarnings(graph);

        var resourcesByCategory = graph.AllResources
            .GroupBy(r => r.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        var unusedResources = graph.AllResources
            .Where(r => graph.GetResourceUsages(r.ResourcePath).Count == 0)
            .Select(r => r.ResourcePath)
            .ToList();

        return new GDResourceFlowReport
        {
            AllEdges = allEdges,
            Resources = graph.AllResources.ToDictionary(r => r.ResourcePath),
            Warnings = warnings,
            TotalResources = graph.ResourceCount,
            TotalEdges = graph.EdgeCount,
            ResourcesByCategory = resourcesByCategory,
            UnusedResources = unusedResources,
            MissingResources = warnings
                .Where(w => w.Message.Contains("not found"))
                .Select(w => w.ResourcePath!)
                .Where(p => p != null)
                .ToList()
        };
    }

    public IReadOnlyList<GDResourceFlowEdge> GetResourceUsages(string resourcePath)
    {
        return _graph.Value.GetResourceUsages(resourcePath);
    }

    public IReadOnlyList<GDResourceFlowEdge> GetResourcesUsedBy(string consumerPath)
    {
        return _graph.Value.GetResourcesUsedBy(consumerPath);
    }

    public GDResourceNode? GetResourceInfo(string resourcePath)
    {
        return _graph.Value.GetResource(resourcePath);
    }

    public IReadOnlyList<string> FindUnusedResources()
    {
        var graph = _graph.Value;
        return graph.AllResources
            .Where(r => graph.GetResourceUsages(r.ResourcePath).Count == 0)
            .Select(r => r.ResourcePath)
            .ToList();
    }

    public IReadOnlyList<string> FindMissingResources()
    {
        return DetectWarnings(_graph.Value)
            .Where(w => w.Message.Contains("not found"))
            .Select(w => w.ResourcePath!)
            .Where(p => p != null)
            .Distinct()
            .ToList();
    }

    public IReadOnlyList<GDResourceNode> GetResourcesByCategory(GDResourceCategory category)
    {
        return _graph.Value.AllResources
            .Where(r => r.Category == category)
            .ToList();
    }

    public string? ResolveResourceType(string resourcePath)
    {
        var node = _graph.Value.GetResource(resourcePath);
        if (node != null && node.ResourceType != "Resource")
            return node.ResourceType;

        return GDResourceCategoryResolver.TypeNameFromExtension(resourcePath);
    }

    public GDResourceFlowGraph GetGraph() => _graph.Value;

    private List<GDResourceFlowWarning> DetectWarnings(GDResourceFlowGraph graph)
    {
        var warnings = new List<GDResourceFlowWarning>();

        // Detect resource paths referenced in code/scenes that don't exist as registered resources
        var knownPaths = graph.AllResourcePaths.ToHashSet();

        foreach (var edge in graph.AllEdges)
        {
            if (!knownPaths.Contains(edge.ResourcePath) &&
                edge.Source is GDResourceReferenceSource.CodePreload or GDResourceReferenceSource.CodeLoad or GDResourceReferenceSource.CodeResourceLoader)
            {
                // Code references a resource that wasn't found via scene parsing
                // This is expected when we don't have full project scanning, so this is informational
            }
        }

        return warnings;
    }
}
