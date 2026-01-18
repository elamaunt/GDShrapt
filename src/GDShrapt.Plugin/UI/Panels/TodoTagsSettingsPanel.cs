namespace GDShrapt.Plugin;

/// <summary>
/// Settings panel for configuring TODO tags.
/// </summary>
internal partial class TodoTagsSettingsPanel : Window
{
    private GDConfigManager? _configManager;
    private ItemList _tagList;
    private LineEdit _newTagName;
    private ColorPickerButton _newTagColor;
    private Button _addTagButton;
    private Button _removeTagButton;
    private Button _moveUpButton;
    private Button _moveDownButton;
    private Button _toggleEnabledButton;
    private CheckButton _enabledToggle;
    private CheckButton _caseSensitiveToggle;
    private CheckButton _scanOnStartupToggle;
    private CheckButton _autoRefreshToggle;
    private Button _saveButton;
    private Button _resetButton;
    private Button _cancelButton;

    private List<GDTodoTagDefinition> _workingTags = new();

    /// <summary>
    /// Event fired when settings are saved.
    /// </summary>
    public event Action? SettingsSaved;

    public override void _Ready()
    {
        Title = "TODO Tags Settings";
        Size = new Vector2I(450, 550);
        Exclusive = true;
        CloseRequested += OnCancelPressed;

        CreateUI();
    }

    public void Initialize(GDConfigManager configManager)
    {
        _configManager = configManager;
        LoadFromConfig();
    }

    private void CreateUI()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 15);
        margin.AddThemeConstantOverride("margin_right", 15);
        margin.AddThemeConstantOverride("margin_top", 15);
        margin.AddThemeConstantOverride("margin_bottom", 15);
        AddChild(margin);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(mainVBox);

        // Title
        var title = new Label { Text = "TODO Tags Settings" };
        title.AddThemeFontSizeOverride("font_size", 16);
        mainVBox.AddChild(title);
        mainVBox.AddChild(new HSeparator());

        // General settings section
        var generalLabel = new Label { Text = "General" };
        generalLabel.AddThemeFontSizeOverride("font_size", 14);
        mainVBox.AddChild(generalLabel);

        _enabledToggle = new CheckButton { Text = "Enable TODO Tags scanning" };
        mainVBox.AddChild(_enabledToggle);

        _scanOnStartupToggle = new CheckButton { Text = "Scan on project startup" };
        mainVBox.AddChild(_scanOnStartupToggle);

        _autoRefreshToggle = new CheckButton { Text = "Auto-refresh when files change" };
        mainVBox.AddChild(_autoRefreshToggle);

        _caseSensitiveToggle = new CheckButton { Text = "Case-sensitive matching" };
        mainVBox.AddChild(_caseSensitiveToggle);

        mainVBox.AddChild(new HSeparator());

        // Tags section
        var tagsLabel = new Label { Text = "Tag Definitions" };
        tagsLabel.AddThemeFontSizeOverride("font_size", 14);
        mainVBox.AddChild(tagsLabel);

        // Tags list with edit controls
        var tagsHBox = new HBoxContainer();
        tagsHBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        tagsHBox.AddThemeConstantOverride("separation", 8);
        mainVBox.AddChild(tagsHBox);

        _tagList = new ItemList
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SelectMode = ItemList.SelectModeEnum.Single,
            CustomMinimumSize = new Vector2(200, 150)
        };
        _tagList.ItemSelected += OnTagSelected;
        tagsHBox.AddChild(_tagList);

        // Tag control buttons
        var tagButtonsVBox = new VBoxContainer();
        tagButtonsVBox.AddThemeConstantOverride("separation", 4);
        tagsHBox.AddChild(tagButtonsVBox);

        _moveUpButton = new Button { Text = "Up", Disabled = true };
        _moveUpButton.Pressed += OnMoveUpPressed;
        tagButtonsVBox.AddChild(_moveUpButton);

        _moveDownButton = new Button { Text = "Down", Disabled = true };
        _moveDownButton.Pressed += OnMoveDownPressed;
        tagButtonsVBox.AddChild(_moveDownButton);

        tagButtonsVBox.AddChild(new HSeparator());

        _toggleEnabledButton = new Button { Text = "Toggle", Disabled = true };
        _toggleEnabledButton.Pressed += OnToggleEnabledPressed;
        tagButtonsVBox.AddChild(_toggleEnabledButton);

        _removeTagButton = new Button { Text = "Remove", Disabled = true };
        _removeTagButton.Pressed += OnRemovePressed;
        tagButtonsVBox.AddChild(_removeTagButton);

        mainVBox.AddChild(new HSeparator());

        // Add new tag section
        var addTagLabel = new Label { Text = "Add New Tag" };
        mainVBox.AddChild(addTagLabel);

        var addHBox = new HBoxContainer();
        addHBox.AddThemeConstantOverride("separation", 8);
        mainVBox.AddChild(addHBox);

        addHBox.AddChild(new Label { Text = "Name:" });
        _newTagName = new LineEdit
        {
            PlaceholderText = "TAG",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(80, 0)
        };
        _newTagName.TextSubmitted += _ => OnAddPressed();
        addHBox.AddChild(_newTagName);

        addHBox.AddChild(new Label { Text = "Color:" });
        _newTagColor = new ColorPickerButton
        {
            Color = new Color(0.5f, 0.8f, 1.0f),
            CustomMinimumSize = new Vector2(60, 0)
        };
        addHBox.AddChild(_newTagColor);

        _addTagButton = new Button { Text = "Add" };
        _addTagButton.Pressed += OnAddPressed;
        addHBox.AddChild(_addTagButton);

        mainVBox.AddChild(new HSeparator());

        // Action buttons
        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", 8);
        buttonRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        _resetButton = new Button { Text = "Reset to Defaults" };
        _resetButton.Pressed += OnResetPressed;
        buttonRow.AddChild(_resetButton);

        _cancelButton = new Button { Text = "Cancel" };
        _cancelButton.Pressed += OnCancelPressed;
        buttonRow.AddChild(_cancelButton);

        _saveButton = new Button { Text = "Save" };
        _saveButton.Pressed += OnSavePressed;
        buttonRow.AddChild(_saveButton);

        mainVBox.AddChild(buttonRow);
    }

    private void LoadFromConfig()
    {
        if (_configManager == null)
            return;

        var config = _configManager.Config.Plugin?.TodoTags ?? new GDTodoTagsConfig();

        _enabledToggle.ButtonPressed = config.Enabled;
        _scanOnStartupToggle.ButtonPressed = config.ScanOnStartup;
        _autoRefreshToggle.ButtonPressed = config.AutoRefresh;
        _caseSensitiveToggle.ButtonPressed = config.CaseSensitive;

        _workingTags = config.Tags.Select(t => new GDTodoTagDefinition
        {
            Name = t.Name,
            Color = t.Color,
            Enabled = t.Enabled,
            DefaultPriority = t.DefaultPriority
        }).ToList();

        RefreshTagList();
    }

    private void RefreshTagList()
    {
        _tagList.Clear();

        foreach (var tag in _workingTags)
        {
            var text = $"{tag.Name} ({(tag.Enabled ? "enabled" : "disabled")})";
            var idx = _tagList.AddItem(text);

            if (TryParseColor(tag.Color, out var color))
            {
                _tagList.SetItemCustomFgColor(idx, color);
            }

            if (!tag.Enabled)
            {
                _tagList.SetItemCustomFgColor(idx, new Color(0.5f, 0.5f, 0.5f));
            }
        }

        UpdateButtonStates();
    }

    private bool TryParseColor(string hex, out Color color)
    {
        color = Colors.White;
        try
        {
            if (string.IsNullOrEmpty(hex))
                return false;

            hex = hex.TrimStart('#');

            if (hex.Length == 6)
            {
                var r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
                var g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
                var b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
                color = new Color(r, g, b);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private string ColorToHex(Color color)
    {
        var r = (int)(color.R * 255);
        var g = (int)(color.G * 255);
        var b = (int)(color.B * 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private void UpdateButtonStates()
    {
        var selected = _tagList.GetSelectedItems();
        var hasSelection = selected.Length > 0;
        var index = hasSelection ? selected[0] : -1;

        _removeTagButton.Disabled = !hasSelection;
        _toggleEnabledButton.Disabled = !hasSelection;
        _moveUpButton.Disabled = !hasSelection || index <= 0;
        _moveDownButton.Disabled = !hasSelection || index >= _workingTags.Count - 1;
    }

    private void OnTagSelected(long index)
    {
        UpdateButtonStates();
    }

    private void OnAddPressed()
    {
        var name = _newTagName.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(name))
            return;

        if (_workingTags.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            Logger.Warning($"Tag '{name}' already exists");
            return;
        }

        _workingTags.Add(new GDTodoTagDefinition
        {
            Name = name,
            Color = ColorToHex(_newTagColor.Color),
            Enabled = true,
            DefaultPriority = GDTodoPriority.Normal
        });

        _newTagName.Text = "";
        RefreshTagList();
    }

    private void OnRemovePressed()
    {
        var selected = _tagList.GetSelectedItems();
        if (selected.Length == 0)
            return;

        var idx = selected[0];
        _workingTags.RemoveAt(idx);
        RefreshTagList();
    }

    private void OnToggleEnabledPressed()
    {
        var selected = _tagList.GetSelectedItems();
        if (selected.Length == 0)
            return;

        var idx = selected[0];
        _workingTags[idx].Enabled = !_workingTags[idx].Enabled;
        RefreshTagList();
        _tagList.Select(idx);
    }

    private void OnMoveUpPressed()
    {
        var selected = _tagList.GetSelectedItems();
        if (selected.Length == 0 || selected[0] <= 0)
            return;

        var idx = selected[0];
        (_workingTags[idx], _workingTags[idx - 1]) = (_workingTags[idx - 1], _workingTags[idx]);
        RefreshTagList();
        _tagList.Select(idx - 1);
    }

    private void OnMoveDownPressed()
    {
        var selected = _tagList.GetSelectedItems();
        if (selected.Length == 0 || selected[0] >= _workingTags.Count - 1)
            return;

        var idx = selected[0];
        (_workingTags[idx], _workingTags[idx + 1]) = (_workingTags[idx + 1], _workingTags[idx]);
        RefreshTagList();
        _tagList.Select(idx + 1);
    }

    private void OnSavePressed()
    {
        if (_configManager == null)
            return;

        // Ensure Plugin section exists
        _configManager.Config.Plugin ??= new GDPluginConfig();
        _configManager.Config.Plugin.TodoTags ??= new GDTodoTagsConfig();

        var config = _configManager.Config.Plugin.TodoTags;

        config.Enabled = _enabledToggle.ButtonPressed;
        config.ScanOnStartup = _scanOnStartupToggle.ButtonPressed;
        config.AutoRefresh = _autoRefreshToggle.ButtonPressed;
        config.CaseSensitive = _caseSensitiveToggle.ButtonPressed;
        config.Tags = _workingTags;

        _configManager.SaveConfig();
        Logger.Info("TODO Tags settings saved");

        SettingsSaved?.Invoke();
        Hide();
    }

    private void OnResetPressed()
    {
        _workingTags = new List<GDTodoTagDefinition>
        {
            new("TODO", "#4FC3F7", GDTodoPriority.Normal),
            new("FIXME", "#FF8A65", GDTodoPriority.High),
            new("HACK", "#FFD54F", GDTodoPriority.Normal),
            new("NOTE", "#81C784", GDTodoPriority.Low),
            new("BUG", "#EF5350", GDTodoPriority.High),
            new("XXX", "#CE93D8", GDTodoPriority.Low)
        };

        _enabledToggle.ButtonPressed = true;
        _scanOnStartupToggle.ButtonPressed = true;
        _autoRefreshToggle.ButtonPressed = true;
        _caseSensitiveToggle.ButtonPressed = false;

        RefreshTagList();
    }

    private void OnCancelPressed()
    {
        Hide();
    }
}
