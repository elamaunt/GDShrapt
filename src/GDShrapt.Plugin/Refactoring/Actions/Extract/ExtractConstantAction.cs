using GDShrapt.Semantics;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Extracts a literal value (number, string, bool) into a constant at class level.
/// Delegates to GDExtractConstantService for the actual logic.
/// </summary>
internal class ExtractConstantAction : RefactoringActionBase
{
    private readonly GDExtractConstantService _service = new();

    public override string Id => "extract_constant";
    public override string DisplayName => "Extract Constant";
    public override RefactoringCategory Category => RefactoringCategory.Extract;
    public override string Shortcut => "Ctrl+Alt+C";
    public override int Priority => 10;

    public override bool IsAvailable(RefactoringContext context)
    {
        if (context?.ContainingClass == null)
            return false;

        var semanticsContext = context.BuildSemanticsContext();
        return semanticsContext != null && _service.CanExecute(semanticsContext);
    }

    protected override string ValidateContext(RefactoringContext context)
    {
        var baseError = base.ValidateContext(context);
        if (baseError != null) return baseError;

        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null)
            return "Failed to build refactoring context";

        if (!_service.CanExecute(semanticsContext))
            return "No literal expression found to extract";

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
            throw new RefactoringException(plan.ErrorMessage ?? "Failed to plan extract constant");

        // Show name input dialog
        var nameDialog = new NameInputDialog();
        context.DialogParent?.AddChild(nameDialog);

        var constName = await nameDialog.ShowForResult(
            "Extract Constant",
            plan.SuggestedName,
            $"const {plan.SuggestedName} = {plan.LiteralValue}");

        nameDialog.QueueFree();

        if (string.IsNullOrEmpty(constName))
        {
            Logger.Info("ExtractConstantAction: Cancelled by user");
            return;
        }

        // Re-plan with the user-provided name
        plan = _service.Plan(semanticsContext, constName);
        if (!plan.Success)
            throw new RefactoringException(plan.ErrorMessage ?? "Failed to plan extract constant");

        // Build preview info
        var originalCode = plan.LiteralValue;
        var resultCode = $"const {plan.SuggestedName} = {plan.LiteralValue}\n\n// Inserted at line {plan.InsertionLine}";

        // Show preview dialog
        var previewDialog = new RefactoringPreviewDialog();
        context.DialogParent?.AddChild(previewDialog);

        try
        {
            var title = plan.ConflictingNames.Count > 0
                ? "Extract Constant (name conflict resolved)"
                : "Extract Constant";

            // In Base Plugin: Apply is disabled (Pro required)
            var canApply = false;
            var proMessage = "GDShrapt Pro required to apply this refactoring";

            var result = await previewDialog.ShowForResult(
                title,
                $"// Literal: {originalCode}",
                resultCode,
                canApply,
                "Apply",
                proMessage);

            if (result.ShouldApply && canApply)
            {
                // Execute the refactoring
                var executeResult = _service.Execute(semanticsContext, plan.SuggestedName);
                if (!executeResult.Success)
                    throw new RefactoringException(executeResult.ErrorMessage ?? "Failed to execute extract constant");

                // Apply edits to the editor
                ApplyEdits(context, executeResult);
            }
        }
        finally
        {
            previewDialog.QueueFree();
        }
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
}
