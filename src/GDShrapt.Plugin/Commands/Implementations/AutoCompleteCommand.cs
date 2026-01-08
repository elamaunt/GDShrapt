using GDShrapt.Reader;
using System;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal class AutoCompleteCommand : Command
{
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

        // Try each completion pattern
        if (TryCompleteFor(controller, lineText, line, index)) return;
        if (TryCompleteWhile(controller, lineText, line, index)) return;
        if (TryCompleteIf(controller, lineText, line, index)) return;
        if (TryCompleteElif(controller, lineText, line, index)) return;
        if (TryCompleteElse(controller, lineText, line, index)) return;
        if (TryCompleteMatch(controller, lineText, line, index)) return;
        if (TryCompleteFunc(controller, lineText, line, index)) return;
        if (TryCompleteClass(controller, lineText, line, index)) return;
        if (TryCompleteEnum(controller, lineText, line, index)) return;
        if (TryCompleteSignal(controller, lineText, line, index)) return;
        if (TryCompleteVar(controller, lineText, line, index)) return;
        if (TryCompleteConst(controller, lineText, line, index)) return;
        if (TryCompleteAwait(controller, lineText, line, index)) return;
        if (TryCompleteReturn(controller, lineText, line, index)) return;
        if (TryCompletePass(controller, lineText, line, index)) return;
        if (TryCompleteBreak(controller, lineText, line, index)) return;
        if (TryCompleteContinue(controller, lineText, line, index)) return;

        Logger.Info("Autocompletion not matched");
        await Task.CompletedTask;
    }

    private bool MatchesKeyword(string lineText, string keyword)
    {
        return lineText.EndsWith($" {keyword}", StringComparison.Ordinal) ||
               lineText.EndsWith($"\t{keyword}", StringComparison.Ordinal) ||
               lineText.Equals(keyword, StringComparison.Ordinal);
    }

    private bool TryCompleteFor(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "for")) return false;

        var forStatement = new GDForStatement();
        forStatement[1] = GD.Syntax.Space();
        forStatement.Variable = GD.Syntax.Identifier("i");
        forStatement[2] = GD.Syntax.Space();
        forStatement.InKeyword = new GDInKeyword();
        forStatement[3] = GD.Syntax.Space();
        forStatement.Collection = GD.Expression.Call(GD.Expression.Identifier("range"), GD.Expression.Number(10));
        forStatement.Colon = new GDColon();
        forStatement[5] = GD.Syntax.Space();

        var insertion = forStatement.ToString();
        controller.InsertTextAtCursor(insertion);
        controller.Select(line, index + 1, line, index + 2); // Select 'i'

        Logger.Info("Autocompletion: for loop");
        return true;
    }

    private bool TryCompleteWhile(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "while")) return false;

        var whileStatement = new GDWhileStatement();
        whileStatement[1] = GD.Syntax.Space();
        whileStatement.Condition = GD.Expression.Bool(true);
        whileStatement.Colon = new GDColon();
        whileStatement[3] = GD.Syntax.Space();

        var insertion = whileStatement.ToString();
        controller.InsertTextAtCursor(insertion);
        controller.Select(line, index + 1, line, index + 5); // Select 'true'

        Logger.Info("Autocompletion: while loop");
        return true;
    }

    private bool TryCompleteIf(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "if")) return false;

        var ifStatement = new GDIfStatement();
        var ifBranch = new GDIfBranch();
        ifStatement.IfBranch = ifBranch;

        ifBranch[1] = GD.Syntax.Space();
        ifBranch.Condition = GD.Expression.Bool(true);
        ifBranch.Colon = new GDColon();
        ifBranch[3] = GD.Syntax.Space();

        var insertion = ifStatement.ToString();
        controller.InsertTextAtCursor(insertion);
        controller.Select(line, index + 1, line, index + 5); // Select 'true'

        Logger.Info("Autocompletion: if statement");
        return true;
    }

    private bool TryCompleteElif(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "elif")) return false;

        // elif condition:
        controller.InsertTextAtCursor(" true:");
        controller.Select(line, index + 1, line, index + 5); // Select 'true'

        Logger.Info("Autocompletion: elif branch");
        return true;
    }

    private bool TryCompleteElse(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "else")) return false;

        // else:
        controller.InsertTextAtCursor(":");

        Logger.Info("Autocompletion: else branch");
        return true;
    }

    private bool TryCompleteMatch(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "match")) return false;

        // match expression:
        controller.InsertTextAtCursor(" value:");
        controller.Select(line, index + 1, line, index + 6); // Select 'value'

        Logger.Info("Autocompletion: match statement");
        return true;
    }

    private bool TryCompleteFunc(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "func")) return false;

        // func name():
        controller.InsertTextAtCursor(" _new_function():");
        controller.Select(line, index + 1, line, index + 14); // Select '_new_function'

        Logger.Info("Autocompletion: function declaration");
        return true;
    }

    private bool TryCompleteClass(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "class")) return false;

        // class ClassName:
        controller.InsertTextAtCursor(" NewClass:");
        controller.Select(line, index + 1, line, index + 9); // Select 'NewClass'

        Logger.Info("Autocompletion: inner class");
        return true;
    }

    private bool TryCompleteEnum(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "enum")) return false;

        // enum Name { VALUE }
        controller.InsertTextAtCursor(" Name { VALUE }");
        controller.Select(line, index + 1, line, index + 5); // Select 'Name'

        Logger.Info("Autocompletion: enum declaration");
        return true;
    }

    private bool TryCompleteSignal(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "signal")) return false;

        // signal name()
        controller.InsertTextAtCursor(" signal_name()");
        controller.Select(line, index + 1, line, index + 12); // Select 'signal_name'

        Logger.Info("Autocompletion: signal declaration");
        return true;
    }

    private bool TryCompleteVar(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "var")) return false;

        // var name = value
        controller.InsertTextAtCursor(" variable_name = null");
        controller.Select(line, index + 1, line, index + 14); // Select 'variable_name'

        Logger.Info("Autocompletion: variable declaration");
        return true;
    }

    private bool TryCompleteConst(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "const")) return false;

        // const NAME = value
        controller.InsertTextAtCursor(" CONSTANT_NAME = 0");
        controller.Select(line, index + 1, line, index + 14); // Select 'CONSTANT_NAME'

        Logger.Info("Autocompletion: constant declaration");
        return true;
    }

    private bool TryCompleteAwait(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "await")) return false;

        // await signal
        controller.InsertTextAtCursor(" signal");
        controller.Select(line, index + 1, line, index + 7); // Select 'signal'

        Logger.Info("Autocompletion: await expression");
        return true;
    }

    private bool TryCompleteReturn(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "return")) return false;

        // return value
        controller.InsertTextAtCursor(" ");

        Logger.Info("Autocompletion: return statement");
        return true;
    }

    private bool TryCompletePass(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "pass")) return false;

        // pass - no additional text needed
        Logger.Info("Autocompletion: pass statement");
        return true;
    }

    private bool TryCompleteBreak(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "break")) return false;

        // break - no additional text needed
        Logger.Info("Autocompletion: break statement");
        return true;
    }

    private bool TryCompleteContinue(IScriptEditor controller, string lineText, int line, int index)
    {
        if (!MatchesKeyword(lineText, "continue")) return false;

        // continue - no additional text needed
        Logger.Info("Autocompletion: continue statement");
        return true;
    }
}
