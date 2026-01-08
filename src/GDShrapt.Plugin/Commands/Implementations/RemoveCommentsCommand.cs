using GDShrapt.Reader;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal class RemoveCommentsCommand : Command
{
    public RemoveCommentsCommand(GDShraptPlugin plugin)
        : base(plugin)
    {
    }

    public override async Task Execute(IScriptEditor controller)
    {
        Logger.Info("Remove comments requested");

        if (!controller.IsValid)
        {
            Logger.Info($"Remove comments cancelled: Editor is not valid");
            return;
        }

        var @class = controller.GetClass();

        @class = (GDClassDeclaration)@class.Clone();

        var comments = @class.AllTokens.OfType<GDComment>().ToArray();

        Logger.Info($"Removing comments {comments.Length}");

        for (int i = 0; i < comments.Length; i++)
            comments[i].RemoveFromParent();

        var newCode = @class.ToString();

        Logger.Info($"Resulted code length now {newCode.Length}");

        var lineBefore = controller.CursorLine;
        var columnBefore = controller.CursorColumn;

        controller.Text = @class.ToString();
        controller.CursorLine = lineBefore;
        controller.CursorColumn = columnBefore;

        Logger.Info($"All comments have been removed");
    }
}
