using GDShrapt.Plugin.Refactoring.UI;
using GDShrapt.Reader;
using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GD = GDShrapt.Reader.GD;

namespace GDShrapt.Plugin.Refactoring.Actions.Extract;

/// <summary>
/// Extracts a literal value (number, string, bool) into a constant at class level.
/// </summary>
internal class ExtractConstantAction : IRefactoringAction
{
    public string Id => "extract_constant";
    public string DisplayName => "Extract Constant";
    public RefactoringCategory Category => RefactoringCategory.Extract;
    public string Shortcut => "Ctrl+Alt+C";
    public int Priority => 10;

    public bool IsAvailable(RefactoringContext context)
    {
        if (context?.ContainingClass == null)
            return false;

        // Available when cursor is on a literal (number, string, bool)
        // or when a literal is selected
        if (context.IsOnLiteral)
            return true;

        if (context.HasSelection && context.SelectedExpression != null)
        {
            return context.SelectedExpression is GDNumberExpression
                || context.SelectedExpression is GDStringExpression
                || context.SelectedExpression is GDBoolExpression;
        }

        return false;
    }

    public async Task ExecuteAsync(RefactoringContext context)
    {
        Logger.Info("ExtractConstantAction: Starting execution");

        var editor = context.Editor;
        var @class = context.ContainingClass;

        // Get the literal expression to extract
        GDExpression literalExpr = GetLiteralExpression(context);
        if (literalExpr == null)
        {
            Logger.Info("ExtractConstantAction: No literal expression found");
            return;
        }

        var literalValue = literalExpr.ToString();
        Logger.Info($"ExtractConstantAction: Extracting literal '{literalValue}'");

        // Collect existing names to avoid conflicts
        var existingNames = CollectExistingNames(@class, context);

        // Suggest a constant name (ensure uniqueness)
        var suggestedName = SuggestConstantName(literalExpr);
        suggestedName = EnsureUniqueName(suggestedName, existingNames);

        // Show dialog for constant name
        var dialog = new NameInputDialog();
        context.DialogParent?.AddChild(dialog);

        var constName = await dialog.ShowForResult("Extract Constant", suggestedName, $"const {suggestedName} = {literalValue}");

        dialog.QueueFree();

        if (string.IsNullOrEmpty(constName))
        {
            Logger.Info("ExtractConstantAction: Cancelled by user");
            return;
        }

        // Validate and clean the constant name
        constName = ValidateConstantName(constName);

        // Check for name conflicts with user-provided name
        var conflictInfo = CheckNameConflict(constName, @class, context);
        if (conflictInfo != null)
        {
            Logger.Warning($"ExtractConstantAction: Name conflict detected - {conflictInfo}");

            // Suggest a unique alternative
            var uniqueName = EnsureUniqueName(constName, existingNames);

            // Show conflict warning and ask user to confirm or use alternative
            var confirmDialog = new NameInputDialog();
            context.DialogParent?.AddChild(confirmDialog);

            var resolvedName = await confirmDialog.ShowForResult(
                "Name Conflict",
                uniqueName,
                $"'{constName}' already exists ({conflictInfo}). Use '{uniqueName}' instead?");

            confirmDialog.QueueFree();

            if (string.IsNullOrEmpty(resolvedName))
            {
                Logger.Info("ExtractConstantAction: Cancelled due to name conflict");
                return;
            }

            constName = ValidateConstantName(resolvedName);

            // Re-check if the new name is also conflicting
            if (CheckNameConflict(constName, @class, context) != null)
            {
                Logger.Error($"ExtractConstantAction: Name '{constName}' still conflicts, aborting");
                return;
            }
        }

        Logger.Info($"ExtractConstantAction: Using constant name '{constName}'");

        // Get position info for the literal
        var startLine = literalExpr.StartLine;
        var startColumn = literalExpr.StartColumn;
        var endLine = literalExpr.EndLine;
        var endColumn = literalExpr.EndColumn;

        // Create the constant declaration
        var constDecl = CreateConstantDeclaration(constName, literalExpr);
        var constText = constDecl.ToString();

        // Find insertion point (after extends/class_name, before other members)
        var insertLine = FindConstantInsertionLine(@class);

        Logger.Info($"ExtractConstantAction: Inserting constant at line {insertLine}");

        // Replace the literal with the constant name in the editor
        editor.Select(startLine, startColumn, endLine, endColumn);
        editor.Cut();
        editor.InsertTextAtCursor(constName);

        // Insert the constant declaration at the top of the class
        // Move to insertion line and add the constant
        var insertText = $"{constText}\n";

        // Get the current line content at insertion point
        editor.CursorLine = insertLine;
        editor.CursorColumn = 0;
        editor.InsertTextAtCursor(insertText);

        // Reload the script to update AST
        editor.ReloadScriptFromText();

        Logger.Info("ExtractConstantAction: Completed successfully");
    }

    /// <summary>
    /// Collects all existing identifier names in the class scope to detect conflicts.
    /// </summary>
    private HashSet<string> CollectExistingNames(GDClassDeclaration @class, RefactoringContext context)
    {
        var names = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        // Add class member names (constants, variables, methods, signals, enums)
        foreach (var member in @class.Members)
        {
            if (member is GDVariableDeclaration varDecl && varDecl.Identifier != null)
            {
                names.Add(varDecl.Identifier.Sequence);
            }
            else if (member is GDMethodDeclaration methodDecl && methodDecl.Identifier != null)
            {
                names.Add(methodDecl.Identifier.Sequence);
            }
            else if (member is GDSignalDeclaration signalDecl && signalDecl.Identifier != null)
            {
                names.Add(signalDecl.Identifier.Sequence);
            }
            else if (member is GDEnumDeclaration enumDecl && enumDecl.Identifier != null)
            {
                names.Add(enumDecl.Identifier.Sequence);

                // Also add enum values
                if (enumDecl.Values != null)
                {
                    foreach (var enumValue in enumDecl.Values.OfType<GDEnumValueDeclaration>())
                    {
                        if (enumValue.Identifier != null)
                            names.Add(enumValue.Identifier.Sequence);
                    }
                }
            }
            else if (member is GDInnerClassDeclaration innerClass && innerClass.Identifier != null)
            {
                names.Add(innerClass.Identifier.Sequence);
            }
        }

        // Add GDScript built-in keywords and common names
        AddBuiltinNames(names);

        return names;
    }

    /// <summary>
    /// Adds GDScript built-in names that should be avoided.
    /// </summary>
    private void AddBuiltinNames(HashSet<string> names)
    {
        // GDScript keywords
        var keywords = new[]
        {
            "if", "elif", "else", "for", "while", "match", "break", "continue",
            "pass", "return", "class", "class_name", "extends", "is", "as",
            "self", "signal", "func", "static", "const", "enum", "var",
            "onready", "export", "setget", "tool", "yield", "assert", "preload",
            "await", "in", "not", "and", "or", "true", "false", "null",
            "PI", "TAU", "INF", "NAN"
        };

        foreach (var keyword in keywords)
        {
            names.Add(keyword);
            names.Add(keyword.ToUpperInvariant());
        }
    }

    /// <summary>
    /// Checks if a name conflicts with existing identifiers.
    /// Returns a description of the conflict or null if no conflict.
    /// </summary>
    private string CheckNameConflict(string name, GDClassDeclaration @class, RefactoringContext context)
    {
        var upperName = name.ToUpperInvariant();

        foreach (var member in @class.Members)
        {
            if (member is GDVariableDeclaration varDecl && varDecl.Identifier != null)
            {
                if (string.Equals(varDecl.Identifier.Sequence, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    var type = varDecl.ConstKeyword != null ? "constant" : "variable";
                    return $"{type} at line {varDecl.StartLine}";
                }
            }
            else if (member is GDMethodDeclaration methodDecl && methodDecl.Identifier != null)
            {
                if (string.Equals(methodDecl.Identifier.Sequence, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return $"method at line {methodDecl.StartLine}";
                }
            }
            else if (member is GDSignalDeclaration signalDecl && signalDecl.Identifier != null)
            {
                if (string.Equals(signalDecl.Identifier.Sequence, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return $"signal at line {signalDecl.StartLine}";
                }
            }
            else if (member is GDEnumDeclaration enumDecl)
            {
                if (enumDecl.Identifier != null &&
                    string.Equals(enumDecl.Identifier.Sequence, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return $"enum at line {enumDecl.StartLine}";
                }

                // Check enum values
                if (enumDecl.Values != null)
                {
                    foreach (var enumValue in enumDecl.Values.OfType<GDEnumValueDeclaration>())
                    {
                        if (enumValue.Identifier != null &&
                            string.Equals(enumValue.Identifier.Sequence, name, System.StringComparison.OrdinalIgnoreCase))
                        {
                            return $"enum value at line {enumValue.StartLine}";
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Ensures the name is unique by appending a number if necessary.
    /// </summary>
    private string EnsureUniqueName(string baseName, HashSet<string> existingNames)
    {
        if (!existingNames.Contains(baseName))
            return baseName;

        // Try appending numbers
        for (int i = 2; i <= 100; i++)
        {
            var candidateName = $"{baseName}_{i}";
            if (!existingNames.Contains(candidateName))
                return candidateName;
        }

        // Fallback with timestamp
        return $"{baseName}_{System.DateTime.Now.Ticks % 10000}";
    }

    private GDExpression GetLiteralExpression(RefactoringContext context)
    {
        // First check if a specific expression is selected
        if (context.SelectedExpression != null)
        {
            if (context.SelectedExpression is GDNumberExpression ||
                context.SelectedExpression is GDStringExpression ||
                context.SelectedExpression is GDBoolExpression)
            {
                return context.SelectedExpression;
            }
        }

        // Check node at cursor
        if (context.NodeAtCursor is GDNumberExpression numExpr)
            return numExpr;
        if (context.NodeAtCursor is GDStringExpression strExpr)
            return strExpr;
        if (context.NodeAtCursor is GDBoolExpression boolExpr)
            return boolExpr;

        // Check if we can find a literal in parent
        var node = context.NodeAtCursor;
        while (node != null)
        {
            if (node is GDNumberExpression n)
                return n;
            if (node is GDStringExpression s)
                return s;
            if (node is GDBoolExpression b)
                return b;
            node = node.Parent as GDNode;
        }

        return null;
    }

    private string SuggestConstantName(GDExpression literal)
    {
        if (literal is GDNumberExpression numExpr)
        {
            var numStr = numExpr.ToString();
            // Handle negative numbers
            if (numStr.StartsWith("-"))
                numStr = "NEG_" + numStr.Substring(1);
            // Handle decimals
            numStr = numStr.Replace(".", "_");
            return "VALUE_" + numStr.ToUpperInvariant();
        }

        if (literal is GDStringExpression strExpr)
        {
            var str = strExpr.ToString();
            // Remove quotes
            str = str.Trim('"', '\'');
            // Convert to SCREAMING_SNAKE_CASE
            str = ToScreamingSnakeCase(str);
            // Limit length
            if (str.Length > 30)
                str = str.Substring(0, 30);
            // Ensure valid identifier
            if (string.IsNullOrEmpty(str) || !char.IsLetter(str[0]))
                str = "STRING_" + str;
            return str;
        }

        if (literal is GDBoolExpression boolExpr)
        {
            return boolExpr.Value == true ? "IS_ENABLED" : "IS_DISABLED";
        }

        return "CONSTANT";
    }

    private string ToScreamingSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Replace non-alphanumeric with underscores
        var result = Regex.Replace(input, @"[^a-zA-Z0-9]", "_");
        // Insert underscore before uppercase letters
        result = Regex.Replace(result, @"([a-z])([A-Z])", "$1_$2");
        // Convert to uppercase
        result = result.ToUpperInvariant();
        // Remove consecutive underscores
        result = Regex.Replace(result, @"_+", "_");
        // Trim underscores
        result = result.Trim('_');

        return result;
    }

    private string ValidateConstantName(string name)
    {
        // Ensure valid GDScript identifier
        name = name.Trim();

        // Replace invalid characters
        name = Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");

        // Ensure starts with letter or underscore
        if (!string.IsNullOrEmpty(name) && char.IsDigit(name[0]))
            name = "_" + name;

        // Convert to uppercase for constants
        name = name.ToUpperInvariant();

        return string.IsNullOrEmpty(name) ? "CONSTANT" : name;
    }

    private GDVariableDeclaration CreateConstantDeclaration(string name, GDExpression value)
    {
        // Clone the expression to avoid modifying the original AST
        var clonedValue = (GDExpression)value.Clone();
        return GD.Declaration.Const(name, clonedValue);
    }

    private int FindConstantInsertionLine(GDClassDeclaration @class)
    {
        // Find the best line to insert the constant
        // After: extends, class_name
        // Before: other members (or with other constants)

        var insertLine = 0;

        // Skip extends declaration if present
        if (@class.Extends != null)
            insertLine = System.Math.Max(insertLine, @class.Extends.EndLine + 1);

        // Skip class_name if present
        if (@class.ClassName != null)
            insertLine = System.Math.Max(insertLine, @class.ClassName.EndLine + 1);

        // Find existing constants and insert after them
        foreach (var member in @class.Members.OfType<GDVariableDeclaration>())
        {
            if (member.ConstKeyword != null)
            {
                insertLine = System.Math.Max(insertLine, member.EndLine + 1);
            }
            else
            {
                // Found a non-constant variable, insert before it
                break;
            }
        }

        return insertLine;
    }
}
