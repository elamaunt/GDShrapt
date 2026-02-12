using System.Collections.Generic;
using System.IO;
using System.Linq;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Base rename handler that wraps GDRenameService.
/// Supports only Strict confidence mode (deterministic, type-verified edits).
/// </summary>
public class GDRenameHandler : IGDRenameHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDRenameService _service;
    protected readonly IGDGoToDefHandler? _goToDefHandler;

    public GDRenameHandler(GDScriptProject project, GDProjectSemanticModel? projectModel = null, IGDGoToDefHandler? goToDefHandler = null)
    {
        _project = project;
        _service = new GDRenameService(project, projectModel);
        _goToDefHandler = goToDefHandler;
    }

    /// <inheritdoc />
    public virtual string? ResolveSymbolAtPosition(string filePath, int line, int column)
    {
        if (_goToDefHandler == null)
            return null;

        var fullPath = Path.GetFullPath(filePath);
        var definition = _goToDefHandler.FindDefinition(fullPath, line - 1, column - 1);
        return definition?.SymbolName;
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

    /// <inheritdoc />
    public virtual int ApplyEdits(IReadOnlyList<GDTextEdit> edits)
    {
        var byFile = edits.GroupBy(e => e.FilePath);
        int fileCount = 0;
        foreach (var group in byFile)
        {
            _service.ApplyEditsToFile(group.Key, group);
            fileCount++;
        }
        return fileCount;
    }
}
