using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/inlayHint requests.
/// Thin wrapper over IGDInlayHintHandler from CLI.Core.
/// </summary>
public class GDLspInlayHintHandler
{
    private readonly IGDInlayHintHandler _handler;

    public GDLspInlayHintHandler(IGDInlayHintHandler handler)
    {
        _handler = handler;
    }

    public Task<GDLspInlayHint[]?> HandleAsync(GDInlayHintParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        // Convert LSP 0-based to CLI.Core 1-based
        var startLine = @params.Range.Start.Line + 1;
        var endLine = @params.Range.End.Line + 1;

        // Delegate to CLI.Core handler
        var result = _handler.GetInlayHints(filePath, startLine, endLine);
        if (result == null || result.Count == 0)
            return Task.FromResult<GDLspInlayHint[]?>(null);

        // Convert CLI.Core hints to LSP inlay hints
        var hints = new List<GDLspInlayHint>();
        foreach (var hint in result)
        {
            hints.Add(ConvertToLspHint(hint));
        }

        return Task.FromResult<GDLspInlayHint[]?>(hints.ToArray());
    }

    private static GDLspInlayHint ConvertToLspHint(GDInlayHint hint)
    {
        return new GDLspInlayHint
        {
            Position = new GDLspPosition
            {
                Line = hint.Line - 1,  // Convert 1-based to 0-based
                Character = hint.Column - 1
            },
            Label = hint.Label,
            Kind = ConvertHintKind(hint.Kind),
            PaddingLeft = hint.PaddingLeft,
            PaddingRight = hint.PaddingRight,
            Tooltip = hint.Tooltip
        };
    }

    private static int ConvertHintKind(CLI.Core.GDInlayHintKind kind)
    {
        return kind switch
        {
            CLI.Core.GDInlayHintKind.Type => GDInlayHintKind.Type,
            CLI.Core.GDInlayHintKind.Parameter => GDInlayHintKind.Parameter,
            _ => GDInlayHintKind.Type
        };
    }
}
