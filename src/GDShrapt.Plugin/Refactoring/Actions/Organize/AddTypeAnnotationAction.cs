using GDShrapt.Reader;
using GDShrapt.Semantics;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Adds type annotation to a variable or parameter declaration based on inferred type.
/// Delegates to GDAddTypeAnnotationService for type inference and shows preview before applying.
/// </summary>
internal class AddTypeAnnotationAction : RefactoringActionBase
{
    private readonly GDAddTypeAnnotationService _service = new();

    public override string Id => "add_type_annotation";
    public override string DisplayName => "Add Type Annotation";
    public override RefactoringCategory Category => RefactoringCategory.Organize;
    public override int Priority => 10;

    public override bool IsAvailable(RefactoringContext context)
    {
        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null)
            return false;

        return _service.CanExecute(semanticsContext);
    }

    protected override string ValidateContext(RefactoringContext context)
    {
        if (context.Editor == null)
            return "No editor available";

        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null)
            return "Cannot build semantics context";

        if (!_service.CanExecute(semanticsContext))
            return "No declaration suitable for type annotation at cursor";

        return null;
    }

    protected override async Task ExecuteInternalAsync(RefactoringContext context)
    {
        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null)
        {
            Logger.Warning("AddTypeAnnotationAction: Cannot build semantics context");
            return;
        }

        // Delegate planning to the service
        var plan = _service.Plan(semanticsContext);

        if (!plan.Success)
        {
            Logger.Info($"AddTypeAnnotationAction: {plan.ErrorMessage}");
            return;
        }

        // Show preview and apply
        await ShowPreviewAndApply(context, plan);
    }

    private async Task ShowPreviewAndApply(RefactoringContext context, GDAddTypeAnnotationResult plan)
    {
        var previewDialog = new RefactoringPreviewDialog();
        context.DialogParent?.AddChild(previewDialog);

        try
        {
            var title = $"Add Type Annotation ({plan.Confidence})";
            var targetDescription = plan.Target switch
            {
                TypeAnnotationTarget.ClassVariable => "class variable",
                TypeAnnotationTarget.LocalVariable => "local variable",
                TypeAnnotationTarget.Parameter => "parameter",
                _ => "declaration"
            };

            var originalCode = $"{plan.IdentifierName}";
            var resultCode = $"{plan.IdentifierName}: {plan.TypeName}";

            // In Base Plugin: Apply is disabled (Pro required)
            var canApply = false;
            var proMessage = "GDShrapt Pro required to apply this refactoring";

            var result = await previewDialog.ShowForResult(
                title,
                $"// Add type annotation to {targetDescription}\n" +
                $"// Inferred type: {plan.TypeName}\n" +
                $"// Confidence: {plan.Confidence}\n" +
                $"// Reason: {plan.ConfidenceReason ?? "Type inference"}\n\n" +
                originalCode,
                resultCode,
                canApply,
                "Apply",
                proMessage);

            if (result.ShouldApply && canApply)
            {
                // Execute the refactoring via service
                var executeResult = _service.Execute(context.BuildSemanticsContext());
                if (executeResult.Success)
                {
                    GDEditorEditApplicator.ApplyEdits(context, executeResult);
                    Logger.Info("AddTypeAnnotationAction: Completed successfully");
                }
                else
                {
                    Logger.Warning($"AddTypeAnnotationAction: {executeResult.ErrorMessage}");
                }
            }
        }
        finally
        {
            previewDialog.QueueFree();
        }
    }
}
