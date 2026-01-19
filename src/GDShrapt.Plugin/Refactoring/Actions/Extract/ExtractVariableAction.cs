using GDShrapt.Semantics;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Extracts an expression into a local variable.
/// Delegates to GDExtractVariableService for the actual logic.
/// </summary>
internal class ExtractVariableAction : GDRefactoringActionBase
{
    private readonly GDExtractVariableService _service = new();

    public override string Id => "extract_variable";
    public override string DisplayName => "Extract Variable";
    public override GDRefactoringCategory Category => GDRefactoringCategory.Extract;
    public override string Shortcut => "Ctrl+Alt+V";
    public override int Priority => 5;

    public override bool IsAvailable(GDPluginRefactoringContext context)
    {
        if (context?.ContainingMethod == null)
            return false;

        var semanticsContext = context.BuildSemanticsContext();
        return semanticsContext != null && _service.CanExecute(semanticsContext);
    }

    protected override string ValidateContext(GDPluginRefactoringContext context)
    {
        if (context.Editor == null)
            return "No editor available";

        if (context.ContainingMethod == null)
            return "Not inside a method";

        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null)
            return "Failed to build refactoring context";

        if (!_service.CanExecute(semanticsContext))
            return "No expression found to extract";

        return null;
    }

    protected override async Task ExecuteInternalAsync(GDPluginRefactoringContext context)
    {
        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null)
            throw new GDRefactoringException("Failed to build refactoring context");

        // Get the expression to extract and suggest a name
        var expression = semanticsContext.SelectedExpression;
        if (expression == null)
            throw new GDRefactoringException("No expression found to extract");

        var suggestedName = _service.SuggestVariableName(expression);

        // Plan the refactoring with suggested name
        var plan = _service.Plan(semanticsContext, suggestedName);
        if (!plan.Success)
            throw new GDRefactoringException(plan.ErrorMessage ?? "Failed to plan extract variable");

        // Show name input dialog first
        var nameDialog = new NameInputDialog();
        context.DialogParent?.AddChild(nameDialog);

        var varName = await nameDialog.ShowForResult(
            "Extract Variable",
            plan.SuggestedName,
            $"var {plan.SuggestedName} = {plan.ExpressionText}");

        nameDialog.QueueFree();

        if (string.IsNullOrEmpty(varName))
        {
            Logger.Info("ExtractVariableAction: Cancelled by user");
            return;
        }

        // Re-plan with the user-provided name
        plan = _service.Plan(semanticsContext, varName);
        if (!plan.Success)
            throw new GDRefactoringException(plan.ErrorMessage ?? "Failed to plan extract variable");

        // Build preview info
        var originalCode = plan.ExpressionText;
        var resultCode = BuildPreviewCode(plan);

        // Show preview dialog
        var previewDialog = new RefactoringPreviewDialog();
        context.DialogParent?.AddChild(previewDialog);

        try
        {
            var title = plan.OccurrencesCount > 1
                ? $"Extract Variable ({plan.OccurrencesCount} occurrences found)"
                : "Extract Variable";

            // In Base Plugin: Apply is disabled (Pro required)
            var canApply = false;
            var proMessage = "GDShrapt Pro required to apply this refactoring";

            var result = await previewDialog.ShowForResult(
                title,
                $"// Expression: {originalCode}\n// Type: {plan.InferredType ?? "Variant"} ({plan.TypeConfidence})",
                resultCode,
                canApply,
                "Apply",
                proMessage);

            if (result.ShouldApply && canApply)
            {
                // Execute the refactoring
                var executeResult = _service.Execute(semanticsContext, varName, replaceAll: false);
                if (!executeResult.Success)
                    throw new GDRefactoringException(executeResult.ErrorMessage ?? "Failed to execute extract variable");

                // Apply edits to the editor
                ApplyEdits(context, executeResult);
            }
        }
        finally
        {
            previewDialog.QueueFree();
        }
    }

    private string BuildPreviewCode(GDExtractVariableResult plan)
    {
        var typeAnnotation = !string.IsNullOrEmpty(plan.InferredType) && plan.InferredType != "Variant"
            ? $": {plan.InferredType}"
            : "";

        var varDecl = $"var {plan.SuggestedName}{typeAnnotation} = {plan.ExpressionText}";

        return $"{varDecl}\n\n// ... {plan.SuggestedName} used in place of the expression";
    }

    private void ApplyEdits(GDPluginRefactoringContext context, GDRefactoringResult result)
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
