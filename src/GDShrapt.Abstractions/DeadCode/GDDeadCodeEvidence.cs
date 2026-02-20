using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Evidence details for why a code element was classified as dead.
/// Populated when CollectEvidence option is enabled (--explain mode).
/// </summary>
public class GDDeadCodeEvidence
{
    /// <summary>
    /// Number of call sites scanned across the project.
    /// </summary>
    public int CallSitesScanned { get; set; }

    /// <summary>
    /// Number of signal connections checked.
    /// </summary>
    public int SignalConnectionsChecked { get; set; }

    /// <summary>
    /// Whether the element was checked against Godot virtual/entrypoint methods.
    /// </summary>
    public bool IsVirtualOrEntrypoint { get; set; }

    /// <summary>
    /// Number of dynamic dispatch probes performed (duck-typed call resolution).
    /// </summary>
    public int DuckTypeMatches { get; set; }

    /// <summary>
    /// Number of project-wide index lookups performed (cross-file access checks).
    /// </summary>
    public int CrossFileAccessChecks { get; set; }

    /// <summary>
    /// Reflection call sites that make this method reachable (for --explain output).
    /// </summary>
    public List<string>? ReflectionSites { get; set; }
}
