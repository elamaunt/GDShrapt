using System;
using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Kind of dependency edge between files.
/// </summary>
public enum GDDependencyEdgeKind
{
    Extends,
    ExtendsPath,
    Preload,
    Load,
    SceneScript,
    SceneSubScene,
    SignalCode,
    SignalScene
}

/// <summary>
/// A single typed dependency edge between two files.
/// </summary>
public class GDFileDependencyEdge
{
    public string FromPath { get; set; } = "";
    public string ToPath { get; set; } = "";
    public GDDependencyEdgeKind Kind { get; set; }
    public string? Detail { get; set; }
}

/// <summary>
/// Typed edges for a specific file (both directions).
/// </summary>
public class GDFileDependencyEdges
{
    public string FilePath { get; set; } = "";
    public IReadOnlyList<GDFileDependencyEdge> Outgoing { get; set; } = Array.Empty<GDFileDependencyEdge>();
    public IReadOnlyList<GDFileDependencyEdge> Incoming { get; set; } = Array.Empty<GDFileDependencyEdge>();
}
