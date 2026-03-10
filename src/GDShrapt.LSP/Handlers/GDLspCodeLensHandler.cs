using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/codeLens requests.
/// Thin wrapper over IGDCodeLensHandler from CLI.Core.
/// </summary>
public class GDLspCodeLensHandler
{
    private readonly IGDCodeLensHandler _handler;

    public GDLspCodeLensHandler(IGDCodeLensHandler handler)
    {
        _handler = handler;
    }

    public Task<GDLspCodeLens[]?> HandleAsync(GDCodeLensParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        var result = _handler.GetCodeLenses(filePath);
        if (result == null || result.Count == 0)
            return Task.FromResult<GDLspCodeLens[]?>(null);

        var uri = @params.TextDocument.Uri;
        var lenses = new List<GDLspCodeLens>();
        foreach (var lens in result)
        {
            lenses.Add(ConvertToLspCodeLens(lens, uri));
        }

        return Task.FromResult<GDLspCodeLens[]?>(lenses.ToArray());
    }

    private static GDLspCodeLens ConvertToLspCodeLens(GDCodeLens lens, string uri)
    {
        var line = lens.Line - 1;
        var character = lens.StartColumn - 1;

        return new GDLspCodeLens
        {
            Range = new GDLspRange
            {
                Start = new GDLspPosition
                {
                    Line = line,
                    Character = character
                },
                End = new GDLspPosition
                {
                    Line = line,
                    Character = lens.EndColumn - 1
                }
            },
            Command = new GDLspCommand
            {
                Title = lens.Label,
                Command = lens.CommandName ?? "",
                Arguments = [uri, line, character, lens.CommandArgument ?? ""]
            }
        };
    }
}
