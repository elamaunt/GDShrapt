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
    protected readonly GDDependencyService _service;

    public GDDependencyHandler(GDScriptProject project)
    {
        _project = project;
        _service = new GDDependencyService(project);
    }

    /// <summary>
    /// Creates a handler with an explicit signal registry.
    /// Use this when you have a GDProjectSemanticModel.
    /// </summary>
    public GDDependencyHandler(GDScriptProject project, GDSignalConnectionRegistry? signalRegistry)
    {
        _project = project;
        _service = new GDDependencyService(project, signalRegistry);
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
