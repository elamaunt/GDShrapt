using System.Collections.Generic;
using System.Text;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for hover information.
/// Extracts symbol info and documentation at a given position.
/// </summary>
public class GDHoverHandler : IGDHoverHandler
{
    protected readonly GDScriptProject _project;

    public GDHoverHandler(GDScriptProject project)
    {
        _project = project;
    }

    /// <inheritdoc />
    public virtual GDHoverInfo? GetHover(string filePath, int line, int column)
    {
        var script = _project.GetScript(filePath);
        if (script?.Analyzer == null || script.Class == null)
            return null;

        // Find the node at the position
        var finder = new GDPositionFinder(script.Class);
        var node = finder.FindNodeAtPosition(line, column);
        if (node == null)
            return null;

        // Get the symbol for this node
        var symbol = script.Analyzer.GetSymbolForNode(node);
        if (symbol == null)
            return null;

        // Build hover content
        var sb = new StringBuilder();
        sb.Append("```gdscript\n");

        // Add symbol kind and name
        sb.Append(GetSymbolKindString(symbol));
        sb.Append(' ');
        sb.Append(symbol.Name);

        // Add type if available
        if (symbol.TypeNode != null)
        {
            sb.Append(": ");
            sb.Append(symbol.TypeNode.ToString());
        }
        else if (!string.IsNullOrEmpty(symbol.TypeName))
        {
            sb.Append(": ");
            sb.Append(symbol.TypeName);
        }

        sb.Append("\n```");

        // Add documentation if available
        var docComment = ExtractDocComment(symbol.DeclarationNode);
        if (!string.IsNullOrEmpty(docComment))
        {
            sb.Append("\n\n---\n\n");
            sb.Append(docComment);
        }

        return new GDHoverInfo
        {
            Content = sb.ToString(),
            Kind = symbol.Kind,
            SymbolName = symbol.Name,
            TypeName = symbol.TypeName ?? symbol.TypeNode?.ToString(),
            Documentation = docComment,
            StartLine = symbol.DeclarationNode?.StartLine,
            StartColumn = symbol.DeclarationNode?.StartColumn,
            EndLine = symbol.DeclarationNode?.EndLine,
            EndColumn = symbol.DeclarationNode?.EndColumn
        };
    }

    /// <summary>
    /// Converts symbol kind to GDScript keyword string.
    /// </summary>
    protected static string GetSymbolKindString(Semantics.GDSymbolInfo symbol)
    {
        return symbol.Kind switch
        {
            Abstractions.GDSymbolKind.Variable => symbol.IsStatic ? "const" : "var",
            Abstractions.GDSymbolKind.Constant => "const",
            Abstractions.GDSymbolKind.Method => "func",
            Abstractions.GDSymbolKind.Signal => "signal",
            Abstractions.GDSymbolKind.Class => "class",
            Abstractions.GDSymbolKind.Enum => "enum",
            Abstractions.GDSymbolKind.EnumValue => "enum value",
            Abstractions.GDSymbolKind.Parameter => "param",
            Abstractions.GDSymbolKind.Iterator => "var",
            _ => "symbol"
        };
    }

    /// <summary>
    /// Extracts documentation comments from above the declaration.
    /// GDScript uses ## for doc comments.
    /// </summary>
    protected static string? ExtractDocComment(GDNode? declaration)
    {
        if (declaration == null)
            return null;

        // In GDScript, doc comments are ## lines above the declaration
        // We look for comment tokens preceding the declaration
        var docLines = new List<string>();

        // Find the first token of the declaration
        GDSyntaxToken? firstToken = null;
        foreach (var token in declaration.AllTokens)
        {
            firstToken = token;
            break;
        }

        if (firstToken == null)
            return null;

        // Walk backwards through tokens looking for ## comments
        var currentToken = firstToken.GlobalPreviousToken;
        while (currentToken != null)
        {
            if (currentToken is GDComment comment)
            {
                var text = comment.ToString().Trim();
                if (text.StartsWith("##"))
                {
                    // Remove the ## prefix and add to doc
                    var docText = text.Substring(2).TrimStart();
                    docLines.Insert(0, docText);
                }
                else
                {
                    // Regular comment (not doc comment) - stop
                    break;
                }
            }
            else if (currentToken is GDNewLine || currentToken is GDSpace)
            {
                // Whitespace is allowed between doc comments
            }
            else
            {
                // Any other token means we've passed the doc comments
                break;
            }

            currentToken = currentToken.GlobalPreviousToken;
        }

        return docLines.Count > 0 ? string.Join("\n", docLines) : null;
    }
}
