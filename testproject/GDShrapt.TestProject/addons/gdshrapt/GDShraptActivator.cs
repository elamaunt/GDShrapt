using GDShrapt.Plugin;
using Godot;

namespace GDShrapt.TestProject;

/// <summary>
/// Activator class for the GDShrapt plugin.
/// Users create this class in their project to enable the plugin.
/// </summary>
[Tool]
public partial class GDShraptActivator : EditorPlugin
{
    private readonly GDShraptPluginHelper _helper = new();

    public override void _EnterTree() => _helper.EnterTree();
    public override void _ExitTree() => _helper.ExitTree();
    public override void _Ready() => _helper.Ready();
    public override void _Process(double delta) => _helper.Process(delta);
    public override bool _Handles(GodotObject @object) => _helper.Handles(@object);
    public override void _MakeVisible(bool visible) => _helper.MakeVisible(visible);
    public override bool _HasMainScreen() => _helper.HasMainScreen();
    public override string _GetPluginName() => _helper.GetPluginName();
    public override Texture2D _GetPluginIcon() => _helper.GetPluginIcon();
}
