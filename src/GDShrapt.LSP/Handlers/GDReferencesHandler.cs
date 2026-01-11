using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/references requests.
/// </summary>
public class GDReferencesHandler
{
    private readonly GDScriptProject _project;

    public GDReferencesHandler(GDScriptProject project)
    {
        _project = project;
    }

    public Task<GDLspLocation[]?> HandleAsync(GDReferencesParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        var script = _project.GetScript(filePath);

        if (script?.Analyzer == null || script.Class == null)
            return Task.FromResult<GDLspLocation[]?>(null);

        // Convert to 1-based line/column
        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        // Find the node at the position
        var node = GDNodeFinder.FindNodeAtPosition(script.Class, line, column);
        if (node == null)
            return Task.FromResult<GDLspLocation[]?>(null);

        // Get the symbol for this node
        var symbol = script.Analyzer.GetSymbolForNode(node);
        if (symbol == null)
            return Task.FromResult<GDLspLocation[]?>(null);

        var locations = new List<GDLspLocation>();

        // Add declaration if requested
        if (@params.Context.IncludeDeclaration && symbol.Declaration != null)
        {
            var declLocation = GDLocationAdapter.FromNode(symbol.Declaration, filePath);
            if (declLocation != null)
            {
                locations.Add(declLocation);
            }
        }

        // Get all references to this symbol in the current file
        var references = script.Analyzer.GetReferencesTo(symbol);
        foreach (var reference in references)
        {
            var refNode = reference.ReferenceNode;
            if (refNode == null)
                continue;

            // Skip declaration if already added
            if (@params.Context.IncludeDeclaration && refNode == symbol.Declaration)
                continue;

            var location = GDLocationAdapter.FromNode(refNode, filePath);
            if (location != null)
            {
                locations.Add(location);
            }
        }

        // Also search in other files for cross-file references
        foreach (var otherScript in _project.ScriptFiles)
        {
            if (otherScript.Reference.FullPath == filePath)
                continue;

            if (otherScript.Analyzer == null)
                continue;

            var otherRefs = otherScript.Analyzer.GetReferencesTo(symbol);
            foreach (var reference in otherRefs)
            {
                var otherNode = reference.ReferenceNode;
                if (otherNode == null)
                    continue;

                var location = GDLocationAdapter.FromNode(otherNode, otherScript.Reference.FullPath);
                if (location != null)
                {
                    locations.Add(location);
                }
            }
        }

        return Task.FromResult<GDLspLocation[]?>(locations.ToArray());
    }
}
