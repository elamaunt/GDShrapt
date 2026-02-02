using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for code metrics analysis.
/// Wraps GDMetricsService.
/// </summary>
public class GDMetricsHandler : IGDMetricsHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDMetricsService _service;

    public GDMetricsHandler(GDScriptProject project)
    {
        _project = project;
        _service = new GDMetricsService(project);
    }

    /// <inheritdoc />
    public virtual GDFileMetrics AnalyzeFile(string filePath)
    {
        var file = _project.GetScript(filePath);
        if (file == null)
            return CreateEmptyFileMetrics(filePath);

        return _service.AnalyzeFile(file);
    }

    /// <inheritdoc />
    public virtual GDProjectMetrics AnalyzeProject()
    {
        return _service.AnalyzeProject();
    }

    private static GDFileMetrics CreateEmptyFileMetrics(string filePath)
    {
        return new GDFileMetrics
        {
            FilePath = filePath,
            FileName = System.IO.Path.GetFileName(filePath),
            Methods = new System.Collections.Generic.List<GDMethodMetrics>()
        };
    }
}
