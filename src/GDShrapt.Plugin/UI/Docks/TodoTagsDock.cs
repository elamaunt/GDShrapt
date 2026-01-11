using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// Bottom dock panel for displaying TODO/FIXME tags from the project.
/// </summary>
internal partial class TodoTagsDock : Control
{
    private Label _headerLabel;
    private Tree _resultsTree;
    private OptionButton _groupByOption;
    private OptionButton _filterByTagOption;
    private Button _refreshButton;
    private Button _settingsButton;
    private CheckButton _autoRefreshToggle;
    private Label _statusLabel;

    private TodoTagsScanner? _scanner;
    private ConfigManager? _configManager;
    private TodoTagsScanResult? _currentResult;
    private TodoGroupingMode _groupingMode = TodoGroupingMode.ByFile;
    private string? _filterTag; // null = all tags

    private readonly Dictionary<string, Color> _tagColors = new();

    /// <summary>
    /// Event fired when user wants to navigate to a TODO item.
    /// </summary>
    public event Action<string, int, int>? NavigateToItem;

    /// <summary>
    /// Event fired when user wants to open settings.
    /// </summary>
    public event Action? OpenSettingsRequested;

    public override void _Ready()
    {
        Name = "TODO Tags";
        CreateUI();
    }

    /// <summary>
    /// Initializes the dock with required dependencies.
    /// </summary>
    public void Initialize(TodoTagsScanner scanner, ConfigManager configManager)
    {
        _scanner = scanner;
        _configManager = configManager;

        // Ensure UI is created
        if (_filterByTagOption == null)
            CreateUI();

        // Subscribe to scanner events
        _scanner.OnScanCompleted += OnScanCompleted;
        _scanner.OnFileScanned += OnFileScanned;

        // Load tag colors from config
        LoadTagColors(configManager.Config.TodoTags);

        // Subscribe to config changes
        configManager.OnConfigChanged += config => LoadTagColors(config.TodoTags);

        // Populate filter dropdown
        PopulateFilterDropdown(configManager.Config.TodoTags);

        // Set initial grouping from config
        _groupingMode = configManager.Config.TodoTags.DefaultGrouping;
        _groupByOption?.Select((int)_groupingMode);
    }

    private void CreateUI()
    {
        // Main container
        var mainVBox = new VBoxContainer();
        mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        mainVBox.AddThemeConstantOverride("separation", 4);
        AddChild(mainVBox);

        // Toolbar
        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 8);
        mainVBox.AddChild(toolbar);

        // Header label
        _headerLabel = new Label
        {
            Text = LocalizationManager.Tr(Strings.TodoTagsTitle),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _headerLabel.AddThemeFontSizeOverride("font_size", 14);
        toolbar.AddChild(_headerLabel);

        // Group by dropdown
        toolbar.AddChild(new Label { Text = "Group:" });
        _groupByOption = new OptionButton();
        _groupByOption.AddItem("By File", (int)TodoGroupingMode.ByFile);
        _groupByOption.AddItem("By Tag", (int)TodoGroupingMode.ByTag);
        _groupByOption.ItemSelected += OnGroupByChanged;
        toolbar.AddChild(_groupByOption);

        // Filter by tag dropdown
        toolbar.AddChild(new Label { Text = "Filter:" });
        _filterByTagOption = new OptionButton();
        _filterByTagOption.AddItem("All Tags", 0);
        _filterByTagOption.ItemSelected += OnFilterChanged;
        toolbar.AddChild(_filterByTagOption);

        // Auto-refresh toggle
        _autoRefreshToggle = new CheckButton
        {
            Text = "Auto",
            ButtonPressed = true,
            TooltipText = "Auto-refresh when files change"
        };
        toolbar.AddChild(_autoRefreshToggle);

        // Refresh button
        _refreshButton = new Button
        {
            Text = "",
            TooltipText = LocalizationManager.Tr(Strings.TodoTagsRefresh)
        };
        _refreshButton.Icon = GetThemeIcon("Reload", "EditorIcons");
        _refreshButton.Pressed += OnRefreshPressed;
        toolbar.AddChild(_refreshButton);

        // Settings button
        _settingsButton = new Button
        {
            Text = "",
            TooltipText = LocalizationManager.Tr(Strings.MenuSettings)
        };
        _settingsButton.Icon = GetThemeIcon("Tools", "EditorIcons");
        _settingsButton.Pressed += OnSettingsPressed;
        toolbar.AddChild(_settingsButton);

        // Separator
        mainVBox.AddChild(new HSeparator());

        // Results tree
        _resultsTree = new Tree
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HideRoot = true,
            SelectMode = Tree.SelectModeEnum.Single,
            Columns = 4
        };
        _resultsTree.SetColumnTitle(0, "Tag");
        _resultsTree.SetColumnTitle(1, "Description");
        _resultsTree.SetColumnTitle(2, "File");
        _resultsTree.SetColumnTitle(3, "Line");
        _resultsTree.SetColumnExpand(0, false);
        _resultsTree.SetColumnExpand(1, true);
        _resultsTree.SetColumnExpand(2, false);
        _resultsTree.SetColumnExpand(3, false);
        _resultsTree.SetColumnCustomMinimumWidth(0, 60);
        _resultsTree.SetColumnCustomMinimumWidth(2, 150);
        _resultsTree.SetColumnCustomMinimumWidth(3, 50);
        _resultsTree.ItemActivated += OnItemActivated;
        mainVBox.AddChild(_resultsTree);

        // Status bar
        _statusLabel = new Label
        {
            Text = "Ready",
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        mainVBox.AddChild(_statusLabel);

        // Minimum size
        CustomMinimumSize = new Vector2(400, 200);
    }

    private void LoadTagColors(TodoTagsConfig config)
    {
        _tagColors.Clear();
        foreach (var tag in config.Tags)
        {
            if (TryParseColor(tag.Color, out var color))
            {
                _tagColors[tag.Name.ToUpperInvariant()] = color;
            }
        }
    }

    private bool TryParseColor(string hex, out Color color)
    {
        color = Colors.White;
        try
        {
            if (string.IsNullOrEmpty(hex))
                return false;

            // Remove # if present
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

    private void PopulateFilterDropdown(TodoTagsConfig config)
    {
        _filterByTagOption.Clear();
        _filterByTagOption.AddItem("All Tags", 0);

        int index = 1;
        foreach (var tag in config.Tags.Where(t => t.Enabled))
        {
            _filterByTagOption.AddItem(tag.Name, index++);
        }
    }

    private void OnScanCompleted(TodoTagsScanResult result)
    {
        _currentResult = result;
        Callable.From(RefreshDisplay).CallDeferred();
        Callable.From(UpdateStatus).CallDeferred();
    }

    private void OnFileScanned(string filePath, List<TodoItem> items)
    {
        if (!_autoRefreshToggle.ButtonPressed)
            return;

        // Trigger a full rescan for simplicity
        _ = _scanner?.ScanProjectAsync();
    }

    private void RefreshDisplay()
    {
        _resultsTree.Clear();

        if (_currentResult == null || _currentResult.Items.Count == 0)
        {
            _headerLabel.Text = LocalizationManager.Tr(Strings.TodoTagsTitle);
            return;
        }

        var filteredItems = _filterTag == null
            ? _currentResult.Items
            : _currentResult.Items.Where(i =>
                i.Tag.Equals(_filterTag, StringComparison.OrdinalIgnoreCase)).ToList();

        _headerLabel.Text = $"{LocalizationManager.Tr(Strings.TodoTagsTitle)} ({filteredItems.Count})";

        var root = _resultsTree.CreateItem();

        if (_groupingMode == TodoGroupingMode.ByFile)
        {
            DisplayGroupedByFile(root, filteredItems);
        }
        else
        {
            DisplayGroupedByTag(root, filteredItems);
        }
    }

    private void DisplayGroupedByFile(TreeItem root, List<TodoItem> items)
    {
        var byFile = items
            .GroupBy(i => i.FilePath)
            .OrderBy(g => g.Key);

        foreach (var fileGroup in byFile)
        {
            var fileName = System.IO.Path.GetFileName(fileGroup.Key);
            var fileItem = _resultsTree.CreateItem(root);
            fileItem.SetText(0, "");
            fileItem.SetText(1, $"{fileName} ({fileGroup.Count()})");
            fileItem.SetText(2, "");
            fileItem.SetText(3, "");
            fileItem.SetIcon(0, GetThemeIcon("GDScript", "EditorIcons"));
            fileItem.Collapsed = false;

            foreach (var item in fileGroup.OrderBy(i => i.Line))
            {
                CreateItemRow(fileItem, item);
            }
        }
    }

    private void DisplayGroupedByTag(TreeItem root, List<TodoItem> items)
    {
        var byTag = items
            .GroupBy(i => i.Tag.ToUpperInvariant())
            .OrderBy(g => g.Key);

        foreach (var tagGroup in byTag)
        {
            var tagItem = _resultsTree.CreateItem(root);
            tagItem.SetText(0, tagGroup.Key);
            tagItem.SetText(1, $"({tagGroup.Count()})");
            tagItem.SetText(2, "");
            tagItem.SetText(3, "");

            if (_tagColors.TryGetValue(tagGroup.Key, out var color))
            {
                tagItem.SetCustomColor(0, color);
            }

            tagItem.Collapsed = false;

            foreach (var item in tagGroup.OrderBy(i => i.FilePath).ThenBy(i => i.Line))
            {
                CreateItemRow(tagItem, item);
            }
        }
    }

    private void CreateItemRow(TreeItem parent, TodoItem item)
    {
        var row = _resultsTree.CreateItem(parent);
        row.SetText(0, item.Tag);
        row.SetText(1, item.Description);
        row.SetText(2, System.IO.Path.GetFileName(item.FilePath));
        row.SetText(3, (item.Line + 1).ToString());

        // Store item for navigation (use ResourcePath for editor navigation)
        row.SetMetadata(0, item.ResourcePath);
        row.SetMetadata(1, item.Line);
        row.SetMetadata(2, item.Column);

        if (_tagColors.TryGetValue(item.Tag.ToUpperInvariant(), out var color))
        {
            row.SetCustomColor(0, color);
        }

        // Set icon based on priority
        var iconName = item.Priority switch
        {
            TodoPriority.High => "StatusError",
            TodoPriority.Normal => "StatusWarning",
            _ => "StatusSuccess"
        };
        row.SetIcon(0, GetThemeIcon(iconName, "EditorIcons"));
    }

    private void OnItemActivated()
    {
        var selected = _resultsTree.GetSelected();
        if (selected == null)
            return;

        var resourcePath = selected.GetMetadata(0);
        var line = selected.GetMetadata(1);
        var column = selected.GetMetadata(2);

        if (resourcePath.VariantType == Variant.Type.String &&
            line.VariantType == Variant.Type.Int &&
            column.VariantType == Variant.Type.Int)
        {
            NavigateToItem?.Invoke(resourcePath.AsString(), (int)line, (int)column);
        }
    }

    private void OnGroupByChanged(long index)
    {
        _groupingMode = (TodoGroupingMode)index;
        RefreshDisplay();
    }

    private void OnFilterChanged(long index)
    {
        if (index == 0)
        {
            _filterTag = null;
        }
        else
        {
            _filterTag = _filterByTagOption.GetItemText((int)index);
        }
        RefreshDisplay();
    }

    private async void OnRefreshPressed()
    {
        _statusLabel.Text = "Scanning...";
        _refreshButton.Disabled = true;

        try
        {
            if (_scanner != null)
            {
                await _scanner.ScanProjectAsync();
            }
        }
        finally
        {
            _refreshButton.Disabled = false;
        }
    }

    private void OnSettingsPressed()
    {
        OpenSettingsRequested?.Invoke();
    }

    private void UpdateStatus()
    {
        if (_currentResult == null)
        {
            _statusLabel.Text = "Ready";
            return;
        }

        var tagCounts = string.Join(", ", _currentResult.TagCounts
            .Select(kvp => $"{kvp.Key}: {kvp.Value}"));

        _statusLabel.Text = $"Found {_currentResult.TotalCount} items ({tagCounts})";
    }

    private Texture2D? GetThemeIcon(string name, string type)
    {
        try
        {
            return EditorInterface.Singleton.GetBaseControl().GetThemeIcon(name, type);
        }
        catch
        {
            return null;
        }
    }
}
