using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/completion requests.
/// Thin wrapper over IGDCompletionHandler from CLI.Core.
/// </summary>
public class GDLspCompletionHandler
{
    private readonly IGDCompletionHandler _handler;
    private readonly GDDocumentManager? _documentManager;

    public GDLspCompletionHandler(IGDCompletionHandler handler, GDDocumentManager? documentManager = null)
    {
        _handler = handler;
        _documentManager = documentManager;
    }

    public Task<GDLspCompletionList?> HandleAsync(GDCompletionParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        // Convert LSP 0-based to CLI.Core 1-based
        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        // Determine trigger context
        var triggerKind = @params.Context?.TriggerKind ?? GDLspCompletionTriggerKind.Invoked;
        var triggerChar = @params.Context?.TriggerCharacter;

        // Delegate to CLI.Core handler
        IReadOnlyList<GDCompletionItem> items;

        if (triggerKind == GDLspCompletionTriggerKind.TriggerCharacter && triggerChar == ".")
        {
            // Extract expression before dot from document content
            var expression = ExtractExpressionBeforeDot(@params.TextDocument.Uri, @params.Position.Line, @params.Position.Character);

            var request = new GDCompletionRequest
            {
                FilePath = filePath,
                Line = line,
                Column = column,
                CompletionType = GDCompletionType.MemberAccess,
                MemberAccessExpression = expression
            };
            items = _handler.GetCompletions(request);
        }
        else if (triggerKind == GDLspCompletionTriggerKind.TriggerCharacter && triggerChar == "$")
        {
            var request = new GDCompletionRequest
            {
                FilePath = filePath,
                Line = line,
                Column = column,
                CompletionType = GDCompletionType.NodePath,
                NodePathPrefix = "$"
            };
            items = _handler.GetCompletions(request);
        }
        else if (triggerKind == GDLspCompletionTriggerKind.TriggerCharacter && triggerChar == "/")
        {
            // Check if we're in a $NodePath/ context
            var nodePathPrefix = ExtractNodePathBeforeSlash(@params.TextDocument.Uri, @params.Position.Line, @params.Position.Character);
            if (nodePathPrefix != null)
            {
                var request = new GDCompletionRequest
                {
                    FilePath = filePath,
                    Line = line,
                    Column = column,
                    CompletionType = GDCompletionType.NodePath,
                    NodePathPrefix = nodePathPrefix
                };
                items = _handler.GetCompletions(request);
            }
            else
            {
                items = [];
            }
        }
        else
        {
            // General completion
            var request = new GDCompletionRequest
            {
                FilePath = filePath,
                Line = line,
                Column = column,
                CompletionType = GDCompletionType.Symbol
            };
            items = _handler.GetCompletions(request);
        }

        if (items.Count == 0)
        {
            // Return empty list instead of null to indicate "no completions" vs "error"
            return Task.FromResult<GDLspCompletionList?>(new GDLspCompletionList
            {
                IsIncomplete = false,
                Items = []
            });
        }

        // Convert CLI.Core items to LSP completion items
        var lspItems = new List<GDLspCompletionItem>();
        foreach (var item in items)
        {
            lspItems.Add(ConvertToLspItem(item));
        }

        var result = new GDLspCompletionList
        {
            IsIncomplete = false,
            Items = lspItems.ToArray()
        };

        return Task.FromResult<GDLspCompletionList?>(result);
    }

    private string? ExtractExpressionBeforeDot(string uri, int line, int character)
    {
        if (_documentManager == null)
            return null;

        var doc = _documentManager.GetDocument(uri);
        if (doc == null)
            return null;

        var lines = doc.Content.Split('\n');
        if (line < 0 || line >= lines.Length)
            return null;

        var lineText = lines[line];
        // character points to the dot position
        var dotPos = character;
        if (dotPos <= 0 || dotPos > lineText.Length)
            return null;

        // Walk backwards from dot to find the expression
        var end = dotPos - 1;
        // Skip trailing whitespace
        while (end >= 0 && lineText[end] == ' ')
            end--;

        if (end < 0)
            return null;

        // Handle closing brackets/parens
        int depth = 0;
        var pos = end;
        while (pos >= 0)
        {
            var ch = lineText[pos];
            if (ch == ')' || ch == ']')
            {
                depth++;
                pos--;
            }
            else if (ch == '(' || ch == '[')
            {
                depth--;
                if (depth < 0)
                    break;
                pos--;
            }
            else if (depth > 0)
            {
                pos--;
            }
            else if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                pos--;
            }
            else if (ch == '.')
            {
                // Chained access: keep going
                pos--;
            }
            else
            {
                break;
            }
        }

        pos++;
        if (pos > end)
            return null;

        var expr = lineText.Substring(pos, end - pos + 1).Trim();
        return string.IsNullOrEmpty(expr) ? null : expr;
    }

    private string? ExtractNodePathBeforeSlash(string uri, int line, int character)
    {
        if (_documentManager == null)
            return null;

        var doc = _documentManager.GetDocument(uri);
        if (doc == null)
            return null;

        var lines = doc.Content.Split('\n');
        if (line < 0 || line >= lines.Length)
            return null;

        var lineText = lines[line];
        var slashPos = character;
        if (slashPos <= 0 || slashPos > lineText.Length)
            return null;

        // Walk backwards from / to find $ prefix
        var pos = slashPos - 1;
        while (pos >= 0)
        {
            var ch = lineText[pos];
            if (ch == '$')
                return lineText.Substring(pos + 1, slashPos - pos - 1);
            if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '/')
                pos--;
            else
                break;
        }

        return null;
    }

    private static GDLspCompletionItem ConvertToLspItem(GDCompletionItem item)
    {
        var lspItem = new GDLspCompletionItem
        {
            Label = item.Label,
            Kind = ConvertItemKind(item.Kind),
            Detail = item.Detail,
            InsertText = item.InsertText,
            // Convert SortPriority (int) to SortText (string) - lower priority = higher in list
            SortText = $"{(item.Preselect ? "0" : "1")}_{item.SortPriority:D3}_{item.Label}"
        };

        if (item.IsSnippet)
            lspItem.InsertTextFormat = GDLspInsertTextFormat.Snippet;

        if (item.Preselect)
            lspItem.Preselect = true;

        // Commit characters based on kind
        if (item.Kind == GDCompletionItemKind.Method)
            lspItem.CommitCharacters = ["("];
        else if (item.Kind == GDCompletionItemKind.Variable || item.Kind == GDCompletionItemKind.Property)
            lspItem.CommitCharacters = ["."];

        // Label details for method signatures
        if (!string.IsNullOrEmpty(item.Documentation) && item.Kind == GDCompletionItemKind.Method)
        {
            lspItem.LabelDetails = new GDLspCompletionItemLabelDetails
            {
                Detail = $"({item.Documentation})"
            };
        }

        return lspItem;
    }

    private static GDLspCompletionItemKind ConvertItemKind(GDCompletionItemKind kind)
    {
        return kind switch
        {
            GDCompletionItemKind.Method => GDLspCompletionItemKind.Method,
            GDCompletionItemKind.Function => GDLspCompletionItemKind.Function,
            GDCompletionItemKind.Variable => GDLspCompletionItemKind.Variable,
            GDCompletionItemKind.Constant => GDLspCompletionItemKind.Constant,
            GDCompletionItemKind.Class => GDLspCompletionItemKind.Class,
            GDCompletionItemKind.Enum => GDLspCompletionItemKind.Enum,
            GDCompletionItemKind.EnumMember => GDLspCompletionItemKind.EnumMember,
            GDCompletionItemKind.Property => GDLspCompletionItemKind.Property,
            GDCompletionItemKind.Field => GDLspCompletionItemKind.Field,
            GDCompletionItemKind.Event => GDLspCompletionItemKind.Event,
            GDCompletionItemKind.Keyword => GDLspCompletionItemKind.Keyword,
            GDCompletionItemKind.Snippet => GDLspCompletionItemKind.Snippet,
            _ => GDLspCompletionItemKind.Text
        };
    }
}
