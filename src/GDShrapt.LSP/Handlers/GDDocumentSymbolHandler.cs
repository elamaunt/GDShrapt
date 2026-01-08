using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.LSP.Adapters;
using GDShrapt.LSP.Protocol.Types;
using GDShrapt.LSP.Server;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.Handlers;

/// <summary>
/// Handles textDocument/documentSymbol requests.
/// </summary>
public class GDDocumentSymbolHandler
{
    private readonly GDScriptProject _project;

    public GDDocumentSymbolHandler(GDScriptProject project)
    {
        _project = project;
    }

    public Task<GDLspDocumentSymbol[]?> HandleAsync(GDDocumentSymbolParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        var script = _project.GetScript(filePath);

        if (script?.Analyzer == null)
            return Task.FromResult<GDLspDocumentSymbol[]?>(null);

        var symbols = new List<GDLspDocumentSymbol>();

        // Add class symbol as root
        if (script.Class != null)
        {
            var classSymbol = new GDLspDocumentSymbol
            {
                Name = script.TypeName ?? System.IO.Path.GetFileNameWithoutExtension(filePath),
                Kind = GDLspSymbolKind.Class,
                Range = GDLocationAdapter.ToLspRange(
                    script.Class.StartLine,
                    script.Class.StartColumn,
                    script.Class.EndLine,
                    script.Class.EndColumn),
                SelectionRange = script.Class.ClassName != null
                    ? GDLocationAdapter.ToLspRange(
                        script.Class.ClassName.StartLine,
                        script.Class.ClassName.StartColumn,
                        script.Class.ClassName.EndLine,
                        script.Class.ClassName.EndColumn)
                    : GDLocationAdapter.ToLspRange(1, 1, 1, 1),
                Children = GetClassMembers(script.Analyzer)
            };
            symbols.Add(classSymbol);
        }
        else
        {
            // No class declaration, add members directly
            symbols.AddRange(GetMemberSymbols(script.Analyzer));
        }

        return Task.FromResult<GDLspDocumentSymbol[]?>(symbols.ToArray());
    }

    private GDLspDocumentSymbol[] GetClassMembers(GDScriptAnalyzer analyzer)
    {
        var members = new List<GDLspDocumentSymbol>();
        members.AddRange(GetMemberSymbols(analyzer));
        return members.ToArray();
    }

    private IEnumerable<GDLspDocumentSymbol> GetMemberSymbols(GDScriptAnalyzer analyzer)
    {
        // Add methods
        foreach (var method in analyzer.GetMethods())
        {
            if (method.Declaration == null)
                continue;

            yield return new GDLspDocumentSymbol
            {
                Name = method.Name,
                Kind = GDLspSymbolKind.Method,
                Detail = method.Type?.ToString(),
                Range = GDLocationAdapter.ToLspRange(
                    method.Declaration.StartLine,
                    method.Declaration.StartColumn,
                    method.Declaration.EndLine,
                    method.Declaration.EndColumn),
                SelectionRange = GDLocationAdapter.ToLspRange(
                    method.Declaration.StartLine,
                    method.Declaration.StartColumn,
                    method.Declaration.StartLine,
                    method.Declaration.StartColumn + method.Name.Length)
            };
        }

        // Add variables
        foreach (var variable in analyzer.GetVariables())
        {
            if (variable.Declaration == null)
                continue;

            yield return new GDLspDocumentSymbol
            {
                Name = variable.Name,
                Kind = variable.IsStatic ? GDLspSymbolKind.Constant : GDLspSymbolKind.Variable,
                Detail = variable.Type?.ToString(),
                Range = GDLocationAdapter.ToLspRange(
                    variable.Declaration.StartLine,
                    variable.Declaration.StartColumn,
                    variable.Declaration.EndLine,
                    variable.Declaration.EndColumn),
                SelectionRange = GDLocationAdapter.ToLspRange(
                    variable.Declaration.StartLine,
                    variable.Declaration.StartColumn,
                    variable.Declaration.StartLine,
                    variable.Declaration.StartColumn + variable.Name.Length)
            };
        }

        // Add signals
        foreach (var signal in analyzer.GetSignals())
        {
            if (signal.Declaration == null)
                continue;

            yield return new GDLspDocumentSymbol
            {
                Name = signal.Name,
                Kind = GDLspSymbolKind.Event,
                Range = GDLocationAdapter.ToLspRange(
                    signal.Declaration.StartLine,
                    signal.Declaration.StartColumn,
                    signal.Declaration.EndLine,
                    signal.Declaration.EndColumn),
                SelectionRange = GDLocationAdapter.ToLspRange(
                    signal.Declaration.StartLine,
                    signal.Declaration.StartColumn,
                    signal.Declaration.StartLine,
                    signal.Declaration.StartColumn + signal.Name.Length)
            };
        }

        // Add constants
        foreach (var constant in analyzer.GetConstants())
        {
            if (constant.Declaration == null)
                continue;

            yield return new GDLspDocumentSymbol
            {
                Name = constant.Name,
                Kind = GDLspSymbolKind.Constant,
                Detail = constant.Type?.ToString(),
                Range = GDLocationAdapter.ToLspRange(
                    constant.Declaration.StartLine,
                    constant.Declaration.StartColumn,
                    constant.Declaration.EndLine,
                    constant.Declaration.EndColumn),
                SelectionRange = GDLocationAdapter.ToLspRange(
                    constant.Declaration.StartLine,
                    constant.Declaration.StartColumn,
                    constant.Declaration.StartLine,
                    constant.Declaration.StartColumn + constant.Name.Length)
            };
        }

        // Add enums
        foreach (var enumSymbol in analyzer.GetEnums())
        {
            if (enumSymbol.Declaration == null)
                continue;

            yield return new GDLspDocumentSymbol
            {
                Name = enumSymbol.Name,
                Kind = GDLspSymbolKind.Enum,
                Range = GDLocationAdapter.ToLspRange(
                    enumSymbol.Declaration.StartLine,
                    enumSymbol.Declaration.StartColumn,
                    enumSymbol.Declaration.EndLine,
                    enumSymbol.Declaration.EndColumn),
                SelectionRange = GDLocationAdapter.ToLspRange(
                    enumSymbol.Declaration.StartLine,
                    enumSymbol.Declaration.StartColumn,
                    enumSymbol.Declaration.StartLine,
                    enumSymbol.Declaration.StartColumn + enumSymbol.Name.Length)
            };
        }

        // Add inner classes
        foreach (var innerClass in analyzer.GetInnerClasses())
        {
            if (innerClass.Declaration == null)
                continue;

            yield return new GDLspDocumentSymbol
            {
                Name = innerClass.Name,
                Kind = GDLspSymbolKind.Class,
                Range = GDLocationAdapter.ToLspRange(
                    innerClass.Declaration.StartLine,
                    innerClass.Declaration.StartColumn,
                    innerClass.Declaration.EndLine,
                    innerClass.Declaration.EndColumn),
                SelectionRange = GDLocationAdapter.ToLspRange(
                    innerClass.Declaration.StartLine,
                    innerClass.Declaration.StartColumn,
                    innerClass.Declaration.StartLine,
                    innerClass.Declaration.StartColumn + innerClass.Name.Length)
            };
        }
    }
}
