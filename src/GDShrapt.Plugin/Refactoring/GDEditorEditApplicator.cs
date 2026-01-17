using System.Collections.Generic;
using System.Linq;
using GDShrapt.Semantics;

namespace GDShrapt.Plugin;

/// <summary>
/// Shared utility for applying text edits from refactoring results to a script editor.
/// Consolidates common edit application logic used across all refactoring actions.
/// </summary>
internal static class GDEditorEditApplicator
{
    /// <summary>
    /// Applies all edits from a refactoring result to the editor.
    /// Edits are sorted in reverse order to avoid line number shifts.
    /// </summary>
    /// <param name="editor">The script editor to apply edits to.</param>
    /// <param name="result">The refactoring result containing edits.</param>
    public static void ApplyEdits(IScriptEditor editor, GDRefactoringResult? result)
    {
        if (editor == null || result?.Edits == null || !result.Edits.Any())
            return;

        ApplyEdits(editor, result.Edits);
    }

    /// <summary>
    /// Applies a collection of text edits to the editor.
    /// Edits are sorted in reverse order to avoid line number shifts.
    /// </summary>
    /// <param name="editor">The script editor to apply edits to.</param>
    /// <param name="edits">The edits to apply.</param>
    public static void ApplyEdits(IScriptEditor editor, IEnumerable<GDTextEdit> edits)
    {
        if (editor == null || edits == null)
            return;

        // Sort edits by line descending, then column descending to avoid line number shifts
        var sortedEdits = edits
            .OrderByDescending(e => e.Line)
            .ThenByDescending(e => e.Column)
            .ToList();

        foreach (var edit in sortedEdits)
        {
            ApplyEdit(editor, edit);
        }

        editor.ReloadScriptFromText();
    }

    /// <summary>
    /// Applies a single text edit to the editor.
    /// </summary>
    /// <param name="editor">The script editor to apply the edit to.</param>
    /// <param name="edit">The edit to apply.</param>
    public static void ApplyEdit(IScriptEditor editor, GDTextEdit edit)
    {
        if (editor == null || edit == null)
            return;

        if (string.IsNullOrEmpty(edit.OldText))
        {
            // Insert only - no text to replace
            editor.CursorLine = edit.Line;
            editor.CursorColumn = edit.Column;
            editor.InsertTextAtCursor(edit.NewText ?? "");
        }
        else
        {
            // Replace existing text
            var oldTextLines = edit.OldText.Split('\n');
            var endLine = edit.Line + oldTextLines.Length - 1;
            var endColumn = oldTextLines.Length > 1
                ? oldTextLines[^1].Length
                : edit.Column + edit.OldText.Length;

            editor.Select(edit.Line, edit.Column, endLine, endColumn);
            editor.Cut();
            editor.InsertTextAtCursor(edit.NewText ?? "");
        }
    }

    /// <summary>
    /// Applies edits from a refactoring context.
    /// Convenience method that extracts the editor from the context.
    /// </summary>
    /// <param name="context">The refactoring context containing the editor.</param>
    /// <param name="result">The refactoring result containing edits.</param>
    public static void ApplyEdits(RefactoringContext context, GDRefactoringResult? result)
    {
        if (context?.Editor == null)
            return;

        ApplyEdits(context.Editor, result);
    }
}
