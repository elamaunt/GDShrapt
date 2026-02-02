using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Abstractions;

/// <summary>
/// Dependency analysis report for an entire project.
/// </summary>
public class GDProjectDependencyReport
{
    /// <summary>
    /// Dependency information for each file.
    /// </summary>
    public IReadOnlyList<GDFileDependencyInfo> Files { get; set; } = new List<GDFileDependencyInfo>();

    /// <summary>
    /// Detected circular dependency cycles.
    /// Each inner list contains file paths forming a cycle.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Cycles { get; set; } = new List<IReadOnlyList<string>>();

    /// <summary>
    /// Whether the project has any circular dependencies.
    /// </summary>
    public bool HasCycles => Cycles.Count > 0;

    /// <summary>
    /// Number of circular dependency cycles.
    /// </summary>
    public int CycleCount => Cycles.Count;

    /// <summary>
    /// Number of files involved in cycles.
    /// </summary>
    public int FilesInCycles => Files.Count(f => f.IsInCycle);

    /// <summary>
    /// Total number of files analyzed.
    /// </summary>
    public int TotalFiles => Files.Count;

    /// <summary>
    /// Files with the most dependents (most "important").
    /// </summary>
    public IEnumerable<GDFileDependencyInfo> MostDependent =>
        Files.OrderByDescending(f => f.Dependents.Count);

    /// <summary>
    /// Files with the most dependencies (most "coupled").
    /// </summary>
    public IEnumerable<GDFileDependencyInfo> MostCoupled =>
        Files.OrderByDescending(f => f.DirectDependencyCount);

    /// <summary>
    /// Gets dependency info for a specific file.
    /// Normalizes path separators for cross-platform compatibility.
    /// </summary>
    public GDFileDependencyInfo? GetFile(string filePath)
    {
        // Normalize path separators for comparison
        var normalizedPath = filePath.Replace('\\', '/');
        return Files.FirstOrDefault(f =>
            string.Equals(f.FilePath.Replace('\\', '/'), normalizedPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates an empty report.
    /// </summary>
    public static GDProjectDependencyReport Empty => new GDProjectDependencyReport();
}
