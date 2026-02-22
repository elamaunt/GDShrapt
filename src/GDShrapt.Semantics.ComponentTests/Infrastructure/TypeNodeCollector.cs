namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Collects ALL nodes for type inference verification:
/// 1. Declarations - via model.Symbols
/// 2. All expressions - via model.TypeSystem.GetType() for each GDExpression
/// </summary>
public class TypeNodeCollector
{
    /// <summary>
    /// Represents a node with its inferred type for verification.
    /// </summary>
    public record TypedNode(
        int Line,
        int Column,
        string Name,
        string NodeKind,
        string InferredType);

    /// <summary>
    /// Collects ALL nodes that a user can click on in IDE for type inspection.
    /// Uses model.Symbols for declarations and model.TypeSystem.GetType() for expressions.
    /// </summary>
    public List<TypedNode> CollectNodes(GDScriptFile file)
    {
        var result = new List<TypedNode>();

        var model = file.SemanticModel;
        if (model == null || file.Class == null)
            return result;

        // Track declaration positions to avoid duplicates with expressions
        var declarationPositions = new HashSet<(int Line, int Column)>();

        // 1. DECLARATIONS - via model.Symbols API
        // Use TypeSystem.GetType for declarations to match what IDE shows to user
        foreach (var symbol in model.Symbols)
        {
            var declNode = symbol.DeclarationNode;
            int line = GetNodeLine(declNode);
            int column = GetNodeColumn(declNode);

            // Skip declarations with no position info (synthetic symbols)
            if (line == 0 && column == 0)
                continue;

            // Use TypeSystem.GetType for all declarations (TypeSystem is always available)
            string type = declNode != null
                ? model.TypeSystem.GetType(declNode).DisplayName
                : symbol.TypeName ?? "Variant";
            string kindName = symbol.Kind.ToString();

            result.Add(new TypedNode(line, column, symbol.Name, kindName, type));
            declarationPositions.Add((line, column));
        }

        // 2. ALL EXPRESSIONS - via model.TypeSystem.GetType() API
        // This is EVERYTHING a user can click on in IDE!
        // TypeSystem is always available (uses GDNullRuntimeProvider if no runtime)
        foreach (var expr in file.Class.AllNodes.OfType<GDExpression>())
        {
            int line = GetNodeLine(expr);
            int column = GetNodeColumn(expr);

            // Skip expressions at declaration positions (already covered by Symbols)
            if (declarationPositions.Contains((line, column)))
                continue;

            // Use the TypeSystem API to get type
            string type = model.TypeSystem.GetType(expr).DisplayName;

            string name = GetExpressionDisplayName(expr);
            string kind = expr.GetType().Name;

            result.Add(new TypedNode(line, column, name, kind, type));
        }

        return result.OrderBy(n => n.Line).ThenBy(n => n.Column).ToList();
    }

    private int GetNodeLine(GDNode? node)
    {
        if (node == null) return 0;
        var token = node.AllTokens.FirstOrDefault();
        return token != null ? token.StartLine + 1 : 0;
    }

    private int GetNodeColumn(GDNode? node)
    {
        if (node == null) return 0;
        var token = node.AllTokens.FirstOrDefault();
        return token?.StartColumn ?? 0;
    }

    private string GetExpressionDisplayName(GDExpression? expr)
    {
        if (expr == null) return "?";

        return expr switch
        {
            GDIdentifierExpression id => id.Identifier?.Sequence ?? "?",
            GDMemberOperatorExpression m => $"{GetExpressionDisplayName(m.CallerExpression)}.{m.Identifier?.Sequence}",
            GDCallExpression c => $"{GetCallerName(c)}()",
            GDIndexerExpression i => $"{GetCallerName(i)}[]",
            GDNumberExpression n => n.Number?.ToString() ?? "number",
            GDStringExpression s => GetTruncatedString(s),
            GDBoolExpression b => b.Value?.ToString()?.ToLower() ?? "bool",
            GDArrayInitializerExpression => "[...]",
            GDDictionaryInitializerExpression => "{...}",
            GDMethodExpression => "func(...)",
            GDAwaitExpression a => $"await {GetExpressionDisplayName(a.Expression)}",
            GDGetNodeExpression g => $"${g.Path?.ToString() ?? "?"}",
            GDIfExpression => "if_expr",
            GDSingleOperatorExpression u => $"{u.OperatorType}{GetExpressionDisplayName(u.TargetExpression)}",
            GDDualOperatorExpression b => $"{GetExpressionDisplayName(b.LeftExpression)}{b.OperatorType}{GetExpressionDisplayName(b.RightExpression)}",
            GDNodePathExpression np => $"^{np.Path?.ToString() ?? "?"}",
            GDMatchCaseVariableExpression mv => $"var {mv.Identifier?.Sequence ?? "?"}",
            GDPassExpression => "pass",
            GDBreakPointExpression => "breakpoint",
            GDBracketExpression br => $"({GetExpressionDisplayName(br.InnerExpression)})",
            GDYieldExpression => "yield",
            GDReturnExpression r => $"return {GetExpressionDisplayName(r.Expression)}",
            GDBreakExpression => "break",
            GDContinueExpression => "continue",
            GDStringNameExpression sn => $"&{sn.String?.Sequence ?? "?"}",
            GDGetUniqueNodeExpression gu => $"%{gu.Name?.ToString() ?? "?"}",
            GDRestExpression => "...",
            GDMatchDefaultOperatorExpression => "_",
            _ => expr.GetType().Name.Replace("GD", "").Replace("Expression", "")
        };
    }

    private string GetCallerName(GDExpression? expr)
    {
        if (expr == null) return "?";

        return expr switch
        {
            GDCallExpression c => GetCallerName(c.CallerExpression),
            GDIndexerExpression i => GetCallerName(i.CallerExpression),
            _ => GetExpressionDisplayName(expr)
        };
    }

    private string GetTruncatedString(GDStringExpression s)
    {
        var seq = s.String?.Sequence;
        if (string.IsNullOrEmpty(seq))
            return "\"\"";
        if (seq.Length <= 10)
            return $"\"{seq}\"";
        return $"\"{seq.Substring(0, 10)}...\"";
    }
}
