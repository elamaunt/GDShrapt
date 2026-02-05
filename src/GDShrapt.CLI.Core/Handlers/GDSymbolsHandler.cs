using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
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

    /// <inheritdoc />
    public virtual Semantics.GDSymbolInfo? FindSymbolByName(string symbolName, string filePath)
    {
        if (string.IsNullOrEmpty(symbolName) || string.IsNullOrEmpty(filePath))
            return null;

        var file = _project.GetScript(filePath);
        return file?.SemanticModel?.FindSymbol(symbolName);
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<Semantics.GDSymbolInfo> GetSymbolsOfKind(string filePath, GDSymbolKind kind)
    {
        var file = _project.GetScript(filePath);
        if (file?.SemanticModel == null)
            return [];

        return file.SemanticModel.Symbols
            .Where(s => s.Kind == kind)
            .ToList();
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDReference> GetReferencesToSymbol(Semantics.GDSymbolInfo symbol, string filePath)
    {
        if (symbol == null || string.IsNullOrEmpty(filePath))
            return [];

        var file = _project.GetScript(filePath);
        if (file?.SemanticModel == null)
            return [];

        return file.SemanticModel.GetReferencesTo(symbol).ToList();
    }

    /// <inheritdoc />
    public virtual string? GetTypeForNode(GDNode node, string filePath)
    {
        if (node == null || string.IsNullOrEmpty(filePath))
            return null;

        var file = _project.GetScript(filePath);
        var typeInfo = file?.SemanticModel?.TypeSystem.GetType(node);
        return typeInfo?.IsVariant == true ? null : typeInfo?.DisplayName;
    }
}
