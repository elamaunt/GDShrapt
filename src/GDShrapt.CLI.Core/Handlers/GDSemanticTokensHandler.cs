using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

public class GDSemanticTokensHandler : IGDSemanticTokensHandler
{
    private readonly GDScriptProject _project;
    private readonly GDProjectSemanticModel? _projectModel;

    public GDSemanticTokensHandler(GDScriptProject project, GDProjectSemanticModel? projectModel)
    {
        _project = project;
        _projectModel = projectModel;
    }

    public IReadOnlyList<GDClassifiedToken> GetClassifiedTokens(string filePath)
    {
        var script = _project.GetScript(filePath);
        if (script == null)
            return [];

        var model = _projectModel.ResolveModel(script);
        if (model == null)
            return [];

        var tokens = new List<GDClassifiedToken>();

        foreach (var symbol in model.Symbols)
        {
            var nameLen = symbol.Name?.Length ?? 0;

            var declToken = symbol.DeclarationIdentifier;
            if (declToken != null)
            {
                tokens.Add(new GDClassifiedToken
                {
                    Line = declToken.StartLine,
                    Column = declToken.StartColumn,
                    Length = nameLen,
                    Kind = symbol.Kind,
                    IsDeclaration = true,
                    IsReadonly = symbol.Kind == GDSymbolKind.Constant,
                    IsStatic = symbol.Symbol is { IsStatic: true },
                    IsWrite = false
                });
            }

            if (symbol.Kind == GDSymbolKind.Signal && symbol.Symbol?.Declaration is GDSignalDeclaration signalDecl)
            {
                foreach (var param in signalDecl.Parameters)
                {
                    var paramId = param.Identifier;
                    if (paramId != null)
                    {
                        tokens.Add(new GDClassifiedToken
                        {
                            Line = paramId.StartLine,
                            Column = paramId.StartColumn,
                            Length = paramId.Sequence.Length,
                            Kind = GDSymbolKind.Parameter,
                            IsDeclaration = true,
                            IsReadonly = false,
                            IsStatic = false,
                            IsWrite = false
                        });
                    }
                }
            }

            var refs = model.GetReferencesTo(symbol);
            foreach (var reference in refs)
            {
                var refToken = reference.IdentifierToken;
                if (refToken == null) continue;

                if (declToken != null &&
                    refToken.StartLine == declToken.StartLine &&
                    refToken.StartColumn == declToken.StartColumn)
                    continue;

                tokens.Add(new GDClassifiedToken
                {
                    Line = refToken.StartLine,
                    Column = refToken.StartColumn,
                    Length = nameLen,
                    Kind = symbol.Kind,
                    IsDeclaration = false,
                    IsReadonly = symbol.Kind == GDSymbolKind.Constant,
                    IsStatic = symbol.Symbol is { IsStatic: true },
                    IsWrite = reference.IsWrite
                });
            }
        }

        tokens.Sort((a, b) =>
        {
            var cmp = a.Line.CompareTo(b.Line);
            return cmp != 0 ? cmp : a.Column.CompareTo(b.Column);
        });

        // Deduplicate — keep first at each position
        var deduped = new List<GDClassifiedToken>();
        for (int i = 0; i < tokens.Count; i++)
        {
            if (i > 0 && tokens[i].Line == tokens[i - 1].Line && tokens[i].Column == tokens[i - 1].Column)
                continue;
            deduped.Add(tokens[i]);
        }

        return deduped;
    }
}
