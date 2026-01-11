namespace GDShrapt.Plugin.Tests;

/// <summary>
/// Mock interface for Godot's ScriptEditor.
/// Allows unit testing without Godot runtime.
/// </summary>
public interface IScriptEditor
{
    /// <summary>
    /// Gets the currently open script.
    /// </summary>
    IScript? GetCurrentScript();

    /// <summary>
    /// Gets all open scripts.
    /// </summary>
    IReadOnlyList<IScript> GetOpenScripts();

    /// <summary>
    /// Navigates to a specific script and line.
    /// </summary>
    void GoToLine(int line);

    /// <summary>
    /// Gets the current line number.
    /// </summary>
    int GetCurrentLine();

    /// <summary>
    /// Gets the current column number.
    /// </summary>
    int GetCurrentColumn();

    /// <summary>
    /// Gets the selected text.
    /// </summary>
    string GetSelectedText();

    /// <summary>
    /// Sets the selected text.
    /// </summary>
    void SetSelectedText(string text);

    /// <summary>
    /// Gets all text in the editor.
    /// </summary>
    string GetText();

    /// <summary>
    /// Sets all text in the editor.
    /// </summary>
    void SetText(string text);

    /// <summary>
    /// Gets the selection start line.
    /// </summary>
    int GetSelectionFromLine();

    /// <summary>
    /// Gets the selection start column.
    /// </summary>
    int GetSelectionFromColumn();

    /// <summary>
    /// Gets the selection end line.
    /// </summary>
    int GetSelectionToLine();

    /// <summary>
    /// Gets the selection end column.
    /// </summary>
    int GetSelectionToColumn();

    /// <summary>
    /// Selects text.
    /// </summary>
    void Select(int fromLine, int fromColumn, int toLine, int toColumn);
}

/// <summary>
/// Mock interface for a script resource.
/// </summary>
public interface IScript
{
    /// <summary>
    /// Gets the resource path.
    /// </summary>
    string ResourcePath { get; }

    /// <summary>
    /// Gets the source code.
    /// </summary>
    string SourceCode { get; set; }

    /// <summary>
    /// Gets the script's base type.
    /// </summary>
    string? GetBaseType();
}
