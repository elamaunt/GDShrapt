using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.LSP;

public class GDDocumentHighlightHandler
{
    private static readonly TimeSpan FindDefTimeout = TimeSpan.FromMilliseconds(500);

    private readonly GDScriptProject _project;
    private readonly GDProjectSemanticModel? _projectModel;
    private readonly IGDGoToDefHandler _goToDefHandler;

    public GDDocumentHighlightHandler(GDScriptProject project, GDProjectSemanticModel? projectModel, IGDGoToDefHandler goToDefHandler)
    {
        _project = project;
        _projectModel = projectModel;
        _goToDefHandler = goToDefHandler;
    }

    public async Task<GDDocumentHighlight[]?> HandleAsync(GDDocumentHighlightParams @params, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return null;

        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        var filename = System.IO.Path.GetFileName(filePath);

        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        GDLspPerformanceTrace.Log("highlight", $"START {filename} L{line}:{column}");

        // FindDefinition can hang on broken AST — run with combined timeout + cancellation
        GDDefinitionLocation? definition = null;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(FindDefTimeout);

        try
        {
            definition = await Task.Run(
                () => _goToDefHandler.FindDefinition(filePath, line, column),
                timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            GDLspPerformanceTrace.Log("highlight", $"FIND-DEF-TIMEOUT {filename}");
            return null;
        }
        catch (OperationCanceledException)
        {
            GDLspPerformanceTrace.Log("highlight", $"FIND-DEF-CANCELLED {filename}");
            return null;
        }
        catch (Exception ex)
        {
            GDLspPerformanceTrace.Log("highlight", $"FIND-DEF-ERROR {filename} {ex.GetType().Name}");
            return null;
        }

        GDLspPerformanceTrace.Log("highlight", $"FIND-DEF-DONE {filename} symbol={definition?.SymbolName} isInfo={definition?.IsInfoOnly}");

        if (definition == null || string.IsNullOrEmpty(definition.SymbolName))
        {
            GDLspPerformanceTrace.Log("highlight", $"END {filename} (no definition)");
            return null;
        }

        if (definition.IsInfoOnly)
        {
            GDLspPerformanceTrace.Log("highlight", $"END {filename} (info only)");
            return null;
        }

        var symbolName = definition.SymbolName;

        // Use per-file semantic model instead of project-wide FindReferences
        var script = _project.GetScript(filePath);
        if (script == null)
        {
            GDLspPerformanceTrace.Log("highlight", $"END {filename} (no script)");
            return null;
        }

        var model = _projectModel?.GetSemanticModel(script) ?? script.SemanticModel;
        if (model == null)
        {
            GDLspPerformanceTrace.Log("highlight", $"END {filename} (no model)");
            return null;
        }

        var refs = model.GetReferencesTo(symbolName);
        var highlights = new List<GDDocumentHighlight>();

        // Add declaration
        var symbol = model.FindSymbol(symbolName);
        if (symbol?.DeclarationNode != null)
        {
            var declNode = symbol.DeclarationNode;
            var declLine = declNode.StartLine;
            var declCol = declNode.StartColumn;
            highlights.Add(new GDDocumentHighlight
            {
                Range = GDLocationAdapter.ToLspRange(
                    declLine + 1,
                    declCol,
                    declLine + 1,
                    declCol + symbolName.Length),
                Kind = GDDocumentHighlightKind.Write
            });
        }

        // Add references
        foreach (var r in refs)
        {
            if (r.ReferenceNode == null)
                continue;

            var refLine = r.IdentifierToken?.StartLine ?? r.ReferenceNode.StartLine;
            var refCol = r.IdentifierToken?.StartColumn ?? r.ReferenceNode.StartColumn;

            var kind = r.IsWrite
                ? GDDocumentHighlightKind.Write
                : GDDocumentHighlightKind.Read;

            highlights.Add(new GDDocumentHighlight
            {
                Range = GDLocationAdapter.ToLspRange(
                    refLine + 1,
                    refCol,
                    refLine + 1,
                    refCol + symbolName.Length),
                Kind = kind
            });
        }

        GDLspPerformanceTrace.Log("highlight", $"END {filename} count={highlights.Count}");

        if (highlights.Count == 0)
            return null;

        return highlights.ToArray();
    }
}
