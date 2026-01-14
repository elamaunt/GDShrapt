using GDShrapt.Reader;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Modern redesigned renaming dialog with improved UI/UX.
/// Features: scroll container, reference icons, select all/deselect buttons.
/// </summary>
internal partial class RenamingDialog : Window
{
    // UI Components
    private VBoxContainer _mainLayout;
    private Label _headerLabel;
    private HSeparator _headerSeparator;
    private HBoxContainer _nameRow;
    private Label _nameLabel;
    private LineEdit _nameEdit;
    private CheckButton _renameOnlyStrongTypedCheck;
    private HSeparator _referencesSeparator;
    private HBoxContainer _referencesHeader;
    private Label _referencesLabel;
    private HBoxContainer _selectButtonsRow;
    private Button _selectAllButton;
    private Button _deselectAllButton;
    private StyledReferencesTree _referencesTree;
    private HSeparator _buttonsSeparator;
    private HBoxContainer _buttonsLayout;
    private Control _buttonsSpacer;
    private Button _cancelButton;
    private Button _okButton;

    private TaskCompletionSource<RenamingParameters?> _showCompletion;
    private LinkedList<GDMemberReference>? _references;
    private readonly List<ReferenceItem> _referenceItems = new();
    private readonly Dictionary<TreeItem, ReferenceItem> _treeItemToReference = new();

    // Constants
    private const int DialogWidth = 550;
    private const int MaxDialogHeight = 550;
    private const int MinDialogHeight = 300;

    /// <summary>
    /// Event fired when user wants to navigate to a reference.
    /// Parameters: file path, line, column.
    /// </summary>
    public event Action<string, int, int> NavigateToReference;

    public RenamingParameters Parameters => new RenamingParameters()
    {
        RenameOnlyStrongTyped = _renameOnlyStrongTypedCheck.ButtonPressed,
        NewName = _nameEdit.Text
    };

    public RenamingDialog()
    {
        Title = LocalizationManager.Tr(Strings.DialogRename);
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
            Text = LocalizationManager.Tr(Strings.DialogRename),
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
            Text = LocalizationManager.Tr(Strings.DialogRenamePrompt),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
        };
        _nameRow.AddChild(_nameLabel);

        _nameEdit = new LineEdit
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            PlaceholderText = "new_name"
        };
        _nameRow.AddChild(_nameEdit);

        // Strong typed checkbox
        _renameOnlyStrongTypedCheck = new CheckButton
        {
            Text = "Rename only strongly-typed references"
        };
        innerLayout.AddChild(_renameOnlyStrongTypedCheck);

        // References separator
        _referencesSeparator = new HSeparator();
        innerLayout.AddChild(_referencesSeparator);

        // References header with select buttons
        _referencesHeader = new HBoxContainer();
        _referencesHeader.AddThemeConstantOverride("separation", 8);
        innerLayout.AddChild(_referencesHeader);

        _referencesLabel = new Label
        {
            Text = "Affected references (0):",
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

        // Styled references tree with syntax highlighting and checkboxes
        _referencesTree = new StyledReferencesTree(checkBoxesEnabled: true)
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 150)
        };
        _referencesTree.ItemActivated += OnReferenceItemActivated;
        _referencesTree.ItemSelected += OnReferenceItemActivated;
        innerLayout.AddChild(_referencesTree);

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
            Text = LocalizationManager.Tr(Strings.MenuRename),
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
        _headerLabel.Text = $"Rename \"{name}\"";
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
        _referencesTree?.SelectAll();
    }

    private void OnDeselectAll()
    {
        _referencesTree?.DeselectAll();
    }

    private void OnReferenceItemActivated()
    {
        var selected = _referencesTree.GetSelected();
        if (selected == null)
            return;

        // Try to get ReferenceItem from tree's internal mapping first
        var refFromTree = _referencesTree.GetReferenceForItem(selected);
        if (refFromTree != null)
        {
            NavigateToReference?.Invoke(refFromTree.FilePath, refFromTree.Line, refFromTree.Column);
            return;
        }

        // Try to get from metadata
        var metadata = selected.GetMetadata(0);
        if (metadata.VariantType != Variant.Type.Object)
            return;

        // Check if it's a MetadataContainer
        var container = metadata.As<MetadataContainer>();
        if (container == null)
            return;

        // Try to get ReferenceItem from first metadata slot
        var firstMeta = container.GetMetadata(0);
        if (firstMeta.VariantType == Variant.Type.Object)
        {
            var obj = firstMeta.AsGodotObject();
            if (obj is ReferenceItem refItem)
            {
                NavigateToReference?.Invoke(refItem.FilePath, refItem.Line, refItem.Column);
                return;
            }
            // If it's HeaderLayoutData, this is a file header - skip to second metadata
        }

        // Could be a file header - try second metadata slot for file path
        var secondMeta = container.GetMetadata(1);
        if (secondMeta.VariantType == Variant.Type.String)
        {
            var filePath = secondMeta.AsString();
            if (!string.IsNullOrEmpty(filePath))
            {
                var firstRef = _referenceItems.FirstOrDefault(r => r.FilePath == filePath);
                if (firstRef != null)
                {
                    NavigateToReference?.Invoke(firstRef.FilePath, firstRef.Line, firstRef.Column);
                }
            }
        }
    }

    public Task<RenamingParameters?> ShowForResult()
    {
        _showCompletion = new TaskCompletionSource<RenamingParameters?>();

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

    public void SetReferencesList(LinkedList<GDMemberReference>? references)
    {
        _references = references;
        _referenceItems.Clear();
        _treeItemToReference.Clear();
        _referencesTree?.Clear();

        // Update label
        var count = references?.Count ?? 0;
        if (_referencesLabel != null)
        {
            _referencesLabel.Text = $"Affected references ({count}):";
        }

        if (references == null || references.Count == 0 || _referencesTree == null)
        {
            UpdateDialogSize();
            return;
        }

        // Convert GDMemberReference to ReferenceItem with highlight info
        foreach (var memberRef in references)
        {
            var refItem = ReferenceItem.FromGDMemberReference(memberRef);
            // Calculate highlight position
            CalculateHighlightPosition(refItem, memberRef.Identifier?.Sequence);
            _referenceItems.Add(refItem);
        }

        // Create tree structure
        var root = _referencesTree.CreateItem();

        // Group references by file
        var groupedByFile = _referenceItems
            .GroupBy(r => r.FilePath ?? "Unknown")
            .OrderBy(g => g.Key);

        foreach (var fileGroup in groupedByFile)
        {
            // Create file header
            var fileName = GetFileName(fileGroup.Key);
            var fileItem = _referencesTree.CreateFileItem(root, fileName, fileGroup.Count(), fileGroup.Key);

            // Create reference items with checkboxes
            foreach (var refItem in fileGroup.OrderBy(r => r.Line))
            {
                var treeItem = CreateReferenceItemWithCheckbox(fileItem, refItem);
                _treeItemToReference[treeItem] = refItem;
            }

            // Expand by default
            fileItem.Collapsed = false;
        }

        // Adjust dialog size based on reference count
        UpdateDialogSize();
    }

    private TreeItem CreateReferenceItemWithCheckbox(TreeItem parent, ReferenceItem reference)
    {
        // StyledReferencesTree already handles checkboxes when checkBoxesEnabled is true
        return _referencesTree.CreateReferenceItem(parent, reference);
    }

    private static void CalculateHighlightPosition(ReferenceItem refItem, string symbolName)
    {
        if (string.IsNullOrEmpty(symbolName) || string.IsNullOrEmpty(refItem.ContextLine))
            return;

        var idx = refItem.ContextLine.IndexOf(symbolName, StringComparison.Ordinal);
        if (idx >= 0)
        {
            refItem.HighlightStart = idx;
            refItem.HighlightEnd = idx + symbolName.Length;
        }
    }

    private static string GetFileName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "Unknown";

        var lastSlash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
        return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
    }

    private void UpdateDialogSize()
    {
        int referenceHeight = Math.Clamp(_referenceItems.Count * 28, 80, 250);
        int totalHeight = Math.Min(MinDialogHeight + referenceHeight, MaxDialogHeight);
        Size = new Vector2I(DialogWidth, totalHeight);
    }

    /// <summary>
    /// Gets the list of selected references for renaming.
    /// </summary>
    public IEnumerable<GDMemberReference> GetSelectedReferences()
    {
        if (_references == null)
            yield break;

        var selectedItems = new HashSet<ReferenceItem>();

        // Collect selected items from tree
        CollectSelectedItems(_referencesTree?.GetRoot(), selectedItems);

        // Match back to GDMemberReference
        foreach (var memberRef in _references)
        {
            var matchingItem = _referenceItems.FirstOrDefault(r =>
                r.Line == memberRef.Identifier?.StartLine &&
                r.Column == memberRef.Identifier?.StartColumn &&
                r.FilePath == memberRef.Script?.FullPath);

            if (matchingItem != null && selectedItems.Contains(matchingItem))
            {
                yield return memberRef;
            }
        }
    }

    private void CollectSelectedItems(TreeItem item, HashSet<ReferenceItem> selected)
    {
        if (item == null) return;

        // If this item is checked and has a reference
        if (item.IsEditable(0) && item.IsChecked(0))
        {
            // Try local mapping first
            if (_treeItemToReference.TryGetValue(item, out var refItem))
            {
                selected.Add(refItem);
            }
            // Try getting from StyledReferencesTree
            else
            {
                var treeRefItem = _referencesTree?.GetReferenceForItem(item);
                if (treeRefItem != null)
                {
                    selected.Add(treeRefItem);
                }
            }
        }

        // Process children
        var child = item.GetFirstChild();
        while (child != null)
        {
            CollectSelectedItems(child, selected);
            child = child.GetNext();
        }
    }

    // Legacy compatibility field for old code
    private readonly List<ReferenceCell> _referenceCells = new();

    /// <summary>
    /// Placeholder class for backwards compatibility.
    /// </summary>
    private class ReferenceCell
    {
        public GDMemberReference Reference { get; }
        public bool IsSelected { get; private set; }

        public ReferenceCell(GDMemberReference reference)
        {
            Reference = reference;
            IsSelected = true;
        }

        public void SetSelected(bool selected) => IsSelected = selected;
    }
}
