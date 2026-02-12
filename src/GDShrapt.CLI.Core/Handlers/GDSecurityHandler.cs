using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Base handler for security vulnerability scanning.
/// Delegates to GDSecurityScanningService.
/// </summary>
public class GDSecurityHandler : IGDSecurityHandler
{
    protected readonly GDProjectSemanticModel _projectModel;
    protected readonly GDSecurityScanningService _service;

    public GDSecurityHandler(GDProjectSemanticModel projectModel)
    {
        _projectModel = projectModel ?? throw new System.ArgumentNullException(nameof(projectModel));
        _service = projectModel.Security;
    }

    /// <inheritdoc />
    public virtual GDSecurityReport AnalyzeProject()
    {
        return _service.AnalyzeProject();
    }
}
