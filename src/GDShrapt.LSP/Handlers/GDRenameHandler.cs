using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.LSP.Adapters;
using GDShrapt.LSP.Protocol.Types;
using GDShrapt.LSP.Server;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.Handlers;

/// <summary>
/// Handles textDocument/rename requests.
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

        // Collect all edits grouped by file URI
        var changes = new Dictionary<string, List<GDLspTextEdit>>();

        // Add declaration edit
        if (symbol.Declaration != null)
        {
            var declIdentifier = GetIdentifierFromDeclaration(symbol.Declaration);
            if (declIdentifier != null)
            {
                AddEdit(changes, filePath, declIdentifier, newName);
            }
        }

        // Get all references to this symbol in the current file
        var references = script.Analyzer.GetReferencesTo(symbol);
        foreach (var reference in references)
        {
            var refNode = reference.ReferenceNode;
            if (refNode == null)
                continue;

            // Skip declaration (already added)
            if (refNode == symbol.Declaration)
                continue;

            var identifier = GetIdentifierFromNode(refNode);
            if (identifier != null)
            {
                AddEdit(changes, filePath, identifier, newName);
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

                var identifier = GetIdentifierFromNode(otherNode);
                if (identifier != null)
                {
                    AddEdit(changes, otherScript.Reference.FullPath, identifier, newName);
                }
            }
        }

        if (changes.Count == 0)
            return Task.FromResult<GDWorkspaceEdit?>(null);

        // Convert to arrays
        var changesDict = new Dictionary<string, GDLspTextEdit[]>();
        foreach (var kvp in changes)
        {
            var uri = GDDocumentManager.PathToUri(kvp.Key);
            changesDict[uri] = kvp.Value.ToArray();
        }

        return Task.FromResult<GDWorkspaceEdit?>(new GDWorkspaceEdit
        {
            Changes = changesDict
        });
    }

    private void AddEdit(Dictionary<string, List<GDLspTextEdit>> changes, string filePath, GDSyntaxToken token, string newName)
    {
        if (!changes.TryGetValue(filePath, out var edits))
        {
            edits = new List<GDLspTextEdit>();
            changes[filePath] = edits;
        }

        var range = GDLocationAdapter.RangeFromToken(token);
        if (range != null)
        {
            edits.Add(new GDLspTextEdit
            {
                Range = range,
                NewText = newName
            });
        }
    }

    private GDSyntaxToken? GetIdentifierFromDeclaration(GDNode declaration)
    {
        // For method declarations, get the identifier
        if (declaration is GDMethodDeclaration method)
            return method.Identifier;

        // For variable declarations, get the identifier
        if (declaration is GDVariableDeclaration variable)
            return variable.Identifier;

        // For parameter declarations
        if (declaration is GDParameterDeclaration parameter)
            return parameter.Identifier;

        // For class declarations
        if (declaration is GDInnerClassDeclaration innerClass)
            return innerClass.Identifier;

        // For signal declarations
        if (declaration is GDSignalDeclaration signal)
            return signal.Identifier;

        // For enum declarations
        if (declaration is GDEnumDeclaration enumDecl)
            return enumDecl.Identifier;

        // For enum values
        if (declaration is GDEnumValueDeclaration enumValue)
            return enumValue.Identifier;

        return null;
    }

    private GDSyntaxToken? GetIdentifierFromNode(GDNode node)
    {
        // For identifier expressions
        if (node is GDIdentifierExpression identExpr)
            return identExpr.Identifier;

        // For member access, get the identifier part
        if (node is GDMemberOperatorExpression memberOp)
            return memberOp.Identifier;

        // For call expressions, get the identifier
        if (node is GDCallExpression call)
        {
            if (call.CallerExpression is GDIdentifierExpression callIdent)
                return callIdent.Identifier;
            if (call.CallerExpression is GDMemberOperatorExpression callMember)
                return callMember.Identifier;
        }

        // Try to get identifier from declarations
        return GetIdentifierFromDeclaration(node);
    }
}
