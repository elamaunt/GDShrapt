using System.Collections.Generic;
using System.IO;

namespace GDShrapt.Abstractions;

/// <summary>
/// Dependency information for a single GDScript file.
/// </summary>
public class GDFileDependencyInfo
{
    /// <summary>
    /// Full path to the file.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// File name without path.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Class name this file extends (e.g., "Node2D", "CharacterBody3D").
    /// </summary>
    public string? ExtendsClass { get; set; }

    /// <summary>
    /// Script path this file extends (e.g., "res://base/entity.gd").
    /// </summary>
    public string? ExtendsScript { get; set; }

    /// <summary>
    /// Paths of preloaded scripts (const X = preload(...)).
    /// </summary>
    public IReadOnlyList<string> Preloads { get; set; } = new List<string>();

    /// <summary>
    /// Paths of dynamically loaded scripts (load(...) calls).
    /// </summary>
    public IReadOnlyList<string> Loads { get; set; } = new List<string>();

    /// <summary>
    /// Files that connect to signals from this file.
    /// </summary>
    public IReadOnlyList<string> SignalListeners { get; set; } = new List<string>();

    /// <summary>
    /// Files that emit signals this file listens to.
    /// </summary>
    public IReadOnlyList<string> SignalSources { get; set; } = new List<string>();

    /// <summary>
    /// Files that depend on this file (transitively).
    /// </summary>
    public IReadOnlyList<string> Dependents { get; set; } = new List<string>();

    /// <summary>
    /// Files this file depends on (transitively).
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; set; } = new List<string>();

    /// <summary>
    /// Whether this file is part of a circular dependency.
    /// </summary>
    public bool IsInCycle { get; set; }

    /// <summary>
    /// Cycle members if this file is in a cycle.
    /// </summary>
    public IReadOnlyList<string>? CycleMembers { get; set; }

    /// <summary>
    /// Total count of direct dependencies.
    /// </summary>
    public int DirectDependencyCount =>
        Preloads.Count + Loads.Count + (ExtendsScript != null ? 1 : 0);

    public GDFileDependencyInfo()
    {
    }

    public GDFileDependencyInfo(string filePath)
    {
        FilePath = filePath;
    }
}
