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
    private readonly GDProjectConfig? _config;

    public GDFormattingHandler(IGDFormatHandler handler, GDProjectConfig? config = null)
    {
        _handler = handler;
        _config = config;
    }

    public Task<GDLspTextEdit[]?> HandleAsync(GDDocumentFormattingParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        // Merge project config with LSP editor settings (LSP indent params take priority)
        var config = MergeOptions(@params.Options);

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

    private GDFormatterConfig MergeOptions(GDFormattingOptions lspOptions)
    {
        var config = _config?.Formatter ?? new GDFormatterConfig();

        // LSP editor settings override project config
        config.IndentSize = lspOptions.TabSize;
        config.IndentStyle = lspOptions.InsertSpaces ? GDIndentationStyle.Spaces : GDIndentationStyle.Tabs;

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
