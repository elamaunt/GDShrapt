using GDShrapt.Reader;
using Godot;

namespace GDShrapt.Plugin;

internal class GDScriptEditor : IScriptEditor
{
    readonly TabController _tabController;
    readonly TextEdit? _textEdit;
    readonly GDShraptPlugin _plugin;
    readonly Control _tab;

    public GDScriptEditor(TabController tabController, TextEdit? textEdit, GDShraptPlugin plugin, Control tab)
    {
        _tabController = tabController;
        _textEdit = textEdit;
        _plugin = plugin;
        _tab = tab;
    }

    /// <summary>
    /// Returns true if the editor is properly initialized with a TextEdit control.
    /// </summary>
    public bool IsValid => _textEdit != null && GodotObject.IsInstanceValid(_textEdit);

    /// <summary>
    /// Returns true if there is an active text selection.
    /// </summary>
    public bool HasSelection => _textEdit?.HasSelection() ?? false;

    public GDScriptMap ScriptMap => _plugin.GetScriptMap(ScriptPath);
    public string ScriptPath => _tabController.ControlledScript?.ResourcePath ?? string.Empty;
    public string Name => _tabController.ControlledScript?.ResourceName ?? string.Empty;

    public string Text
    {
        get => _textEdit?.Text ?? string.Empty;
        set { if (_textEdit != null) _textEdit.Text = value; }
    }

    public int CursorLine
    {
        get => _textEdit?.GetCaretLine() ?? 0;
        set
        {
            if (_textEdit == null) return;
            Logger.Debug($"Set cursor line {value}");
            _textEdit.SetCaretLine(value);
        }
    }

    public int CursorColumn
    {
        get => _textEdit?.GetCaretColumn() ?? 0;
        set
        {
            if (_textEdit == null) return;
            Logger.Debug($"Set cursor column {value}");
            _textEdit.SetCaretColumn(value);
        }
    }

    public int SelectionStartLine => _textEdit?.GetSelectionFromLine() ?? 0;
    public int SelectionStartColumn => _textEdit?.GetSelectionFromColumn() ?? 0;
    public int SelectionEndLine => _textEdit?.GetSelectionToLine() ?? 0;
    public int SelectionEndColumn => _textEdit?.GetSelectionToColumn() ?? 0;

    public void Cut() => _textEdit?.Cut();

    public GDClassDeclaration? GetClass() => _plugin.GetScriptMap(ScriptPath)?.Class;

    public string GetLine(int line) => _textEdit?.GetLine(line) ?? string.Empty;
    public void InsertTextAtCursor(string text) => _textEdit?.InsertTextAtCaret(text);
    public void ReloadScriptFromText() => _plugin.GetScriptMap(ScriptPath)?.Reload(Text);

    public void RequestGodotLookup()
    {
        _tabController.RequestGodotLookup();
    }

    public void Select(int startLine, int startColumn, int endLine, int endColumn)
    {
        if (_textEdit == null) return;
        Logger.Debug($"Select token in the Editor {{{startLine},{startColumn}}}, {{{endLine},{endColumn}}}");
        _textEdit.Select(startLine, startColumn, endLine, endColumn);
    }

    public void SelectToken(GDSyntaxToken token)
    {
        var startLine = token.StartLine;
        var startColumn = token.StartColumn;
        var endLine = token.EndLine;
        var endColumn = token.EndColumn;

        Select(startLine, startColumn, endLine, endColumn);

        CursorLine = startLine;
        CursorColumn = startColumn;
    }
}
