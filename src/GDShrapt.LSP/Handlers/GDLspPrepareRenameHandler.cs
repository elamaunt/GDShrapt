using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

public class GDLspPrepareRenameHandler
{
    private readonly IGDGoToDefHandler _goToDefHandler;

    public GDLspPrepareRenameHandler(IGDGoToDefHandler goToDefHandler)
    {
        _goToDefHandler = goToDefHandler;
    }

    public Task<GDPrepareRenameResult?> HandleAsync(GDPrepareRenameParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        // Convert LSP 0-based to CLI.Core 1-based
        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        var definition = _goToDefHandler.FindDefinition(filePath, line, column);
        if (definition == null || string.IsNullOrEmpty(definition.SymbolName))
            return Task.FromResult<GDPrepareRenameResult?>(null);

        var symbolName = definition.SymbolName;

        // Build range for the symbol at the cursor position
        var range = GDLocationAdapter.ToLspRange(
            line,                              // 1-based line
            column - 1,                        // Convert 1-based column back to 0-based for range start
            line,
            column - 1 + symbolName.Length);

        return Task.FromResult<GDPrepareRenameResult?>(new GDPrepareRenameResult
        {
            Range = range,
            Placeholder = symbolName
        });
    }
}
