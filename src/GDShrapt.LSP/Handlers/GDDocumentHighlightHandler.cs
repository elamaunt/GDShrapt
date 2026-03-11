using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/documentHighlight requests.
/// Thin wrapper over IGDHighlightHandler from CLI.Core.
/// </summary>
public class GDLspDocumentHighlightHandler
{
    private static readonly TimeSpan FindDefTimeout = TimeSpan.FromMilliseconds(500);

    private readonly IGDHighlightHandler _highlightHandler;
    private readonly IGDGoToDefHandler _goToDefHandler;

    public GDLspDocumentHighlightHandler(IGDHighlightHandler highlightHandler, IGDGoToDefHandler goToDefHandler)
    {
        _highlightHandler = highlightHandler;
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

        var highlights = _highlightHandler.GetHighlights(filePath, symbolName);
        if (highlights.Count == 0)
        {
            GDLspPerformanceTrace.Log("highlight", $"END {filename} (no highlights)");
            return null;
        }

        var result = new List<GDDocumentHighlight>(highlights.Count);
        foreach (var h in highlights)
        {
            result.Add(new GDDocumentHighlight
            {
                Range = GDLocationAdapter.ToLspRange(
                    h.Line,
                    h.Column,
                    h.Line,
                    h.Column + h.Length),
                Kind = h.IsWrite ? GDDocumentHighlightKind.Write : GDDocumentHighlightKind.Read
            });
        }

        GDLspPerformanceTrace.Log("highlight", $"END {filename} count={result.Count}");

        return result.ToArray();
    }
}
