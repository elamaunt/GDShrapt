using GDShrapt.Reader;
using GDShrapt.Semantics;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Surrounds selected statements with an if statement.
/// Delegates to GDSurroundWithService for the actual logic.
/// This is a "safe" operation that can be applied directly in Base.
/// </summary>
internal class SurroundWithIfAction : RefactoringActionBase
{
    private readonly GDSurroundWithService _service = new();

    public override string Id => "surround_with_if";
    public override string DisplayName => "Surround with if";
    public override RefactoringCategory Category => RefactoringCategory.Surround;
    public override int Priority => 10;

    public override bool IsAvailable(RefactoringContext context)
    {
        if (context?.ContainingMethod == null)
            return false;

        // Must have selected statements or be on a statement
        return context.HasStatementsSelected || IsOnStatement(context);
    }

    private bool IsOnStatement(RefactoringContext context)
    {
        var node = context.NodeAtCursor;
        while (node != null)
        {
            if (node is GDStatement && !(node is GDMethodDeclaration))
                return true;
            node = node.Parent as GDNode;
        }
        return false;
    }

    protected override string ValidateContext(RefactoringContext context)
    {
        if (context.Editor == null)
            return "No editor available";

        if (!context.HasStatementsSelected && !IsOnStatement(context))
            return "No statements to surround";

        return null;
    }

    protected override async Task ExecuteInternalAsync(RefactoringContext context)
    {
        var editor = context.Editor;

        int startLine, endLine;
        string selectedCode;

        if (context.HasSelection)
        {
            startLine = context.SelectionStartLine;
            endLine = context.SelectionEndLine;
            selectedCode = context.SelectedText;
        }
        else
        {
            // Get the current statement
            var stmt = FindContainingStatement(context);
            if (stmt == null)
            {
                Logger.Info("SurroundWithIfAction: No statement found");
                return;
            }
            startLine = stmt.StartLine;
            endLine = stmt.EndLine;

            // Get the text of the statement
            var lines = new System.Collections.Generic.List<string>();
            for (int i = startLine; i <= endLine; i++)
            {
                lines.Add(editor.GetLine(i));
            }
            selectedCode = string.Join("\n", lines);
        }

        if (string.IsNullOrEmpty(selectedCode))
        {
            Logger.Info("SurroundWithIfAction: No code to surround");
            return;
        }

        // Try to use service if we have a valid semantics context
        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext != null && semanticsContext.HasSelection)
        {
            var plan = _service.PlanSurroundWithIf(semanticsContext, "condition");

            if (plan.Success)
            {
                // Show preview dialog (safe operation - Apply is enabled in Base)
                var previewDialog = new RefactoringPreviewDialog();
                context.DialogParent?.AddChild(previewDialog);

                try
                {
                    // Safe operation: canApply = true in Base
                    var canApply = true;

                    var result = await previewDialog.ShowForResult(
                        "Surround with if",
                        plan.OriginalCode,
                        plan.ResultCode,
                        canApply,
                        "Apply");

                    if (result.ShouldApply)
                    {
                        // Execute the surround operation
                        var executeResult = _service.SurroundWithIf(semanticsContext, "condition");
                        if (executeResult.Success && executeResult.Edits != null)
                        {
                            ApplyEdits(context, executeResult);
                            Logger.Info("SurroundWithIfAction: Completed via service");
                            return;
                        }
                    }
                    else
                    {
                        Logger.Info("SurroundWithIfAction: Cancelled by user");
                        return;
                    }
                }
                finally
                {
                    previewDialog.QueueFree();
                }
            }
        }

        // Fallback to direct implementation if service fails
        Logger.Info($"SurroundWithIfAction: Using fallback, surrounding lines {startLine}-{endLine}");

        // Get indentation from the first line
        var firstLine = editor.GetLine(startLine);
        var baseIndent = GetIndentation(firstLine);
        var tabIndent = "\t"; // GDScript uses tabs

        // Indent the selected code
        var indentedCode = IndentCode(selectedCode, tabIndent);

        // Build the if statement
        var resultCode = new System.Text.StringBuilder();
        resultCode.Append(baseIndent);
        resultCode.AppendLine("if condition:");
        resultCode.Append(indentedCode);

        // Select and replace the code
        editor.Select(startLine, 0, endLine, editor.GetLine(endLine).Length);
        editor.Cut();
        editor.InsertTextAtCursor(resultCode.ToString());

        // Position cursor at 'condition' for easy editing
        editor.CursorLine = startLine;
        editor.CursorColumn = baseIndent.Length + 3; // After "if "

        // Select 'condition' text
        editor.Select(startLine, baseIndent.Length + 3, startLine, baseIndent.Length + 12);

        editor.ReloadScriptFromText();

        Logger.Info("SurroundWithIfAction: Completed successfully (fallback)");

        await Task.CompletedTask;
    }

    private void ApplyEdits(RefactoringContext context, GDRefactoringResult result)
    {
        var editor = context.Editor;

        // Sort edits by line descending to avoid line number shifts
        var sortedEdits = new System.Collections.Generic.List<GDTextEdit>(result.Edits);
        sortedEdits.Sort((a, b) => b.Line.CompareTo(a.Line));

        foreach (var edit in sortedEdits)
        {
            if (string.IsNullOrEmpty(edit.OldText))
            {
                // Insert only
                editor.CursorLine = edit.Line;
                editor.CursorColumn = edit.Column;
                editor.InsertTextAtCursor(edit.NewText);
            }
            else
            {
                // Replace
                var oldTextLines = edit.OldText.Split('\n');
                var endLine = edit.Line + oldTextLines.Length - 1;
                var endColumn = oldTextLines.Length > 1
                    ? oldTextLines[^1].Length
                    : edit.Column + edit.OldText.Length;

                editor.Select(edit.Line, edit.Column, endLine, endColumn);
                editor.Cut();
                editor.InsertTextAtCursor(edit.NewText);
            }
        }

        editor.ReloadScriptFromText();
    }

    private GDStatement FindContainingStatement(RefactoringContext context)
    {
        var node = context.NodeAtCursor;
        while (node != null)
        {
            if (node is GDStatement stmt && !(node is GDMethodDeclaration))
                return stmt;
            node = node.Parent as GDNode;
        }
        return null;
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

    private string IndentCode(string code, string additionalIndent)
    {
        var lines = code.Split('\n');
        var result = new System.Text.StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            // Don't indent empty lines
            if (!string.IsNullOrWhiteSpace(line))
            {
                result.Append(additionalIndent);
            }
            result.Append(line);
            if (i < lines.Length - 1)
                result.AppendLine();
        }

        return result.ToString();
    }
}
