using System.Collections.Generic;
using System.Linq;
using System.Text;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for hover information.
/// Extracts symbol info and documentation at a given position.
/// Uses GDProjectSemanticModel as the unified entry point for all semantic queries.
/// </summary>
public class GDHoverHandler : IGDHoverHandler
{
    protected readonly GDProjectSemanticModel _projectModel;

    public GDHoverHandler(GDProjectSemanticModel projectModel)
    {
        _projectModel = projectModel;
    }

    /// <inheritdoc />
    public virtual GDHoverInfo? GetHover(string filePath, int line, int column)
    {
        var script = _projectModel.Project.GetScript(filePath);
        var semanticModel = script != null ? _projectModel.GetSemanticModel(script) : null;
        if (semanticModel == null || script?.Class == null)
            return null;

        // Convert 1-based CLI position to 0-based AST position
        var node = semanticModel.GetNodeAtPosition(line - 1, column - 1);
        if (node == null)
            return null;

        var symbol = semanticModel.GetSymbolForNode(node);
        if (symbol == null)
            return null;

        var content = BuildHoverContent(symbol, semanticModel, node);
        var docComment = ExtractDocComment(symbol.DeclarationNode);

        if (!string.IsNullOrEmpty(docComment))
        {
            content += "\n\n---\n\n";
            content += docComment;
        }

        // Use identifier token for hover range (just the name, not the entire declaration)
        var posToken = symbol.PositionToken;

        return new GDHoverInfo
        {
            Content = content,
            Kind = symbol.Kind,
            SymbolName = symbol.Name,
            TypeName = symbol.TypeName ?? symbol.TypeNode?.ToString(),
            Documentation = docComment,
            StartLine = posToken != null ? posToken.StartLine + 1 : null,
            StartColumn = posToken?.StartColumn,
            EndLine = posToken != null ? posToken.StartLine + 1 : null,
            EndColumn = posToken != null ? posToken.StartColumn + symbol.Name.Length : null
        };
    }

    /// <summary>
    /// Builds rich hover content based on symbol kind.
    /// Shows declared type, inferred type, flow narrowing, union types, and duck-type constraints.
    /// </summary>
    protected virtual string BuildHoverContent(Semantics.GDSymbolInfo symbol, GDSemanticModel semanticModel, GDNode node)
    {
        return symbol.Kind switch
        {
            GDSymbolKind.Method => BuildMethodHoverWithContext(symbol, semanticModel, node),
            GDSymbolKind.Signal => BuildSignalHover(symbol),
            GDSymbolKind.Class => BuildClassHover(symbol),
            GDSymbolKind.Enum => BuildEnumHover(symbol),
            GDSymbolKind.EnumValue => BuildEnumValueHover(symbol),
            _ => BuildVariableHover(symbol, semanticModel, node)
        };
    }

    private string BuildMethodHoverWithContext(Semantics.GDSymbolInfo symbol, GDSemanticModel semanticModel, GDNode node)
    {
        var hover = BuildMethodHover(symbol);

        // Check if the call has an injected/inferred return type different from the declared one
        var callExpr = node?.Parent as GDCallExpression
                    ?? (node?.Parent?.Parent as GDCallExpression);
        if (callExpr != null)
        {
            var inferredReturn = semanticModel.TypeSystem.GetType(callExpr);
            if (!inferredReturn.IsVariant && inferredReturn.DisplayName != symbol.ReturnTypeName)
            {
                hover += $"\n\ninferred return: `{inferredReturn.DisplayName}`";
            }
        }

        return hover;
    }

    private string BuildMethodHover(Semantics.GDSymbolInfo symbol)
    {
        var sb = new StringBuilder();
        sb.Append("```gdscript\n");

        if (symbol.IsStatic)
            sb.Append("static ");

        sb.Append("func ");
        sb.Append(symbol.Name);
        sb.Append('(');

        if (symbol.Parameters != null && symbol.Parameters.Count > 0)
        {
            var paramParts = new List<string>();
            foreach (var param in symbol.Parameters)
            {
                var part = param.Name;
                if (!string.IsNullOrEmpty(param.TypeName))
                    part += ": " + param.TypeName;
                if (param.HasDefaultValue)
                    part += " = ...";
                paramParts.Add(part);
            }
            sb.Append(string.Join(", ", paramParts));
        }

        sb.Append(')');

        if (!string.IsNullOrEmpty(symbol.ReturnTypeName))
        {
            sb.Append(" -> ");
            sb.Append(symbol.ReturnTypeName);
        }

        sb.Append("\n```");

        if (!string.IsNullOrEmpty(symbol.DeclaringTypeName))
        {
            sb.Append("\n\n*");
            sb.Append(symbol.DeclaringTypeName);
            sb.Append('*');
        }

        return sb.ToString();
    }

    private string BuildSignalHover(Semantics.GDSymbolInfo symbol)
    {
        var sb = new StringBuilder();
        sb.Append("```gdscript\n");
        sb.Append("signal ");
        sb.Append(symbol.Name);

        if (symbol.Parameters != null && symbol.Parameters.Count > 0)
        {
            sb.Append('(');
            var paramParts = new List<string>();
            foreach (var param in symbol.Parameters)
            {
                var part = param.Name;
                if (!string.IsNullOrEmpty(param.TypeName))
                    part += ": " + param.TypeName;
                paramParts.Add(part);
            }
            sb.Append(string.Join(", ", paramParts));
            sb.Append(')');
        }

        sb.Append("\n```");

        if (!string.IsNullOrEmpty(symbol.DeclaringTypeName))
        {
            sb.Append("\n\n*");
            sb.Append(symbol.DeclaringTypeName);
            sb.Append('*');
        }

        return sb.ToString();
    }

    private string BuildClassHover(Semantics.GDSymbolInfo symbol)
    {
        var sb = new StringBuilder();
        sb.Append("```gdscript\n");
        sb.Append("class ");
        sb.Append(symbol.Name);

        if (!string.IsNullOrEmpty(symbol.TypeName))
        {
            sb.Append(" extends ");
            sb.Append(symbol.TypeName);
        }

        sb.Append("\n```");
        return sb.ToString();
    }

    private string BuildEnumHover(Semantics.GDSymbolInfo symbol)
    {
        var sb = new StringBuilder();
        sb.Append("```gdscript\n");
        sb.Append("enum ");
        sb.Append(symbol.Name);
        sb.Append("\n```");

        if (!string.IsNullOrEmpty(symbol.DeclaringTypeName))
        {
            sb.Append("\n\n*");
            sb.Append(symbol.DeclaringTypeName);
            sb.Append('*');
        }

        return sb.ToString();
    }

    private string BuildEnumValueHover(Semantics.GDSymbolInfo symbol)
    {
        var sb = new StringBuilder();
        sb.Append("```gdscript\n");

        if (!string.IsNullOrEmpty(symbol.DeclaringTypeName))
        {
            sb.Append(symbol.DeclaringTypeName);
            sb.Append('.');
        }
        sb.Append(symbol.Name);

        sb.Append("\n```");
        return sb.ToString();
    }

    /// <summary>
    /// Builds hover content for variable-like symbols (variables, constants, parameters, iterators, properties, match bindings).
    /// Shows declared type, inferred type, initializer, flow narrowing, union types, and duck-type constraints.
    /// </summary>
    private string BuildVariableHover(Semantics.GDSymbolInfo symbol, GDSemanticModel semanticModel, GDNode node)
    {
        var sb = new StringBuilder();
        sb.Append("```gdscript\n");

        sb.Append(GetSymbolKindString(symbol));
        sb.Append(' ');
        sb.Append(symbol.Name);

        // Get declared type
        var declaredType = symbol.TypeNode?.ToString() ?? symbol.TypeName;

        // Get inferred type from flow analysis
        string? inferredType = null;
        string? narrowedType = null;
        GDFlowVariableType? flowVarType = null;

        if (node is GDIdentifierExpression identExpr)
        {
            var varName = symbol.Name;
            flowVarType = semanticModel.GetFlowVariableType(varName, node);

            if (flowVarType != null)
            {
                if (flowVarType.IsNarrowed && flowVarType.NarrowedFromType != null)
                    narrowedType = flowVarType.NarrowedFromType.DisplayName;

                // Get the effective inferred type (could be union)
                inferredType = flowVarType.EffectiveTypeFormatted;

                if (inferredType == "Variant" || inferredType == declaredType)
                    inferredType = null;
            }
        }

        // Fallback: infer type from initializer when declared type is null and flow analysis didn't help
        if (string.IsNullOrEmpty(declaredType) && inferredType == null
            && symbol.DeclarationNode is GDVariableDeclaration varDecl && varDecl.Initializer != null)
        {
            var initializerType = semanticModel.TypeSystem.GetType(varDecl.Initializer);
            if (!initializerType.IsVariant)
            {
                inferredType = initializerType.DisplayName;

                // Enrich plain container types with usage-based generic parameters
                if (inferredType == "Dictionary" || inferredType == "Array")
                {
                    var containerType = semanticModel.TypeSystem.GetContainerElementType(symbol.Name);
                    if (containerType != null && containerType.HasElementTypes)
                        inferredType = containerType.ToString();
                }
            }
        }

        // Show declared type
        if (!string.IsNullOrEmpty(declaredType))
        {
            sb.Append(": ");
            sb.Append(declaredType);
        }

        // Show initializer for constants
        if (symbol.Kind == GDSymbolKind.Constant && symbol.DeclarationNode is GDVariableDeclaration constDecl)
        {
            var initializer = constDecl.Initializer;
            if (initializer != null)
            {
                sb.Append(" = ");
                sb.Append(initializer.ToString());
            }
        }

        sb.Append("\n```");

        // Show inferred type if different from declared
        var annotations = new List<string>();

        if (narrowedType != null)
        {
            annotations.Add($"narrowed to: `{narrowedType}`");
        }
        else if (inferredType != null)
        {
            annotations.Add($"inferred: `{inferredType}`");
        }

        // Show duck-type constraints
        if (flowVarType?.DuckType != null)
        {
            var duckInfo = BuildDuckTypeInfo(flowVarType.DuckType);
            if (!string.IsNullOrEmpty(duckInfo))
                annotations.Add(duckInfo);
        }

        // Show parameter annotation
        if (symbol.Kind == GDSymbolKind.Parameter)
            annotations.Add("*(parameter)*");

        // Show declaring type
        if (!string.IsNullOrEmpty(symbol.DeclaringTypeName))
            annotations.Add($"*{symbol.DeclaringTypeName}*");

        if (annotations.Count > 0)
        {
            sb.Append("\n\n");
            sb.Append(string.Join("  \n", annotations));
        }

        return sb.ToString();
    }

    private static string? BuildDuckTypeInfo(GDDuckType duckType)
    {
        var parts = new List<string>();

        foreach (var method in duckType.RequiredMethods.OrderBy(m => m.Key))
            parts.Add($".{method.Key}()");

        foreach (var prop in duckType.RequiredProperties.Keys.OrderBy(p => p))
            parts.Add($".{prop}");

        foreach (var signal in duckType.RequiredSignals.OrderBy(s => s))
            parts.Add($".{signal}");

        if (parts.Count == 0)
            return null;

        var result = "duck type: `{ " + string.Join(", ", parts) + " }`";

        if (duckType.PossibleTypes.Count > 0)
        {
            var possibleTypes = string.Join(" | ", duckType.PossibleTypes.Select(t => t.DisplayName).OrderBy(t => t));
            result += $"  \npossible types: `{possibleTypes}`";
        }

        return result;
    }

    /// <summary>
    /// Converts symbol kind to GDScript keyword string.
    /// </summary>
    protected static string GetSymbolKindString(Semantics.GDSymbolInfo symbol)
    {
        return symbol.Kind switch
        {
            GDSymbolKind.Variable => symbol.IsStatic ? "const" : "var",
            GDSymbolKind.Constant => "const",
            GDSymbolKind.Property => symbol.IsStatic ? "const" : "var",
            GDSymbolKind.Method => "func",
            GDSymbolKind.Signal => "signal",
            GDSymbolKind.Class => "class",
            GDSymbolKind.Enum => "enum",
            GDSymbolKind.EnumValue => "enum value",
            GDSymbolKind.Parameter => "var",
            GDSymbolKind.Iterator => "var",
            GDSymbolKind.MatchCaseBinding => "var",
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

        var docLines = new List<string>();

        GDSyntaxToken? firstToken = null;
        foreach (var token in declaration.AllTokens)
        {
            firstToken = token;
            break;
        }

        if (firstToken == null)
            return null;

        var currentToken = firstToken.GlobalPreviousToken;
        while (currentToken != null)
        {
            if (currentToken is GDComment comment)
            {
                var text = comment.ToString().Trim();
                if (text.StartsWith("##"))
                {
                    var docText = text.Substring(2).TrimStart();
                    docLines.Insert(0, docText);
                }
                else
                {
                    break;
                }
            }
            else if (currentToken is GDNewLine || currentToken is GDSpace)
            {
                // Whitespace is allowed between doc comments
            }
            else
            {
                break;
            }

            currentToken = currentToken.GlobalPreviousToken;
        }

        return docLines.Count > 0 ? string.Join("\n", docLines) : null;
    }
}
