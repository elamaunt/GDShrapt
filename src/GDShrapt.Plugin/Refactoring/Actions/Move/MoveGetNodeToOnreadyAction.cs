using GDShrapt.Plugin.Refactoring.UI;
using GDShrapt.Reader;
using Godot;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GDShrapt.Plugin.Refactoring.Actions.Move;

/// <summary>
/// Moves a get_node() call or $NodePath expression from inside a method
/// to an @onready variable declaration at class level.
/// </summary>
internal class MoveGetNodeToOnreadyAction : IRefactoringAction
{
    public string Id => "move_getnode_to_onready";
    public string DisplayName => "Move to @onready";
    public RefactoringCategory Category => RefactoringCategory.Move;
    public string Shortcut => null;
    public int Priority => 10;

    public bool IsAvailable(RefactoringContext context)
    {
        if (context?.ContainingClass == null)
            return false;

        // Must be inside a method, not at class level
        if (context.ContainingMethod == null)
            return false;

        // Must be on a get_node call or $NodePath
        return context.IsOnGetNodeCall || context.IsOnNodePath;
    }

    public async Task ExecuteAsync(RefactoringContext context)
    {
        Logger.Info("MoveGetNodeToOnreadyAction: Starting execution");

        var editor = context.Editor;
        var @class = context.ContainingClass;

        // Get the node path expression
        var nodePathExpr = GetNodePathExpression(context);
        if (nodePathExpr == null)
        {
            Logger.Info("MoveGetNodeToOnreadyAction: No node path expression found");
            return;
        }

        var nodePath = ExtractNodePath(nodePathExpr);
        Logger.Info($"MoveGetNodeToOnreadyAction: Node path: {nodePath}");

        // Suggest a variable name
        var suggestedName = SuggestVariableName(nodePath);

        // Show dialog for variable name
        var dialog = new NameInputDialog();
        context.DialogParent?.AddChild(dialog);

        var varName = await dialog.ShowForResult(
            "Move to @onready",
            suggestedName,
            $"@onready var {suggestedName} = {nodePathExpr}");

        dialog.QueueFree();

        if (string.IsNullOrEmpty(varName))
        {
            Logger.Info("MoveGetNodeToOnreadyAction: Cancelled by user");
            return;
        }

        // Validate variable name
        varName = ValidateVariableName(varName);
        Logger.Info($"MoveGetNodeToOnreadyAction: Using variable name '{varName}'");

        // Get position of the expression to replace
        var exprStartLine = nodePathExpr.StartLine;
        var exprStartColumn = nodePathExpr.StartColumn;
        var exprEndLine = nodePathExpr.EndLine;
        var exprEndColumn = nodePathExpr.EndColumn;

        // Create the @onready declaration
        var onreadyDecl = $"@onready var {varName} = {nodePathExpr}\n";

        // Find insertion point for @onready (after extends/class_name, with other @onready vars)
        var insertLine = FindOnreadyInsertionLine(@class);

        Logger.Info($"MoveGetNodeToOnreadyAction: Inserting @onready at line {insertLine}");

        // Replace the expression with the variable name
        editor.Select(exprStartLine, exprStartColumn, exprEndLine, exprEndColumn);
        editor.Cut();
        editor.InsertTextAtCursor(varName);

        // Insert the @onready declaration
        editor.CursorLine = insertLine;
        editor.CursorColumn = 0;
        editor.InsertTextAtCursor(onreadyDecl);

        editor.ReloadScriptFromText();
        Logger.Info("MoveGetNodeToOnreadyAction: Completed successfully");
    }

    private GDExpression GetNodePathExpression(RefactoringContext context)
    {
        var node = context.NodeAtCursor;

        // Check for $NodePath
        if (node is GDNodePathExpression nodePath)
            return nodePath;

        // Check parent for $NodePath
        var parentNodePath = context.FindParent<GDNodePathExpression>();
        if (parentNodePath != null)
            return parentNodePath;

        // Check for get_node() call
        if (node is GDCallExpression call)
        {
            var calledExpr = call.CallerExpression?.ToString();
            if (calledExpr == "get_node" || calledExpr == "get_node_or_null")
                return call;
        }

        // Check parent for get_node call
        var parentCall = context.FindParent<GDCallExpression>();
        if (parentCall != null)
        {
            var calledExpr = parentCall.CallerExpression?.ToString();
            if (calledExpr == "get_node" || calledExpr == "get_node_or_null")
                return parentCall;
        }

        return null;
    }

    private string ExtractNodePath(GDExpression expr)
    {
        if (expr is GDNodePathExpression nodePath)
        {
            // Extract path from $NodePath
            var fullText = nodePath.ToString();
            // Remove leading $ or %
            if (fullText.StartsWith("$") || fullText.StartsWith("%"))
                return fullText.Substring(1);
            return fullText;
        }

        if (expr is GDCallExpression call)
        {
            // Extract path from get_node("path")
            var args = call.Parameters;
            if (args != null && args.Count > 0)
            {
                var firstArg = args.First()?.ToString();
                if (firstArg != null)
                {
                    // Remove quotes
                    return firstArg.Trim('"', '\'');
                }
            }
        }

        return "node";
    }

    private string SuggestVariableName(string nodePath)
    {
        // Extract the last part of the path
        // $Player/Sprite -> _sprite
        // $UI/HealthBar -> _health_bar
        // ../Enemy -> _enemy

        var parts = nodePath.Split('/');
        var lastPart = parts.Last();

        // Remove special characters
        lastPart = lastPart.TrimStart('.', '%', '$');

        // Convert to snake_case
        lastPart = ToSnakeCase(lastPart);

        // Add underscore prefix for private variable
        if (!lastPart.StartsWith("_"))
            lastPart = "_" + lastPart;

        return lastPart;
    }

    private string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "node";

        // Insert underscore before uppercase letters
        var result = Regex.Replace(input, @"([a-z])([A-Z])", "$1_$2");
        // Convert to lowercase
        result = result.ToLowerInvariant();
        // Replace non-alphanumeric with underscores
        result = Regex.Replace(result, @"[^a-z0-9_]", "_");
        // Remove consecutive underscores
        result = Regex.Replace(result, @"_+", "_");
        // Trim underscores
        result = result.Trim('_');

        return string.IsNullOrEmpty(result) ? "node" : result;
    }

    private string ValidateVariableName(string name)
    {
        name = name.Trim();

        // Replace invalid characters
        name = Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");

        // Ensure starts with letter or underscore
        if (!string.IsNullOrEmpty(name) && char.IsDigit(name[0]))
            name = "_" + name;

        // Convert to snake_case
        name = ToSnakeCase(name);

        // Add underscore prefix if missing
        if (!string.IsNullOrEmpty(name) && !name.StartsWith("_"))
            name = "_" + name;

        return string.IsNullOrEmpty(name) ? "_node" : name;
    }

    private int FindOnreadyInsertionLine(GDClassDeclaration @class)
    {
        var insertLine = 0;

        // Skip extends declaration if present
        if (@class.Extends != null)
            insertLine = System.Math.Max(insertLine, @class.Extends.EndLine + 1);

        // Skip class_name if present
        if (@class.ClassName != null)
            insertLine = System.Math.Max(insertLine, @class.ClassName.EndLine + 1);

        // Find existing @onready variables and insert after them
        // Note: @onready is an attribute (GDAttribute) that precedes the variable declaration
        // We look for variables that have @onready attributes by checking preceding nodes
        foreach (var member in @class.Members.OfType<GDVariableDeclaration>())
        {
            // Check if this variable has an @onready attribute by checking if it's not const/static
            // and is initialized (typical @onready pattern)
            if (member.ConstKeyword == null && member.Initializer != null)
            {
                // This might be an @onready variable - insert after it
                insertLine = System.Math.Max(insertLine, member.EndLine + 1);
            }
        }

        // Also check for constants (insert after them)
        foreach (var member in @class.Members.OfType<GDVariableDeclaration>())
        {
            if (member.ConstKeyword != null)
            {
                insertLine = System.Math.Max(insertLine, member.EndLine + 1);
            }
        }

        return insertLine;
    }
}
