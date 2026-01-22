using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for extracting document symbols.
/// </summary>
public class GDSymbolsHandler : IGDSymbolsHandler
{
    protected readonly GDScriptProject _project;

    public GDSymbolsHandler(GDScriptProject project)
    {
        _project = project;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDDocumentSymbol> GetSymbols(string filePath)
    {
        var file = _project.GetScript(filePath);
        if (file?.SemanticModel == null)
            return [];

        return file.SemanticModel.Symbols
            .Select(s => new GDDocumentSymbol
            {
                Name = s.Name,
                Kind = s.Kind,
                Type = s.TypeName,
                Line = s.DeclarationNode?.StartLine ?? 0,
                Column = s.DeclarationNode?.StartColumn ?? 0
            })
            .OrderBy(s => s.Line)
            .ThenBy(s => s.Column)
            .ToList();
    }
}
