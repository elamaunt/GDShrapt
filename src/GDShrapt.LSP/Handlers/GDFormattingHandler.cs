using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/formatting requests.
/// </summary>
public class GDFormattingHandler
{
    private readonly GDScriptProject _project;

    public GDFormattingHandler(GDScriptProject project)
    {
        _project = project;
    }

    public Task<GDLspTextEdit[]?> HandleAsync(GDDocumentFormattingParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        var script = _project.GetScript(filePath);

        if (script?.Class == null)
            return Task.FromResult<GDLspTextEdit[]?>(null);

        // Get the current content from AST
        var content = script.Class.ToString();
        if (string.IsNullOrEmpty(content))
            return Task.FromResult<GDLspTextEdit[]?>(null);

        // Convert LSP formatting options to GDFormatterOptions
        var formatterOptions = ConvertOptions(@params.Options);

        // Create formatter and format the code
        var formatter = new GDFormatter(formatterOptions);
        string formatted;
        try
        {
            formatted = formatter.FormatCode(content);
        }
        catch
        {
            // If formatting fails, return null (no changes)
            return Task.FromResult<GDLspTextEdit[]?>(null);
        }

        // If content is unchanged, return empty array
        if (content == formatted)
            return Task.FromResult<GDLspTextEdit[]?>([]);

        // Count lines in original content to determine the range
        var lineCount = CountLines(content);

        // Return a single edit replacing the entire document
        var edit = new GDLspTextEdit
        {
            Range = new GDLspRange(0, 0, lineCount, 0),
            NewText = formatted
        };

        return Task.FromResult<GDLspTextEdit[]?>([edit]);
    }

    private static GDFormatterOptions ConvertOptions(GDFormattingOptions lspOptions)
    {
        var options = new GDFormatterOptions
        {
            IndentSize = lspOptions.TabSize,
            IndentStyle = lspOptions.InsertSpaces ? IndentStyle.Spaces : IndentStyle.Tabs
        };

        if (lspOptions.TrimTrailingWhitespace.HasValue)
        {
            options.RemoveTrailingWhitespace = lspOptions.TrimTrailingWhitespace.Value;
        }

        if (lspOptions.InsertFinalNewline.HasValue)
        {
            options.EnsureTrailingNewline = lspOptions.InsertFinalNewline.Value;
        }

        if (lspOptions.TrimFinalNewlines.HasValue)
        {
            options.RemoveMultipleTrailingNewlines = lspOptions.TrimFinalNewlines.Value;
        }

        return options;
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
