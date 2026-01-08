using GDShrapt.Reader;

namespace GDShrapt.Plugin;

internal interface IScriptEditor
{
    GDScriptMap ScriptMap { get; }
    string ScriptPath { get; }
    string Name { get; }

    /// <summary>
    /// Returns true if the editor is properly initialized and can be used.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Returns true if there is an active text selection.
    /// </summary>
    bool HasSelection { get; }

    string Text { get; set; }
    int CursorLine { get; set; }
    int CursorColumn { get; set; }
    GDClassDeclaration? GetClass();
    void SelectToken(GDSyntaxToken token);
    int SelectionStartLine { get; }
    int SelectionStartColumn { get; }
    int SelectionEndLine { get; }
    int SelectionEndColumn { get; }
    void Select(int startLine, int startColumn, int endLine, int endColumn);
    void Cut();
    void InsertTextAtCursor(string text);
    string GetLine(int line);
    void ReloadScriptFromText();
    void RequestGodotLookup();
}
