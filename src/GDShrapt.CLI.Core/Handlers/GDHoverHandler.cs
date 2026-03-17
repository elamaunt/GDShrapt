using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        if (IsTscnFile(filePath))
            return GetHoverInTscn(filePath, line, column);

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

        // Show cross-hierarchy bridge info for class members
        var crossHierarchyInfo = BuildCrossHierarchyInfo(symbol, script);
        if (!string.IsNullOrEmpty(crossHierarchyInfo))
            content += "\n\n" + crossHierarchyInfo;

        // Use pre-cached documentation (built-in docs or cross-file ## comments)
        var documentation = symbol.Documentation;

        // Fallback: extract from AST for same-file symbols not yet cached
        if (string.IsNullOrEmpty(documentation))
            documentation = ExtractDocComment(symbol.DeclarationNode);

        if (!string.IsNullOrEmpty(documentation))
        {
            // Convert BBCode to Markdown for built-in types (no DeclarationNode = built-in)
            if (symbol.DeclarationNode == null)
                documentation = GDBBCodeToMarkdownConverter.Convert(documentation);

            content += "\n\n---\n\n";
            content += documentation;
        }

        // Use identifier token for hover range (just the name, not the entire declaration)
        var posToken = symbol.PositionToken;
        if (posToken == null && node != null)
        {
            if (node is GDMemberOperatorExpression memberOp)
                posToken = memberOp.Identifier;
            else if (node is GDIdentifierExpression identExpr)
                posToken = identExpr.Identifier;
            else
                posToken = node.FirstLeafToken;
        }

        return new GDHoverInfo
        {
            Content = content,
            Kind = symbol.Kind,
            SymbolName = symbol.Name,
            TypeName = symbol.TypeName ?? symbol.TypeNode?.ToString(),
            Documentation = documentation,
            StartLine = posToken != null ? posToken.StartLine + 1 : null,
            StartColumn = posToken?.StartColumn,
            EndLine = posToken != null ? posToken.EndLine + 1 : null,
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

            // Show injected origin info for the call result if available
            var resultVarNode = callExpr.Parent;
            if (resultVarNode is GDVariableDeclarationStatement varStmt)
            {
                var varName = varStmt.Identifier?.Sequence;
                if (varName != null)
                {
                    var flowVar = semanticModel.GetFlowVariableType(varName, varStmt);
                    if (flowVar != null && flowVar.CurrentType.HasOrigins)
                    {
                        var originInfo = BuildOriginInfo(flowVar);
                        if (!string.IsNullOrEmpty(originInfo))
                            hover += $"\n\n{originInfo}";
                    }
                }
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

        if (node is GDIdentifierExpression || symbol.DeclarationNode != null)
        {
            var varName = symbol.Name;
            var flowTarget = node is GDIdentifierExpression ? node : symbol.DeclarationNode;
            flowVarType = semanticModel.GetFlowVariableType(varName, flowTarget);

            if (flowVarType != null)
            {
                if (flowVarType.IsNarrowed && flowVarType.NarrowedFromType != null)
                    narrowedType = flowVarType.NarrowedFromType.DisplayName;

                // Get the effective inferred type (could be union)
                inferredType = flowVarType.EffectiveTypeFormatted;

                if (inferredType == "Variant" || inferredType == declaredType)
                    inferredType = null;

                // After null check (if x:), suppress nullable inferred type
                if (narrowedType == null && flowVarType.IsGuaranteedNonNull && inferredType != null)
                {
                    var effectiveType = flowVarType.CurrentType.IsSingleType
                        ? flowVarType.CurrentType.Types.First()
                        : flowVarType.CurrentType.ToSemanticType();

                    if (effectiveType is GDUnionSemanticType union && union.IsNullable)
                    {
                        var nonNullType = union.WithoutNull();
                        var nonNullDisplay = nonNullType.DisplayName;
                        if (nonNullDisplay == declaredType)
                            inferredType = null;
                        else
                            narrowedType = nonNullDisplay;
                    }
                }
            }
        }

        // Enrich bare container types (Dictionary, Array) with usage-based profiles
        if (inferredType == null && (declaredType == "Dictionary" || declaredType == "Array"))
        {
            var containerType = semanticModel.TypeSystem.GetContainerElementType(symbol.Name);
            if (containerType == null || !containerType.HasElementTypes)
            {
                var className = semanticModel.ScriptFile?.TypeName;
                if (!string.IsNullOrEmpty(className))
                    containerType = semanticModel.TypeSystem.GetClassContainerElementType(className, symbol.Name);
            }
            if (containerType != null && containerType.HasElementTypes)
                inferredType = containerType.ToString();
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
                    if (containerType == null || !containerType.HasElementTypes)
                    {
                        var className = semanticModel.ScriptFile?.TypeName;
                        if (!string.IsNullOrEmpty(className))
                            containerType = semanticModel.TypeSystem.GetClassContainerElementType(className, symbol.Name);
                    }
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

        // Show origin provenance chain
        if (flowVarType != null && flowVarType.CurrentType.HasOrigins)
        {
            var originInfo = BuildOriginInfo(flowVarType);
            if (!string.IsNullOrEmpty(originInfo))
                annotations.Add(originInfo);
        }

        // Show duck-type constraints
        if (flowVarType?.DuckType != null)
        {
            var duckInfo = BuildDuckTypeInfo(flowVarType.DuckType);
            if (!string.IsNullOrEmpty(duckInfo))
                annotations.Add(duckInfo);
        }

        // Show escape points
        if (flowVarType != null && flowVarType.EscapePoints.Count > 0)
        {
            var escapeInfo = BuildEscapeInfo(flowVarType.EscapePoints);
            if (!string.IsNullOrEmpty(escapeInfo))
                annotations.Add(escapeInfo);
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

    private string? BuildCrossHierarchyInfo(Semantics.GDSymbolInfo symbol, GDScriptFile script)
    {
        if (symbol.Kind != GDSymbolKind.Method && symbol.Kind != GDSymbolKind.Signal)
            return null;

        if (script?.FullPath == null)
            return null;

        var collector = new GDSymbolReferenceCollector(_projectModel.Project, _projectModel);
        var allRefs = collector.CollectAllReferences(symbol.Name, script.FullPath);

        if (!allRefs.IsBridgeConnected)
            return null;

        var ownType = script.TypeName;
        var otherTypeEntries = allRefs.Primary.References
            .Where(r => r.Kind == GDSymbolReferenceKind.Declaration
                && r.Script?.TypeName != null
                && !string.Equals(r.Script.TypeName, ownType, StringComparison.Ordinal))
            .Select(r => (TypeName: r.Script!.TypeName!, ResPath: r.Script.ResPath ?? r.FilePath))
            .GroupBy(e => e.TypeName)
            .Select(g => (TypeName: g.Key, ResPath: g.First().ResPath))
            .OrderBy(e => e.TypeName)
            .ToList();

        if (otherTypeEntries.Count == 0)
            return null;

        var typeList = string.Join(" | ", otherTypeEntries.Select(e => $"`{e.TypeName}`"));
        var result = $"**bridge**: dynamically connected to {typeList} via untyped calls ({otherTypeEntries.Count} files)";

        const int maxShown = 5;
        foreach (var entry in otherTypeEntries.Take(maxShown))
            result += $"  \n— `{entry.TypeName}` → {entry.ResPath}";

        if (otherTypeEntries.Count > maxShown)
            result += $"  \n— ... and {otherTypeEntries.Count - maxShown} more";

        return result;
    }

    private static string? BuildOriginInfo(GDFlowVariableType flowVarType)
    {
        var parts = new List<string>();

        foreach (var (type, origins) in flowVarType.CurrentType.GetAllOrigins())
        {
            if (origins.Count == 0)
                continue;

            var origin = origins[0];
            var desc = origin.Description ?? origin.Kind.ToString();
            var confidence = origin.Confidence.ToString().ToLowerInvariant();

            var entry = $"`{type.DisplayName}` ← {desc} ({confidence})";

            // Show attached abstract value
            if (origin.Value != null)
                entry += $" = `{origin.Value.DisplayValue}`";

            // Show object state summary
            if (origin.ObjectState != null)
            {
                var scenePath = origin.ObjectState.GetRootSceneSnapshot()?.ScenePath;
                if (scenePath != null)
                    entry += $" [scene: {scenePath}]";

                var collisionLayers = origin.ObjectState.GetCurrentCollisionLayers();
                if (collisionLayers != null)
                    entry += $" [layers: {collisionLayers}]";
            }

            parts.Add(entry);
        }

        if (parts.Count == 0)
            return null;

        return "origin: " + string.Join("  \n", parts);
    }

    private static string? BuildEscapeInfo(IReadOnlyList<GDEscapePoint> escapePoints)
    {
        if (escapePoints.Count == 0)
            return null;

        if (escapePoints.Count == 1)
        {
            var ep = escapePoints[0];
            var desc = ep.Description ?? ep.Kind.ToString();
            return $"⚠ escapes: {desc}";
        }

        return $"⚠ escapes: {escapePoints.Count} point(s)";
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

        var firstToken = declaration.FirstLeafToken;

        if (firstToken == null)
            return null;

        var currentToken = firstToken.GlobalPreviousToken;
        int consecutiveNewLines = 0;
        while (currentToken != null)
        {
            if (currentToken is GDComment comment)
            {
                var text = comment.ToString().Trim();
                if (text.StartsWith("##"))
                {
                    var docText = text.Substring(2).TrimStart();
                    docLines.Insert(0, docText);
                    consecutiveNewLines = 0;
                }
                else
                {
                    break;
                }
            }
            else if (currentToken is GDNewLine)
            {
                consecutiveNewLines++;
                if (consecutiveNewLines >= 2)
                    break;
            }
            else if (currentToken is GDSpace
                     || currentToken is GDIntendation || currentToken is GDCarriageReturnToken)
            {
                // Non-newline whitespace is allowed between doc comments
            }
            else if (IsInsideAttribute(currentToken))
            {
                consecutiveNewLines = 0;
            }
            else
            {
                break;
            }

            currentToken = currentToken.GlobalPreviousToken;
        }

        return docLines.Count > 0 ? string.Join("\n", docLines) : null;
    }

    private static bool IsInsideAttribute(GDSyntaxToken token)
    {
        var parent = token.Parent;
        while (parent != null)
        {
            if (parent is GDClassAttribute)
                return true;
            parent = parent.Parent;
        }
        return false;
    }

    private static bool IsTscnFile(string filePath)
    {
        return filePath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase)
            || filePath.EndsWith(".tres", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly Regex TscnResPathRegex = new(@"""(res://[^""]+)""", RegexOptions.Compiled);

    private GDHoverInfo? GetHoverInTscn(string filePath, int line, int column)
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch
        {
            return null;
        }

        if (line < 1 || line > lines.Length)
            return null;

        var lineText = lines[line - 1];
        var col0 = column - 1;

        foreach (Match match in TscnResPathRegex.Matches(lineText))
        {
            var pathStart = match.Index + 1;
            var pathEnd = match.Index + match.Length - 1;

            if (col0 >= pathStart && col0 < pathEnd)
            {
                var resPath = match.Groups[1].Value;
                return BuildTscnResourceHover(resPath, filePath, line, match.Index + 1, match.Index + match.Length - 1);
            }
        }

        return null;
    }

    private GDHoverInfo? BuildTscnResourceHover(string resPath, string sceneFilePath, int line, int startCol, int endCol)
    {
        var project = _projectModel.Project;
        var projectPath = project.ProjectPath;
        if (string.IsNullOrEmpty(projectPath))
            return null;

        var relativePath = resPath.Substring("res://".Length);
        var resolvedPath = Path.GetFullPath(Path.Combine(projectPath, relativePath));

        var sb = new StringBuilder();

        if (resPath.EndsWith(".gd", StringComparison.OrdinalIgnoreCase))
        {
            var script = project.GetScript(resolvedPath);
            if (script?.Class != null)
            {
                var className = script.Class.ClassName?.Identifier?.Sequence;
                var extends = script.Class.Extends?.Type?.ToString();

                sb.Append("```gdscript\n");
                if (!string.IsNullOrEmpty(className))
                {
                    sb.Append("class ");
                    sb.Append(className);
                    if (!string.IsNullOrEmpty(extends))
                    {
                        sb.Append(" extends ");
                        sb.Append(extends);
                    }
                }
                else if (!string.IsNullOrEmpty(extends))
                {
                    sb.Append("extends ");
                    sb.Append(extends);
                }
                sb.Append("\n```");

                var methodCount = script.Class.Members?.OfType<GDMethodDeclaration>().Count() ?? 0;
                var varCount = script.Class.Members?.OfType<GDVariableDeclaration>().Count() ?? 0;
                sb.Append($"\n\n**GDScript** `{Path.GetFileName(resolvedPath)}`");
                sb.Append($"  \n{methodCount} method(s), {varCount} variable(s)");
            }
            else
            {
                sb.Append($"**GDScript** `{Path.GetFileName(resolvedPath)}`");
            }
        }
        else if (resPath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append($"**Scene** `{Path.GetFileName(resolvedPath)}`");

            var sceneInfo = project.SceneTypesProvider?.GetSceneInfo(resPath);
            if (sceneInfo != null)
            {
                var rootNode = sceneInfo.Nodes.FirstOrDefault();
                if (rootNode != null)
                    sb.Append($"  \nRoot: `{rootNode.NodeType}`");

                sb.Append($"  \n{sceneInfo.Nodes.Count} node(s)");
            }
        }
        else if (resPath.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append($"**Resource** `{Path.GetFileName(resolvedPath)}`");
        }
        else
        {
            var ext = Path.GetExtension(resolvedPath).TrimStart('.');
            sb.Append($"**{ext.ToUpperInvariant()}** `{Path.GetFileName(resolvedPath)}`");
        }

        sb.Append($"\n\n`{resPath}`");

        if (!File.Exists(resolvedPath))
            sb.Append("\n\n*File not found*");

        return new GDHoverInfo
        {
            Content = sb.ToString(),
            SymbolName = Path.GetFileName(resolvedPath),
            StartLine = line,
            StartColumn = startCol,
            EndLine = line,
            EndColumn = endCol
        };
    }
}
