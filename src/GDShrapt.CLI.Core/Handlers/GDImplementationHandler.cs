using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for finding implementations (overrides/subclasses).
/// Delegates to GDImplementationService.
/// </summary>
public class GDImplementationHandler : IGDImplementationHandler
{
    private readonly GDScriptProject _project;
    private readonly GDProjectSemanticModel _projectModel;

    public GDImplementationHandler(GDScriptProject project, GDProjectSemanticModel projectModel)
    {
        _project = project;
        _projectModel = projectModel;
    }

    /// <inheritdoc />
    public IReadOnlyList<GDDefinitionLocation> FindImplementations(string filePath, int line, int column)
    {
        var script = _project.GetScript(filePath);
        if (script?.Class == null)
            return [];

        var cursor = new GDCursorPosition(line - 1, column - 1);
        var context = new GDRefactoringContext(script, script.Class, cursor, GDSelectionInfo.None, _project);

        var results = _projectModel.Services.Implementation.FindImplementations(context);
        if (results.Count == 0)
            return [];

        return results.Select(r => new GDDefinitionLocation
        {
            FilePath = r.FilePath,
            Line = r.Line + 1,
            Column = r.Column,
            SymbolName = r.SymbolName,
            Kind = r.Kind
        }).ToList();
    }
}
