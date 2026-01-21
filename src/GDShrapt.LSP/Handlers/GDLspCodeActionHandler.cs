using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/codeAction requests.
/// Thin wrapper over IGDCodeActionHandler from CLI.Core.
/// </summary>
public class GDLspCodeActionHandler
{
    private readonly IGDCodeActionHandler _handler;

    public GDLspCodeActionHandler(IGDCodeActionHandler handler)
    {
        _handler = handler;
    }

    public Task<GDLspCodeAction[]?> HandleAsync(GDCodeActionParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        // Convert LSP 0-based to CLI.Core 1-based
        var startLine = @params.Range.Start.Line + 1;
        var endLine = @params.Range.End.Line + 1;

        // Delegate to CLI.Core handler
        var result = _handler.GetCodeActions(filePath, startLine, endLine);
        if (result == null || result.Count == 0)
            return Task.FromResult<GDLspCodeAction[]?>(null);

        // Convert CLI.Core actions to LSP code actions
        var actions = new List<GDLspCodeAction>();
        foreach (var action in result)
        {
            actions.Add(ConvertToLspAction(action, @params.TextDocument.Uri));
        }

        return Task.FromResult<GDLspCodeAction[]?>(actions.ToArray());
    }

    private static GDLspCodeAction ConvertToLspAction(GDCodeAction action, string uri)
    {
        var lspAction = new GDLspCodeAction
        {
            Title = action.Title,
            Kind = ConvertActionKind(action.Kind),
            IsPreferred = action.IsPreferred
        };

        // Convert edits if present
        if (action.Edits.Count > 0)
        {
            var edits = new List<GDLspTextEdit>();
            foreach (var edit in action.Edits)
            {
                // Convert 1-based to 0-based
                edits.Add(new GDLspTextEdit
                {
                    Range = new GDLspRange(
                        edit.StartLine - 1,
                        edit.StartColumn - 1,
                        edit.EndLine - 1,
                        edit.EndColumn - 1),
                    NewText = edit.NewText
                });
            }

            lspAction.Edit = new GDWorkspaceEdit
            {
                Changes = new Dictionary<string, GDLspTextEdit[]>
                {
                    [uri] = edits.ToArray()
                }
            };
        }

        return lspAction;
    }

    private static string ConvertActionKind(CLI.Core.GDCodeActionKind kind)
    {
        return kind switch
        {
            CLI.Core.GDCodeActionKind.QuickFix => GDLspCodeActionKind.QuickFix,
            CLI.Core.GDCodeActionKind.Refactor => GDLspCodeActionKind.Refactor,
            CLI.Core.GDCodeActionKind.Source => GDLspCodeActionKind.Source,
            _ => GDLspCodeActionKind.QuickFix
        };
    }
}

/// <summary>
/// LSP code action kind constants.
/// </summary>
internal static class GDLspCodeActionKind
{
    public const string QuickFix = "quickfix";
    public const string Refactor = "refactor";
    public const string Source = "source";
}
