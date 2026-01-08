namespace GDShrapt.Plugin.Tests.Mocks;

/// <summary>
/// Mock interface for Godot's EditorInterface.
/// Allows unit testing without Godot runtime.
/// </summary>
public interface IEditorInterface
{
    /// <summary>
    /// Gets the current script editor.
    /// </summary>
    IScriptEditor GetScriptEditor();

    /// <summary>
    /// Gets the editor settings.
    /// </summary>
    IEditorSettings GetEditorSettings();

    /// <summary>
    /// Gets the base control for the editor.
    /// </summary>
    object GetBaseControl();

    /// <summary>
    /// Gets the currently edited scene root.
    /// </summary>
    object? GetEditedSceneRoot();

    /// <summary>
    /// Opens a script in the editor.
    /// </summary>
    void EditScript(string path, int line = 0, int column = 0);

    /// <summary>
    /// Gets the project path.
    /// </summary>
    string GetProjectPath();
}

/// <summary>
/// Mock interface for editor settings.
/// </summary>
public interface IEditorSettings
{
    /// <summary>
    /// Gets a setting value.
    /// </summary>
    object? GetSetting(string name);

    /// <summary>
    /// Sets a setting value.
    /// </summary>
    void SetSetting(string name, object value);

    /// <summary>
    /// Checks if a setting exists.
    /// </summary>
    bool HasSetting(string name);
}
