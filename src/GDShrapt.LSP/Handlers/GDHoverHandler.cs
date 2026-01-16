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
        // We need to look at the tokens before the declaration
        var sb = new StringBuilder();
        var classDecl = declaration.ClassDeclaration as GDClassDeclaration;
        if (classDecl == null)
            return null;

        // Get the source text and find comments before this declaration
        var source = classDecl.ToString();
        if (string.IsNullOrEmpty(source))
            return null;

        var lines = source.Split('\n');
        var declLine = declaration.StartLine;

        // Look backwards for ## comments
        var docLines = new System.Collections.Generic.List<string>();
        for (int i = declLine - 2; i >= 0; i--) // -2 because lines are 1-based and we want the line before
        {
            if (i >= lines.Length)
                continue;

            var line = lines[i].Trim();
            if (line.StartsWith("##"))
            {
                // Remove the ## prefix and add to doc
                var docText = line.Substring(2).TrimStart();
                docLines.Insert(0, docText);
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                // Empty line - stop if we already have doc lines
                if (docLines.Count > 0)
                    break;
            }
            else
            {
                // Non-comment line - stop
                break;
            }
        }

        return docLines.Count > 0 ? string.Join("\n", docLines) : null;
    }
}
