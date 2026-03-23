using System.Collections.Generic;
using System.Linq;
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
        GDSemanticModel? model;
        bool isBuiltInFile = false;

        if (script == null)
        {
            if (GDBuiltInFileHelper.IsBuiltInTypeFile(filePath))
            {
                var builtInFile = GDBuiltInFileHelper.GetOrParse(filePath, _projectModel?.RuntimeProvider);
                if (builtInFile?.SemanticModel == null)
                    return [];
                model = builtInFile.SemanticModel;
                isBuiltInFile = true;
            }
            else
            {
                return [];
            }
        }
        else
        {
            model = _projectModel?.ResolveModel(script);
            if (model == null)
                return [];
        }

        return ClassifyTokens(model, isBuiltInFile);
    }

    private IReadOnlyList<GDClassifiedToken> ClassifyTokens(GDSemanticModel model, bool skipOverrideCheck)
    {
        var tokens = new List<GDClassifiedToken>();

        foreach (var symbol in model.Symbols)
        {
            var nameLen = symbol.Name?.Length ?? 0;

            var declToken = symbol.DeclarationIdentifier;
            if (declToken != null)
            {
                var isAbstract = false;
                var isOverride = false;

                if (symbol.Kind == GDSymbolKind.Method && symbol.Symbol?.Declaration is GDMethodDeclaration methodDecl)
                {
                    isAbstract = methodDecl.AttributesDeclaredBefore
                        .Any(attr => attr.Attribute?.IsAbstract() == true);

                    if (!isAbstract && !skipOverrideCheck)
                        isOverride = IsMethodOverride(model, symbol.Name);
                }

                tokens.Add(new GDClassifiedToken
                {
                    Line = declToken.StartLine,
                    Column = declToken.StartColumn,
                    Length = nameLen,
                    Kind = symbol.Kind,
                    IsDeclaration = true,
                    IsReadonly = symbol.Kind == GDSymbolKind.Constant,
                    IsStatic = symbol.Symbol is { IsStatic: true },
                    IsWrite = false,
                    IsAbstract = isAbstract,
                    IsOverride = isOverride
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

    private bool IsMethodOverride(GDSemanticModel model, string? methodName)
    {
        if (string.IsNullOrEmpty(methodName))
            return false;

        var provider = model.RuntimeProvider;
        if (provider == null)
            return false;

        var baseType = model.BaseTypeName;
        var visited = new HashSet<string>();

        while (!string.IsNullOrEmpty(baseType) && visited.Add(baseType))
        {
            // Check built-in types
            var member = provider.GetMember(baseType, methodName);
            if (member != null && member.Kind == GDRuntimeMemberKind.Method)
                return true;

            // Check project scripts
            var baseScript = _project.GetScriptByTypeName(baseType);
            if (baseScript != null)
            {
                var baseModel = _projectModel?.ResolveModel(baseScript);
                if (baseModel != null)
                {
                    var symbols = baseModel.FindSymbols(methodName);
                    if (symbols.Any(s => s.Kind == GDSymbolKind.Method))
                        return true;
                }
            }

            baseType = provider.GetBaseType(baseType);
        }

        return false;
    }
}
