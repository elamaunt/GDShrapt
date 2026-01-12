using System;
using System.Collections.Generic;
using Godot;

namespace GDShrapt.Plugin;

/// <summary>
/// REPL (Read-Eval-Print-Loop) dock for executing GDScript expressions
/// on nodes in the current scene.
/// </summary>
internal partial class ReplDock : Control
{
    private GDShraptPlugin _plugin;

    // UI controls
    private OptionButton _nodeSelector;
    private LineEdit _searchField;
    private Button _refreshButton;
    private LineEdit _inputField;
    private Button _executeButton;
    private Button _clearButton;
    private RichTextLabel _outputArea;
    private Label _statusLabel;

    // State
    private readonly ReplExecutor _executor = new();
    private readonly ReplHistory _history = new();
    private Node _selectedNode;
    private bool _disclaimerShown = false;
    private List<(string path, Node node)> _allNodes = new();
    private string _currentFilter = "";

    public override void _Ready()
    {
        Logger.Info("ReplDock._Ready() called");
        CreateUI();
    }

    public void Initialize(GDShraptPlugin plugin)
    {
        Logger.Info("ReplDock.Initialize() called");
        _plugin = plugin;

        // Ensure UI is created (since _Ready may not be called)
        if (_nodeSelector == null)
            CreateUI();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        // Show disclaimer when dock becomes visible for the first time
        if (what == NotificationVisibilityChanged && Visible && !_disclaimerShown)
        {
            ShowDisclaimer();
            PopulateNodeSelector();
        }
    }

    private void CreateUI()
    {
        // Prevent double creation
        if (_nodeSelector != null)
            return;

        Logger.Info($"ReplDock.CreateUI() called, GetChildCount={GetChildCount()}");

        var mainVBox = new VBoxContainer();
        mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        mainVBox.AddThemeConstantOverride("separation", 4);
        AddChild(mainVBox);

        // Toolbar
        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 6);
        mainVBox.AddChild(toolbar);

        // Node selector label
        toolbar.AddChild(new Label { Text = "Node:" });

        // Search field for filtering nodes
        _searchField = new LineEdit
        {
            PlaceholderText = "Filter nodes...",
            CustomMinimumSize = new Vector2(120, 0)
        };
        _searchField.TextChanged += OnSearchTextChanged;
        toolbar.AddChild(_searchField);

        // Node selector dropdown
        _nodeSelector = new OptionButton
        {
            CustomMinimumSize = new Vector2(200, 0),
            TooltipText = "Select a node from the current scene"
        };
        _nodeSelector.ItemSelected += OnNodeSelected;
        toolbar.AddChild(_nodeSelector);

        // Refresh button
        _refreshButton = new Button
        {
            Text = "",
            TooltipText = "Refresh node list"
        };
        try
        {
            _refreshButton.Icon = EditorInterface.Singleton.GetBaseControl().GetThemeIcon("Reload", "EditorIcons");
        }
        catch { }
        _refreshButton.Pressed += OnRefreshPressed;
        toolbar.AddChild(_refreshButton);

        // Spacer
        toolbar.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Status label
        _statusLabel = new Label { Text = "No scene loaded" };
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        toolbar.AddChild(_statusLabel);

        // Separator
        mainVBox.AddChild(new HSeparator());

        // Input row
        var inputRow = new HBoxContainer();
        inputRow.AddThemeConstantOverride("separation", 6);
        mainVBox.AddChild(inputRow);

        // Prompt label
        inputRow.AddChild(new Label { Text = ">" });

        // Input field
        _inputField = new LineEdit
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = "Enter GDScript expression..."
        };
        _inputField.TextSubmitted += OnInputSubmitted;
        _inputField.GuiInput += OnInputGuiInput;
        inputRow.AddChild(_inputField);

        // Execute button
        _executeButton = new Button
        {
            Text = "Execute",
            TooltipText = "Execute expression (Enter)"
        };
        _executeButton.Pressed += OnExecutePressed;
        inputRow.AddChild(_executeButton);

        // Clear button
        _clearButton = new Button
        {
            Text = "Clear",
            TooltipText = "Clear output"
        };
        _clearButton.Pressed += OnClearPressed;
        inputRow.AddChild(_clearButton);

        // Separator
        mainVBox.AddChild(new HSeparator());

        // Output area
        var outputScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        mainVBox.AddChild(outputScroll);

        _outputArea = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SelectionEnabled = true
        };
        _outputArea.AddThemeColorOverride("default_color", new Color(0.9f, 0.9f, 0.9f));
        outputScroll.AddChild(_outputArea);

        CustomMinimumSize = new Vector2(300, 200);
    }

    private void ShowDisclaimer()
    {
        _disclaimerShown = true;

        var dialog = new AcceptDialog
        {
            Title = "Experimental Feature",
            DialogText = @"REPL (Read-Eval-Print-Loop) is an EXPERIMENTAL feature.

- Expressions are executed directly on scene nodes
- This can modify game state and cause unexpected behavior
- Use at your own risk
- Not recommended for production scenes

Proceed with caution!",
            OkButtonText = "I Understand"
        };

        GetTree().Root.AddChild(dialog);
        dialog.PopupCentered();
    }

    private void PopulateNodeSelector()
    {
        _nodeSelector.Clear();
        _allNodes.Clear();
        _nodeSelector.AddItem("-- Select Node --", 0);

        var sceneRoot = EditorInterface.Singleton?.GetEditedSceneRoot();
        if (sceneRoot == null)
        {
            _statusLabel.Text = "No scene loaded";
            return;
        }

        CollectNodes(sceneRoot, "", _allNodes);
        ApplyFilter();
    }

    private void CollectNodes(Node node, string parentPath, List<(string path, Node node)> result)
    {
        var nodeName = node.Name.ToString();
        var currentPath = string.IsNullOrEmpty(parentPath)
            ? nodeName
            : $"{parentPath}/{nodeName}";

        result.Add((currentPath, node));

        foreach (var child in node.GetChildren())
        {
            CollectNodes(child, currentPath, result);
        }
    }

    private void ApplyFilter()
    {
        _nodeSelector.Clear();
        _nodeSelector.AddItem("-- Select Node --", 0);

        var filter = _currentFilter.ToLowerInvariant();
        int addedCount = 0;
        int index = 1;

        foreach (var (path, node) in _allNodes)
        {
            // Apply filter
            if (!string.IsNullOrEmpty(filter))
            {
                if (!path.ToLowerInvariant().Contains(filter) &&
                    !node.GetClass().ToLowerInvariant().Contains(filter))
                {
                    continue;
                }
            }

            var displayName = $"{path} ({node.GetClass()})";
            _nodeSelector.AddItem(displayName, index);
            _nodeSelector.SetItemMetadata(index, path);
            index++;
            addedCount++;
        }

        _statusLabel.Text = $"{addedCount}/{_allNodes.Count} nodes";
    }

    private void OnSearchTextChanged(string newText)
    {
        _currentFilter = newText;
        ApplyFilter();
    }

    private void OnNodeSelected(long index)
    {
        if (index == 0)
        {
            _selectedNode = null;
            _statusLabel.Text = $"{_allNodes.Count} nodes";
            return;
        }

        var path = _nodeSelector.GetItemMetadata((int)index).AsString();
        if (string.IsNullOrEmpty(path))
            return;

        var sceneRoot = EditorInterface.Singleton?.GetEditedSceneRoot();
        if (sceneRoot == null)
            return;

        // Find the node by path
        foreach (var (nodePath, node) in _allNodes)
        {
            if (nodePath == path)
            {
                _selectedNode = node;
                _statusLabel.Text = $"Selected: {node.Name} ({node.GetClass()})";
                break;
            }
        }
    }

    private void OnRefreshPressed()
    {
        PopulateNodeSelector();
    }

    private void OnExecutePressed()
    {
        ExecuteCurrentInput();
    }

    private void OnInputSubmitted(string text)
    {
        ExecuteCurrentInput();
    }

    private void OnInputGuiInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            switch (keyEvent.Keycode)
            {
                case Key.Up:
                    // Navigate to previous command
                    var prev = _history.GetPrevious();
                    if (prev != null)
                    {
                        _inputField.Text = prev;
                        _inputField.CaretColumn = prev.Length;
                    }
                    GetViewport().SetInputAsHandled();
                    break;

                case Key.Down:
                    // Navigate to next command
                    var next = _history.GetNext();
                    _inputField.Text = next ?? "";
                    _inputField.CaretColumn = _inputField.Text.Length;
                    GetViewport().SetInputAsHandled();
                    break;
            }
        }
    }

    private void ExecuteCurrentInput()
    {
        var input = _inputField.Text.Trim();
        if (string.IsNullOrEmpty(input))
            return;

        // Add to history
        _history.Add(input);
        _history.ResetNavigation();

        // Check if node is selected
        if (_selectedNode == null || !GodotObject.IsInstanceValid(_selectedNode))
        {
            AppendOutput(input, ReplResult.Error("No valid node selected"));
            _inputField.Clear();
            return;
        }

        // Execute
        var result = _executor.Execute(input, _selectedNode);
        AppendOutput(input, result);

        // Clear input
        _inputField.Clear();
    }

    private void AppendOutput(string input, ReplResult result)
    {
        var text = _outputArea.Text;
        if (!string.IsNullOrEmpty(text))
            text += "\n";

        // Input line (colored)
        text += $"[color=#88aaff]> {EscapeBBCode(input)}[/color]\n";

        // Result
        if (result.Success)
        {
            var output = result.FormatOutput();
            text += $"[color=#aaffaa]{EscapeBBCode(output)}[/color]";
        }
        else
        {
            text += $"[color=#ff8888]{EscapeBBCode(result.FormatOutput())}[/color]";
        }

        _outputArea.Text = text;

        // Scroll to bottom
        Callable.From(() =>
        {
            var scroll = _outputArea.GetParent<ScrollContainer>();
            if (scroll != null)
            {
                scroll.ScrollVertical = (int)scroll.GetVScrollBar().MaxValue;
            }
        }).CallDeferred();
    }

    private void OnClearPressed()
    {
        _outputArea.Text = "";
    }

    private static string EscapeBBCode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        return text.Replace("[", "[lb]").Replace("]", "[rb]");
    }
}
