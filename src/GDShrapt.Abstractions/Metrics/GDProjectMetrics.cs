using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Aggregated code metrics for an entire project.
/// </summary>
public class GDProjectMetrics
{
    /// <summary>
    /// Number of GDScript files in the project.
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Total lines across all files.
    /// </summary>
    public int TotalLines { get; set; }

    /// <summary>
    /// Total code lines across all files.
    /// </summary>
    public int CodeLines { get; set; }

    /// <summary>
    /// Total comment lines across all files.
    /// </summary>
    public int CommentLines { get; set; }

    /// <summary>
    /// Total number of classes (inner classes + files).
    /// </summary>
    public int ClassCount { get; set; }

    /// <summary>
    /// Total number of methods/functions.
    /// </summary>
    public int MethodCount { get; set; }

    /// <summary>
    /// Total number of signals declared.
    /// </summary>
    public int SignalCount { get; set; }

    /// <summary>
    /// Average cyclomatic complexity across all methods.
    /// </summary>
    public double AverageComplexity { get; set; }

    /// <summary>
    /// Average maintainability index across all files.
    /// </summary>
    public double AverageMaintainability { get; set; }

    /// <summary>
    /// Detailed metrics for each file.
    /// </summary>
    public IReadOnlyList<GDFileMetrics> Files { get; set; } = new List<GDFileMetrics>();
}
