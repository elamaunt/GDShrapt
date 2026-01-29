using System.Collections.Generic;
using System.Linq;
using System.Text;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for surrounding selected code with control structures.
/// </summary>
public class GDSurroundWithService : GDRefactoringServiceBase
{
    /// <summary>
    /// Checks if the surround with refactoring can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return false;

        // Must have some code selected
        return context.HasSelection;
    }

    #region Plan Methods

    /// <summary>
    /// Plans surrounding the selected code with an if statement.
    /// </summary>
    public GDSurroundWithResult PlanSurroundWithIf(GDRefactoringContext context, string condition = "true")
    {
        if (!CanExecute(context))
            return GDSurroundWithResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDSurroundWithResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildIfSurround(selectedCode, condition, indent);
        var affectedLinesCount = CountLines(selectedCode);

        return GDSurroundWithResult.Planned("if", selectedCode, newCode, affectedLinesCount);
    }

    /// <summary>
    /// Plans surrounding the selected code with an if-else statement.
    /// </summary>
    public GDSurroundWithResult PlanSurroundWithIfElse(GDRefactoringContext context, string condition = "true")
    {
        if (!CanExecute(context))
            return GDSurroundWithResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDSurroundWithResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildIfElseSurround(selectedCode, condition, indent);
        var affectedLinesCount = CountLines(selectedCode);

        return GDSurroundWithResult.Planned("if-else", selectedCode, newCode, affectedLinesCount);
    }

    /// <summary>
    /// Plans surrounding the selected code with a for loop.
    /// </summary>
    public GDSurroundWithResult PlanSurroundWithFor(GDRefactoringContext context, string iterator = "i", string collection = "range(10)")
    {
        if (!CanExecute(context))
            return GDSurroundWithResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDSurroundWithResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildForSurround(selectedCode, iterator, collection, indent);
        var affectedLinesCount = CountLines(selectedCode);

        return GDSurroundWithResult.Planned("for", selectedCode, newCode, affectedLinesCount);
    }

    /// <summary>
    /// Plans surrounding the selected code with a while loop.
    /// </summary>
    public GDSurroundWithResult PlanSurroundWithWhile(GDRefactoringContext context, string condition = "true")
    {
        if (!CanExecute(context))
            return GDSurroundWithResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDSurroundWithResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildWhileSurround(selectedCode, condition, indent);
        var affectedLinesCount = CountLines(selectedCode);

        return GDSurroundWithResult.Planned("while", selectedCode, newCode, affectedLinesCount);
    }

    /// <summary>
    /// Plans surrounding the selected code with a match statement.
    /// </summary>
    public GDSurroundWithResult PlanSurroundWithMatch(GDRefactoringContext context, string expression = "value")
    {
        if (!CanExecute(context))
            return GDSurroundWithResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDSurroundWithResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildMatchSurround(selectedCode, expression, indent);
        var affectedLinesCount = CountLines(selectedCode);

        return GDSurroundWithResult.Planned("match", selectedCode, newCode, affectedLinesCount);
    }

    /// <summary>
    /// Plans surrounding the selected code with a function.
    /// </summary>
    public GDSurroundWithResult PlanSurroundWithFunc(GDRefactoringContext context, string funcName = "_new_function")
    {
        if (!CanExecute(context))
            return GDSurroundWithResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDSurroundWithResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildFuncSurround(selectedCode, funcName, indent);
        var affectedLinesCount = CountLines(selectedCode);

        return GDSurroundWithResult.Planned("func", selectedCode, newCode, affectedLinesCount);
    }

    /// <summary>
    /// Plans surrounding the selected code with a try-except block.
    /// </summary>
    public GDSurroundWithResult PlanSurroundWithTry(GDRefactoringContext context)
    {
        if (!CanExecute(context))
            return GDSurroundWithResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDSurroundWithResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildTrySurround(selectedCode, indent);
        var affectedLinesCount = CountLines(selectedCode);

        return GDSurroundWithResult.Planned("try", selectedCode, newCode, affectedLinesCount);
    }

    #endregion

    /// <summary>
    /// Surrounds the selected code with an if statement.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="condition">The condition expression (default: "true")</param>
    public GDRefactoringResult SurroundWithIf(GDRefactoringContext context, string condition = "true")
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDRefactoringResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildIfSurround(selectedCode, condition, indent);

        return CreateReplacementEdit(context, selectedCode, newCode);
    }

    /// <summary>
    /// Surrounds the selected code with an if-else statement.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="condition">The condition expression (default: "true")</param>
    public GDRefactoringResult SurroundWithIfElse(GDRefactoringContext context, string condition = "true")
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDRefactoringResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildIfElseSurround(selectedCode, condition, indent);

        return CreateReplacementEdit(context, selectedCode, newCode);
    }

    /// <summary>
    /// Surrounds the selected code with a for loop.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="iterator">The iterator variable name (default: "i")</param>
    /// <param name="collection">The collection to iterate (default: "range(10)")</param>
    public GDRefactoringResult SurroundWithFor(GDRefactoringContext context, string iterator = "i", string collection = "range(10)")
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDRefactoringResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildForSurround(selectedCode, iterator, collection, indent);

        return CreateReplacementEdit(context, selectedCode, newCode);
    }

    /// <summary>
    /// Surrounds the selected code with a while loop.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="condition">The loop condition (default: "true")</param>
    public GDRefactoringResult SurroundWithWhile(GDRefactoringContext context, string condition = "true")
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDRefactoringResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildWhileSurround(selectedCode, condition, indent);

        return CreateReplacementEdit(context, selectedCode, newCode);
    }

    /// <summary>
    /// Surrounds the selected code with a match statement.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="expression">The expression to match (default: "value")</param>
    public GDRefactoringResult SurroundWithMatch(GDRefactoringContext context, string expression = "value")
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDRefactoringResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildMatchSurround(selectedCode, expression, indent);

        return CreateReplacementEdit(context, selectedCode, newCode);
    }

    /// <summary>
    /// Surrounds the selected code with a function.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="funcName">The function name (default: "_new_function")</param>
    public GDRefactoringResult SurroundWithFunc(GDRefactoringContext context, string funcName = "_new_function")
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDRefactoringResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildFuncSurround(selectedCode, funcName, indent);

        return CreateReplacementEdit(context, selectedCode, newCode);
    }

    /// <summary>
    /// Surrounds the selected code with a try-except block (GDScript 4.x).
    /// </summary>
    /// <param name="context">The refactoring context</param>
    public GDRefactoringResult SurroundWithTry(GDRefactoringContext context)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot surround code at this position");

        var selectedCode = GetSelectedCode(context);
        if (string.IsNullOrWhiteSpace(selectedCode))
            return GDRefactoringResult.Failed("No code selected");

        var indent = GetIndentation(context);
        var newCode = BuildTrySurround(selectedCode, indent);

        return CreateReplacementEdit(context, selectedCode, newCode);
    }

    /// <summary>
    /// Gets all available surround options.
    /// </summary>
    public IReadOnlyList<GDSurroundOption> GetAvailableOptions()
    {
        return new List<GDSurroundOption>
        {
            new GDSurroundOption("if", "if condition:", "Surround with if statement"),
            new GDSurroundOption("if-else", "if condition: ... else:", "Surround with if-else statement"),
            new GDSurroundOption("for", "for i in collection:", "Surround with for loop"),
            new GDSurroundOption("while", "while condition:", "Surround with while loop"),
            new GDSurroundOption("match", "match expression:", "Surround with match statement"),
            new GDSurroundOption("func", "func name():", "Surround with function"),
            new GDSurroundOption("try", "try: ... except:", "Surround with try-except block")
        };
    }

    #region Helper Methods

    private string GetSelectedCode(GDRefactoringContext context)
    {
        if (context.HasSelection)
        {
            return context.Selection.Text;
        }

        return null;
    }

    private string GetIndentation(GDRefactoringContext context)
    {
        if (context.HasSelection)
        {
            // Get indentation from the first line of selection
            var text = context.Selection.Text;
            if (!string.IsNullOrEmpty(text))
            {
                var firstLine = text.Split('\n')[0];
                var indent = "";
                foreach (var c in firstLine)
                {
                    if (c == '\t' || c == ' ')
                        indent += c;
                    else
                        break;
                }
                return indent;
            }
        }

        // Default indentation
        return "\t";
    }

    private string IndentCode(string code, string additionalIndent)
    {
        if (string.IsNullOrEmpty(code))
            return code;

        var lines = code.Split('\n');
        var sb = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!string.IsNullOrWhiteSpace(line))
            {
                sb.Append(additionalIndent);
            }
            sb.Append(line);
            if (i < lines.Length - 1)
            {
                sb.Append('\n');
            }
        }

        return sb.ToString();
    }

    private string BuildIfSurround(string code, string condition, string baseIndent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{baseIndent}if {condition}:");
        sb.Append(IndentCode(code, "\t"));
        return sb.ToString();
    }

    private string BuildIfElseSurround(string code, string condition, string baseIndent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{baseIndent}if {condition}:");
        sb.AppendLine(IndentCode(code, "\t"));
        sb.AppendLine($"{baseIndent}else:");
        sb.Append($"{baseIndent}\tpass");
        return sb.ToString();
    }

    private string BuildForSurround(string code, string iterator, string collection, string baseIndent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{baseIndent}for {iterator} in {collection}:");
        sb.Append(IndentCode(code, "\t"));
        return sb.ToString();
    }

    private string BuildWhileSurround(string code, string condition, string baseIndent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{baseIndent}while {condition}:");
        sb.Append(IndentCode(code, "\t"));
        return sb.ToString();
    }

    private string BuildMatchSurround(string code, string expression, string baseIndent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{baseIndent}match {expression}:");
        sb.AppendLine($"{baseIndent}\t_:");
        sb.Append(IndentCode(code, "\t\t"));
        return sb.ToString();
    }

    private string BuildFuncSurround(string code, string funcName, string baseIndent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{baseIndent}func {funcName}():");
        sb.Append(IndentCode(code, "\t"));
        return sb.ToString();
    }

    private string BuildTrySurround(string code, string baseIndent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{baseIndent}try:");
        sb.AppendLine(IndentCode(code, "\t"));
        sb.AppendLine($"{baseIndent}except:");
        sb.Append($"{baseIndent}\tpass");
        return sb.ToString();
    }

    private GDRefactoringResult CreateReplacementEdit(GDRefactoringContext context, string oldCode, string newCode)
    {
        var filePath = context.Script.Reference.FullPath;

        int startLine, startColumn, endLine, endColumn;

        if (context.HasSelection)
        {
            startLine = context.Selection.StartLine;
            startColumn = context.Selection.StartColumn;
            endLine = context.Selection.EndLine;
            endColumn = context.Selection.EndColumn;
        }
        else
        {
            return GDRefactoringResult.Failed("Could not determine replacement range");
        }

        var edit = new GDTextEdit(
            filePath,
            startLine,
            startColumn,
            oldCode,
            newCode);

        return GDRefactoringResult.Succeeded(edit);
    }

    private static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int count = 1;
        foreach (var c in text)
        {
            if (c == '\n')
                count++;
        }
        return count;
    }

    #endregion
}

/// <summary>
/// Represents a surround with option.
/// </summary>
public class GDSurroundOption
{
    /// <summary>
    /// Short identifier for the option (e.g., "if", "for", "while").
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Template preview showing the structure.
    /// </summary>
    public string Template { get; }

    /// <summary>
    /// Description of what the option does.
    /// </summary>
    public string Description { get; }

    public GDSurroundOption(string id, string template, string description)
    {
        Id = id;
        Template = template;
        Description = description;
    }

    public override string ToString() => $"{Id}: {Description}";
}

/// <summary>
/// Result of surround with planning operation.
/// </summary>
public class GDSurroundWithResult : GDRefactoringResult
{
    /// <summary>
    /// The surround type (if, for, while, etc.).
    /// </summary>
    public string SurroundType { get; }

    /// <summary>
    /// Original selected code.
    /// </summary>
    public string OriginalCode { get; }

    /// <summary>
    /// Resulting code after surround.
    /// </summary>
    public string ResultCode { get; }

    /// <summary>
    /// Number of lines affected.
    /// </summary>
    public int AffectedLinesCount { get; }

    private GDSurroundWithResult(
        bool success,
        string errorMessage,
        IReadOnlyList<GDTextEdit> edits,
        string surroundType,
        string originalCode,
        string resultCode,
        int affectedLinesCount)
        : base(success, errorMessage, edits)
    {
        SurroundType = surroundType;
        OriginalCode = originalCode;
        ResultCode = resultCode;
        AffectedLinesCount = affectedLinesCount;
    }

    /// <summary>
    /// Creates a planned result with preview information.
    /// </summary>
    public static GDSurroundWithResult Planned(
        string surroundType,
        string originalCode,
        string resultCode,
        int affectedLinesCount)
    {
        return new GDSurroundWithResult(true, null, null, surroundType, originalCode, resultCode, affectedLinesCount);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDSurroundWithResult Failed(string errorMessage)
    {
        return new GDSurroundWithResult(false, errorMessage, null, null, null, null, 0);
    }
}
