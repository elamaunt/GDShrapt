using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Dependency graph for type inference visualization.
/// Contains nodes (methods) and edges (dependencies).
/// </summary>
public class GDInferenceDependencyGraph
{
    /// <summary>
    /// All method nodes in the graph.
    /// </summary>
    public List<GDInferenceNode> Nodes { get; init; } = new();

    /// <summary>
    /// All dependency edges in the graph.
    /// </summary>
    public List<GDInferenceEdge> Edges { get; init; } = new();

    /// <summary>
    /// Gets a node by method key.
    /// </summary>
    public GDInferenceNode? GetNode(string methodKey)
    {
        return Nodes.FirstOrDefault(n => n.MethodKey == methodKey);
    }

    /// <summary>
    /// Gets all edges originating from a method.
    /// </summary>
    public IEnumerable<GDInferenceEdge> GetOutgoingEdges(string methodKey)
    {
        return Edges.Where(e => e.FromMethod == methodKey);
    }

    /// <summary>
    /// Gets all edges pointing to a method.
    /// </summary>
    public IEnumerable<GDInferenceEdge> GetIncomingEdges(string methodKey)
    {
        return Edges.Where(e => e.ToMethod == methodKey);
    }

    /// <summary>
    /// Gets all cyclic edges.
    /// </summary>
    public IEnumerable<GDInferenceEdge> GetCyclicEdges()
    {
        return Edges.Where(e => e.IsPartOfCycle);
    }

    /// <summary>
    /// Gets all nodes that are part of a cycle.
    /// </summary>
    public IEnumerable<GDInferenceNode> GetCyclicNodes()
    {
        return Nodes.Where(n => n.HasCyclicDependency);
    }

    /// <summary>
    /// Builds a dependency graph from a cycle detector.
    /// </summary>
    public static GDInferenceDependencyGraph FromCycleDetector(GDInferenceCycleDetector detector)
    {
        var graph = new GDInferenceDependencyGraph();

        // Build nodes
        var allDeps = detector.GetAllDependencies().ToList();
        var allMethods = allDeps.Select(d => d.FromMethod)
            .Concat(allDeps.Select(d => d.ToMethod))
            .Distinct()
            .ToList();

        foreach (var methodKey in allMethods)
        {
            var outDeps = allDeps.Where(d => d.FromMethod == methodKey).ToList();
            var inDeps = allDeps.Where(d => d.ToMethod == methodKey).ToList();

            var parts = methodKey.Split('.');
            var className = parts.Length > 1 ? parts[0] : "";
            var methodName = parts.Length > 1 ? parts[1] : methodKey;

            graph.Nodes.Add(new GDInferenceNode
            {
                MethodKey = methodKey,
                ClassName = className,
                MethodName = methodName,
                HasCyclicDependency = detector.IsInCycle(methodKey),
                InDegree = inDeps.Count,
                OutDegree = outDeps.Count
            });
        }

        // Build edges
        foreach (var dep in allDeps)
        {
            graph.Edges.Add(new GDInferenceEdge
            {
                FromMethod = dep.FromMethod,
                ToMethod = dep.ToMethod,
                Kind = dep.Kind,
                IsPartOfCycle = dep.IsPartOfCycle
            });
        }

        return graph;
    }
}

/// <summary>
/// A node in the inference dependency graph (represents a method).
/// </summary>
public class GDInferenceNode
{
    /// <summary>
    /// Full method key (ClassName.MethodName).
    /// </summary>
    public string MethodKey { get; init; } = "";

    /// <summary>
    /// Class name containing the method.
    /// </summary>
    public string ClassName { get; init; } = "";

    /// <summary>
    /// Method name.
    /// </summary>
    public string MethodName { get; init; } = "";

    /// <summary>
    /// Whether this method is part of a cycle.
    /// </summary>
    public bool HasCyclicDependency { get; init; }

    /// <summary>
    /// Number of incoming dependencies (methods that call this method).
    /// </summary>
    public int InDegree { get; init; }

    /// <summary>
    /// Number of outgoing dependencies (methods this method calls).
    /// </summary>
    public int OutDegree { get; init; }

    /// <summary>
    /// File path where the method is defined.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Line number where the method is defined.
    /// </summary>
    public int Line { get; init; }

    public override string ToString()
    {
        var cycleMarker = HasCyclicDependency ? " [CYCLE]" : "";
        return $"{MethodKey} (in:{InDegree}, out:{OutDegree}){cycleMarker}";
    }
}

/// <summary>
/// An edge in the inference dependency graph (represents a dependency).
/// </summary>
public class GDInferenceEdge
{
    /// <summary>
    /// The method that depends on another.
    /// </summary>
    public string FromMethod { get; init; } = "";

    /// <summary>
    /// The method being depended on.
    /// </summary>
    public string ToMethod { get; init; } = "";

    /// <summary>
    /// The kind of dependency.
    /// </summary>
    public GDDependencyKind Kind { get; init; }

    /// <summary>
    /// Whether this edge is part of a cycle.
    /// </summary>
    public bool IsPartOfCycle { get; init; }

    public override string ToString()
    {
        var cycleMarker = IsPartOfCycle ? " [CYCLE]" : "";
        return $"{FromMethod} -> {ToMethod} [{Kind}]{cycleMarker}";
    }
}
