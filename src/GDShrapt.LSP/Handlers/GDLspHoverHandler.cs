using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/hover requests.
/// Thin wrapper over IGDHoverHandler from CLI.Core.
/// </summary>
public class GDLspHoverHandler
{
    private readonly IGDHoverHandler _handler;

    public GDLspHoverHandler(IGDHoverHandler handler)
    {
        _handler = handler;
    }

    public async Task<GDLspHover?> HandleAsync(GDHoverParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        // Convert LSP 0-based to CLI.Core 1-based
        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        // Run on thread pool with cancellation support to avoid blocking LSP thread
        var result = await Task.Run(() => _handler.GetHover(filePath, line, column), cancellationToken)
            .ConfigureAwait(false);

        if (result == null)
            return null;

        // Convert CLI.Core result to LSP hover
        var hover = new GDLspHover
        {
            Contents = GDLspMarkupContent.Markdown(result.Content)
        };

        // Set range if available
        if (result.StartLine.HasValue && result.StartColumn.HasValue &&
            result.EndLine.HasValue && result.EndColumn.HasValue)
        {
            hover.Range = GDLocationAdapter.ToLspRange(
                result.StartLine.Value,
                result.StartColumn.Value,
                result.EndLine.Value,
                result.EndColumn.Value);
        }

        return hover;
    }
}
