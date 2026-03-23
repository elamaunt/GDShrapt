using System.Linq;
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

        var results = _handler.FindDefinitions(filePath, line, column);

        if (results.Count == 0)
            return Task.FromResult<(GDLspLocationLink[]?, string?)>((null, null));

        if (results.Count == 1 && results[0].IsInfoOnly)
            return Task.FromResult<(GDLspLocationLink[]?, string?)>((null, results[0].InfoMessage));

        var links = new System.Collections.Generic.List<GDLspLocationLink>();
        foreach (var result in results)
        {
            if (result.IsInfoOnly || string.IsNullOrEmpty(result.FilePath))
                continue;

            var symbolLength = result.SymbolName?.Length ?? 0;
            var selectionRange = GDLocationAdapter.ToLspRange(
                result.Line,
                result.Column,
                result.Line,
                result.Column + symbolLength);

            links.Add(new GDLspLocationLink
            {
                TargetUri = GDDocumentManager.PathToUri(result.FilePath),
                TargetRange = selectionRange,
                TargetSelectionRange = selectionRange
            });
        }

        if (links.Count == 0)
        {
            var infoResult = results.FirstOrDefault(r => r.IsInfoOnly);
            return Task.FromResult<(GDLspLocationLink[]?, string?)>((null, infoResult?.InfoMessage));
        }

        return Task.FromResult<(GDLspLocationLink[]?, string?)>((links.ToArray(), null));
    }
}
