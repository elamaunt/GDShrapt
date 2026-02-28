using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/documentSymbol requests.
/// Thin wrapper over IGDSymbolsHandler from CLI.Core.
/// </summary>
public class GDDocumentSymbolHandler
{
    private readonly IGDSymbolsHandler _handler;

    public GDDocumentSymbolHandler(IGDSymbolsHandler handler)
    {
        _handler = handler;
    }

    public Task<GDLspDocumentSymbol[]?> HandleAsync(GDDocumentSymbolParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        // Delegate to CLI.Core handler
        var result = _handler.GetSymbols(filePath);
        if (result == null || result.Count == 0)
            return Task.FromResult<GDLspDocumentSymbol[]?>(null);

        // Convert CLI.Core symbols to LSP document symbols
        var symbols = new List<GDLspDocumentSymbol>();
        foreach (var symbol in result)
        {
            symbols.Add(ConvertToLspSymbol(symbol));
        }

        return Task.FromResult<GDLspDocumentSymbol[]?>(symbols.ToArray());
    }

    private static GDLspDocumentSymbol ConvertToLspSymbol(GDDocumentSymbol symbol)
    {
        // Create range using line/column info (convert 1-based to 0-based, clamp to >= 0)
        var line = Math.Max(0, symbol.Line - 1);
        var column = Math.Max(0, symbol.Column - 1);

        var lspSymbol = new GDLspDocumentSymbol
        {
            Name = symbol.Name,
            Kind = ConvertSymbolKind(symbol.Kind),
            Detail = symbol.Type,
            Range = new GDLspRange(line, 0, line, column + symbol.Name.Length),
            SelectionRange = new GDLspRange(line, column, line, column + symbol.Name.Length)
        };

        // Convert children
        if (symbol.Children != null && symbol.Children.Count > 0)
        {
            var children = new List<GDLspDocumentSymbol>();
            foreach (var child in symbol.Children)
            {
                children.Add(ConvertToLspSymbol(child));
            }
            lspSymbol.Children = children.ToArray();
        }

        return lspSymbol;
    }

    private static GDLspSymbolKind ConvertSymbolKind(GDSymbolKind kind)
    {
        return kind switch
        {
            GDSymbolKind.Class => GDLspSymbolKind.Class,
            GDSymbolKind.Method => GDLspSymbolKind.Method,
            GDSymbolKind.Variable => GDLspSymbolKind.Variable,
            GDSymbolKind.Constant => GDLspSymbolKind.Constant,
            GDSymbolKind.Signal => GDLspSymbolKind.Event,
            GDSymbolKind.Enum => GDLspSymbolKind.Enum,
            GDSymbolKind.EnumValue => GDLspSymbolKind.EnumMember,
            _ => GDLspSymbolKind.Variable
        };
    }
}
