using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for converting for loops to equivalent while loops with explicit index management.
/// </summary>
public class GDConvertForToWhileService : GDRefactoringServiceBase
{
    /// <summary>
    /// Checks if the convert for-to-while refactoring can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return false;

        // Must be inside a method
        if (context.ContainingMethod == null)
            return false;

        // Check if cursor is on or in a for statement
        return context.IsOnForStatement;
    }

    /// <summary>
    /// Executes the convert for-to-while refactoring.
    /// Returns edits to apply or an error result.
    /// </summary>
    public GDRefactoringResult Execute(GDRefactoringContext context)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot convert for loop at this position");

        var forStmt = context.FindParent<GDForStatement>();
        if (forStmt == null)
            return GDRefactoringResult.Failed("No for statement found at cursor");

        if (forStmt.Variable == null)
            return GDRefactoringResult.Failed("For loop has no iteration variable");

        var variable = forStmt.Variable.Sequence ?? "i";
        var collection = forStmt.Collection?.ToString() ?? "[]";

        // Determine the conversion strategy
        var conversion = DetermineConversion(forStmt, variable, collection);
        if (conversion == null)
            return GDRefactoringResult.Failed("Could not determine conversion strategy");

        // Get indentation from the for statement
        var baseIndent = GetIndentationFromColumn(forStmt.StartColumn);
        var tabIndent = "\t";

        // Build the while loop text
        var whileCode = BuildWhileLoop(conversion, forStmt, baseIndent, tabIndent);

        var filePath = context.Script.Reference.FullPath;
        var edit = new GDTextEdit(
            filePath,
            forStmt.StartLine,
            forStmt.StartColumn,
            forStmt.ToString(),
            whileCode);

        return GDRefactoringResult.Succeeded(edit);
    }

    /// <summary>
    /// Plans the conversion without applying it.
    /// Returns the resulting code for preview.
    /// </summary>
    public GDConvertForToWhileResult Plan(GDRefactoringContext context)
    {
        if (!CanExecute(context))
            return GDConvertForToWhileResult.Failed("Cannot convert for loop at this position");

        var forStmt = context.FindParent<GDForStatement>();
        if (forStmt == null)
            return GDConvertForToWhileResult.Failed("No for statement found at cursor");

        if (forStmt.Variable == null)
            return GDConvertForToWhileResult.Failed("For loop has no iteration variable");

        var variable = forStmt.Variable.Sequence ?? "i";
        var collection = forStmt.Collection?.ToString() ?? "[]";

        var conversion = DetermineConversion(forStmt, variable, collection);
        if (conversion == null)
            return GDConvertForToWhileResult.Failed("Could not determine conversion strategy");

        var baseIndent = GetIndentationFromColumn(forStmt.StartColumn);
        var tabIndent = "\t";

        var whileCode = BuildWhileLoop(conversion, forStmt, baseIndent, tabIndent);

        return new GDConvertForToWhileResult(
            true,
            null,
            forStmt.ToString(),
            whileCode,
            conversion.ConversionType);
    }

    private ConversionInfo DetermineConversion(GDForStatement forStmt, string variable, string collection)
    {
        // Check if it's a range-based for loop: for i in range(n)
        if (forStmt.Collection is GDCallExpression call)
        {
            var methodName = GetCallMethodName(call);
            if (methodName == "range")
            {
                return CreateRangeConversion(variable, call);
            }
        }

        // General collection iteration: for item in collection
        return CreateCollectionConversion(variable, collection);
    }

    private ConversionInfo CreateRangeConversion(string variable, GDCallExpression rangeCall)
    {
        var args = rangeCall.Parameters?.ToList() ?? new List<GDExpression>();

        if (args.Count == 1)
        {
            // range(n) -> for i in 0..n-1
            var end = args[0].ToString();
            return new ConversionInfo
            {
                Initialization = $"var {variable} = 0",
                Condition = $"{variable} < {end}",
                Increment = $"{variable} += 1",
                VariableAssignment = null,
                ConversionType = ForLoopConversionType.RangeSingleArg
            };
        }
        else if (args.Count == 2)
        {
            // range(start, end) -> for i in start..end-1
            var start = args[0].ToString();
            var end = args[1].ToString();
            return new ConversionInfo
            {
                Initialization = $"var {variable} = {start}",
                Condition = $"{variable} < {end}",
                Increment = $"{variable} += 1",
                VariableAssignment = null,
                ConversionType = ForLoopConversionType.RangeTwoArgs
            };
        }
        else if (args.Count == 3)
        {
            // range(start, end, step) -> for i in start..end-1 with step
            var start = args[0].ToString();
            var end = args[1].ToString();
            var step = args[2].ToString();

            // Handle negative step
            var isNegativeStep = step.StartsWith("-");
            var comparison = isNegativeStep ? ">" : "<";

            return new ConversionInfo
            {
                Initialization = $"var {variable} = {start}",
                Condition = $"{variable} {comparison} {end}",
                Increment = $"{variable} += {step}",
                VariableAssignment = null,
                ConversionType = ForLoopConversionType.RangeThreeArgs
            };
        }

        // Fallback to collection conversion
        return CreateCollectionConversion(variable, rangeCall.ToString());
    }

    private ConversionInfo CreateCollectionConversion(string variable, string collection)
    {
        // for item in collection -> while _idx < collection.size(): var item = collection[_idx]
        var indexVar = "_idx";

        return new ConversionInfo
        {
            Initialization = $"var {indexVar} = 0",
            Condition = $"{indexVar} < {collection}.size()",
            VariableAssignment = $"var {variable} = {collection}[{indexVar}]",
            Increment = $"{indexVar} += 1",
            ConversionType = ForLoopConversionType.Collection
        };
    }

    private string BuildWhileLoop(ConversionInfo conversion, GDForStatement forStmt, string baseIndent, string tabIndent)
    {
        var result = new StringBuilder();

        // Add initialization if needed
        if (!string.IsNullOrEmpty(conversion.Initialization))
        {
            result.Append(baseIndent);
            result.AppendLine(conversion.Initialization);
        }

        // Add while header
        result.Append(baseIndent);
        result.AppendLine($"while {conversion.Condition}:");

        // Add variable assignment if needed (for iteration over collection)
        if (!string.IsNullOrEmpty(conversion.VariableAssignment))
        {
            result.Append(baseIndent);
            result.Append(tabIndent);
            result.AppendLine(conversion.VariableAssignment);
        }

        // Add the body
        var bodyText = GetForLoopBodyText(forStmt, baseIndent, tabIndent);
        if (!string.IsNullOrEmpty(bodyText))
        {
            result.Append(bodyText);
        }

        // Add increment at the end of the body
        if (!string.IsNullOrEmpty(conversion.Increment))
        {
            if (!bodyText.EndsWith("\n"))
                result.AppendLine();
            result.Append(baseIndent);
            result.Append(tabIndent);
            result.Append(conversion.Increment);
        }

        return result.ToString().TrimEnd('\r', '\n');
    }

    private string GetForLoopBodyText(GDForStatement forStmt, string baseIndent, string tabIndent)
    {
        if (forStmt.Statements == null || forStmt.Statements.Count == 0)
            return baseIndent + tabIndent + "pass\n";

        var result = new StringBuilder();
        foreach (var stmt in forStmt.Statements)
        {
            if (stmt != null)
            {
                var stmtText = stmt.ToString().Trim();
                if (!string.IsNullOrEmpty(stmtText))
                {
                    result.Append(baseIndent);
                    result.Append(tabIndent);
                    result.AppendLine(stmtText);
                }
            }
        }

        return result.ToString();
    }

    private static string? GetCallMethodName(GDCallExpression call)
    {
        if (call.CallerExpression is GDIdentifierExpression idExpr)
            return idExpr.Identifier?.Sequence;
        return null;
    }

    private static string GetIndentationFromColumn(int column)
    {
        // Approximate tabs based on column (assuming tab = 4 spaces or 1 tab)
        return new string('\t', column / 4);
    }

    private class ConversionInfo
    {
        public string? Initialization { get; set; }
        public string Condition { get; set; } = "";
        public string? VariableAssignment { get; set; }
        public string? Increment { get; set; }
        public ForLoopConversionType ConversionType { get; set; }
    }
}

/// <summary>
/// The type of for loop conversion performed.
/// </summary>
public enum ForLoopConversionType
{
    /// <summary>
    /// range(n) - single argument range.
    /// </summary>
    RangeSingleArg,

    /// <summary>
    /// range(start, end) - two argument range.
    /// </summary>
    RangeTwoArgs,

    /// <summary>
    /// range(start, end, step) - three argument range with step.
    /// </summary>
    RangeThreeArgs,

    /// <summary>
    /// General collection iteration.
    /// </summary>
    Collection
}

/// <summary>
/// Result of the for-to-while conversion planning.
/// </summary>
public class GDConvertForToWhileResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public string OriginalCode { get; }
    public string ConvertedCode { get; }
    public ForLoopConversionType ConversionType { get; }

    public GDConvertForToWhileResult(
        bool success,
        string? errorMessage,
        string originalCode,
        string convertedCode,
        ForLoopConversionType conversionType)
    {
        Success = success;
        ErrorMessage = errorMessage;
        OriginalCode = originalCode;
        ConvertedCode = convertedCode;
        ConversionType = conversionType;
    }

    public static GDConvertForToWhileResult Failed(string errorMessage)
    {
        return new GDConvertForToWhileResult(false, errorMessage, "", "", ForLoopConversionType.Collection);
    }
}
