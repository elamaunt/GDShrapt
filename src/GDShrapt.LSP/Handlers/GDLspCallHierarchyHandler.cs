using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles call hierarchy LSP requests.
/// Thin wrapper over IGDCallHierarchyHandler from CLI.Core.
/// </summary>
public class GDLspCallHierarchyHandler
{
    private readonly IGDCallHierarchyHandler _handler;

    public GDLspCallHierarchyHandler(IGDCallHierarchyHandler handler)
    {
        _handler = handler;
    }

    public Task<GDLspCallHierarchyItem[]?> HandlePrepareAsync(
        GDCallHierarchyPrepareParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        var item = _handler.Prepare(filePath, line, column);
        if (item == null)
            return Task.FromResult<GDLspCallHierarchyItem[]?>(null);

        var lspItem = ConvertToLspItem(item);
        return Task.FromResult<GDLspCallHierarchyItem[]?>(new[] { lspItem });
    }

    public Task<GDLspCallHierarchyIncomingCall[]?> HandleIncomingCallsAsync(
        GDCallHierarchyIncomingCallsParams @params, CancellationToken cancellationToken)
    {
        var coreItem = ConvertFromLspItem(@params.Item);
        if (coreItem == null)
            return Task.FromResult<GDLspCallHierarchyIncomingCall[]?>(null);

        var calls = _handler.GetIncomingCalls(coreItem);
        if (calls.Count == 0)
            return Task.FromResult<GDLspCallHierarchyIncomingCall[]?>([]);

        var result = calls.Select(c => new GDLspCallHierarchyIncomingCall
        {
            From = ConvertToLspItem(c.From),
            FromRanges = c.FromRanges.Select(r => GDLocationAdapter.ToLspRange(
                r.Line, r.Column, r.Line, r.Column)).ToArray()
        }).ToArray();

        return Task.FromResult<GDLspCallHierarchyIncomingCall[]?>(result);
    }

    public Task<GDLspCallHierarchyOutgoingCall[]?> HandleOutgoingCallsAsync(
        GDCallHierarchyOutgoingCallsParams @params, CancellationToken cancellationToken)
    {
        var coreItem = ConvertFromLspItem(@params.Item);
        if (coreItem == null)
            return Task.FromResult<GDLspCallHierarchyOutgoingCall[]?>(null);

        var calls = _handler.GetOutgoingCalls(coreItem);
        if (calls.Count == 0)
            return Task.FromResult<GDLspCallHierarchyOutgoingCall[]?>([]);

        var result = calls.Select(c => new GDLspCallHierarchyOutgoingCall
        {
            To = ConvertToLspItem(c.To),
            FromRanges = c.FromRanges.Select(r => GDLocationAdapter.ToLspRange(
                r.Line, r.Column, r.Line, r.Column)).ToArray()
        }).ToArray();

        return Task.FromResult<GDLspCallHierarchyOutgoingCall[]?>(result);
    }

    private static GDLspCallHierarchyItem ConvertToLspItem(GDCallHierarchyItem item)
    {
        // CLI.Core uses 1-based line, 0-based column
        // LSP uses 0-based line, 0-based column
        var line0 = item.Line - 1;
        var col0 = item.Column;

        return new GDLspCallHierarchyItem
        {
            Name = item.Name,
            Kind = 12, // SymbolKind.Function
            Uri = GDDocumentManager.PathToUri(item.FilePath),
            Range = GDLocationAdapter.ToLspRange(item.Line, col0, item.Line, col0 + item.Name.Length),
            SelectionRange = GDLocationAdapter.ToLspRange(item.Line, col0, item.Line, col0 + item.Name.Length),
            Detail = item.ClassName,
            Data = new GDCallHierarchyItemData
            {
                FilePath = item.FilePath,
                ClassName = item.ClassName,
                MethodName = item.Name,
                Line = item.Line,
                Column = item.Column
            }
        };
    }

    private static GDCallHierarchyItem? ConvertFromLspItem(GDLspCallHierarchyItem lspItem)
    {
        if (lspItem.Data == null)
            return null;

        return new GDCallHierarchyItem
        {
            Name = lspItem.Data.MethodName ?? lspItem.Name,
            ClassName = lspItem.Data.ClassName,
            FilePath = lspItem.Data.FilePath ?? GDDocumentManager.UriToPath(lspItem.Uri),
            Line = lspItem.Data.Line,
            Column = lspItem.Data.Column
        };
    }
}
