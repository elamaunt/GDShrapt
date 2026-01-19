using GDShrapt.Semantics;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Inverts an if/elif/while condition and swaps the branches if applicable.
/// Delegates to GDInvertConditionService for the actual logic.
/// </summary>
internal class InvertConditionAction : GDRefactoringActionBase
{
    private readonly GDInvertConditionService _service = new();

    public override string Id => "invert_condition";
    public override string DisplayName => "Invert Condition";
    public override GDRefactoringCategory Category => GDRefactoringCategory.Convert;
    public override int Priority => 10;

    public override bool IsAvailable(GDPluginRefactoringContext context)
    {
        if (context?.ContainingClass == null)
            return false;

        var semanticsContext = context.BuildSemanticsContext();
        return semanticsContext != null && _service.CanExecute(semanticsContext);
    }

    protected override string ValidateContext(GDPluginRefactoringContext context)
    {
        var baseError = base.ValidateContext(context);
        if (baseError != null) return baseError;

        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null)
            return "Failed to build refactoring context";

        if (!_service.CanExecute(semanticsContext))
            return "No if or while statement found at cursor";

        return null;
    }

    protected override async Task ExecuteInternalAsync(GDPluginRefactoringContext context)
    {
        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null)
            throw new GDRefactoringException("Failed to build refactoring context");

        // Plan the refactoring
        var plan = _service.Plan(semanticsContext);
        if (!plan.Success)
            throw new GDRefactoringException(plan.ErrorMessage ?? "Failed to plan invert condition");

        // Show preview dialog
        var dialog = new RefactoringPreviewDialog();
        context.DialogParent?.AddChild(dialog);

        try
        {
            var title = plan.WillSwapBranches
                ? "Invert Condition (swap if/else)"
                : "Invert Condition";

            // In Base Plugin: Apply is disabled (Pro required)
            // This can be changed to true when Pro licensing is available
            var canApply = false;
            var proMessage = "GDShrapt Pro required to apply this refactoring";

            var result = await dialog.ShowForResult(
                title,
                plan.OriginalCode,
                plan.ResultCode,
                canApply,
                "Apply",
                proMessage);

            if (result.ShouldApply && canApply)
            {
                // Execute the refactoring
                var executeResult = _service.Execute(semanticsContext);
                if (!executeResult.Success)
                    throw new GDRefactoringException(executeResult.ErrorMessage ?? "Failed to execute invert condition");

                // Apply edits to the editor
                ApplyEdits(context, executeResult);
            }
        }
        finally
        {
            dialog.QueueFree();
        }
    }

    private void ApplyEdits(GDPluginRefactoringContext context, GDRefactoringResult result)
    {
        var editor = context.Editor;

        foreach (var edit in result.Edits)
        {
            // Select the old text
            var oldTextLines = edit.OldText.Split('\n');
            var endLine = edit.Line + oldTextLines.Length - 1;
            var endColumn = oldTextLines.Length > 1
                ? oldTextLines[^1].Length
                : edit.Column + edit.OldText.Length;

            editor.Select(edit.Line, edit.Column, endLine, endColumn);
            editor.Cut();
            editor.InsertTextAtCursor(edit.NewText);
        }

        editor.ReloadScriptFromText();
    }
}
