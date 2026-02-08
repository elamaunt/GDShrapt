using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for go-to-definition navigation.
/// Uses GDSemanticModel for symbol lookup per Rule 11.
/// </summary>
public class GDGoToDefHandler : IGDGoToDefHandler
{
    protected readonly GDScriptProject _project;

    public GDGoToDefHandler(GDScriptProject project)
    {
        _project = project;
    }

    /// <inheritdoc />
    public virtual GDDefinitionLocation? FindDefinition(string filePath, int line, int column)
    {
        var script = _project.GetScript(filePath);
        if (script?.SemanticModel == null || script.Class == null)
            return null;

        var identifier = script.SemanticModel.GetIdentifierAtPosition(line, column);
        if (identifier == null)
            return null;

        // Find the symbol definition
        var symbolName = identifier.Sequence;
        return FindDefinitionByName(symbolName, filePath);
    }

    /// <inheritdoc />
    public virtual GDDefinitionLocation? FindDefinitionByName(string symbolName, string? fromFilePath = null)
    {
        // If a specific file is provided, search there first
        if (!string.IsNullOrEmpty(fromFilePath))
        {
            var script = _project.GetScript(fromFilePath);
            var semanticModel = script?.SemanticModel;
            if (semanticModel != null)
            {
                var symbol = semanticModel.FindSymbol(symbolName);
                if (symbol?.DeclarationNode != null)
                {
                    return new GDDefinitionLocation
                    {
                        FilePath = script.Reference.FullPath,
                        Line = symbol.DeclarationNode.StartLine,
                        Column = symbol.DeclarationNode.StartColumn,
                        SymbolName = symbol.Name,
                        Kind = symbol.Kind
                    };
                }
            }
        }

        // Search across all files
        foreach (var script in _project.ScriptFiles)
        {
            var semanticModel = script.SemanticModel;
            if (semanticModel == null)
                continue;

            var symbol = semanticModel.FindSymbol(symbolName);
            if (symbol?.DeclarationNode != null)
            {
                return new GDDefinitionLocation
                {
                    FilePath = script.Reference.FullPath,
                    Line = symbol.DeclarationNode.StartLine,
                    Column = symbol.DeclarationNode.StartColumn,
                    SymbolName = symbol.Name,
                    Kind = symbol.Kind
                };
            }
        }

        return null;
    }
}
