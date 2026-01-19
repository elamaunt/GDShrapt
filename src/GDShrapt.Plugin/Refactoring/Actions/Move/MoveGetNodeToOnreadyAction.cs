using GDShrapt.Semantics;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Moves a get_node() call or $NodePath expression from inside a method
/// to an @onready variable declaration at class level.
/// Delegates to GDGenerateOnreadyService for all logic.
/// </summary>
internal class MoveGetNodeToOnreadyAction : IGDRefactoringAction
{
    private readonly GDGenerateOnreadyService _service = new();

    public string Id => "move_getnode_to_onready";
    public string DisplayName => "Move to @onready";
    public GDRefactoringCategory Category => GDRefactoringCategory.Move;
    public string Shortcut => null;
    public int Priority => 10;

    public bool IsAvailable(GDPluginRefactoringContext context)
    {
        if (context?.ContainingClass == null)
            return false;

        // Must be inside a method, not at class level
        if (context.ContainingMethod == null)
            return false;

        // Delegate to service for detailed check
        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null)
            return false;

        return _service.CanExecute(semanticsContext);
    }

    public async Task ExecuteAsync(GDPluginRefactoringContext context)
    {
        Logger.Info("MoveGetNodeToOnreadyAction: Starting execution");

        var semanticsContext = context.BuildSemanticsContext();
        if (semanticsContext == null || !_service.CanExecute(semanticsContext))
        {
            Logger.Warning("MoveGetNodeToOnreadyAction: Service not available");
            return;
        }

        await ExecuteWithService(context, semanticsContext);
    }

    private async Task ExecuteWithService(GDPluginRefactoringContext context, GDRefactoringContext semanticsContext)
    {
        // Plan the refactoring first
        var plan = _service.Plan(semanticsContext);

        if (!plan.Success)
        {
            Logger.Info($"MoveGetNodeToOnreadyAction: Plan failed - {plan.ErrorMessage}");
            return;
        }

        // Show name input dialog
        var nameDialog = new NameInputDialog();
        context.DialogParent?.AddChild(nameDialog);

        var typeHint = !string.IsNullOrEmpty(plan.InferredType) && plan.InferredType != "Node"
            ? $": {plan.InferredType}"
            : "";

        var varName = await nameDialog.ShowForResult(
            "Move to @onready",
            plan.VariableName,
            $"@onready var {plan.VariableName}{typeHint} = ${plan.NodePath}");

        nameDialog.QueueFree();

        if (string.IsNullOrEmpty(varName))
        {
            Logger.Info("MoveGetNodeToOnreadyAction: Cancelled by user");
            return;
        }

        // Re-plan with the user-provided name
        plan = _service.Plan(semanticsContext, varName);
        if (!plan.Success)
        {
            Logger.Info($"MoveGetNodeToOnreadyAction: Re-plan failed - {plan.ErrorMessage}");
            return;
        }

        // Build preview info
        var originalCode = $"${plan.NodePath}";
        var typeAnnotation = !string.IsNullOrEmpty(plan.InferredType) && plan.InferredType != "Node"
            ? $": {plan.InferredType}"
            : "";
        var resultCode = $"@onready var {plan.VariableName}{typeAnnotation} = ${plan.NodePath}\n\n// Expression replaced with: {plan.VariableName}";

        // Show preview dialog
        var previewDialog = new RefactoringPreviewDialog();
        context.DialogParent?.AddChild(previewDialog);

        try
        {
            var title = $"Move to @onready ({plan.TypeConfidence})";

            // In Base Plugin: Apply is disabled (Pro required)
            var canApply = false;
            var proMessage = "GDShrapt Pro required to apply this refactoring";

            var result = await previewDialog.ShowForResult(
                title,
                $"// Original: {originalCode}\n// Inferred type: {plan.InferredType ?? "Node"}\n// Confidence: {plan.TypeConfidence}\n// Reason: {plan.TypeConfidenceReason ?? "Type inference"}",
                resultCode,
                canApply,
                "Apply",
                proMessage);

            if (result.ShouldApply && canApply)
            {
                // Execute the refactoring via service
                var executeResult = _service.Execute(semanticsContext, varName);
                if (executeResult.Success)
                {
                    GDEditorEditApplicator.ApplyEdits(context, executeResult);
                    Logger.Info("MoveGetNodeToOnreadyAction: Completed via service");
                }
                else
                {
                    Logger.Warning($"MoveGetNodeToOnreadyAction: Execute failed - {executeResult.ErrorMessage}");
                }
            }
        }
        finally
        {
            previewDialog.QueueFree();
        }
    }
}
