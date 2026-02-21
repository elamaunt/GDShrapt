using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Abstractions;

/// <summary>
/// Report containing all detected dead code items.
/// </summary>
public class GDDeadCodeReport
{
    /// <summary>
    /// All detected dead code items.
    /// </summary>
    public IReadOnlyList<GDDeadCodeItem> Items { get; set; } = new List<GDDeadCodeItem>();

    /// <summary>
    /// Total count of dead code items.
    /// </summary>
    public int TotalCount => Items.Count;

    /// <summary>
    /// Count of items with Strict confidence.
    /// </summary>
    public int StrictCount => Items.Count(i => i.Confidence == GDReferenceConfidence.Strict);

    /// <summary>
    /// Count of items with Potential confidence.
    /// </summary>
    public int PotentialCount => Items.Count(i => i.Confidence == GDReferenceConfidence.Potential);

    /// <summary>
    /// Count of items with NameMatch confidence.
    /// </summary>
    public int NameMatchCount => Items.Count(i => i.Confidence == GDReferenceConfidence.NameMatch);

    /// <summary>
    /// Count of unused variables.
    /// </summary>
    public int UnusedVariables => Items.Count(i => i.Kind == GDDeadCodeKind.Variable);

    /// <summary>
    /// Count of unused functions.
    /// </summary>
    public int UnusedFunctions => Items.Count(i => i.Kind == GDDeadCodeKind.Function);

    /// <summary>
    /// Count of unused signals.
    /// </summary>
    public int UnusedSignals => Items.Count(i => i.Kind == GDDeadCodeKind.Signal);

    /// <summary>
    /// Count of unreachable code blocks.
    /// </summary>
    public int UnreachableBlocks => Items.Count(i => i.Kind == GDDeadCodeKind.Unreachable);

    /// <summary>
    /// Groups items by file for easier navigation.
    /// </summary>
    public IEnumerable<IGrouping<string, GDDeadCodeItem>> ByFile =>
        Items.GroupBy(i => i.FilePath);

    /// <summary>
    /// Groups items by kind for summary.
    /// </summary>
    public IEnumerable<IGrouping<GDDeadCodeKind, GDDeadCodeItem>> ByKind =>
        Items.GroupBy(i => i.Kind);

    /// <summary>
    /// Groups items by confidence level.
    /// </summary>
    public IEnumerable<IGrouping<GDReferenceConfidence, GDDeadCodeItem>> ByConfidence =>
        Items.GroupBy(i => i.Confidence);

    /// <summary>
    /// Gets only items with the specified confidence level or higher (more certain).
    /// </summary>
    public IEnumerable<GDDeadCodeItem> WithConfidence(GDReferenceConfidence maxConfidence) =>
        Items.Where(i => i.Confidence <= maxConfidence);

    /// <summary>
    /// Returns the top files by dead code item count.
    /// </summary>
    public IReadOnlyList<(string FilePath, int Count)> TopOffenders(int count = 5) =>
        Items.GroupBy(i => i.FilePath)
             .OrderByDescending(g => g.Count())
             .Take(count)
             .Select(g => (g.Key, g.Count()))
             .ToList();

    /// <summary>
    /// Creates an empty report.
    /// </summary>
    public static GDDeadCodeReport Empty => new GDDeadCodeReport();

    /// <summary>
    /// Whether the report has any items.
    /// </summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>
    /// Items excluded from the report because they are reachable via reflection patterns.
    /// Populated when CollectDroppedByReflection option is enabled.
    /// </summary>
    public IReadOnlyList<GDReflectionDroppedItem> DroppedByReflection { get; set; } = new List<GDReflectionDroppedItem>();

    /// <summary>
    /// Count of items dropped by reflection.
    /// </summary>
    public int DroppedByReflectionCount => DroppedByReflection.Count;

    /// <summary>
    /// Number of files analyzed in the project.
    /// </summary>
    public int FilesAnalyzed { get; set; }

    /// <summary>
    /// Number of scene signal connections that were considered during analysis.
    /// </summary>
    public int SceneSignalConnectionsConsidered { get; set; }

    /// <summary>
    /// Number of Godot virtual methods that were skipped.
    /// </summary>
    public int VirtualMethodsSkipped { get; set; }

    /// <summary>
    /// Number of autoloads resolved for cross-file access.
    /// </summary>
    public int AutoloadsResolved { get; set; }

    /// <summary>
    /// Total number of call sites registered in the project.
    /// </summary>
    public int TotalCallSitesRegistered { get; set; }

    /// <summary>
    /// Whether C# code was detected in the project (mixed GDScript/C# project).
    /// </summary>
    public bool CSharpCodeDetected { get; set; }

    /// <summary>
    /// Number of items excluded from Strict results due to C# Singleton Interop (CSI).
    /// These are autoloaded members that may be reachable from C# code.
    /// </summary>
    public int CSharpInteropExcluded { get; set; }

    public GDDeadCodeReport()
    {
    }

    public GDDeadCodeReport(IReadOnlyList<GDDeadCodeItem> items)
    {
        Items = items;
    }
}
