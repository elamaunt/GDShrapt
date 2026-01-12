using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for extracting literal values into constants at class level.
/// </summary>
public class GDExtractConstantService
{
    /// <summary>
    /// Checks if the extract constant refactoring can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (context?.ClassDeclaration == null)
            return false;

        // Must be on a literal or have a literal selected
        return IsLiteralExpression(context.SelectedExpression) ||
               IsLiteralExpression(context.NodeAtCursor as GDExpression);
    }

    /// <summary>
    /// Plans the extract constant refactoring without applying changes.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="suggestedName">Name for the constant (optional)</param>
    /// <returns>Plan result with preview information</returns>
    public GDExtractConstantResult Plan(GDRefactoringContext context, string suggestedName = null)
    {
        if (!CanExecute(context))
            return GDExtractConstantResult.Failed("Cannot extract constant at this position");

        var literal = GetLiteralExpression(context);
        if (literal == null)
            return GDExtractConstantResult.Failed("No literal expression found");

        var classDecl = context.ClassDeclaration;
        var existingNames = CollectExistingNames(classDecl);

        // Generate or validate suggested name
        var constantName = string.IsNullOrWhiteSpace(suggestedName)
            ? SuggestConstantName(literal)
            : ValidateConstantName(suggestedName);

        constantName = EnsureUniqueName(constantName, existingNames);

        // Find insertion point
        var insertionLine = FindConstantInsertionLine(classDecl);

        // Get literal value
        var literalValue = literal.ToString();

        // Check for conflicts
        var conflictingNames = GetConflictingNames(constantName, classDecl);

        return GDExtractConstantResult.Planned(
            constantName,
            literalValue,
            insertionLine,
            conflictingNames);
    }

    /// <summary>
    /// Executes the extract constant refactoring.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="constantName">Name for the constant</param>
    /// <returns>Result with text edits to apply</returns>
    public GDRefactoringResult Execute(GDRefactoringContext context, string constantName)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot extract constant at this position");

        var literal = GetLiteralExpression(context);
        if (literal == null)
            return GDRefactoringResult.Failed("No literal expression found");

        var validatedName = ValidateConstantName(constantName);
        var filePath = context.Script.Reference.FullPath;
        var classDecl = context.ClassDeclaration;

        // Check for name conflicts
        var conflictInfo = CheckNameConflict(validatedName, classDecl);
        if (conflictInfo != null)
            return GDRefactoringResult.Failed($"Name '{validatedName}' conflicts with existing {conflictInfo}");

        var edits = new List<GDTextEdit>();

        // Find insertion point
        var insertionLine = FindConstantInsertionLine(classDecl);

        // Create constant declaration text
        var literalValue = literal.ToString();
        var constDecl = $"const {validatedName} = {literalValue}";

        // Edit 1: Insert constant declaration
        var insertEdit = new GDTextEdit(
            filePath,
            insertionLine,
            0,
            "",
            $"{constDecl}\n");
        edits.Add(insertEdit);

        // Edit 2: Replace literal with constant reference
        var replaceEdit = new GDTextEdit(
            filePath,
            literal.StartLine,
            literal.StartColumn,
            literalValue,
            validatedName);
        edits.Add(replaceEdit);

        return GDRefactoringResult.Succeeded(edits);
    }

    /// <summary>
    /// Suggests a constant name based on the literal expression.
    /// </summary>
    public string SuggestConstantName(GDExpression literal)
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

    #region Helper Methods

    private bool IsLiteralExpression(GDExpression expression)
    {
        return expression is GDNumberExpression ||
               expression is GDStringExpression ||
               expression is GDBoolExpression;
    }

    private GDExpression GetLiteralExpression(GDRefactoringContext context)
    {
        // First check selected expression
        if (context.SelectedExpression != null && IsLiteralExpression(context.SelectedExpression))
            return context.SelectedExpression;

        // Check node at cursor
        if (context.NodeAtCursor is GDExpression expr && IsLiteralExpression(expr))
            return expr;

        // Walk up the tree to find a literal
        var node = context.NodeAtCursor;
        while (node != null)
        {
            if (node is GDExpression e && IsLiteralExpression(e))
                return e;
            node = node.Parent as GDNode;
        }

        return null;
    }

    private HashSet<string> CollectExistingNames(GDClassDeclaration classDecl)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add class member names
        foreach (var member in classDecl.Members)
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
            else if (member is GDEnumDeclaration enumDecl)
            {
                if (enumDecl.Identifier != null)
                    names.Add(enumDecl.Identifier.Sequence);

                // Add enum values
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

        // Add GDScript keywords
        AddBuiltinNames(names);

        return names;
    }

    private void AddBuiltinNames(HashSet<string> names)
    {
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

    private string CheckNameConflict(string name, GDClassDeclaration classDecl)
    {
        foreach (var member in classDecl.Members)
        {
            if (member is GDVariableDeclaration varDecl && varDecl.Identifier != null)
            {
                if (string.Equals(varDecl.Identifier.Sequence, name, StringComparison.OrdinalIgnoreCase))
                {
                    var type = varDecl.ConstKeyword != null ? "constant" : "variable";
                    return $"{type} at line {varDecl.StartLine}";
                }
            }
            else if (member is GDMethodDeclaration methodDecl && methodDecl.Identifier != null)
            {
                if (string.Equals(methodDecl.Identifier.Sequence, name, StringComparison.OrdinalIgnoreCase))
                    return $"method at line {methodDecl.StartLine}";
            }
            else if (member is GDSignalDeclaration signalDecl && signalDecl.Identifier != null)
            {
                if (string.Equals(signalDecl.Identifier.Sequence, name, StringComparison.OrdinalIgnoreCase))
                    return $"signal at line {signalDecl.StartLine}";
            }
            else if (member is GDEnumDeclaration enumDecl)
            {
                if (enumDecl.Identifier != null &&
                    string.Equals(enumDecl.Identifier.Sequence, name, StringComparison.OrdinalIgnoreCase))
                    return $"enum at line {enumDecl.StartLine}";

                if (enumDecl.Values != null)
                {
                    foreach (var enumValue in enumDecl.Values.OfType<GDEnumValueDeclaration>())
                    {
                        if (enumValue.Identifier != null &&
                            string.Equals(enumValue.Identifier.Sequence, name, StringComparison.OrdinalIgnoreCase))
                            return $"enum value at line {enumValue.StartLine}";
                    }
                }
            }
        }

        return null;
    }

    private List<string> GetConflictingNames(string baseName, GDClassDeclaration classDecl)
    {
        var conflicts = new List<string>();
        var existingNames = CollectExistingNames(classDecl);

        if (existingNames.Contains(baseName))
            conflicts.Add(baseName);

        return conflicts;
    }

    private string EnsureUniqueName(string baseName, HashSet<string> existingNames)
    {
        if (!existingNames.Contains(baseName))
            return baseName;

        for (int i = 2; i <= 100; i++)
        {
            var candidateName = $"{baseName}_{i}";
            if (!existingNames.Contains(candidateName))
                return candidateName;
        }

        return $"{baseName}_{DateTime.Now.Ticks % 10000}";
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
        name = name?.Trim() ?? string.Empty;

        // Replace invalid characters
        name = Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");

        // Ensure starts with letter or underscore
        if (!string.IsNullOrEmpty(name) && char.IsDigit(name[0]))
            name = "_" + name;

        // Convert to uppercase for constants
        name = name.ToUpperInvariant();

        return string.IsNullOrEmpty(name) ? "CONSTANT" : name;
    }

    private int FindConstantInsertionLine(GDClassDeclaration classDecl)
    {
        var insertLine = 0;

        // Skip extends declaration if present
        if (classDecl.Extends != null)
            insertLine = Math.Max(insertLine, classDecl.Extends.EndLine + 1);

        // Skip class_name if present
        if (classDecl.ClassName != null)
            insertLine = Math.Max(insertLine, classDecl.ClassName.EndLine + 1);

        // Find existing constants and insert after them
        foreach (var member in classDecl.Members.OfType<GDVariableDeclaration>())
        {
            if (member.ConstKeyword != null)
            {
                insertLine = Math.Max(insertLine, member.EndLine + 1);
            }
            else
            {
                // Found a non-constant variable, insert before it
                break;
            }
        }

        return insertLine;
    }

    #endregion
}

/// <summary>
/// Result of extract constant planning operation.
/// </summary>
public class GDExtractConstantResult : GDRefactoringResult
{
    /// <summary>
    /// The suggested constant name.
    /// </summary>
    public string SuggestedName { get; }

    /// <summary>
    /// The literal value to be extracted.
    /// </summary>
    public string LiteralValue { get; }

    /// <summary>
    /// The line where the constant will be inserted.
    /// </summary>
    public int InsertionLine { get; }

    /// <summary>
    /// Names that conflict with the suggested name.
    /// </summary>
    public IReadOnlyList<string> ConflictingNames { get; }

    private GDExtractConstantResult(
        bool success,
        string errorMessage,
        IReadOnlyList<GDTextEdit> edits,
        string suggestedName,
        string literalValue,
        int insertionLine,
        IReadOnlyList<string> conflictingNames)
        : base(success, errorMessage, edits)
    {
        SuggestedName = suggestedName;
        LiteralValue = literalValue;
        InsertionLine = insertionLine;
        ConflictingNames = conflictingNames ?? Array.Empty<string>();
    }

    /// <summary>
    /// Creates a planned result with preview information.
    /// </summary>
    public static GDExtractConstantResult Planned(
        string suggestedName,
        string literalValue,
        int insertionLine,
        IReadOnlyList<string> conflictingNames)
    {
        return new GDExtractConstantResult(
            true, null, null,
            suggestedName, literalValue, insertionLine, conflictingNames);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDExtractConstantResult Failed(string errorMessage)
    {
        return new GDExtractConstantResult(
            false, errorMessage, null,
            null, null, 0, null);
    }
}
