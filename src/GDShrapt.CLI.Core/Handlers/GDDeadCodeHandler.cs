using GDShrapt.Abstractions;
using GDShrapt.Semantics;
using System.Linq;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for dead code analysis.
/// Base implementation enforces Strict confidence only.
/// Pro implementation allows higher confidence levels.
/// Requires GDProjectSemanticModel for accurate semantic analysis.
/// </summary>
public class GDDeadCodeHandler : IGDDeadCodeHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDProjectSemanticModel _projectModel;
    protected readonly GDDeadCodeService _service;

    /// <summary>
    /// Creates a dead code handler with semantic analysis support.
    /// </summary>
    /// <param name="projectModel">The project semantic model (required for accurate analysis).</param>
    /// <exception cref="System.ArgumentNullException">Thrown if projectModel is null.</exception>
    public GDDeadCodeHandler(GDProjectSemanticModel projectModel)
    {
        _projectModel = projectModel ?? throw new System.ArgumentNullException(nameof(projectModel));
        _project = projectModel.Project;
        _service = projectModel.DeadCode;
    }

    /// <inheritdoc />
    public virtual GDDeadCodeReport AnalyzeFile(string filePath, GDDeadCodeOptions options)
    {
        var file = _project.GetScript(filePath);
        if (file == null)
            return GDDeadCodeReport.Empty;

        // Base: enforce Strict confidence
        var safeOptions = EnforceBaseConfidence(options);
        var report = _service.AnalyzeFile(file, safeOptions);

        // Filter to only Strict confidence items for Base
        return FilterByConfidence(report, safeOptions.MaxConfidence);
    }

    /// <inheritdoc />
    public virtual GDDeadCodeReport AnalyzeProject(GDDeadCodeOptions options)
    {
        // Base: enforce Strict confidence
        var safeOptions = EnforceBaseConfidence(options);
        var report = _service.AnalyzeProject(safeOptions);

        // Filter to only Strict confidence items for Base
        return FilterByConfidence(report, safeOptions.MaxConfidence);
    }

    /// <summary>
    /// Filters report items by maximum confidence level.
    /// Base implementation filters to Strict only.
    /// </summary>
    protected virtual GDDeadCodeReport FilterByConfidence(GDDeadCodeReport report, GDReferenceConfidence maxConfidence)
    {
        if (maxConfidence == GDReferenceConfidence.Strict)
        {
            var filteredItems = report.Items
                .Where(item => item.Confidence == GDReferenceConfidence.Strict)
                .ToList();
            return new GDDeadCodeReport(filteredItems)
            {
                FilesAnalyzed = report.FilesAnalyzed,
                SceneSignalConnectionsConsidered = report.SceneSignalConnectionsConsidered,
                VirtualMethodsSkipped = report.VirtualMethodsSkipped,
                AutoloadsResolved = report.AutoloadsResolved,
                TotalCallSitesRegistered = report.TotalCallSitesRegistered,
                CSharpCodeDetected = report.CSharpCodeDetected,
                CSharpInteropExcluded = report.CSharpInteropExcluded,
                ResourceFilesConsidered = report.ResourceFilesConsidered,
                DroppedByReflection = report.DroppedByReflection
            };
        }
        return report;
    }

    /// <summary>
    /// Base implementation enforces Strict confidence only.
    /// Override in Pro to allow higher confidence levels.
    /// </summary>
    protected virtual GDDeadCodeOptions EnforceBaseConfidence(GDDeadCodeOptions options)
    {
        // Base only allows Strict confidence for safety
        if (options.MaxConfidence > GDReferenceConfidence.Strict)
        {
            return options.WithStrictConfidenceOnly();
        }
        return options;
    }
}
