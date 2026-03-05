using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/typeDefinition requests.
/// Thin wrapper over IGDTypeDefinitionHandler from CLI.Core.
/// </summary>
public class GDTypeDefinitionLspHandler
{
    private readonly IGDTypeDefinitionHandler _handler;

    public GDTypeDefinitionLspHandler(IGDTypeDefinitionHandler handler)
    {
        _handler = handler;
    }

    public Task<(GDLspLocationLink[]? Links, string? InfoMessage)> HandleAsync(
        GDDefinitionParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        var result = _handler.FindTypeDefinition(filePath, line, column);
        if (result == null)
            return Task.FromResult<(GDLspLocationLink[]?, string?)>((null, null));

        if (result.IsInfoOnly)
            return Task.FromResult<(GDLspLocationLink[]?, string?)>((null, result.InfoMessage));

        var link = new GDLspLocationLink
        {
            TargetUri = GDDocumentManager.PathToUri(result.FilePath),
            TargetRange = GDLocationAdapter.ToLspRange(result.Line, result.Column, result.Line, result.Column + (result.SymbolName?.Length ?? 0)),
            TargetSelectionRange = GDLocationAdapter.ToLspRange(result.Line, result.Column, result.Line, result.Column + (result.SymbolName?.Length ?? 0))
        };

        return Task.FromResult<(GDLspLocationLink[]?, string?)>((new[] { link }, null));
    }
}
