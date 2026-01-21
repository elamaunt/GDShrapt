using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/definition requests.
/// Thin wrapper over IGDGoToDefHandler from CLI.Core.
/// </summary>
public class GDDefinitionHandler
{
    private readonly IGDGoToDefHandler _handler;

    public GDDefinitionHandler(IGDGoToDefHandler handler)
    {
        _handler = handler;
    }

    public Task<GDLspLocation?> HandleAsync(GDDefinitionParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        // Convert LSP 0-based to CLI.Core 1-based
        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        // Delegate to CLI.Core handler
        var result = _handler.FindDefinition(filePath, line, column);
        if (result == null)
            return Task.FromResult<GDLspLocation?>(null);

        // Convert CLI.Core 1-based to LSP 0-based
        var location = new GDLspLocation
        {
            Uri = GDDocumentManager.PathToUri(result.FilePath),
            Range = GDLocationAdapter.ToLspRange(
                result.Line,
                result.Column,
                result.Line,
                result.Column + (result.SymbolName?.Length ?? 0))
        };

        return Task.FromResult<GDLspLocation?>(location);
    }
}
