using GDShrapt.Semantics;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Converts a for loop to an equivalent while loop with explicit index management.
/// Delegates to GDConvertForToWhileService for the actual logic.
/// </summary>
internal class ConvertForToWhileAction : RefactoringActionBase
{
    private readonly GDConvertForToWhileService _service = new();

    public override string Id => "convert_for_to_while";
    public override string DisplayName => "Convert to while loop";
    public override RefactoringCategory Category => RefactoringCategory.Convert;
    public override int Priority => 20;

    public override bool IsAvailable(RefactoringContext context)
    {
        if (context?.ContainingMethod == null)
            return false;

        var semanticsContext = context.BuildSemanticsContext();
        return semanticsContext != null && _service.CanExecute(semanticsContext);
    }

    protected override string ValidateContext(RefactoringContext context)
    {
        if (context.Editor == null)
            return "No editor available";

        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null)
            return "Failed to build refactoring context";

        if (!_service.CanExecute(semanticsContext))
            return "No for statement found at cursor";

        return null;
    }

    protected override async Task ExecuteInternalAsync(RefactoringContext context)
    {
        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null)
            throw new RefactoringException("Failed to build refactoring context");

        // Plan the refactoring
        var plan = _service.Plan(semanticsContext);
        if (!plan.Success)
            throw new RefactoringException(plan.ErrorMessage ?? "Failed to plan conversion");

        // Show preview dialog
        var dialog = new RefactoringPreviewDialog();
        context.DialogParent?.AddChild(dialog);

        try
        {
            var conversionTypeText = plan.ConversionType switch
            {
                ForLoopConversionType.RangeSingleArg => "range(n)",
                ForLoopConversionType.RangeTwoArgs => "range(start, end)",
                ForLoopConversionType.RangeThreeArgs => "range(start, end, step)",
                ForLoopConversionType.Collection => "collection",
                _ => "for loop"
            };

            var title = $"Convert for loop ({conversionTypeText}) to while";

            // In Base Plugin: Apply is disabled (Pro required)
            var canApply = false;
            var proMessage = "GDShrapt Pro required to apply this refactoring";

            var result = await dialog.ShowForResult(
                title,
                plan.OriginalCode,
                plan.ConvertedCode,
                canApply,
                "Apply",
                proMessage);

            if (result.ShouldApply && canApply)
            {
                // Execute the refactoring
                var executeResult = _service.Execute(semanticsContext);
                if (!executeResult.Success)
                    throw new RefactoringException(executeResult.ErrorMessage ?? "Failed to execute conversion");

                // Apply edits to the editor
                ApplyEdits(context, executeResult);
            }
        }
        finally
        {
            dialog.QueueFree();
        }
    }

    private void ApplyEdits(RefactoringContext context, GDRefactoringResult result)
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
