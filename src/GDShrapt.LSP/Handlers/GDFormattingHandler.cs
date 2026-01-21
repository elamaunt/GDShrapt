using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

using GDIndentationStyle = GDShrapt.Semantics.GDIndentationStyle;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/formatting requests.
/// Thin wrapper over IGDFormatHandler from CLI.Core.
/// </summary>
public class GDFormattingHandler
{
    private readonly IGDFormatHandler _handler;

    public GDFormattingHandler(IGDFormatHandler handler)
    {
        _handler = handler;
    }

    public Task<GDLspTextEdit[]?> HandleAsync(GDDocumentFormattingParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        // Convert LSP formatting options to CLI.Core format config
        var config = ConvertOptions(@params.Options);

        // Delegate to CLI.Core handler
        var formatted = _handler.Format(filePath, config);
        if (formatted == null)
            return Task.FromResult<GDLspTextEdit[]?>(null);

        // Read original content to check if unchanged
        var original = System.IO.File.ReadAllText(filePath);
        if (original == formatted)
            return Task.FromResult<GDLspTextEdit[]?>([]);

        // Count lines in original content to determine the range
        var lineCount = CountLines(original);

        // Return a single edit replacing the entire document
        var edit = new GDLspTextEdit
        {
            Range = new GDLspRange(0, 0, lineCount, 0),
            NewText = formatted
        };

        return Task.FromResult<GDLspTextEdit[]?>([edit]);
    }

    private static GDFormatterConfig ConvertOptions(GDFormattingOptions lspOptions)
    {
        var config = new GDFormatterConfig
        {
            IndentSize = lspOptions.TabSize,
            IndentStyle = lspOptions.InsertSpaces ? GDIndentationStyle.Spaces : GDIndentationStyle.Tabs
        };

        return config;
    }

    private static int CountLines(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        var count = 1;
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '\n')
                count++;
        }
        return count;
    }
}
