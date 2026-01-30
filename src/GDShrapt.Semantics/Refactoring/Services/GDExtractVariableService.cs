using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for extracting selected expressions into local variables.
/// </summary>
public class GDExtractVariableService : GDRefactoringServiceBase
{
    /// <summary>
    /// Checks if the extract variable refactoring can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return false;

        // Must have an expression selected or be on an expression
        return context.HasExpressionSelected || context.SelectedExpression != null;
    }

    /// <summary>
    /// Plans the extract variable refactoring without applying changes.
    /// </summary>
    public GDExtractVariableResult Plan(GDRefactoringContext context, string variableName = null)
    {
        if (!CanExecute(context))
            return GDExtractVariableResult.Failed("Cannot extract variable at this position");

        var expression = context.SelectedExpression;
        if (expression == null)
            return GDExtractVariableResult.Failed("No expression selected");

        var normalizedName = GDNamingUtilities.NormalizeVariableName(variableName ?? "new_variable");

        // Infer type with confidence level
        var helper = new GDTypeInferenceHelper(context.GetSemanticModel());
        var inferredType = helper.InferExpressionType(expression);

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
    public GDRefactoringResult Execute(GDRefactoringContext context, string variableName, bool replaceAll = false)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot extract variable at this position");

        var expression = context.SelectedExpression;
        if (expression == null)
            return GDRefactoringResult.Failed("No expression selected");

        var normalizedName = GDNamingUtilities.NormalizeVariableName(variableName ?? "new_variable");
        var filePath = GetFilePath(context);

        // Get the statement containing the expression
        var containingStatement = expression.GetContainingStatement();
        if (containingStatement == null)
            return GDRefactoringResult.Failed("Could not find containing statement");

        // Build the variable declaration with inferred type
        var helper = new GDTypeInferenceHelper(context.GetSemanticModel());
        var inferredType = helper.InferExpressionType(expression);
        var varDecl = BuildVariableDeclaration(normalizedName, inferredType.TypeName, expression.ToString());

        // Get indentation for the variable declaration
        var indent = containingStatement.GetIndentation();
        var expressionText = expression.ToString();

        // Build edits
        var builder = GDTextEditBuilder.ForFile(filePath)
            .InsertLine(containingStatement.StartLine, $"{indent}{varDecl}")
            .Replace(expression.StartLine, expression.StartColumn, expressionText, normalizedName);

        // If replaceAll, find and replace other occurrences
        if (replaceAll)
        {
            var occurrences = FindOccurrences(context.ClassDeclaration, expression);
            foreach (var occurrence in occurrences)
            {
                if (occurrence != expression) // Skip the original
                {
                    builder.Replace(occurrence.StartLine, occurrence.StartColumn,
                        occurrence.ToString(), normalizedName);
                }
            }
        }

        return builder.ToResult();
    }

    /// <summary>
    /// Suggests a variable name based on the expression.
    /// </summary>
    public string SuggestVariableName(GDExpression expression)
    {
        if (expression == null)
            return "value";

        // Try to derive name from expression type
        return expression switch
        {
            GDCallExpression call => GDNamingUtilities.ToSnakeCase(call.CallerExpression?.ToString() ?? "result"),
            GDMemberOperatorExpression member => GDNamingUtilities.ToSnakeCase(member.Identifier?.Sequence ?? "value"),
            GDIdentifierExpression ident => ident.Identifier?.Sequence ?? "value",
            GDStringExpression => "text",
            GDNumberExpression => "value",
            GDArrayInitializerExpression => "items",
            GDDictionaryInitializerExpression => "dict",
            _ => "value"
        };
    }

    #region Helper Methods

    private static string BuildVariableDeclaration(string name, string? inferredType, string value)
    {
        if (!string.IsNullOrEmpty(inferredType) && inferredType != "Variant")
        {
            return $"var {name}: {inferredType} = {value}";
        }
        return $"var {name} = {value}";
    }

    private static List<GDExpression> FindOccurrences(GDClassDeclaration classDecl, GDExpression expression)
    {
        var targetText = expression.ToString();
        return classDecl.FindNodes<GDExpression>(e => e.ToString() == targetText).ToList();
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
    /// Confidence level of the type inference.
    /// </summary>
    public GDTypeConfidence TypeConfidence { get; }

    /// <summary>
    /// Reason for the confidence level.
    /// </summary>
    public string? TypeConfidenceReason { get; }

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
        GDTypeConfidence typeConfidence,
        string? typeConfidenceReason,
        int occurrencesCount,
        string expressionText)
        : base(success, errorMessage, edits)
    {
        SuggestedName = suggestedName;
        InferredType = inferredType;
        TypeConfidence = typeConfidence;
        TypeConfidenceReason = typeConfidenceReason;
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
            suggestedName, inferredType, GDTypeConfidence.Unknown, null, occurrencesCount, expressionText);
    }

    /// <summary>
    /// Creates a planned result with preview information and confidence level.
    /// </summary>
    public static GDExtractVariableResult Planned(
        string suggestedName,
        GDInferredType inferredType,
        int occurrencesCount,
        string expressionText)
    {
        return new GDExtractVariableResult(
            true, null, null,
            suggestedName,
            inferredType?.TypeName,
            inferredType?.Confidence ?? GDTypeConfidence.Unknown,
            inferredType?.Reason,
            occurrencesCount,
            expressionText);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDExtractVariableResult Failed(string errorMessage)
    {
        return new GDExtractVariableResult(
            false, errorMessage, null,
            null, null, GDTypeConfidence.Unknown, null, 0, null);
    }
}
