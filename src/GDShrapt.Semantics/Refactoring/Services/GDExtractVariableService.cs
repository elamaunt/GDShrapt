using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for extracting selected expressions into local variables.
/// </summary>
public class GDExtractVariableService
{
    /// <summary>
    /// Checks if the extract variable refactoring can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (context?.ClassDeclaration == null)
            return false;

        // Must have an expression selected or be on an expression
        return context.HasExpressionSelected || context.SelectedExpression != null;
    }

    /// <summary>
    /// Plans the extract variable refactoring without applying changes.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="variableName">Name for the new variable (optional)</param>
    /// <returns>Plan result with preview information</returns>
    public GDExtractVariableResult Plan(GDRefactoringContext context, string variableName = null)
    {
        if (!CanExecute(context))
            return GDExtractVariableResult.Failed("Cannot extract variable at this position");

        var expression = context.SelectedExpression;
        if (expression == null)
            return GDExtractVariableResult.Failed("No expression selected");

        var normalizedName = NormalizeVariableName(variableName);
        var inferredType = context.InferType(expression);

        // Count occurrences of the same expression
        var occurrences = FindOccurrences(context.ClassDeclaration, expression);

        return GDExtractVariableResult.Planned(
            normalizedName,
            inferredType,
            occurrences.Count,
            expression.ToString());
    }

    /// <summary>
    /// Executes the extract variable refactoring.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="variableName">Name for the new variable</param>
    /// <param name="replaceAll">Whether to replace all occurrences of the same expression</param>
    /// <returns>Result with text edits to apply</returns>
    public GDRefactoringResult Execute(GDRefactoringContext context, string variableName, bool replaceAll = false)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot extract variable at this position");

        var expression = context.SelectedExpression;
        if (expression == null)
            return GDRefactoringResult.Failed("No expression selected");

        var normalizedName = NormalizeVariableName(variableName);
        var filePath = context.Script.Reference.FullPath;

        // Get the statement containing the expression
        var containingStatement = FindContainingStatement(expression);
        if (containingStatement == null)
            return GDRefactoringResult.Failed("Could not find containing statement");

        // Build the variable declaration
        var inferredType = context.InferType(expression);
        var varDecl = BuildVariableDeclaration(normalizedName, inferredType, expression.ToString());

        var edits = new List<GDTextEdit>();

        // Get indentation for the variable declaration
        var indent = GetIndentation(containingStatement);

        // Edit 1: Insert variable declaration before the containing statement
        var insertEdit = new GDTextEdit(
            filePath,
            containingStatement.StartLine,
            0,
            "",
            $"{indent}{varDecl}\n");
        edits.Add(insertEdit);

        // Edit 2: Replace the expression with the variable reference
        var expressionText = expression.ToString();
        var replaceEdit = new GDTextEdit(
            filePath,
            expression.StartLine,
            expression.StartColumn,
            expressionText,
            normalizedName);
        edits.Add(replaceEdit);

        // If replaceAll, find and replace other occurrences
        if (replaceAll)
        {
            var occurrences = FindOccurrences(context.ClassDeclaration, expression);
            foreach (var occurrence in occurrences)
            {
                if (occurrence != expression) // Skip the original
                {
                    var occEdit = new GDTextEdit(
                        filePath,
                        occurrence.StartLine,
                        occurrence.StartColumn,
                        occurrence.ToString(),
                        normalizedName);
                    edits.Add(occEdit);
                }
            }
        }

        return GDRefactoringResult.Succeeded(edits);
    }

    /// <summary>
    /// Suggests a variable name based on the expression.
    /// </summary>
    public string SuggestVariableName(GDExpression expression)
    {
        if (expression == null)
            return "value";

        // Try to derive name from expression type
        switch (expression)
        {
            case GDCallExpression call:
                // Use function name as base
                var funcName = call.CallerExpression?.ToString() ?? "result";
                return ToSnakeCase(funcName);

            case GDMemberOperatorExpression member:
                // Use member name
                var memberName = member.Identifier?.Sequence ?? "value";
                return ToSnakeCase(memberName);

            case GDIdentifierExpression ident:
                // Already an identifier
                return ident.Identifier?.Sequence ?? "value";

            case GDStringExpression:
                return "text";

            case GDNumberExpression:
                return "value";

            case GDArrayInitializerExpression:
                return "items";

            case GDDictionaryInitializerExpression:
                return "dict";

            default:
                return "value";
        }
    }

    #region Helper Methods

    private string NormalizeVariableName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "new_variable";

        // Convert to snake_case
        var result = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                result.Append(char.ToLowerInvariant(c));
            }
            else if (char.IsWhiteSpace(c))
            {
                result.Append('_');
            }
        }

        var normalized = result.ToString();
        if (string.IsNullOrEmpty(normalized) || char.IsDigit(normalized[0]))
            return "new_variable";

        return normalized;
    }

    private string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "value";

        var result = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && !char.IsUpper(name[i - 1]))
                    result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    private string BuildVariableDeclaration(string name, string inferredType, string value)
    {
        if (!string.IsNullOrEmpty(inferredType) && inferredType != "Variant")
        {
            return $"var {name}: {inferredType} = {value}";
        }
        return $"var {name} = {value}";
    }

    private GDStatement FindContainingStatement(GDExpression expression)
    {
        var node = expression as GDNode;
        while (node != null)
        {
            if (node.Parent is GDStatement stmt)
                return stmt;
            node = node.Parent as GDNode;
        }
        return null;
    }

    private string GetIndentation(GDStatement statement)
    {
        // Find indentation from the statement's position
        if (statement?.Parent is GDStatementsList stmtList)
        {
            var indent = stmtList.Tokens.OfType<GDIntendation>().FirstOrDefault();
            if (indent != null)
            {
                return new string('\t', indent.LineIntendationThreshold);
            }
        }

        // Find from parent node
        var node = statement?.Parent;
        while (node != null)
        {
            var indentToken = node.Tokens.OfType<GDIntendation>().FirstOrDefault();
            if (indentToken != null)
            {
                return new string('\t', indentToken.LineIntendationThreshold);
            }
            node = node.Parent as GDNode;
        }

        return "\t"; // Default to single tab
    }

    private List<GDExpression> FindOccurrences(GDClassDeclaration classDecl, GDExpression expression)
    {
        var occurrences = new List<GDExpression>();
        var targetText = expression.ToString();

        foreach (var expr in classDecl.AllNodes.OfType<GDExpression>())
        {
            if (expr.ToString() == targetText)
            {
                occurrences.Add(expr);
            }
        }

        return occurrences;
    }

    #endregion
}

/// <summary>
/// Result of extract variable planning operation.
/// </summary>
public class GDExtractVariableResult : GDRefactoringResult
{
    /// <summary>
    /// The suggested variable name.
    /// </summary>
    public string SuggestedName { get; }

    /// <summary>
    /// The inferred type of the expression.
    /// </summary>
    public string InferredType { get; }

    /// <summary>
    /// Number of occurrences of the same expression found.
    /// </summary>
    public int OccurrencesCount { get; }

    /// <summary>
    /// The expression text that will be extracted.
    /// </summary>
    public string ExpressionText { get; }

    private GDExtractVariableResult(
        bool success,
        string errorMessage,
        IReadOnlyList<GDTextEdit> edits,
        string suggestedName,
        string inferredType,
        int occurrencesCount,
        string expressionText)
        : base(success, errorMessage, edits)
    {
        SuggestedName = suggestedName;
        InferredType = inferredType;
        OccurrencesCount = occurrencesCount;
        ExpressionText = expressionText;
    }

    /// <summary>
    /// Creates a planned result with preview information.
    /// </summary>
    public static GDExtractVariableResult Planned(
        string suggestedName,
        string inferredType,
        int occurrencesCount,
        string expressionText)
    {
        return new GDExtractVariableResult(
            true, null, null,
            suggestedName, inferredType, occurrencesCount, expressionText);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDExtractVariableResult Failed(string errorMessage)
    {
        return new GDExtractVariableResult(
            false, errorMessage, null,
            null, null, 0, null);
    }
}
