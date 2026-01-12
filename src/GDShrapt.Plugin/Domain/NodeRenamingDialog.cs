using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Dialog for renaming node paths.
/// Shows affected files grouped by type (scene files vs GDScript files).
/// </summary>
internal partial class NodeRenamingDialog : Window
{
    // UI Components
    private VBoxContainer _mainLayout;
    private Label _headerLabel;
    private HSeparator _headerSeparator;
    private HBoxContainer _nameRow;
    private Label _nameLabel;
    private LineEdit _nameEdit;
    private Label _warningLabel;
    private HSeparator _referencesSeparator;
    private HBoxContainer _referencesHeader;
    private Label _referencesLabel;
    private HBoxContainer _selectButtonsRow;
    private Button _selectAllButton;
    private Button _deselectAllButton;
    private ScrollContainer _scrollContainer;
    private VBoxContainer _referencesList;
    private HSeparator _buttonsSeparator;
    private HBoxContainer _buttonsLayout;
    private Control _buttonsSpacer;
    private Button _cancelButton;
    private Button _okButton;

    private TaskCompletionSource<NodeRenamingParameters?> _showCompletion;
    private List<GDNodePathReference> _references = new();
    private readonly List<NodeReferenceCell> _referenceCells = new();

    // Constants
    private const int DialogWidth = 550;
    private const int MaxDialogHeight = 550;
    private const int MinDialogHeight = 300;

    public NodeRenamingParameters Parameters => new()
    {
        NewName = _nameEdit.Text,
        SelectedReferences = GetSelectedReferences().ToList()
    };

    public NodeRenamingDialog()
    {
        Title = "Rename Node";
        Exclusive = true;
        Transient = true;
        WrapControls = true;
        Unresizable = false;

        CreateUI();
        ConnectSignals();
    }

    private void CreateUI()
    {
        // Main container with padding
        _mainLayout = new VBoxContainer();
        _mainLayout.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _mainLayout.AddThemeConstantOverride("separation", 8);
        AddChild(_mainLayout);

        // Add margin container for padding
        var marginContainer = new MarginContainer();
        marginContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        marginContainer.AddThemeConstantOverride("margin_left", 16);
        marginContainer.AddThemeConstantOverride("margin_right", 16);
        marginContainer.AddThemeConstantOverride("margin_top", 16);
        marginContainer.AddThemeConstantOverride("margin_bottom", 16);

        var innerLayout = new VBoxContainer();
        innerLayout.AddThemeConstantOverride("separation", 8);
        marginContainer.AddChild(innerLayout);
        _mainLayout.AddChild(marginContainer);

        // Header
        _headerLabel = new Label
        {
            Text = "Rename Node",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _headerLabel.AddThemeFontSizeOverride("font_size", 16);
        innerLayout.AddChild(_headerLabel);

        // Header separator
        _headerSeparator = new HSeparator();
        innerLayout.AddChild(_headerSeparator);

        // Name input row
        _nameRow = new HBoxContainer();
        _nameRow.AddThemeConstantOverride("separation", 8);
        innerLayout.AddChild(_nameRow);

        _nameLabel = new Label
        {
            Text = "New name:",
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
        };
        _nameRow.AddChild(_nameLabel);

        _nameEdit = new LineEdit
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            PlaceholderText = "new_name"
        };
        _nameRow.AddChild(_nameEdit);

        // Warning label (initially hidden)
        _warningLabel = new Label
        {
            Text = "",
            Visible = false
        };
        _warningLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.6f, 0.2f));
        innerLayout.AddChild(_warningLabel);

        // References separator
        _referencesSeparator = new HSeparator();
        innerLayout.AddChild(_referencesSeparator);

        // References header with select buttons
        _referencesHeader = new HBoxContainer();
        _referencesHeader.AddThemeConstantOverride("separation", 8);
        innerLayout.AddChild(_referencesHeader);

        _referencesLabel = new Label
        {
            Text = "Affected files (0):",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _referencesHeader.AddChild(_referencesLabel);

        _selectButtonsRow = new HBoxContainer();
        _selectButtonsRow.AddThemeConstantOverride("separation", 4);
        _referencesHeader.AddChild(_selectButtonsRow);

        _selectAllButton = new Button
        {
            Text = "Select All",
            CustomMinimumSize = new Vector2(70, 0)
        };
        _selectButtonsRow.AddChild(_selectAllButton);

        _deselectAllButton = new Button
        {
            Text = "Deselect",
            CustomMinimumSize = new Vector2(70, 0)
        };
        _selectButtonsRow.AddChild(_deselectAllButton);

        // Scroll container for references
        _scrollContainer = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 150)
        };
        innerLayout.AddChild(_scrollContainer);

        _referencesList = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _referencesList.AddThemeConstantOverride("separation", 2);
        _scrollContainer.AddChild(_referencesList);

        // Buttons separator
        _buttonsSeparator = new HSeparator();
        innerLayout.AddChild(_buttonsSeparator);

        // Buttons row
        _buttonsLayout = new HBoxContainer();
        _buttonsLayout.AddThemeConstantOverride("separation", 8);
        innerLayout.AddChild(_buttonsLayout);

        // Spacer to push buttons to the right
        _buttonsSpacer = new Control
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _buttonsLayout.AddChild(_buttonsSpacer);

        _cancelButton = new Button
        {
            Text = LocalizationManager.Tr(Strings.DialogCancel),
            CustomMinimumSize = new Vector2(80, 0)
        };
        _buttonsLayout.AddChild(_cancelButton);

        _okButton = new Button
        {
            Text = "Rename",
            CustomMinimumSize = new Vector2(80, 0)
        };
        _buttonsLayout.AddChild(_okButton);

        // Apply initial size
        Size = new Vector2I(DialogWidth, MinDialogHeight);
    }

    private void ConnectSignals()
    {
        _nameEdit.TextSubmitted += OnTextSubmitted;
        _okButton.Pressed += OnRename;
        _cancelButton.Pressed += OnCancelled;
        _selectAllButton.Pressed += OnSelectAll;
        _deselectAllButton.Pressed += OnDeselectAll;
        CloseRequested += OnCancelled;
    }

    private void OnTextSubmitted(string newText)
    {
        OnRename();
    }

    public void SetCurrentName(string name)
    {
        _nameEdit.Text = name;
        _headerLabel.Text = $"Rename node \"{name}\"";
    }

    public void SetWarning(string warning)
    {
        if (string.IsNullOrEmpty(warning))
        {
            _warningLabel.Visible = false;
        }
        else
        {
            _warningLabel.Text = warning;
            _warningLabel.Visible = true;
        }
    }

    public void OnCancelled()
    {
        _showCompletion?.TrySetResult(null);
        Hide();
    }

    public void OnRename()
    {
        _showCompletion?.TrySetResult(Parameters);
        Hide();
    }

    private void OnSelectAll()
    {
        foreach (var cell in _referenceCells)
        {
            cell.SetSelected(true);
        }
    }

    private void OnDeselectAll()
    {
        foreach (var cell in _referenceCells)
        {
            cell.SetSelected(false);
        }
    }

    public Task<NodeRenamingParameters?> ShowForResult(string nodeName, IEnumerable<GDNodePathReference> references)
    {
        _showCompletion = new TaskCompletionSource<NodeRenamingParameters?>();

        SetCurrentName(nodeName);
        SetReferencesList(references);

        // Center on screen
        var screenSize = DisplayServer.ScreenGetSize();
        Position = new Vector2I(
            (screenSize.X - Size.X) / 2,
            (screenSize.Y - Size.Y) / 2
        );

        Popup();

        _nameEdit.SelectAll();
        _nameEdit.GrabFocus();
        _nameEdit.CaretColumn = _nameEdit.Text?.Length ?? 0;

        return _showCompletion.Task;
    }

    public void SetReferencesList(IEnumerable<GDNodePathReference> references)
    {
        _references = references.ToList();
        _referenceCells.Clear();

        // Clear existing children
        foreach (var child in _referencesList.GetChildren())
        {
            if (child is Node node)
                node.QueueFree();
        }

        // Group references by type
        var sceneRefs = _references.Where(r =>
            r.Type == GDNodePathReference.RefType.SceneNodeName ||
            r.Type == GDNodePathReference.RefType.SceneParentPath).ToList();
        var scriptRefs = _references.Where(r =>
            r.Type == GDNodePathReference.RefType.GDScript).ToList();

        int totalCount = sceneRefs.Count + scriptRefs.Count;
        _referencesLabel.Text = $"Affected files ({totalCount}):";

        // Add scene files section
        if (sceneRefs.Any())
        {
            AddSectionHeader("Scene Files", sceneRefs.Count);
            foreach (var reference in sceneRefs)
            {
                var cell = new NodeReferenceCell(reference);
                _referenceCells.Add(cell);
                _referencesList.AddChild(cell);
            }
        }

        // Add GDScript files section
        if (scriptRefs.Any())
        {
            AddSectionHeader("GDScript Files", scriptRefs.Count);
            foreach (var reference in scriptRefs)
            {
                var cell = new NodeReferenceCell(reference);
                _referenceCells.Add(cell);
                _referencesList.AddChild(cell);
            }
        }

        // Adjust dialog size based on reference count
        UpdateDialogSize();
    }

    private void AddSectionHeader(string title, int count)
    {
        var header = new Label
        {
            Text = $"── {title} ({count}) ──",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        header.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        header.AddThemeFontSizeOverride("font_size", 12);
        _referencesList.AddChild(header);
    }

    private void UpdateDialogSize()
    {
        int referenceHeight = Math.Clamp(_referenceCells.Count * 28 + 50, 80, 250);
        int totalHeight = Math.Min(MinDialogHeight + referenceHeight, MaxDialogHeight);
        Size = new Vector2I(DialogWidth, totalHeight);
    }

    public IEnumerable<GDNodePathReference> GetSelectedReferences()
    {
        return _referenceCells
            .Where(c => c.IsSelected)
            .Select(c => c.Reference);
    }

    /// <summary>
    /// Cell representing a single node path reference in the list.
    /// </summary>
    private partial class NodeReferenceCell : HBoxContainer
    {
        private readonly CheckBox _checkBox;
        private readonly TextureRect _iconRect;
        private readonly Label _fileLabel;
        private readonly Label _lineLabel;
        private readonly Label _contextLabel;

        public GDNodePathReference Reference { get; }
        public bool IsSelected => _checkBox.ButtonPressed;

        public NodeReferenceCell(GDNodePathReference reference)
        {
            Reference = reference;

            AddThemeConstantOverride("separation", 4);
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            // Checkbox
            _checkBox = new CheckBox
            {
                ButtonPressed = true, // Selected by default
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
            };
            AddChild(_checkBox);

            // Icon
            _iconRect = new TextureRect
            {
                CustomMinimumSize = new Vector2(16, 16),
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            };
            SetIconForReference(reference);
            AddChild(_iconRect);

            // File name
            _fileLabel = new Label
            {
                Text = reference.DisplayName ?? "Unknown",
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                CustomMinimumSize = new Vector2(100, 0),
                ClipText = true
            };
            _fileLabel.AddThemeColorOverride("font_color", GetColorForType(reference.Type));
            AddChild(_fileLabel);

            // Line info
            _lineLabel = new Label
            {
                Text = $"Line {reference.LineNumber}",
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                CustomMinimumSize = new Vector2(60, 0)
            };
            _lineLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
            AddChild(_lineLabel);

            // Context (truncated)
            var contextText = reference.DisplayContext ?? "";
            if (contextText.Length > 40)
                contextText = contextText.Substring(0, 37) + "...";

            _contextLabel = new Label
            {
                Text = contextText,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                ClipText = true
            };
            _contextLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            _contextLabel.AddThemeFontSizeOverride("font_size", 11);
            AddChild(_contextLabel);
        }

        private void SetIconForReference(GDNodePathReference reference)
        {
            try
            {
                string iconName = reference.Type switch
                {
                    GDNodePathReference.RefType.SceneNodeName => "PackedScene",
                    GDNodePathReference.RefType.SceneParentPath => "NodePath",
                    GDNodePathReference.RefType.GDScript => "Script",
                    _ => "Script"
                };

                var icon = EditorInterface.Singleton.GetBaseControl().GetThemeIcon(iconName, "EditorIcons");
                _iconRect.Texture = icon;
            }
            catch
            {
                // Icon not found, leave empty
            }
        }

        private Color GetColorForType(GDNodePathReference.RefType type)
        {
            return type switch
            {
                GDNodePathReference.RefType.SceneNodeName => new Color(0.9f, 0.7f, 0.5f),
                GDNodePathReference.RefType.SceneParentPath => new Color(0.8f, 0.6f, 0.4f),
                GDNodePathReference.RefType.GDScript => new Color(0.7f, 0.85f, 1.0f),
                _ => new Color(0.8f, 0.8f, 0.8f)
            };
        }

        public void SetSelected(bool selected)
        {
            _checkBox.ButtonPressed = selected;
        }
    }
}

/// <summary>
/// Parameters returned from the node renaming dialog.
/// </summary>
internal class NodeRenamingParameters
{
    public string NewName { get; set; }
    public List<GDNodePathReference> SelectedReferences { get; set; } = new();
}
