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
    protected readonly IGDRuntimeProvider? _runtimeProvider;

    public GDSymbolsHandler(GDScriptProject project, IGDRuntimeProvider? runtimeProvider = null)
    {
        _project = project;
        _runtimeProvider = runtimeProvider;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDDocumentSymbol> GetSymbols(string filePath)
    {
        var file = _project.GetScript(filePath);
        if (file?.SemanticModel == null)
        {
            if (file?.Class != null)
                return ExtractAstSymbols(file.Class);

            if (GDBuiltInFileHelper.IsBuiltInTypeFile(filePath))
            {
                var builtInFile = GDBuiltInFileHelper.GetOrParse(filePath, _runtimeProvider);
                if (builtInFile?.SemanticModel != null)
                    return ExtractSymbols(builtInFile.SemanticModel);
            }
            return [];
        }

        return ExtractSymbols(file.SemanticModel);
    }

    private static IReadOnlyList<GDDocumentSymbol> ExtractSymbols(GDSemanticModel model)
    {
        return model.Symbols
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

    private static IReadOnlyList<GDDocumentSymbol> ExtractAstSymbols(GDClassDeclaration classDecl)
    {
        var symbols = new List<GDDocumentSymbol>();

        foreach (var member in classDecl.Members)
        {
            switch (member)
            {
                case GDMethodDeclaration method when method.Identifier != null:
                    symbols.Add(new GDDocumentSymbol
                    {
                        Name = method.Identifier.Sequence ?? "",
                        Kind = GDSymbolKind.Method,
                        Line = method.Identifier.StartLine,
                        Column = method.Identifier.StartColumn
                    });
                    break;
                case GDVariableDeclaration variable when variable.Identifier != null:
                    symbols.Add(new GDDocumentSymbol
                    {
                        Name = variable.Identifier.Sequence ?? "",
                        Kind = variable.IsConstant ? GDSymbolKind.Constant : GDSymbolKind.Variable,
                        Line = variable.Identifier.StartLine,
                        Column = variable.Identifier.StartColumn
                    });
                    break;
                case GDSignalDeclaration signal when signal.Identifier != null:
                    symbols.Add(new GDDocumentSymbol
                    {
                        Name = signal.Identifier.Sequence ?? "",
                        Kind = GDSymbolKind.Signal,
                        Line = signal.Identifier.StartLine,
                        Column = signal.Identifier.StartColumn
                    });
                    break;
                case GDEnumDeclaration enumDecl when enumDecl.Identifier != null:
                    symbols.Add(new GDDocumentSymbol
                    {
                        Name = enumDecl.Identifier.Sequence ?? "",
                        Kind = GDSymbolKind.Enum,
                        Line = enumDecl.Identifier.StartLine,
                        Column = enumDecl.Identifier.StartColumn
                    });
                    break;
                case GDInnerClassDeclaration innerClass when innerClass.Identifier != null:
                    symbols.Add(new GDDocumentSymbol
                    {
                        Name = innerClass.Identifier.Sequence ?? "",
                        Kind = GDSymbolKind.Class,
                        Line = innerClass.Identifier.StartLine,
                        Column = innerClass.Identifier.StartColumn
                    });
                    break;
            }
        }

        return symbols.OrderBy(s => s.Line).ThenBy(s => s.Column).ToList();
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
