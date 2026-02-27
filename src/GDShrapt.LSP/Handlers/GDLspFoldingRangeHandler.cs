using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

public class GDLspFoldingRangeHandler
{
    private readonly IGDFoldingRangeHandler _handler;

    public GDLspFoldingRangeHandler(IGDFoldingRangeHandler handler)
    {
        _handler = handler;
    }

    public Task<GDFoldingRange[]?> HandleAsync(GDFoldingRangeParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        var regions = _handler.GetFoldingRegions(filePath);
        if (regions == null || regions.Count == 0)
            return Task.FromResult<GDFoldingRange[]?>(null);

        // AST positions are 0-based, LSP foldingRange is 0-based — no conversion needed
        var foldingRanges = new List<GDFoldingRange>();
        foreach (var region in regions)
        {
            foldingRanges.Add(new GDFoldingRange
            {
                StartLine = region.StartLine,
                StartCharacter = region.StartColumn,
                EndLine = region.EndLine,
                EndCharacter = region.EndColumn,
                Kind = region.Kind
            });
        }

        return Task.FromResult<GDFoldingRange[]?>(foldingRanges.ToArray());
    }
}
