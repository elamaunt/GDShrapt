using GDShrapt.Reader;
using System.Collections.Generic;
using System.Text;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for inverting conditions in if/elif/while statements using De Morgan's laws.
/// </summary>
public class GDInvertConditionService : GDRefactoringServiceBase
{
    /// <summary>
    /// Checks if the invert condition refactoring can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return false;

        // Available when cursor is on if, elif, or while statement
        return context.IsOnIfStatement || context.IsOnWhileStatement;
    }

    /// <summary>
    /// Plans the invert condition refactoring without applying changes.
    /// Returns preview information for display.
    /// </summary>
    public GDInvertConditionResult Plan(GDRefactoringContext context)
    {
        if (!CanExecute(context))
            return GDInvertConditionResult.Failed("Cannot invert condition at this position");

        var ifStmt = context.FindParent<GDIfStatement>();
        if (ifStmt != null)
        {
            return PlanInvertIfStatement(context, ifStmt);
        }

        var whileStmt = context.FindParent<GDWhileStatement>();
        if (whileStmt != null)
        {
            return PlanInvertWhileCondition(context, whileStmt);
        }

        return GDInvertConditionResult.Failed("No if or while statement found at cursor");
    }

    private GDInvertConditionResult PlanInvertIfStatement(GDRefactoringContext context, GDIfStatement ifStmt)
    {
        var condition = ifStmt.IfBranch?.Condition;

        if (condition == null)
            return GDInvertConditionResult.Failed("No condition found in if statement");

        var originalCondition = condition.ToString();
        var invertedCondition = InvertExpression(condition);

        var hasElse = ifStmt.ElseBranch != null && ifStmt.ElseBranch.Statements?.Count > 0;
        var hasElif = ifStmt.ElifBranchesList != null && ifStmt.ElifBranchesList.Count > 0;
        var willSwapBranches = hasElse && !hasElif;

        var originalCode = ifStmt.ToString();
        string resultCode;

        if (willSwapBranches)
        {
            resultCode = BuildSwappedIfElseCode(ifStmt, invertedCondition);
        }
        else
        {
            resultCode = originalCode.Replace(originalCondition, invertedCondition);
        }

        return GDInvertConditionResult.Planned(
            originalCondition,
            invertedCondition,
            willSwapBranches,
            originalCode,
            resultCode);
    }

    private string BuildSwappedIfElseCode(GDIfStatement ifStmt, string invertedCondition)
    {
        var ifBranch = ifStmt.IfBranch;
        var elseBranch = ifStmt.ElseBranch;

        if (ifBranch?.Statements == null || elseBranch?.Statements == null)
            return ifStmt.ToString();

        var ifStatementsText = GetStatementsText(ifBranch.Statements);
        var elseStatementsText = GetStatementsText(elseBranch.Statements);

        var indent = GetIndentationFromStatementPosition(ifStmt);
        var innerIndent = indent + "\t";

        var newText = new StringBuilder();
        newText.Append($"if {invertedCondition}:\n");
        newText.Append(AddIndentation(elseStatementsText, innerIndent));
        newText.Append($"\n{indent}else:\n");
        newText.Append(AddIndentation(ifStatementsText, innerIndent));

        return newText.ToString();
    }

    private GDInvertConditionResult PlanInvertWhileCondition(GDRefactoringContext context, GDWhileStatement whileStmt)
    {
        var condition = whileStmt.Condition;

        if (condition == null)
            return GDInvertConditionResult.Failed("No condition found in while statement");

        var originalCondition = condition.ToString();
        var invertedCondition = InvertExpression(condition);
        var originalCode = whileStmt.ToString();
        var resultCode = originalCode.Replace(originalCondition, invertedCondition);

        return GDInvertConditionResult.Planned(
            originalCondition,
            invertedCondition,
            false, // while statements don't swap branches
            originalCode,
            resultCode);
    }

    /// <summary>
    /// Executes the invert condition refactoring.
    /// Returns edits to apply or an error result.
    /// </summary>
    public GDRefactoringResult Execute(GDRefactoringContext context)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot invert condition at this position");

        var ifStmt = context.FindParent<GDIfStatement>();
        if (ifStmt != null)
        {
            return InvertIfStatement(context, ifStmt);
        }

        var whileStmt = context.FindParent<GDWhileStatement>();
        if (whileStmt != null)
        {
            return InvertWhileCondition(context, whileStmt);
        }

        return GDRefactoringResult.Failed("No if or while statement found at cursor");
    }

    private GDRefactoringResult InvertIfStatement(GDRefactoringContext context, GDIfStatement ifStmt)
    {
        var condition = ifStmt.IfBranch?.Condition;

        if (condition == null)
            return GDRefactoringResult.Failed("No condition found in if statement");

        // Invert the condition
        var invertedConditionText = InvertExpression(condition);

        // Check if we should swap if/else branches
        var hasElse = ifStmt.ElseBranch != null && ifStmt.ElseBranch.Statements?.Count > 0;
        var hasElif = ifStmt.ElifBranchesList != null && ifStmt.ElifBranchesList.Count > 0;

        var filePath = context.Script.Reference.FullPath;

        if (hasElse && !hasElif)
        {
            // Simple if/else - swap branches and invert condition
            return SwapIfElseBranches(filePath, ifStmt, invertedConditionText);
        }
        else
        {
            // Just invert the condition, don't swap branches
            // (elif chains are complex to swap correctly)
            var edit = new GDTextEdit(
                filePath,
                condition.StartLine,
                condition.StartColumn,
                condition.ToString(),
                invertedConditionText);

            return GDRefactoringResult.Succeeded(edit);
        }
    }

    private GDRefactoringResult SwapIfElseBranches(string filePath, GDIfStatement ifStmt, string invertedCondition)
    {
        var ifBranch = ifStmt.IfBranch;
        var elseBranch = ifStmt.ElseBranch;

        if (ifBranch?.Statements == null || elseBranch?.Statements == null)
            return GDRefactoringResult.Failed("Cannot swap branches: missing statements");

        // Get the text of both branches
        var ifStatementsText = GetStatementsText(ifBranch.Statements);
        var elseStatementsText = GetStatementsText(elseBranch.Statements);

        // Get the indentation from the if statement position
        var indent = GetIndentationFromStatementPosition(ifStmt);
        var innerIndent = indent + "\t";

        // Build the new swapped if/else
        var newText = new StringBuilder();
        newText.Append($"if {invertedCondition}:\n");
        newText.Append(AddIndentation(elseStatementsText, innerIndent));
        newText.Append($"\n{indent}else:\n");
        newText.Append(AddIndentation(ifStatementsText, innerIndent));

        // Replace the entire if statement
        var edit = new GDTextEdit(
            filePath,
            ifStmt.StartLine,
            ifStmt.StartColumn,
            GetFullStatementText(ifStmt),
            newText.ToString());

        return GDRefactoringResult.Succeeded(edit);
    }

    private string GetIndentationFromStatementPosition(GDStatement stmt)
    {
        // Build indentation based on column position
        var column = stmt.StartColumn;
        return new string('\t', column / 4); // Approximate tabs based on column
    }

    private string GetFullStatementText(GDIfStatement ifStmt)
    {
        // Reconstruct the full if statement text from AST
        return ifStmt.ToString();
    }

    private GDRefactoringResult InvertWhileCondition(GDRefactoringContext context, GDWhileStatement whileStmt)
    {
        var condition = whileStmt.Condition;

        if (condition == null)
            return GDRefactoringResult.Failed("No condition found in while statement");

        // Invert the condition
        var invertedConditionText = InvertExpression(condition);

        var filePath = context.Script.Reference.FullPath;
        var edit = new GDTextEdit(
            filePath,
            condition.StartLine,
            condition.StartColumn,
            condition.ToString(),
            invertedConditionText);

        return GDRefactoringResult.Succeeded(edit);
    }

    #region Expression Inversion Logic

    /// <summary>
    /// Inverts an expression using De Morgan's laws and operator inversions.
    /// </summary>
    public string InvertExpression(GDExpression expr)
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
            _ when IsAndOperator(op) => $"(not {WrapIfComplex(dual.LeftExpression, left)}) or (not {WrapIfComplex(dual.RightExpression, right)})",
            _ when IsOrOperator(op) => $"(not {WrapIfComplex(dual.LeftExpression, left)}) and (not {WrapIfComplex(dual.RightExpression, right)})",

            // Default: wrap with not
            _ => $"not ({dual})"
        };
    }

    private static bool IsEqualOperator(GDDualOperator op) => op?.ToString()?.Trim() == "==";
    private static bool IsNotEqualOperator(GDDualOperator op) => op?.ToString()?.Trim() == "!=";
    private static bool IsGreaterOperator(GDDualOperator op) => op?.ToString()?.Trim() == ">";
    private static bool IsLessOperator(GDDualOperator op) => op?.ToString()?.Trim() == "<";
    private static bool IsGreaterOrEqualOperator(GDDualOperator op) => op?.ToString()?.Trim() == ">=";
    private static bool IsLessOrEqualOperator(GDDualOperator op) => op?.ToString()?.Trim() == "<=";
    private static bool IsAndOperator(GDDualOperator op)
    {
        var text = op?.ToString()?.Trim()?.ToLower();
        return text == "and" || text == "&&";
    }
    private static bool IsOrOperator(GDDualOperator op)
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

    private static bool NeedsParentheses(GDExpression expr)
    {
        // Complex expressions need parentheses when wrapped with "not"
        return expr is GDDualOperatorExpression
            || expr is GDIfExpression  // Ternary if expression in GDScript
            || expr is GDCallExpression;
    }

    private static string WrapIfComplex(GDExpression expr, string text)
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

    #endregion

    #region Helper Methods

    private static string GetStatementsText(GDStatementsList statements)
    {
        if (statements == null || statements.Count == 0)
            return "pass";

        var result = new StringBuilder();
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

    private static string AddIndentation(string text, string indent)
    {
        var lines = text.Split('\n');
        var result = new StringBuilder();
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

    #endregion
}

/// <summary>
/// Result of invert condition planning operation.
/// </summary>
public class GDInvertConditionResult : GDRefactoringResult
{
    /// <summary>
    /// Original condition text before inversion.
    /// </summary>
    public string OriginalCondition { get; }

    /// <summary>
    /// Inverted condition text.
    /// </summary>
    public string InvertedCondition { get; }

    /// <summary>
    /// Whether if/else branches will be swapped.
    /// </summary>
    public bool WillSwapBranches { get; }

    /// <summary>
    /// Full original statement text (for preview).
    /// </summary>
    public string OriginalCode { get; }

    /// <summary>
    /// Full resulting statement text (for preview).
    /// </summary>
    public string ResultCode { get; }

    private GDInvertConditionResult(
        bool success,
        string errorMessage,
        IReadOnlyList<GDTextEdit> edits,
        string originalCondition,
        string invertedCondition,
        bool willSwapBranches,
        string originalCode,
        string resultCode)
        : base(success, errorMessage, edits)
    {
        OriginalCondition = originalCondition;
        InvertedCondition = invertedCondition;
        WillSwapBranches = willSwapBranches;
        OriginalCode = originalCode;
        ResultCode = resultCode;
    }

    /// <summary>
    /// Creates a planned result with preview information.
    /// </summary>
    public static GDInvertConditionResult Planned(
        string originalCondition,
        string invertedCondition,
        bool willSwapBranches,
        string originalCode,
        string resultCode)
    {
        return new GDInvertConditionResult(
            true, null, null,
            originalCondition, invertedCondition, willSwapBranches,
            originalCode, resultCode);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDInvertConditionResult Failed(string errorMessage)
    {
        return new GDInvertConditionResult(
            false, errorMessage, null,
            null, null, false, null, null);
    }
}
