using Godot;
using GDShrapt.Plugin.Refactoring;
using GDShrapt.Semantics;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Extracts selected statements into a new method.
/// Delegates to GDExtractMethodService in Semantics.
/// </summary>
internal class ExtractMethodCommand : Command
{
    private readonly GDExtractMethodService _service = new();
    private NewMethodNameDialog _newNameDialog;

    public ExtractMethodCommand(GDShraptPlugin plugin)
        : base(plugin)
    {
    }

    public override async Task Execute(IScriptEditor controller)
    {
        Logger.Info("Extract method requested");

        if (!controller.IsValid)
        {
            Logger.Info("Extract method cancelled: Editor is not valid");
            return;
        }

        if (!controller.HasSelection)
        {
            Logger.Info("Extract method cancelled: No code selected");
            return;
        }

        // Build refactoring context using the builder
        var contextBuilder = new RefactoringContextBuilder(Map);
        var semanticsContext = contextBuilder.BuildSemanticsContext(controller);

        if (semanticsContext == null)
        {
            Logger.Info("Extract method cancelled: Could not build context");
            return;
        }

        // Check if extraction is possible
        if (!_service.CanExecute(semanticsContext))
        {
            Logger.Info("Extract method cancelled: Cannot extract at this position");
            return;
        }

        // Ask for new method name
        var newMethodName = await AskNewMethodName();
        if (string.IsNullOrEmpty(newMethodName))
        {
            Logger.Info("Extract method cancelled: new method name is null or empty");
            return;
        }

        // Plan the extraction to show preview (optional - for debugging)
        var planResult = _service.Plan(semanticsContext, newMethodName);
        if (!planResult.Success)
        {
            Logger.Info($"Extract method cancelled: {planResult.ErrorMessage}");
            return;
        }

        Logger.Info($"Extract method: Creating method '{planResult.MethodName}' with {planResult.DetectedParameters.Count} parameters");
        Logger.Debug($"Generated method:\n{planResult.GeneratedMethodCode}");
        Logger.Debug($"Generated call: {planResult.GeneratedCallCode}");

        // Execute the extraction
        var result = _service.Execute(semanticsContext, newMethodName);
        if (!result.Success)
        {
            Logger.Error($"Extract method failed: {result.ErrorMessage}");
            return;
        }

        // Apply the edits to the editor
        ApplyEdits(controller, result);

        Logger.Info("Method extraction has been completed");
    }

    private async Task<string> AskNewMethodName()
    {
        if (_newNameDialog == null)
        {
            Editor.AddChild(_newNameDialog = new NewMethodNameDialog());
        }

        _newNameDialog.Position = new Vector2I(
            (int)(Editor.Position.X + Editor.Size.X / 2),
            (int)(Editor.Position.Y + Editor.Size.Y / 2));

        return await _newNameDialog.ShowForResult();
    }

    private void ApplyEdits(IScriptEditor controller, GDRefactoringResult result)
    {
        if (result.Edits == null || result.Edits.Count == 0)
            return;

        // Apply edits in reverse order to preserve line numbers
        // Sort by line descending, then by column descending
        var sortedEdits = new System.Collections.Generic.List<GDTextEdit>(result.Edits);
        sortedEdits.Sort((a, b) =>
        {
            var lineCmp = b.Line.CompareTo(a.Line);
            return lineCmp != 0 ? lineCmp : b.Column.CompareTo(a.Column);
        });

        foreach (var edit in sortedEdits)
        {
            ApplySingleEdit(controller, edit);
        }

        controller.ReloadScriptFromText();
    }

    private void ApplySingleEdit(IScriptEditor controller, GDTextEdit edit)
    {
        if (string.IsNullOrEmpty(edit.OldText))
        {
            // Insertion
            controller.CursorLine = edit.Line;
            controller.CursorColumn = edit.Column;
            controller.InsertTextAtCursor(edit.NewText);
        }
        else
        {
            // Replacement - select old text and replace
            var endColumn = edit.Column + edit.OldText.Length;
            var lines = edit.OldText.Split('\n');
            var endLine = edit.Line + lines.Length - 1;

            if (lines.Length > 1)
            {
                endColumn = lines[^1].Length;
            }

            controller.Select(edit.Line, edit.Column, endLine, endColumn);
            controller.Cut();
            controller.InsertTextAtCursor(edit.NewText);
        }
    }
}
