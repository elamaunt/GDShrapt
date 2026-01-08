using GDShrapt.Reader;
using System.Threading.Tasks;

namespace GDShrapt.Plugin.Refactoring.Actions.Surround;

/// <summary>
/// Surrounds selected statements with an if statement.
/// </summary>
internal class SurroundWithIfAction : RefactoringActionBase
{
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

        Logger.Info($"SurroundWithIfAction: Surrounding lines {startLine}-{endLine}");

        // Get indentation from the first line
        var firstLine = editor.GetLine(startLine);
        var baseIndent = GetIndentation(firstLine);
        var tabIndent = "\t"; // GDScript uses tabs

        // Indent the selected code
        var indentedCode = IndentCode(selectedCode, tabIndent);

        // Build the if statement
        var result = new System.Text.StringBuilder();
        result.Append(baseIndent);
        result.AppendLine("if condition:");
        result.Append(indentedCode);

        // Select and replace the code
        editor.Select(startLine, 0, endLine, editor.GetLine(endLine).Length);
        editor.Cut();
        editor.InsertTextAtCursor(result.ToString());

        // Position cursor at 'condition' for easy editing
        editor.CursorLine = startLine;
        editor.CursorColumn = baseIndent.Length + 3; // After "if "

        // Select 'condition' text
        editor.Select(startLine, baseIndent.Length + 3, startLine, baseIndent.Length + 12);

        editor.ReloadScriptFromText();

        Logger.Info("SurroundWithIfAction: Completed successfully");

        await Task.CompletedTask;
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
