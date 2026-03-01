using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/definition requests.
/// Thin wrapper over IGDGoToDefHandler from CLI.Core.
/// Returns LocationLink[] to enable symbol selection at target.
/// </summary>
public class GDDefinitionHandler
{
    private readonly IGDGoToDefHandler _handler;

    public GDDefinitionHandler(IGDGoToDefHandler handler)
    {
        _handler = handler;
    }

    public Task<(GDLspLocationLink[]? Links, string? InfoMessage)> HandleAsync(GDDefinitionParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        var result = _handler.FindDefinition(filePath, line, column);
        if (result == null)
            return Task.FromResult<(GDLspLocationLink[]?, string?)>((null, null));

        if (result.IsInfoOnly)
            return Task.FromResult<(GDLspLocationLink[]?, string?)>((null, result.InfoMessage));

        var symbolLength = result.SymbolName?.Length ?? 0;

        var selectionRange = GDLocationAdapter.ToLspRange(
            result.Line,
            result.Column,
            result.Line,
            result.Column + symbolLength);

        var link = new GDLspLocationLink
        {
            TargetUri = GDDocumentManager.PathToUri(result.FilePath),
            TargetRange = selectionRange,
            TargetSelectionRange = selectionRange
        };

        return Task.FromResult<(GDLspLocationLink[]?, string?)>((new[] { link }, null));
    }
}
