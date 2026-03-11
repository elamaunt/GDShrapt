using System.Collections.Generic;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

public class GDHighlightHandler : IGDHighlightHandler
{
    private readonly GDScriptProject _project;
    private readonly GDProjectSemanticModel? _projectModel;

    public GDHighlightHandler(GDScriptProject project, GDProjectSemanticModel? projectModel)
    {
        _project = project;
        _projectModel = projectModel;
    }

    public IReadOnlyList<GDHighlightLocation> GetHighlights(string filePath, string symbolName)
    {
        var script = _project.GetScript(filePath);
        if (script == null)
            return [];

        var model = _projectModel.ResolveModel(script);
        if (model == null)
            return [];

        var highlights = new List<GDHighlightLocation>();

        var symbol = model.FindSymbol(symbolName);
        if (symbol != null)
        {
            var declId = symbol.DeclarationIdentifier;
            if (declId != null)
            {
                highlights.Add(new GDHighlightLocation
                {
                    Line = declId.StartLine + 1,
                    Column = declId.StartColumn,
                    Length = symbolName.Length,
                    IsWrite = true,
                    IsDeclaration = true
                });
            }
        }

        var refs = model.GetReferencesTo(symbolName);
        foreach (var r in refs)
        {
            if (r.ReferenceNode == null)
                continue;

            var refLine = r.IdentifierToken?.StartLine ?? r.ReferenceNode.StartLine;
            var refCol = r.IdentifierToken?.StartColumn ?? r.ReferenceNode.StartColumn;

            highlights.Add(new GDHighlightLocation
            {
                Line = refLine + 1,
                Column = refCol,
                Length = symbolName.Length,
                IsWrite = r.IsWrite,
                IsDeclaration = false
            });
        }

        return highlights;
    }
}
