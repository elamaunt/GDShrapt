using GDShrapt.Abstractions;
using GDShrapt.Semantics;

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
        _service = new GDDeadCodeService(projectModel);
    }

    /// <inheritdoc />
    public virtual GDDeadCodeReport AnalyzeFile(string filePath, GDDeadCodeOptions options)
    {
        var file = _project.GetScript(filePath);
        if (file == null)
            return GDDeadCodeReport.Empty;

        // Base: enforce Strict confidence
        var safeOptions = EnforceBaseConfidence(options);
        return _service.AnalyzeFile(file, safeOptions);
    }

    /// <inheritdoc />
    public virtual GDDeadCodeReport AnalyzeProject(GDDeadCodeOptions options)
    {
        // Base: enforce Strict confidence
        var safeOptions = EnforceBaseConfidence(options);
        return _service.AnalyzeProject(safeOptions);
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
            return new GDDeadCodeOptions
            {
                MaxConfidence = GDReferenceConfidence.Strict,
                IncludeVariables = options.IncludeVariables,
                IncludeFunctions = options.IncludeFunctions,
                IncludeSignals = options.IncludeSignals,
                IncludeParameters = options.IncludeParameters,
                IncludePrivate = options.IncludePrivate,
                IncludeUnreachable = options.IncludeUnreachable
            };
        }
        return options;
    }
}
