using GDShrapt.Reader;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Inverts an if/elif/while condition and swaps the branches if applicable.
/// </summary>
internal class InvertConditionAction : RefactoringActionBase
{
    public override string Id => "invert_condition";
    public override string DisplayName => "Invert Condition";
    public override RefactoringCategory Category => RefactoringCategory.Convert;
    public override int Priority => 10;

    public override bool IsAvailable(RefactoringContext context)
    {
        if (context?.ContainingClass == null)
            return false;

        // Available when cursor is on if, elif, or while statement
        return context.GetIfStatement() != null
            || context.GetWhileStatement() != null
            || context.IsInIfCondition;
    }

    protected override string ValidateContext(RefactoringContext context)
    {
        var baseError = base.ValidateContext(context);
        if (baseError != null) return baseError;

        var ifStmt = context.GetIfStatement();
        var whileStmt = context.GetWhileStatement();

        if (ifStmt == null && whileStmt == null)
            return "No if or while statement found at cursor";

        return null;
    }

    protected override async Task ExecuteInternalAsync(RefactoringContext context)
    {
        // Try to get the statement
        var ifStmt = context.GetIfStatement();
        var whileStmt = context.GetWhileStatement();

        if (ifStmt != null)
        {
            await InvertIfStatement(context, ifStmt);
        }
        else if (whileStmt != null)
        {
            await InvertWhileCondition(context, whileStmt);
        }
        else
        {
            throw new RefactoringException("No statement found to invert");
        }
    }

    private Task InvertIfStatement(RefactoringContext context, GDIfStatement ifStmt)
    {
        var editor = context.Editor;
        var condition = ifStmt.IfBranch?.Condition;

        if (condition == null)
        {
            Logger.Info("InvertConditionAction: No condition found in if statement");
            return Task.CompletedTask;
        }

        // Invert the condition
        var invertedConditionText = InvertExpression(condition);

        // Get position of the condition
        var startLine = condition.StartLine;
        var startColumn = condition.StartColumn;
        var endLine = condition.EndLine;
        var endColumn = condition.EndColumn;

        Logger.Info($"InvertConditionAction: Inverting condition at ({startLine}:{startColumn}) - ({endLine}:{endColumn})");
        Logger.Info($"InvertConditionAction: Original: {condition}");
        Logger.Info($"InvertConditionAction: Inverted: {invertedConditionText}");

        // Check if we should swap if/else branches
        var hasElse = ifStmt.ElseBranch != null && ifStmt.ElseBranch.Statements?.Count > 0;
        var hasElif = ifStmt.ElifBranchesList != null && ifStmt.ElifBranchesList.Count > 0;

        if (hasElse && !hasElif)
        {
            // Simple if/else - swap branches and invert condition
            SwapIfElseBranches(editor, ifStmt, invertedConditionText);
        }
        else
        {
            // Just invert the condition, don't swap branches
            // (elif chains are complex to swap correctly)
            editor.Select(startLine, startColumn, endLine, endColumn);
            editor.Cut();
            editor.InsertTextAtCursor(invertedConditionText);
        }

        editor.ReloadScriptFromText();
        return Task.CompletedTask;
    }

    private void SwapIfElseBranches(IScriptEditor editor, GDIfStatement ifStmt, string invertedCondition)
    {
        var ifBranch = ifStmt.IfBranch;
        var elseBranch = ifStmt.ElseBranch;

        if (ifBranch?.Statements == null || elseBranch?.Statements == null)
            return;

        // Get the text of both branches
        var ifStatementsText = GetStatementsText(ifBranch.Statements);
        var elseStatementsText = GetStatementsText(elseBranch.Statements);

        // Calculate the full statement range
        var startLine = ifStmt.StartLine;
        var startColumn = ifStmt.StartColumn;
        var endLine = ifStmt.EndLine;
        var endColumn = ifStmt.EndColumn;

        // Build the new swapped if/else
        var indent = GetIndentation(editor, startLine);
        var innerIndent = indent + "\t";

        var newText = $"if {invertedCondition}:\n";
        newText += AddIndentation(elseStatementsText, innerIndent);
        newText += $"\n{indent}else:\n";
        newText += AddIndentation(ifStatementsText, innerIndent);

        // Replace the entire if statement
        editor.Select(startLine, startColumn, endLine, endColumn);
        editor.Cut();
        editor.InsertTextAtCursor(newText);
    }

    private string GetStatementsText(GDStatementsList statements)
    {
        if (statements == null || statements.Count == 0)
            return "pass";

        var result = new System.Text.StringBuilder();
        foreach (var stmt in statements)
        {
            if (stmt != null)
            {
                var stmtText = stmt.ToString().Trim();
                if (!string.IsNullOrEmpty(stmtText))
                {
                    if (result.Length > 0)
                        result.AppendLine();
                    result.Append(stmtText);
                }
            }
        }
        return result.Length > 0 ? result.ToString() : "pass";
    }

    private string AddIndentation(string text, string indent)
    {
        var lines = text.Split('\n');
        var result = new System.Text.StringBuilder();
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart('\t', ' ');
            if (!string.IsNullOrEmpty(trimmed))
            {
                if (result.Length > 0)
                    result.AppendLine();
                result.Append(indent + trimmed);
            }
        }
        return result.ToString();
    }

    private string GetIndentation(IScriptEditor editor, int line)
    {
        var lineText = editor.GetLine(line);
        var indent = new System.Text.StringBuilder();
        foreach (var c in lineText)
        {
            if (c == '\t' || c == ' ')
                indent.Append(c);
            else
                break;
        }
        return indent.ToString();
    }

    private Task InvertWhileCondition(RefactoringContext context, GDWhileStatement whileStmt)
    {
        var editor = context.Editor;
        var condition = whileStmt.Condition;

        if (condition == null)
        {
            Logger.Info("InvertConditionAction: No condition found in while statement");
            return Task.CompletedTask;
        }

        // Invert the condition
        var invertedConditionText = InvertExpression(condition);

        // Get position of the condition
        var startLine = condition.StartLine;
        var startColumn = condition.StartColumn;
        var endLine = condition.EndLine;
        var endColumn = condition.EndColumn;

        Logger.Info($"InvertConditionAction: Inverting while condition");
        Logger.Info($"InvertConditionAction: Original: {condition}");
        Logger.Info($"InvertConditionAction: Inverted: {invertedConditionText}");

        // Replace the condition
        editor.Select(startLine, startColumn, endLine, endColumn);
        editor.Cut();
        editor.InsertTextAtCursor(invertedConditionText);

        editor.ReloadScriptFromText();
        return Task.CompletedTask;
    }

    private string InvertExpression(GDExpression expr)
    {
        if (expr == null)
            return "not true";

        // Handle different expression types
        if (expr is GDDualOperatorExpression dual)
        {
            return InvertDualOperator(dual);
        }

        if (expr is GDSingleOperatorExpression single)
        {
            return InvertSingleOperator(single);
        }

        if (expr is GDBoolExpression boolExpr)
        {
            return boolExpr.Value == true ? "false" : "true";
        }

        if (expr is GDBracketExpression bracket)
        {
            // Handle parenthesized expression
            var innerInverted = InvertExpression(bracket.InnerExpression);
            // Check if we need to keep parentheses
            if (innerInverted.StartsWith("not "))
                return innerInverted;
            return $"not ({bracket.InnerExpression})";
        }

        // Default: wrap with "not"
        var exprStr = expr.ToString();
        // If expression is complex, wrap in parentheses
        if (NeedsParentheses(expr))
            return $"not ({exprStr})";
        return $"not {exprStr}";
    }

    private string InvertDualOperator(GDDualOperatorExpression dual)
    {
        var left = dual.LeftExpression?.ToString() ?? "";
        var right = dual.RightExpression?.ToString() ?? "";
        var op = dual.Operator;

        // Map operators to their inverses
        return op switch
        {
            // Comparison operators
            _ when IsEqualOperator(op) => $"{left} != {right}",
            _ when IsNotEqualOperator(op) => $"{left} == {right}",
            _ when IsGreaterOperator(op) => $"{left} <= {right}",
            _ when IsLessOperator(op) => $"{left} >= {right}",
            _ when IsGreaterOrEqualOperator(op) => $"{left} < {right}",
            _ when IsLessOrEqualOperator(op) => $"{left} > {right}",

            // Logical operators (De Morgan's laws)
            // not (a and b) = (not a) or (not b)
            // not (a or b) = (not a) and (not b)
            // Parentheses are required to preserve correct operator precedence
            _ when IsAndOperator(op) => $"(not {WrapIfComplex(dual.LeftExpression, left)}) or (not {WrapIfComplex(dual.RightExpression, right)})",
            _ when IsOrOperator(op) => $"(not {WrapIfComplex(dual.LeftExpression, left)}) and (not {WrapIfComplex(dual.RightExpression, right)})",

            // Default: wrap with not
            _ => $"not ({dual})"
        };
    }

    private bool IsEqualOperator(GDDualOperator op) => op?.ToString()?.Trim() == "==";
    private bool IsNotEqualOperator(GDDualOperator op) => op?.ToString()?.Trim() == "!=";
    private bool IsGreaterOperator(GDDualOperator op) => op?.ToString()?.Trim() == ">";
    private bool IsLessOperator(GDDualOperator op) => op?.ToString()?.Trim() == "<";
    private bool IsGreaterOrEqualOperator(GDDualOperator op) => op?.ToString()?.Trim() == ">=";
    private bool IsLessOrEqualOperator(GDDualOperator op) => op?.ToString()?.Trim() == "<=";
    private bool IsAndOperator(GDDualOperator op)
    {
        var text = op?.ToString()?.Trim()?.ToLower();
        return text == "and" || text == "&&";
    }
    private bool IsOrOperator(GDDualOperator op)
    {
        var text = op?.ToString()?.Trim()?.ToLower();
        return text == "or" || text == "||";
    }

    private string InvertSingleOperator(GDSingleOperatorExpression single)
    {
        var operand = single.TargetExpression;
        var op = single.Operator?.ToString()?.Trim()?.ToLower();

        // "not x" -> "x"
        if (op == "not" || op == "!")
        {
            return operand?.ToString() ?? "true";
        }

        // Other single operators: wrap with not
        return $"not ({single})";
    }

    private bool NeedsParentheses(GDExpression expr)
    {
        // Complex expressions need parentheses when wrapped with "not"
        return expr is GDDualOperatorExpression
            || expr is GDIfExpression  // Ternary if expression in GDScript
            || expr is GDCallExpression;
    }

    private string WrapIfComplex(GDExpression expr, string text)
    {
        // If the expression is a comparison or simple identifier, no extra wrapping needed
        if (expr is GDIdentifierExpression || expr is GDNumberExpression || expr is GDBoolExpression)
            return text;

        // If it's already a comparison, it doesn't need extra parentheses for the 'not' prefix
        if (expr is GDDualOperatorExpression dual)
        {
            var op = dual.Operator?.ToString()?.Trim();
            if (op == "==" || op == "!=" || op == "<" || op == ">" || op == "<=" || op == ">=")
                return text;
        }

        // For logical operators or other complex expressions, wrap in parentheses
        if (expr is GDDualOperatorExpression || expr is GDIfExpression)
            return $"({text})";

        return text;
    }
}
