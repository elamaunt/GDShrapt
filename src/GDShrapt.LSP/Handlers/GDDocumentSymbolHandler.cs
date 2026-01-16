using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.LSP;

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
            if (method.DeclarationNode == null)
                continue;

            yield return new GDLspDocumentSymbol
            {
                Name = method.Name,
                Kind = GDLspSymbolKind.Method,
                Detail = method.TypeNode?.ToString(),
                Range = GDLocationAdapter.ToLspRange(
                    method.DeclarationNode.StartLine,
                    method.DeclarationNode.StartColumn,
                    method.DeclarationNode.EndLine,
                    method.DeclarationNode.EndColumn),
                SelectionRange = GDLocationAdapter.ToLspRange(
                    method.DeclarationNode.StartLine,
                    method.DeclarationNode.StartColumn,
                    method.DeclarationNode.StartLine,
                    method.DeclarationNode.StartColumn + method.Name.Length)
            };
        }

        // Add variables
        foreach (var variable in analyzer.GetVariables())
        {
            if (variable.DeclarationNode == null)
                continue;

            yield return new GDLspDocumentSymbol
            {
                Name = variable.Name,
                Kind = variable.IsStatic ? GDLspSymbolKind.Constant : GDLspSymbolKind.Variable,
                Detail = variable.TypeNode?.ToString(),
                Range = GDLocationAdapter.ToLspRange(
                    variable.DeclarationNode.StartLine,
                    variable.DeclarationNode.StartColumn,
                    variable.DeclarationNode.EndLine,
                    variable.DeclarationNode.EndColumn),
                SelectionRange = GDLocationAdapter.ToLspRange(
                    variable.DeclarationNode.StartLine,
                    variable.DeclarationNode.StartColumn,
                    variable.DeclarationNode.StartLine,
                    variable.DeclarationNode.StartColumn + variable.Name.Length)
            };
        }

        // Add signals
        foreach (var signal in analyzer.GetSignals())
        {
            if (signal.DeclarationNode == null)
                continue;

            yield return new GDLspDocumentSymbol
            {
                Name = signal.Name,
                Kind = GDLspSymbolKind.Event,
                Range = GDLocationAdapter.ToLspRange(
                    signal.DeclarationNode.StartLine,
                    signal.DeclarationNode.StartColumn,
                    signal.DeclarationNode.EndLine,
                    signal.DeclarationNode.EndColumn),
                SelectionRange = GDLocationAdapter.ToLspRange(
                    signal.DeclarationNode.StartLine,
                    signal.DeclarationNode.StartColumn,
                    signal.DeclarationNode.StartLine,
                    signal.DeclarationNode.StartColumn + signal.Name.Length)
            };
        }

        // Add constants
        foreach (var constant in analyzer.GetConstants())
        {
            if (constant.DeclarationNode == null)
                continue;

            yield return new GDLspDocumentSymbol
            {
                Name = constant.Name,
                Kind = GDLspSymbolKind.Constant,
                Detail = constant.TypeNode?.ToString(),
                Range = GDLocationAdapter.ToLspRange(
                    constant.DeclarationNode.StartLine,
                    constant.DeclarationNode.StartColumn,
                    constant.DeclarationNode.EndLine,
                    constant.DeclarationNode.EndColumn),
                SelectionRange = GDLocationAdapter.ToLspRange(
                    constant.DeclarationNode.StartLine,
                    constant.DeclarationNode.StartColumn,
                    constant.DeclarationNode.StartLine,
                    constant.DeclarationNode.StartColumn + constant.Name.Length)
            };
        }

        // Add enums
        foreach (var enumSymbol in analyzer.GetEnums())
        {
            if (enumSymbol.DeclarationNode == null)
                continue;

            yield return new GDLspDocumentSymbol
            {
                Name = enumSymbol.Name,
                Kind = GDLspSymbolKind.Enum,
                Range = GDLocationAdapter.ToLspRange(
                    enumSymbol.DeclarationNode.StartLine,
                    enumSymbol.DeclarationNode.StartColumn,
                    enumSymbol.DeclarationNode.EndLine,
                    enumSymbol.DeclarationNode.EndColumn),
                SelectionRange = GDLocationAdapter.ToLspRange(
                    enumSymbol.DeclarationNode.StartLine,
                    enumSymbol.DeclarationNode.StartColumn,
                    enumSymbol.DeclarationNode.StartLine,
                    enumSymbol.DeclarationNode.StartColumn + enumSymbol.Name.Length)
            };
        }

        // Add inner classes
        foreach (var innerClass in analyzer.GetInnerClasses())
        {
            if (innerClass.DeclarationNode == null)
                continue;

            yield return new GDLspDocumentSymbol
            {
                Name = innerClass.Name,
                Kind = GDLspSymbolKind.Class,
                Range = GDLocationAdapter.ToLspRange(
                    innerClass.DeclarationNode.StartLine,
                    innerClass.DeclarationNode.StartColumn,
                    innerClass.DeclarationNode.EndLine,
                    innerClass.DeclarationNode.EndColumn),
                SelectionRange = GDLocationAdapter.ToLspRange(
                    innerClass.DeclarationNode.StartLine,
                    innerClass.DeclarationNode.StartColumn,
                    innerClass.DeclarationNode.StartLine,
                    innerClass.DeclarationNode.StartColumn + innerClass.Name.Length)
            };
        }
    }
}
