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

    public GDLspCompletionHandler(IGDCompletionHandler handler)
    {
        _handler = handler;
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
            // Member access completion - need to determine the type from context
            // For now, use general completion which handles member access internally
            var request = new GDCompletionRequest
            {
                FilePath = filePath,
                Line = line,
                Column = column,
                CompletionType = GDCompletionType.MemberAccess
            };
            items = _handler.GetCompletions(request);
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

    private static GDLspCompletionItem ConvertToLspItem(GDCompletionItem item)
    {
        return new GDLspCompletionItem
        {
            Label = item.Label,
            Kind = ConvertItemKind(item.Kind),
            Detail = item.Detail,
            InsertText = item.InsertText,
            // Convert SortPriority (int) to SortText (string) - lower priority = higher in list
            SortText = item.SortPriority.ToString("D5")
        };
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
