using System.Collections.Generic;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Base rename handler that wraps GDRenameService.
/// Supports only Strict confidence mode (deterministic, type-verified edits).
/// Pro version adds Potential and NameMatch confidence modes.
/// </summary>
public class GDRenameHandler : IGDRenameHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDRenameService _service;

    public GDRenameHandler(GDScriptProject project, GDProjectSemanticModel? projectModel = null)
    {
        _project = project;
        _service = new GDRenameService(project, projectModel);
    }

    /// <inheritdoc />
    public virtual bool ValidateIdentifier(string name, out string? error)
    {
        return _service.ValidateIdentifier(name, out error);
    }

    /// <inheritdoc />
    public virtual GDRenameResult Plan(string oldName, string newName, string? filePath = null)
    {
        return _service.PlanRename(oldName, newName, filePath);
    }

    /// <inheritdoc />
    public virtual void ApplyEdits(string filePath, IEnumerable<GDTextEdit> edits)
    {
        _service.ApplyEditsToFile(filePath, edits);
    }
}
