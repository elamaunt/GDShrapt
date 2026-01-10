using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for formatting GDScript code.
/// Wraps GDFormatter as a refactoring service.
/// </summary>
public class GDFormatCodeService
{
    private readonly GDFormatter _formatter;

    /// <summary>
    /// Creates a new format code service with default options.
    /// </summary>
    public GDFormatCodeService()
    {
        _formatter = new GDFormatter();
    }

    /// <summary>
    /// Creates a new format code service with specified options.
    /// </summary>
    public GDFormatCodeService(GDFormatterOptions options)
    {
        _formatter = new GDFormatter(options);
    }

    /// <summary>
    /// Gets or sets the formatter options.
    /// </summary>
    public GDFormatterOptions Options
    {
        get => _formatter.Options;
        set => _formatter.Options = value;
    }

    /// <summary>
    /// Checks if the format code refactoring can be executed at the given context.
    /// Always returns true if the context has valid code.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        return context?.ClassDeclaration != null;
    }

    /// <summary>
    /// Checks if the code is already properly formatted.
    /// </summary>
    public bool IsFormatted(GDRefactoringContext context)
    {
        if (context?.ClassDeclaration == null)
            return true;

        var originalCode = context.ClassDeclaration.ToString();
        return _formatter.IsFormatted(originalCode);
    }

    /// <summary>
    /// Gets a detailed format check result.
    /// </summary>
    public FormatCheckResult Check(GDRefactoringContext context)
    {
        if (context?.ClassDeclaration == null)
            return FormatCheckResult.AlreadyFormatted(string.Empty);

        var originalCode = context.ClassDeclaration.ToString();
        return _formatter.Check(originalCode);
    }

    /// <summary>
    /// Plans the format code refactoring on the entire file.
    /// Returns preview information for display.
    /// </summary>
    public GDFormatCodeResult PlanFormatFile(GDRefactoringContext context)
    {
        if (!CanExecute(context))
            return GDFormatCodeResult.Failed("Cannot format code at this position");

        var originalCode = context.ClassDeclaration.ToString();
        var formattedCode = _formatter.FormatCode(originalCode);

        var hasChanges = formattedCode != originalCode;
        var changedLinesCount = hasChanges ? CountChangedLines(originalCode, formattedCode) : 0;
        var appliedRules = GetEnabledRules().Select(r => r.Name).ToList();

        return GDFormatCodeResult.Planned(originalCode, formattedCode, hasChanges, changedLinesCount, appliedRules);
    }

    /// <summary>
    /// Alias for PlanFormatFile for backward compatibility.
    /// </summary>
    public GDFormatCodeResult Plan(GDRefactoringContext context) => PlanFormatFile(context);

    /// <summary>
    /// Plans the format code refactoring on the current selection.
    /// Returns error if no selection is present.
    /// </summary>
    public GDFormatCodeResult PlanFormatSelection(GDRefactoringContext context)
    {
        if (!CanExecute(context))
            return GDFormatCodeResult.Failed("Cannot format code at this position");

        if (!context.HasSelection)
            return GDFormatCodeResult.Failed("No text selected");

        var selectedText = context.Selection.Text;
        if (string.IsNullOrEmpty(selectedText))
            return GDFormatCodeResult.Failed("No text selected");

        var formattedText = FormatPartialCode(selectedText);
        var hasChanges = formattedText != selectedText;
        var changedLinesCount = hasChanges ? CountChangedLines(selectedText, formattedText) : 0;
        var appliedRules = GetEnabledRules().Select(r => r.Name).ToList();

        return GDFormatCodeResult.Planned(selectedText, formattedText, hasChanges, changedLinesCount, appliedRules);
    }

    /// <summary>
    /// Plans the format code refactoring on a specific line range.
    /// Returns preview information for display.
    /// </summary>
    public GDFormatCodeResult PlanFormatRange(GDRefactoringContext context, int startLine, int endLine)
    {
        if (!CanExecute(context))
            return GDFormatCodeResult.Failed("Cannot format code at this position");

        var originalCode = context.ClassDeclaration.ToString();
        var lines = originalCode.Split('\n');

        if (startLine < 0 || endLine >= lines.Length || startLine > endLine)
            return GDFormatCodeResult.Failed("Invalid line range");

        var rangeBuilder = new StringBuilder();
        for (int i = startLine; i <= endLine; i++)
        {
            rangeBuilder.Append(lines[i]);
            if (i < endLine)
                rangeBuilder.Append('\n');
        }

        var rangeText = rangeBuilder.ToString();
        var formattedText = FormatPartialCode(rangeText);
        var hasChanges = formattedText != rangeText;
        var changedLinesCount = hasChanges ? CountChangedLines(rangeText, formattedText) : 0;
        var appliedRules = GetEnabledRules().Select(r => r.Name).ToList();

        return GDFormatCodeResult.Planned(rangeText, formattedText, hasChanges, changedLinesCount, appliedRules);
    }

    private int CountChangedLines(string original, string formatted)
    {
        var originalLines = original.Split('\n');
        var formattedLines = formatted.Split('\n');
        var maxLen = Math.Max(originalLines.Length, formattedLines.Length);
        var changedCount = 0;

        for (int i = 0; i < maxLen; i++)
        {
            var origLine = i < originalLines.Length ? originalLines[i] : "";
            var fmtLine = i < formattedLines.Length ? formattedLines[i] : "";
            if (origLine != fmtLine)
                changedCount++;
        }

        return changedCount;
    }

    /// <summary>
    /// Formats the entire file.
    /// </summary>
    public GDRefactoringResult FormatFile(GDRefactoringContext context)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot format code at this position");

        var filePath = context.Script.Reference.FullPath;
        var originalCode = context.ClassDeclaration.ToString();

        var formattedCode = _formatter.FormatCode(originalCode);

        if (formattedCode == originalCode)
            return GDRefactoringResult.Empty;

        var edit = new GDTextEdit(
            filePath,
            0,
            0,
            originalCode,
            formattedCode);

        return GDRefactoringResult.Succeeded(edit);
    }

    /// <summary>
    /// Alias for FormatFile for backward compatibility.
    /// </summary>
    public GDRefactoringResult Execute(GDRefactoringContext context) => FormatFile(context);

    /// <summary>
    /// Formats only the selected text. Returns error if no selection is present.
    /// </summary>
    public GDRefactoringResult FormatSelection(GDRefactoringContext context)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot format code at this position");

        if (!context.HasSelection)
            return GDRefactoringResult.Failed("No text selected");

        var filePath = context.Script.Reference.FullPath;
        var selection = context.Selection;

        // Extract the selected text and format it
        var selectedText = selection.Text;
        if (string.IsNullOrEmpty(selectedText))
            return GDRefactoringResult.Failed("No text selected");

        // Try to format as statements if possible
        var formattedText = FormatPartialCode(selectedText);

        if (formattedText == selectedText)
            return GDRefactoringResult.Empty;

        var edit = new GDTextEdit(
            filePath,
            selection.StartLine,
            selection.StartColumn,
            selectedText,
            formattedText);

        return GDRefactoringResult.Succeeded(edit);
    }

    /// <summary>
    /// Executes the format code refactoring on a specific line range.
    /// </summary>
    public GDRefactoringResult FormatRange(GDRefactoringContext context, int startLine, int endLine)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot format code at this position");

        var filePath = context.Script.Reference.FullPath;
        var originalCode = context.ClassDeclaration.ToString();
        var lines = originalCode.Split('\n');

        if (startLine < 0 || endLine >= lines.Length || startLine > endLine)
            return GDRefactoringResult.Failed("Invalid line range");

        // Extract lines in range
        var rangeBuilder = new StringBuilder();
        for (int i = startLine; i <= endLine; i++)
        {
            rangeBuilder.Append(lines[i]);
            if (i < endLine)
                rangeBuilder.Append('\n');
        }

        var rangeText = rangeBuilder.ToString();
        var formattedText = FormatPartialCode(rangeText);

        if (formattedText == rangeText)
            return GDRefactoringResult.Empty;

        var edit = new GDTextEdit(
            filePath,
            startLine,
            0,
            rangeText,
            formattedText);

        return GDRefactoringResult.Succeeded(edit);
    }

    /// <summary>
    /// Formats a single method declaration.
    /// </summary>
    public string FormatMethod(GDMethodDeclaration method)
    {
        if (method == null)
            return string.Empty;

        var methodClone = (GDMethodDeclaration)method.Clone();
        _formatter.Format(methodClone);
        return methodClone.ToString();
    }

    /// <summary>
    /// Formats a single expression.
    /// </summary>
    public string FormatExpression(GDExpression expression)
    {
        if (expression == null)
            return string.Empty;

        return _formatter.FormatExpression(expression.ToString());
    }

    /// <summary>
    /// Formats code using style extracted from sample code.
    /// </summary>
    public GDRefactoringResult FormatWithStyle(GDRefactoringContext context, string sampleCode)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot format code at this position");

        var filePath = context.Script.Reference.FullPath;
        var originalCode = context.ClassDeclaration.ToString();

        var formattedCode = _formatter.FormatCodeWithStyle(originalCode, sampleCode);

        if (formattedCode == originalCode)
            return GDRefactoringResult.Empty;

        var edit = new GDTextEdit(
            filePath,
            0,
            0,
            originalCode,
            formattedCode);

        return GDRefactoringResult.Succeeded(edit);
    }

    /// <summary>
    /// Formats partial code (statements or expressions).
    /// </summary>
    private string FormatPartialCode(string code)
    {
        if (string.IsNullOrEmpty(code))
            return code;

        // Try to parse as statements first
        try
        {
            var reader = new GDScriptReader();

            // Wrap in a dummy function to parse as statements
            var wrappedCode = $"func _dummy():\n\t{code.Replace("\n", "\n\t")}";
            var tree = reader.ParseFileContent(wrappedCode);

            if (tree != null)
            {
                _formatter.Format(tree);
                var result = tree.ToString();

                // Extract the formatted statements (remove the dummy function wrapper)
                var lines = result.Split('\n');
                if (lines.Length > 1)
                {
                    var stmtBuilder = new StringBuilder();
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        // Remove one level of indentation
                        if (line.StartsWith("\t"))
                            line = line.Substring(1);
                        stmtBuilder.AppendLine(line);
                    }
                    return stmtBuilder.ToString().TrimEnd();
                }
            }
        }
        catch
        {
            // If parsing as statements fails, try as expression
        }

        // Try to parse as expression
        try
        {
            return _formatter.FormatExpression(code);
        }
        catch
        {
            // Return original if nothing works
            return code;
        }
    }

    /// <summary>
    /// Gets the enabled formatting rules.
    /// </summary>
    public IEnumerable<GDFormatRule> GetEnabledRules()
    {
        return _formatter.GetEnabledRules();
    }

    /// <summary>
    /// Gets the disabled formatting rules.
    /// </summary>
    public IEnumerable<GDFormatRule> GetDisabledRules()
    {
        return _formatter.GetDisabledRules();
    }
}

/// <summary>
/// Result of format code planning operation.
/// </summary>
public class GDFormatCodeResult : GDRefactoringResult
{
    /// <summary>
    /// Original unformatted code.
    /// </summary>
    public string OriginalCode { get; }

    /// <summary>
    /// Formatted code.
    /// </summary>
    public string FormattedCode { get; }

    /// <summary>
    /// Whether code needed formatting.
    /// </summary>
    public bool HasChanges { get; }

    /// <summary>
    /// Number of lines changed.
    /// </summary>
    public int ChangedLinesCount { get; }

    /// <summary>
    /// Names of rules that were applied.
    /// </summary>
    public IReadOnlyList<string> AppliedRules { get; }

    private GDFormatCodeResult(
        bool success,
        string errorMessage,
        IReadOnlyList<GDTextEdit> edits,
        string originalCode,
        string formattedCode,
        bool hasChanges,
        int changedLinesCount,
        IReadOnlyList<string> appliedRules)
        : base(success, errorMessage, edits)
    {
        OriginalCode = originalCode;
        FormattedCode = formattedCode;
        HasChanges = hasChanges;
        ChangedLinesCount = changedLinesCount;
        AppliedRules = appliedRules ?? Array.Empty<string>();
    }

    /// <summary>
    /// Creates a planned result with preview information.
    /// </summary>
    public static GDFormatCodeResult Planned(
        string originalCode,
        string formattedCode,
        bool hasChanges,
        int changedLinesCount,
        IReadOnlyList<string> appliedRules)
    {
        return new GDFormatCodeResult(
            true, null, null,
            originalCode, formattedCode, hasChanges, changedLinesCount, appliedRules);
    }

    /// <summary>
    /// Creates a result indicating no changes are needed.
    /// </summary>
    public static GDFormatCodeResult NoChanges(string code)
    {
        return new GDFormatCodeResult(
            true, null, null,
            code, code, false, 0, Array.Empty<string>());
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDFormatCodeResult Failed(string errorMessage)
    {
        return new GDFormatCodeResult(
            false, errorMessage, null,
            null, null, false, 0, null);
    }
}
