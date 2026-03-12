using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles gdshrapt/codeLensReferences custom request.
/// Returns cached reference locations from CodeLens computation,
/// avoiding expensive re-computation when clicking on a CodeLens link.
/// </summary>
public class GDCodeLensReferencesHandler
{
    private readonly IGDCodeLensHandler _codeLensHandler;
    private readonly IGDFindRefsHandler _findRefsHandler;

    public GDCodeLensReferencesHandler(IGDCodeLensHandler codeLensHandler, IGDFindRefsHandler findRefsHandler)
    {
        _codeLensHandler = codeLensHandler;
        _findRefsHandler = findRefsHandler;
    }

    public Task<GDLspLocation[]?> HandleAsync(GDCodeLensReferencesParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.Uri);
        var symbolName = @params.SymbolName;

        if (string.IsNullOrEmpty(symbolName))
            return Task.FromResult<GDLspLocation[]?>(null);

        var cached = _codeLensHandler.GetCachedReferences(symbolName, filePath);
        if (cached != null && cached.Count > 0)
        {
            var locations = new List<GDLspLocation>(cached.Count);
            foreach (var r in cached)
            {
                locations.Add(new GDLspLocation
                {
                    Uri = GDDocumentManager.PathToUri(r.FilePath),
                    Range = GDLocationAdapter.ToLspRange(
                        r.Line,
                        r.Column - 1,
                        r.Line,
                        r.EndColumn - 1)
                });
            }
            return Task.FromResult<GDLspLocation[]?>(locations.ToArray());
        }

        // Unpack signal connection key: "signal:methodName@lineNumber" → methodName
        var lookupName = symbolName;
        if (symbolName.StartsWith("signal:", StringComparison.Ordinal))
        {
            var atIdx = symbolName.IndexOf('@', 7);
            lookupName = atIdx > 7 ? symbolName.Substring(7, atIdx - 7) : symbolName.Substring(7);
        }

        // Fallback: cache miss, compute fresh
        var groups = _findRefsHandler.FindReferences(lookupName, filePath);
        if (groups == null || groups.Count == 0)
            return Task.FromResult<GDLspLocation[]?>(null);

        var result = new List<GDLspLocation>();
        foreach (var group in groups)
        {
            AddGroupLocations(group, lookupName, result);
        }

        return Task.FromResult<GDLspLocation[]?>(result.ToArray());
    }

    private static void AddGroupLocations(GDReferenceGroup group, string symbolName, List<GDLspLocation> result)
    {
        foreach (var loc in group.Locations)
        {
            if (loc.Confidence != null &&
                loc.Confidence != Abstractions.GDReferenceConfidence.Strict &&
                loc.Confidence != Abstractions.GDReferenceConfidence.Union)
                continue;

            var col0 = loc.Column - 1;
            result.Add(new GDLspLocation
            {
                Uri = GDDocumentManager.PathToUri(loc.FilePath),
                Range = GDLocationAdapter.ToLspRange(
                    loc.Line,
                    col0,
                    loc.Line,
                    col0 + symbolName.Length)
            });
        }

        foreach (var ovr in group.Overrides)
            AddGroupLocations(ovr, symbolName, result);
    }
}
