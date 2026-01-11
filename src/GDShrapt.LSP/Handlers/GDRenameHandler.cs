using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/rename requests.
/// Uses GDRenameService from GDShrapt.Semantics for rename operations.
/// </summary>
public class GDRenameHandler
{
    private readonly GDScriptProject _project;

    public GDRenameHandler(GDScriptProject project)
    {
        _project = project;
    }

    public Task<GDWorkspaceEdit?> HandleAsync(GDRenameParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        var script = _project.GetScript(filePath);

        if (script?.Analyzer == null || script.Class == null)
            return Task.FromResult<GDWorkspaceEdit?>(null);

        // Convert to 1-based line/column
        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        // Find the node at the position
        var node = GDNodeFinder.FindNodeAtPosition(script.Class, line, column);
        if (node == null)
            return Task.FromResult<GDWorkspaceEdit?>(null);

        // Get the symbol for this node
        var symbol = script.Analyzer.GetSymbolForNode(node);
        if (symbol == null)
            return Task.FromResult<GDWorkspaceEdit?>(null);

        var newName = @params.NewName;
        if (string.IsNullOrWhiteSpace(newName))
            return Task.FromResult<GDWorkspaceEdit?>(null);

        // Use GDRenameService for the rename operation
        var renameService = new GDRenameService(_project);

        // Plan the rename
        var result = renameService.PlanRename(symbol, newName);

        // If rename failed or has conflicts, return null
        if (!result.Success || result.Conflicts.Count > 0)
            return Task.FromResult<GDWorkspaceEdit?>(null);

        // LSP uses only strict edits (type-confirmed references)
        // Potential edits are excluded to avoid false positives
        // They can be handled separately in plugin UI with user confirmation
        if (result.StrictEdits.Count == 0)
            return Task.FromResult<GDWorkspaceEdit?>(null);

        // Convert GDTextEdit to LSP workspace edit (strict only)
        var changes = ConvertToWorkspaceEdit(result.StrictEdits);

        return Task.FromResult<GDWorkspaceEdit?>(new GDWorkspaceEdit
        {
            Changes = changes
        });
    }

    private static Dictionary<string, GDLspTextEdit[]> ConvertToWorkspaceEdit(IReadOnlyList<GDTextEdit> edits)
    {
        var changesByFile = new Dictionary<string, List<GDLspTextEdit>>();

        foreach (var edit in edits)
        {
            var uri = GDDocumentManager.PathToUri(edit.FilePath);

            if (!changesByFile.TryGetValue(uri, out var fileEdits))
            {
                fileEdits = new List<GDLspTextEdit>();
                changesByFile[uri] = fileEdits;
            }

            // Convert 1-based line/column to 0-based LSP positions
            // GDTextEdit has OldText which gives us the length for the end position
            var startLine = edit.Line - 1;
            var startColumn = edit.Column - 1;
            var endLine = startLine;
            var endColumn = startColumn + edit.OldText.Length;

            fileEdits.Add(new GDLspTextEdit
            {
                Range = new GDLspRange(startLine, startColumn, endLine, endColumn),
                NewText = edit.NewText
            });
        }

        // Convert to arrays
        var result = new Dictionary<string, GDLspTextEdit[]>();
        foreach (var kvp in changesByFile)
        {
            result[kvp.Key] = kvp.Value.ToArray();
        }

        return result;
    }
}
