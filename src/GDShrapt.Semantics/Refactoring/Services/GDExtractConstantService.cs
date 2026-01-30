using System;
using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for extracting literal values into constants at class level.
/// </summary>
public class GDExtractConstantService : GDRefactoringServiceBase
{
    /// <summary>
    /// Checks if the extract constant refactoring can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return false;

        // Must be on a literal or have a literal selected
        return context.SelectedExpression.IsLiteral() ||
               (context.NodeAtCursor as GDExpression).IsLiteral();
    }

    /// <summary>
    /// Plans the extract constant refactoring without applying changes.
    /// </summary>
    public GDExtractConstantResult Plan(GDRefactoringContext context, string suggestedName = null)
    {
        if (!CanExecute(context))
            return GDExtractConstantResult.Failed("Cannot extract constant at this position");

        var literal = GetLiteralExpression(context);
        if (literal == null)
            return GDExtractConstantResult.Failed("No literal expression found");

        var classDecl = context.ClassDeclaration;
        var existingNames = GDMemberCollector.CollectAllReservedNames(classDecl);

        // Generate or validate suggested name
        var constantName = string.IsNullOrWhiteSpace(suggestedName)
            ? SuggestConstantName(literal)
            : ValidateConstantName(suggestedName);

        constantName = GDNamingUtilities.GenerateUniqueName(constantName, existingNames);

        // Find insertion point
        var insertionLine = GDIndentationUtilities.FindConstantInsertionLine(classDecl);

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
    public GDRefactoringResult Execute(GDRefactoringContext context, string constantName)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot extract constant at this position");

        var literal = GetLiteralExpression(context);
        if (literal == null)
            return GDRefactoringResult.Failed("No literal expression found");

        var validatedName = ValidateConstantName(constantName);
        var filePath = GetFilePath(context);
        var classDecl = context.ClassDeclaration;

        // Check for name conflicts
        var conflictInfo = GDMemberCollector.CheckNameConflict(validatedName, classDecl);
        if (conflictInfo != null)
            return GDRefactoringResult.Failed($"Name '{validatedName}' conflicts with existing {conflictInfo}");

        // Find insertion point
        var insertionLine = GDIndentationUtilities.FindConstantInsertionLine(classDecl);

        // Create constant declaration text
        var literalValue = literal.ToString();
        var constDecl = $"const {validatedName} = {literalValue}";

        // Build edits using GDTextEditBuilder
        var builder = GDTextEditBuilder.ForFile(filePath)
            .InsertLine(insertionLine, constDecl)
            .Replace(literal.StartLine, literal.StartColumn, literalValue, validatedName);

        return builder.ToResult();
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
            str = GDNamingUtilities.ToScreamingSnakeCase(str);
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

    private GDExpression GetLiteralExpression(GDRefactoringContext context)
    {
        // First check selected expression
        if (context.SelectedExpression.IsLiteral())
            return context.SelectedExpression;

        // Check node at cursor
        if (context.NodeAtCursor is GDExpression expr && expr.IsLiteral())
            return expr;

        // Walk up the tree to find a literal
        var node = context.NodeAtCursor;
        while (node != null)
        {
            if (node is GDExpression e && e.IsLiteral())
                return e;
            node = node.Parent as GDNode;
        }

        return null;
    }

    private List<string> GetConflictingNames(string baseName, GDClassDeclaration classDecl)
    {
        var conflicts = new List<string>();
        var existingNames = GDMemberCollector.CollectAllReservedNames(classDecl);

        if (existingNames.Contains(baseName))
            conflicts.Add(baseName);

        return conflicts;
    }

    private static string ValidateConstantName(string name)
    {
        return GDNamingUtilities.NormalizeConstantName(name);
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
