using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Base handler for duplicate code detection.
/// Delegates to GDDuplicateDetectionService.
/// </summary>
public class GDDuplicateHandler : IGDDuplicateHandler
{
    protected readonly GDProjectSemanticModel _projectModel;
    protected readonly GDDuplicateDetectionService _service;

    public GDDuplicateHandler(GDProjectSemanticModel projectModel)
    {
        _projectModel = projectModel ?? throw new System.ArgumentNullException(nameof(projectModel));
        _service = projectModel.Duplicates;
    }

    /// <inheritdoc />
    public virtual GDDuplicateReport AnalyzeProject(GDDuplicateOptions options)
    {
        return _service.AnalyzeProject(options);
    }

    /// <inheritdoc />
    public virtual GDDuplicateReport AnalyzeProjectWithBaseline(GDDuplicateOptions options, GDDuplicateReport? baseline)
    {
        return _service.AnalyzeProjectWithBaseline(options, baseline);
    }
}
