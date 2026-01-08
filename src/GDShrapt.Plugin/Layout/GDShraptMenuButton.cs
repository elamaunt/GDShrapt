using Godot;
using System;
using System.Collections.Generic;

namespace GDShrapt.Plugin;

internal partial class GDShraptMenuButton : MenuButton
{
    private readonly GDShraptPlugin _plugin;
    private readonly Dictionary<int, Action> _actions = new Dictionary<int, Action>();

    public GDShraptMenuButton(GDShraptPlugin plugin)
    {
        _plugin = plugin;
        Text = "GDShrapt";

        var popup = GetPopup();

        void addItem(string name, int index, Action exec)
        {
            popup.AddItem(name, index);
            _actions[index] = exec;
        }

        addItem("Preferences", 1, _plugin.OpenPreferences);
        addItem("About", 2, _plugin.OpenAbout);

        popup.IdPressed += OnItemPressed;
    }

    public void OnItemPressed(long id)
    {
        if (_actions.TryGetValue((int)id, out var exec))
            exec();
    }
}
