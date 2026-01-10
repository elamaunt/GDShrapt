using GDShrapt.Semantics;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal class AutoCompleteCommand : Command
{
    private readonly GDSnippetService _snippetService = new();

    public AutoCompleteCommand(GDShraptPlugin plugin)
        : base(plugin)
    {
    }

    public override async Task Execute(IScriptEditor controller)
    {
        Logger.Info("Autocompletion requested");

        if (!controller.IsValid)
        {
            Logger.Info($"Autocompletion cancelled: Editor is not valid");
            return;
        }

        var line = controller.CursorLine;
        var lineText = controller.GetLine(line);
        var index = controller.CursorColumn;

        if (lineText.Length < index)
        {
            Logger.Info("Autocompletion cancelled");
            return;
        }

        // Use the snippet service to match keywords
        if (_snippetService.TryMatchKeyword(lineText, out var keyword))
        {
            var snippet = _snippetService.GetSnippetForKeyword(keyword);
            if (snippet != null)
            {
                // Get the insertion text from the snippet
                var insertionText = _snippetService.GetInsertionText(snippet);

                // Insert the text
                controller.InsertTextAtCursor(insertionText);

                // Select the first placeholder if available
                if (_snippetService.GetFirstPlaceholderSelection(snippet, index, out var startCol, out var endCol))
                {
                    controller.Select(line, startCol, line, endCol);
                }

                Logger.Info($"Autocompletion: {snippet.Description}");
                return;
            }
        }

        Logger.Info("Autocompletion not matched");
        await Task.CompletedTask;
    }
}
