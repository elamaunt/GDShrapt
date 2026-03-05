using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/implementation requests.
/// Thin wrapper over IGDImplementationHandler from CLI.Core.
/// </summary>
public class GDImplementationLspHandler
{
    private readonly IGDImplementationHandler _handler;

    public GDImplementationLspHandler(IGDImplementationHandler handler)
    {
        _handler = handler;
    }

    public Task<GDLspLocation[]?> HandleAsync(GDDefinitionParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        var results = _handler.FindImplementations(filePath, line, column);
        if (results.Count == 0)
            return Task.FromResult<GDLspLocation[]?>(null);

        var locations = results.Select(r => new GDLspLocation
        {
            Uri = GDDocumentManager.PathToUri(r.FilePath),
            Range = GDLocationAdapter.ToLspRange(
                r.Line, r.Column, r.Line, r.Column + (r.SymbolName?.Length ?? 0))
        }).ToArray();

        return Task.FromResult<GDLspLocation[]?>(locations);
    }
}
