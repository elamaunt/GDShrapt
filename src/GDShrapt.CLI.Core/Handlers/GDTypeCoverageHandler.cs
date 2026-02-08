using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for type annotation coverage analysis.
/// Wraps GDTypeCoverageService.
/// </summary>
public class GDTypeCoverageHandler : IGDTypeCoverageHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDProjectSemanticModel _projectModel;
    protected readonly GDTypeCoverageService _service;

    public GDTypeCoverageHandler(GDProjectSemanticModel projectModel)
    {
        _projectModel = projectModel ?? throw new System.ArgumentNullException(nameof(projectModel));
        _project = projectModel.Project;
        _service = projectModel.TypeCoverage;
    }

    /// <inheritdoc />
    public virtual GDTypeCoverageReport AnalyzeFile(string filePath)
    {
        var file = _project.GetScript(filePath);
        if (file == null)
            return GDTypeCoverageReport.Empty;

        return _service.AnalyzeFile(file);
    }

    /// <inheritdoc />
    public virtual GDTypeCoverageReport AnalyzeProject()
    {
        return _service.AnalyzeProject();
    }
}
