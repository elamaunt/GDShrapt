using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/definition requests.
/// </summary>
public class GDDefinitionHandler
{
    private readonly GDScriptProject _project;

    public GDDefinitionHandler(GDScriptProject project)
    {
        _project = project;
    }

    public Task<GDLspLocation?> HandleAsync(GDDefinitionParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        var script = _project.GetScript(filePath);

        if (script?.Analyzer == null || script.Class == null)
            return Task.FromResult<GDLspLocation?>(null);

        // Convert to 1-based line/column
        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        // Find the node at the position (look for identifier expressions which are GDNodes)
        var node = GDNodeFinder.FindNodeAtPosition(script.Class, line, column);
        if (node == null)
            return Task.FromResult<GDLspLocation?>(null);

        // Get the symbol for this node
        var symbol = script.Analyzer.GetSymbolForNode(node);
        if (symbol?.DeclarationNode == null)
            return Task.FromResult<GDLspLocation?>(null);

        // Get the file containing the declaration
        // First check if it's in the current file
        var declarationFile = filePath;

        // If symbol is not declared in this script, search across project
        if (symbol.DeclarationNode.Parent == null ||
            !IsDeclarationInScript(symbol.DeclarationNode, script.Class))
        {
            // Try to find the script containing this symbol
            var declaringScript = FindScriptWithSymbol(symbol);
            if (declaringScript != null)
            {
                declarationFile = declaringScript.Reference.FullPath;
            }
        }

        var location = GDLocationAdapter.FromNode(symbol.DeclarationNode, declarationFile);
        return Task.FromResult(location);
    }

    /// <summary>
    /// Checks if a declaration node is within the given class.
    /// </summary>
    private static bool IsDeclarationInScript(GDNode declaration, GDClassDeclaration? classDecl)
    {
        if (classDecl == null)
            return false;

        var current = declaration;
        while (current != null)
        {
            if (current == classDecl)
                return true;
            current = current.Parent as GDNode;
        }
        return false;
    }

    /// <summary>
    /// Finds the script containing a symbol declaration.
    /// </summary>
    private GDScriptFile? FindScriptWithSymbol(GDSymbolInfo symbol)
    {
        if (symbol?.DeclarationNode == null)
            return null;

        // Search through all scripts in the project
        foreach (var script in _project.ScriptFiles)
        {
            if (script.Class == null)
                continue;

            if (IsDeclarationInScript(symbol.DeclarationNode, script.Class))
            {
                return script;
            }
        }

        return null;
    }
}
