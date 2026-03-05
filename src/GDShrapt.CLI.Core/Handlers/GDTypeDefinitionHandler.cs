using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for type definition navigation.
/// Delegates to GDTypeDefinitionService for type resolution,
/// then to IGDGoToDefHandler for navigating to the type.
/// </summary>
public class GDTypeDefinitionHandler : IGDTypeDefinitionHandler
{
    private readonly GDScriptProject _project;
    private readonly GDProjectSemanticModel _projectModel;
    private readonly IGDGoToDefHandler _goToDefHandler;

    public GDTypeDefinitionHandler(GDScriptProject project, GDProjectSemanticModel projectModel, IGDGoToDefHandler goToDefHandler)
    {
        _project = project;
        _projectModel = projectModel;
        _goToDefHandler = goToDefHandler;
    }

    /// <inheritdoc />
    public GDDefinitionLocation? FindTypeDefinition(string filePath, int line, int column)
    {
        var script = _project.GetScript(filePath);
        if (script?.Class == null)
            return null;

        var cursor = new GDCursorPosition(line - 1, column - 1);
        var context = new GDRefactoringContext(script, script.Class, cursor, GDSelectionInfo.None, _project);

        var result = _projectModel.Services.TypeDefinition.ResolveTypeDefinition(context);
        if (result == null)
            return null;

        if (result.IsBuiltIn)
        {
            return GDDefinitionLocation.WithInfo($"'{result.TypeName}' is a built-in Godot type");
        }

        // Navigate to the type definition using GoToDefHandler
        return _goToDefHandler.FindDefinitionByName(result.TypeName, filePath);
    }
}
