using GDShrapt.Plugin.Refactoring.UI;
using GDShrapt.Reader;
using System.Threading.Tasks;

namespace GDShrapt.Plugin.Refactoring.Actions.Extract;

/// <summary>
/// Extracts an expression into a local variable.
/// </summary>
internal class ExtractVariableAction : RefactoringActionBase
{
    public override string Id => "extract_variable";
    public override string DisplayName => "Extract Variable";
    public override RefactoringCategory Category => RefactoringCategory.Extract;
    public override string Shortcut => "Ctrl+Alt+V";
    public override int Priority => 5;

    public override bool IsAvailable(RefactoringContext context)
    {
        if (context?.ContainingMethod == null)
            return false;

        // Must have a selected expression or be on an expression
        if (context.HasSelection && context.SelectedExpression != null)
            return true;

        // Check if cursor is on an expression that can be extracted
        if (context.NodeAtCursor is GDExpression expr && CanExtract(expr))
            return true;

        return false;
    }

    private bool CanExtract(GDExpression expr)
    {
        // Don't extract simple identifiers (already variables)
        if (expr is GDIdentifierExpression)
            return false;

        // Don't extract literals that are too simple
        // (unless they're in complex expressions)
        if (expr.Parent is GDVariableDeclaration || expr.Parent is GDVariableDeclarationStatement)
            return false;

        return true;
    }

    protected override string ValidateContext(RefactoringContext context)
    {
        if (context.Editor == null)
            return "No editor available";

        if (context.ContainingMethod == null)
            return "Not inside a method";

        var expression = GetExpressionToExtract(context);
        if (expression == null)
            return "No expression found to extract";

        return null;
    }

    protected override async Task ExecuteInternalAsync(RefactoringContext context)
    {
        var editor = context.Editor;
        var method = context.ContainingMethod;

        // Get the expression to extract
        GDExpression expression = GetExpressionToExtract(context);
        if (expression == null)
        {
            throw new RefactoringException("No expression found to extract");
        }

        var exprText = expression.ToString();
        Logger.Info($"ExtractVariableAction: Extracting expression '{exprText}'");

        // Suggest a variable name based on expression type
        var suggestedName = SuggestVariableName(expression, context);

        // Show dialog for variable name
        var dialog = new NameInputDialog();
        context.DialogParent?.AddChild(dialog);

        var varName = await dialog.ShowForResult("Extract Variable", suggestedName, $"var {suggestedName} = {exprText}");

        dialog.QueueFree();

        if (string.IsNullOrEmpty(varName))
        {
            Logger.Info("ExtractVariableAction: Cancelled by user");
            return;
        }

        varName = ValidateVariableName(varName);
        Logger.Info($"ExtractVariableAction: Using variable name '{varName}'");

        // Get position info for the expression
        var startLine = expression.StartLine;
        var startColumn = expression.StartColumn;
        var endLine = expression.EndLine;
        var endColumn = expression.EndColumn;

        // Find the statement containing this expression
        var containingStatement = FindContainingStatement(expression);
        if (containingStatement == null)
        {
            Logger.Info("ExtractVariableAction: Could not find containing statement");
            return;
        }

        // Calculate indentation from the containing statement
        var statementLine = containingStatement.StartLine;
        var lineText = editor.GetLine(statementLine);
        var indent = GetIndentation(lineText);

        // Create the variable declaration
        var varDecl = $"{indent}var {varName} = {exprText}\n";

        // Insert the variable declaration before the containing statement
        editor.CursorLine = statementLine;
        editor.CursorColumn = 0;
        editor.InsertTextAtCursor(varDecl);

        // Adjust positions because we inserted a line
        var adjustedStartLine = startLine + 1;
        var adjustedEndLine = endLine + 1;

        // Replace the expression with the variable name
        editor.Select(adjustedStartLine, startColumn, adjustedEndLine, endColumn);
        editor.Cut();
        editor.InsertTextAtCursor(varName);

        editor.ReloadScriptFromText();

        Logger.Info("ExtractVariableAction: Completed successfully");
    }

    private GDExpression GetExpressionToExtract(RefactoringContext context)
    {
        if (context.SelectedExpression != null)
            return context.SelectedExpression;

        if (context.NodeAtCursor is GDExpression expr)
            return expr;

        // Walk up to find an expression
        var node = context.NodeAtCursor;
        while (node != null)
        {
            if (node is GDExpression e && CanExtract(e))
                return e;
            node = node.Parent as GDNode;
        }

        return null;
    }

    private string SuggestVariableName(GDExpression expr, RefactoringContext context)
    {
        // Try to infer a name based on expression type
        if (expr is GDCallExpression call)
        {
            // Use method name: get_node() -> node, calculate_damage() -> damage
            var methodName = GetCallMethodName(call);
            if (!string.IsNullOrEmpty(methodName))
            {
                if (methodName.StartsWith("get_"))
                    return methodName.Substring(4);
                if (methodName.StartsWith("is_") || methodName.StartsWith("has_") || methodName.StartsWith("can_"))
                    return methodName;
                return methodName + "_result";
            }
        }

        if (expr is GDMemberOperatorExpression memberOp)
        {
            // Use member name: player.health -> health
            return memberOp.Identifier?.Sequence ?? "value";
        }

        if (expr is GDIndexerExpression)
        {
            return "item";
        }

        if (expr is GDDualOperatorExpression dualOp)
        {
            // For arithmetic: a + b -> sum, a - b -> diff, a * b -> product
            if (dualOp.OperatorType == GDDualOperatorType.Addition)
                return "sum";
            if (dualOp.OperatorType == GDDualOperatorType.Subtraction)
                return "difference";
            if (dualOp.OperatorType == GDDualOperatorType.Multiply)
                return "product";
            if (dualOp.OperatorType == GDDualOperatorType.Division)
                return "quotient";
            // For comparison: a == b -> is_equal
            if (IsComparisonOperator(dualOp.OperatorType))
                return "result";
        }

        if (expr is GDArrayInitializerExpression)
            return "items";

        if (expr is GDDictionaryInitializerExpression)
            return "dict";

        // Try to get type information from analyzer
        var analyzer = context.ScriptMap?.Analyzer;
        if (analyzer != null)
        {
            var typeName = analyzer.GetTypeForNode(expr);
            if (!string.IsNullOrEmpty(typeName))
            {
                return ToSnakeCase(typeName);
            }
        }

        return "value";
    }

    private string GetCallMethodName(GDCallExpression call)
    {
        if (call.CallerExpression is GDIdentifierExpression idExpr)
            return idExpr.Identifier?.Sequence;

        if (call.CallerExpression is GDMemberOperatorExpression memberOp)
            return memberOp.Identifier?.Sequence;

        return null;
    }

    private bool IsComparisonOperator(GDDualOperatorType opType)
    {
        return opType == GDDualOperatorType.Equal ||
               opType == GDDualOperatorType.NotEqual ||
               opType == GDDualOperatorType.LessThan ||
               opType == GDDualOperatorType.LessThanOrEqual ||
               opType == GDDualOperatorType.MoreThan ||
               opType == GDDualOperatorType.MoreThanOrEqual;
    }

    private string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "value";

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c) && i > 0)
            {
                result.Append('_');
            }
            result.Append(char.ToLowerInvariant(c));
        }

        var str = result.ToString();
        // Remove common prefixes like "GD" or "GDScript"
        if (str.StartsWith("g_d_"))
            str = str.Substring(4);

        return str;
    }

    private string ValidateVariableName(string name)
    {
        name = name.Trim();

        // Replace invalid characters
        name = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");

        // Ensure starts with letter or underscore
        if (!string.IsNullOrEmpty(name) && char.IsDigit(name[0]))
            name = "_" + name;

        // Convert to snake_case (lowercase)
        name = name.ToLowerInvariant();

        return string.IsNullOrEmpty(name) ? "value" : name;
    }

    private GDStatement FindContainingStatement(GDExpression expr)
    {
        var node = expr as GDNode;
        while (node != null)
        {
            if (node is GDStatement stmt)
                return stmt;
            node = node.Parent as GDNode;
        }
        return null;
    }

    private string GetIndentation(string lineText)
    {
        var indent = new System.Text.StringBuilder();
        foreach (var c in lineText)
        {
            if (c == ' ' || c == '\t')
                indent.Append(c);
            else
                break;
        }
        return indent.ToString();
    }
}
