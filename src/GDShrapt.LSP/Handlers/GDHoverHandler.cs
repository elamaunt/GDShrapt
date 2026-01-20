using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/hover requests.
/// </summary>
public class GDHoverHandler
{
    private readonly GDScriptProject _project;

    public GDHoverHandler(GDScriptProject project)
    {
        _project = project;
    }

    public Task<GDLspHover?> HandleAsync(GDHoverParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        var script = _project.GetScript(filePath);

        if (script?.Analyzer == null || script.Class == null)
            return Task.FromResult<GDLspHover?>(null);

        // Convert to 1-based line/column
        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        // Find the node at the position
        var node = GDNodeFinder.FindNodeAtPosition(script.Class, line, column);
        if (node == null)
            return Task.FromResult<GDLspHover?>(null);

        // Get the symbol for this node
        var symbol = script.Analyzer.GetSymbolForNode(node);
        if (symbol == null)
            return Task.FromResult<GDLspHover?>(null);

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

        var hover = new GDLspHover
        {
            Contents = GDLspMarkupContent.Markdown(sb.ToString()),
            Range = symbol.DeclarationNode != null
                ? GDLocationAdapter.ToLspRange(
                    symbol.DeclarationNode.StartLine,
                    symbol.DeclarationNode.StartColumn,
                    symbol.DeclarationNode.EndLine,
                    symbol.DeclarationNode.EndColumn)
                : null
        };

        return Task.FromResult<GDLspHover?>(hover);
    }

    private static string GetSymbolKindString(GDSymbolInfo symbol)
    {
        return symbol.Kind switch
        {
            GDSymbolKind.Variable => symbol.IsStatic ? "const" : "var",
            GDSymbolKind.Constant => "const",
            GDSymbolKind.Method => "func",
            GDSymbolKind.Signal => "signal",
            GDSymbolKind.Class => "class",
            GDSymbolKind.Enum => "enum",
            GDSymbolKind.EnumValue => "enum value",
            GDSymbolKind.Parameter => "param",
            GDSymbolKind.Iterator => "var",
            _ => "symbol"
        };
    }

    /// <summary>
    /// Extracts documentation comments from above the declaration.
    /// GDScript uses ## for doc comments.
    /// </summary>
    private static string? ExtractDocComment(GDNode? declaration)
    {
        if (declaration == null)
            return null;

        // In GDScript, doc comments are ## lines above the declaration
        // We look for comment tokens preceding the declaration
        var docLines = new System.Collections.Generic.List<string>();

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
