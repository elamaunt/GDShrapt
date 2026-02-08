using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for file dependency analysis.
/// Wraps GDDependencyService.
/// </summary>
public class GDDependencyHandler : IGDDependencyHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDProjectSemanticModel _projectModel;
    protected readonly GDDependencyService _service;

    public GDDependencyHandler(GDProjectSemanticModel projectModel)
    {
        _projectModel = projectModel ?? throw new System.ArgumentNullException(nameof(projectModel));
        _project = projectModel.Project;
        _service = projectModel.Dependencies;
    }

    /// <inheritdoc />
    public virtual GDFileDependencyInfo AnalyzeFile(string filePath)
    {
        var file = _project.GetScript(filePath);
        if (file == null)
            return new GDFileDependencyInfo(filePath);

        return _service.AnalyzeFile(file);
    }

    /// <inheritdoc />
    public virtual GDProjectDependencyReport AnalyzeProject()
    {
        return _service.AnalyzeProject();
    }
}
