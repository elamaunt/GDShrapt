using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

public class GDDocumentHighlightHandler
{
    private readonly IGDFindRefsHandler _findRefsHandler;
    private readonly IGDGoToDefHandler _goToDefHandler;

    public GDDocumentHighlightHandler(IGDFindRefsHandler findRefsHandler, IGDGoToDefHandler goToDefHandler)
    {
        _findRefsHandler = findRefsHandler;
        _goToDefHandler = goToDefHandler;
    }

    public Task<GDDocumentHighlight[]?> HandleAsync(GDDocumentHighlightParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        var normalizedFilePath = NormalizePath(filePath);

        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        var definition = _goToDefHandler.FindDefinition(filePath, line, column);
        if (definition == null || string.IsNullOrEmpty(definition.SymbolName))
            return Task.FromResult<GDDocumentHighlight[]?>(null);

        if (definition.IsInfoOnly)
            return Task.FromResult<GDDocumentHighlight[]?>(null);

        var symbolName = definition.SymbolName;

        var groups = _findRefsHandler.FindReferences(symbolName, filePath);

        var highlights = new List<GDDocumentHighlight>();

        if (groups != null && groups.Count > 0)
        {
            foreach (var reference in FlattenLocations(groups))
            {
                if (!NormalizePath(reference.FilePath).Equals(normalizedFilePath, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (reference.Confidence != null && reference.Confidence != GDReferenceConfidence.Strict)
                    continue;

                var kind = reference.IsWrite || reference.IsDeclaration
                    ? GDDocumentHighlightKind.Write
                    : GDDocumentHighlightKind.Read;

                var col0 = reference.Column - 1;
                highlights.Add(new GDDocumentHighlight
                {
                    Range = GDLocationAdapter.ToLspRange(
                        reference.Line,
                        col0,
                        reference.Line,
                        col0 + symbolName.Length),
                    Kind = kind
                });
            }
        }

        if (highlights.Count == 0)
            return Task.FromResult<GDDocumentHighlight[]?>(null);

        return Task.FromResult<GDDocumentHighlight[]?>(highlights.ToArray());
    }

    private static string NormalizePath(string path) =>
        path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

    private static IEnumerable<GDCliReferenceLocation> FlattenLocations(IEnumerable<GDReferenceGroup> groups)
    {
        foreach (var group in groups)
        {
            foreach (var loc in group.Locations)
                yield return loc;

            foreach (var loc in FlattenLocations(group.Overrides))
                yield return loc;
        }
    }
}
