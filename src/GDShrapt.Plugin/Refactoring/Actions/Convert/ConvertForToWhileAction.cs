using GDShrapt.Reader;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Converts a for loop to an equivalent while loop with explicit index management.
/// </summary>
internal class ConvertForToWhileAction : RefactoringActionBase
{
    public override string Id => "convert_for_to_while";
    public override string DisplayName => "Convert to while loop";
    public override RefactoringCategory Category => RefactoringCategory.Convert;
    public override int Priority => 20;

    public override bool IsAvailable(RefactoringContext context)
    {
        if (context?.ContainingMethod == null)
            return false;

        // Check if cursor is on or in a for statement
        return context.IsOnForStatement;
    }

    protected override string ValidateContext(RefactoringContext context)
    {
        if (context.Editor == null)
            return "No editor available";

        var forStmt = context.GetForStatement();
        if (forStmt == null)
            return "No for statement found at cursor";

        if (forStmt.Variable == null)
            return "For loop has no iteration variable";

        return null;
    }

    protected override async Task ExecuteInternalAsync(RefactoringContext context)
    {
        var editor = context.Editor;
        var forStmt = context.GetForStatement();

        if (forStmt == null)
        {
            throw new RefactoringException("No for statement found");
        }

        var variable = forStmt.Variable?.Sequence ?? "i";
        var collection = forStmt.Collection?.ToString() ?? "[]";

        Logger.Info($"ConvertForToWhileAction: Converting 'for {variable} in {collection}'");

        // Determine the type of for loop and conversion strategy
        var conversion = DetermineConversion(forStmt, variable, collection);

        if (conversion == null)
        {
            Logger.Info("ConvertForToWhileAction: Could not determine conversion");
            return;
        }

        // Get the position of the for statement
        var startLine = forStmt.StartLine;
        var endLine = forStmt.EndLine;

        // Get the body of the for loop
        var bodyText = GetForLoopBody(editor, forStmt);

        // Get indentation
        var firstLineText = editor.GetLine(startLine);
        var baseIndent = GetIndentation(firstLineText);
        var tabIndent = "\t";

        // Build the while loop
        var result = new System.Text.StringBuilder();

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

        // Add the body (already indented)
        result.Append(bodyText);

        // Add increment at the end of the body
        if (!string.IsNullOrEmpty(conversion.Increment))
        {
            if (!bodyText.EndsWith("\n"))
                result.AppendLine();
            result.Append(baseIndent);
            result.Append(tabIndent);
            result.Append(conversion.Increment);
        }

        // Select and replace the for statement
        editor.Select(startLine, 0, endLine, editor.GetLine(endLine).Length);
        editor.Cut();
        editor.InsertTextAtCursor(result.ToString().TrimEnd('\n', '\r'));

        editor.ReloadScriptFromText();

        Logger.Info("ConvertForToWhileAction: Completed successfully");

        await Task.CompletedTask;
    }

    private ConversionResult DetermineConversion(GDForStatement forStmt, string variable, string collection)
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

    private ConversionResult CreateRangeConversion(string variable, GDCallExpression rangeCall)
    {
        var args = rangeCall.Parameters?.ToList() ?? new System.Collections.Generic.List<GDExpression>();

        if (args.Count == 1)
        {
            // range(n) -> for i in 0..n-1
            var end = args[0].ToString();
            return new ConversionResult
            {
                Initialization = $"var {variable} = 0",
                Condition = $"{variable} < {end}",
                Increment = $"{variable} += 1",
                VariableAssignment = null
            };
        }
        else if (args.Count == 2)
        {
            // range(start, end) -> for i in start..end-1
            var start = args[0].ToString();
            var end = args[1].ToString();
            return new ConversionResult
            {
                Initialization = $"var {variable} = {start}",
                Condition = $"{variable} < {end}",
                Increment = $"{variable} += 1",
                VariableAssignment = null
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

            return new ConversionResult
            {
                Initialization = $"var {variable} = {start}",
                Condition = $"{variable} {comparison} {end}",
                Increment = $"{variable} += {step}",
                VariableAssignment = null
            };
        }

        // Fallback
        return CreateCollectionConversion(variable, rangeCall.ToString());
    }

    private ConversionResult CreateCollectionConversion(string variable, string collection)
    {
        // for item in collection -> while _idx < collection.size(): var item = collection[_idx]
        var indexVar = "_idx";

        return new ConversionResult
        {
            Initialization = $"var {indexVar} = 0",
            Condition = $"{indexVar} < {collection}.size()",
            VariableAssignment = $"var {variable} = {collection}[{indexVar}]",
            Increment = $"{indexVar} += 1"
        };
    }

    private string GetCallMethodName(GDCallExpression call)
    {
        if (call.CallerExpression is GDIdentifierExpression idExpr)
            return idExpr.Identifier?.Sequence;
        return null;
    }

    private string GetForLoopBody(IScriptEditor editor, GDForStatement forStmt)
    {
        if (forStmt.Statements == null)
            return "";

        var bodyStartLine = forStmt.Statements.StartLine;
        var bodyEndLine = forStmt.Statements.EndLine;

        var lines = new System.Collections.Generic.List<string>();
        for (int i = bodyStartLine; i <= bodyEndLine; i++)
        {
            lines.Add(editor.GetLine(i));
        }

        return string.Join("\n", lines);
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

    private class ConversionResult
    {
        public string Initialization { get; set; }
        public string Condition { get; set; }
        public string VariableAssignment { get; set; }
        public string Increment { get; set; }
    }
}
