using GDShrapt.Semantics;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal class RemoveCommentsCommand : Command
{
    private readonly GDRemoveCommentsService _service = new();
    private readonly GDPluginRefactoringContextBuilder _contextBuilder;

    public RemoveCommentsCommand(GDShraptPlugin plugin)
        : base(plugin)
    {
        _contextBuilder = new GDPluginRefactoringContextBuilder(plugin.ScriptProject);
    }

    public override async Task Execute(IScriptEditor controller)
    {
        Logger.Info("Remove comments requested");

        if (!controller.IsValid)
        {
            Logger.Info($"Remove comments cancelled: Editor is not valid");
            return;
        }

        // Build semantic context
        var context = _contextBuilder.BuildSemanticsContext(controller);

        if (context == null)
        {
            Logger.Info("Remove comments cancelled: Could not build context");
            return;
        }

        // Check if operation is possible
        if (!_service.CanExecute(context))
        {
            Logger.Info("Remove comments cancelled: No comments to remove");
            return;
        }

        // Get comment count for logging
        var commentCount = _service.GetCommentCount(context);
        Logger.Info($"Removing comments {commentCount}");

        // Execute the service
        var result = _service.Execute(context);

        if (!result.Success)
        {
            Logger.Warning($"Remove comments failed: {result.ErrorMessage}");
            return;
        }

        // Apply the edits - we get a single edit that replaces the entire content
        if (result.Edits.Count > 0)
        {
            var edit = result.Edits[0];

            var lineBefore = controller.CursorLine;
            var columnBefore = controller.CursorColumn;

            controller.Text = edit.NewText;
            controller.CursorLine = lineBefore;
            controller.CursorColumn = columnBefore;

            Logger.Info($"All comments have been removed");
        }

        await Task.CompletedTask;
    }
}
