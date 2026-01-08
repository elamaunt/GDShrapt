using Godot;
using GDShrapt.Plugin.Localization;

namespace GDShrapt.Plugin;

/// <summary>
/// Helper class for GDShrapt plugin initialization.
/// This class does NOT inherit from EditorPlugin to avoid duplicate type registration in Godot.
///
/// Users should create a class extending EditorPlugin in their project's addons folder
/// and call this helper's methods.
///
/// Example usage:
/// <code>
/// [Tool]
/// public partial class GDShraptActivator : EditorPlugin
/// {
///     private readonly GDShraptPluginHelper _helper = new();
///
///     public override void _EnterTree() => _helper.EnterTree();
///     public override void _ExitTree() => _helper.ExitTree();
///     public override void _Ready() => _helper.Ready();
///     public override void _Process(double delta) => _helper.Process(delta);
///     public override bool _Handles(GodotObject obj) => _helper.Handles(obj);
///     public override void _MakeVisible(bool visible) => _helper.MakeVisible(visible);
///     public override bool _HasMainScreen() => _helper.HasMainScreen();
///     public override string _GetPluginName() => _helper.GetPluginName();
///     public override Texture2D _GetPluginIcon() => _helper.GetPluginIcon();
/// }
/// </code>
/// </summary>
public sealed class GDShraptPluginHelper
{
    private GDShraptPlugin? _internalPlugin;

    /// <summary>
    /// Gets the internal plugin instance.
    /// </summary>
    public GDShraptPlugin? InternalPlugin => _internalPlugin;

    /// <summary>
    /// Call from _EnterTree().
    /// </summary>
    public void EnterTree()
    {
        try
        {
            GD.Print("GDShrapt: Plugin loading...");

            // Initialize localization
            LocalizationManager.Initialize(GetEditorLanguage());

            // Create and initialize the internal plugin
            _internalPlugin = new GDShraptPlugin();
            _internalPlugin._EnterTree();

            GD.Print("GDShrapt: Plugin activated successfully");
            Logger.Info("GDShrapt plugin activated");
        }
        catch (System.Exception ex)
        {
            GD.PushError($"GDShrapt: Failed to initialize plugin - {ex.Message}");
            Logger.Error("Failed to initialize GDShrapt plugin", ex);
        }
    }

    /// <summary>
    /// Call from _ExitTree().
    /// </summary>
    public void ExitTree()
    {
        try
        {
            _internalPlugin?._ExitTree();
            _internalPlugin = null;

            Logger.Info("GDShrapt plugin deactivated");
            Logger.Close();
        }
        catch (System.Exception ex)
        {
            Logger.Error("Error during GDShrapt plugin shutdown", ex);
        }
    }

    /// <summary>
    /// Call from _Ready().
    /// </summary>
    public void Ready()
    {
        _internalPlugin?._Ready();
    }

    /// <summary>
    /// Call from _Process(double delta).
    /// </summary>
    public void Process(double delta)
    {
        _internalPlugin?._Process(delta);
    }

    /// <summary>
    /// Call from _Handles(GodotObject obj).
    /// </summary>
    public bool Handles(GodotObject @object)
    {
        return _internalPlugin?._Handles(@object) ?? false;
    }

    /// <summary>
    /// Call from _MakeVisible(bool visible).
    /// </summary>
    public void MakeVisible(bool visible)
    {
        _internalPlugin?._MakeVisible(visible);
    }

    /// <summary>
    /// Call from _HasMainScreen().
    /// </summary>
    public bool HasMainScreen()
    {
        return _internalPlugin?._HasMainScreen() ?? false; // No main screen - using bottom docks
    }

    /// <summary>
    /// Call from _GetPluginName().
    /// </summary>
    public string GetPluginName()
    {
        return _internalPlugin?._GetPluginName() ?? "GDShrapt";
    }

    /// <summary>
    /// Call from _GetPluginIcon().
    /// </summary>
    public Texture2D? GetPluginIcon()
    {
        return _internalPlugin?._GetPluginIcon();
    }

    /// <summary>
    /// Gets the current editor language.
    /// </summary>
    private static string GetEditorLanguage()
    {
        try
        {
            var settings = EditorInterface.Singleton?.GetEditorSettings();
            if (settings != null)
            {
                var lang = settings.GetSetting("interface/editor/editor_language").AsString();
                if (!string.IsNullOrEmpty(lang))
                {
                    // Convert Godot language codes to our format
                    return lang.Split('_')[0].ToLowerInvariant();
                }
            }
        }
        catch
        {
            // Fallback to English
        }

        return "en";
    }
}
