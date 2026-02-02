using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Code metrics for a single GDScript file.
/// </summary>
public class GDFileMetrics
{
    /// <summary>
    /// Full path to the file.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// File name without path.
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// Total number of lines in the file.
    /// </summary>
    public int TotalLines { get; set; }

    /// <summary>
    /// Number of lines containing code (non-blank, non-comment).
    /// </summary>
    public int CodeLines { get; set; }

    /// <summary>
    /// Number of comment lines.
    /// </summary>
    public int CommentLines { get; set; }

    /// <summary>
    /// Number of blank lines.
    /// </summary>
    public int BlankLines { get; set; }

    /// <summary>
    /// Number of inner class declarations.
    /// </summary>
    public int ClassCount { get; set; }

    /// <summary>
    /// Number of methods/functions.
    /// </summary>
    public int MethodCount { get; set; }

    /// <summary>
    /// Number of signals declared.
    /// </summary>
    public int SignalCount { get; set; }

    /// <summary>
    /// Number of class-level variables.
    /// </summary>
    public int VariableCount { get; set; }

    /// <summary>
    /// Average cyclomatic complexity across all methods.
    /// </summary>
    public double AverageComplexity { get; set; }

    /// <summary>
    /// Maximum cyclomatic complexity among all methods.
    /// </summary>
    public int MaxComplexity { get; set; }

    /// <summary>
    /// Maximum nesting depth among all methods.
    /// </summary>
    public int MaxNestingDepth { get; set; }

    /// <summary>
    /// Average maintainability index across all methods.
    /// </summary>
    public double MaintainabilityIndex { get; set; }

    /// <summary>
    /// Detailed metrics for each method.
    /// </summary>
    public IReadOnlyList<GDMethodMetrics> Methods { get; set; } = new List<GDMethodMetrics>();

    public GDFileMetrics()
    {
    }

    public GDFileMetrics(string filePath, string fileName)
    {
        FilePath = filePath;
        FileName = fileName;
    }
}
