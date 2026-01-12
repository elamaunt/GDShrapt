using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Confidence mode for rename operations.
/// Determines which references are included in the rename.
/// </summary>
public enum GDConfidenceMode
{
    /// <summary>
    /// Only type-resolved references (always available in Base).
    /// </summary>
    Strict,

    /// <summary>
    /// Include duck-typed references (Pro required).
    /// </summary>
    Potential,

    /// <summary>
    /// Include heuristic name matches (Pro required).
    /// </summary>
    NameMatch
}

/// <summary>
/// Result from the rename preview dialog.
/// </summary>
public class RenamePreviewResult
{
    /// <summary>
    /// True if user clicked Apply.
    /// </summary>
    public bool ShouldApply { get; set; }

    /// <summary>
    /// True if user cancelled the dialog.
    /// </summary>
    public bool Cancelled { get; set; }

    /// <summary>
    /// The new name entered by user.
    /// </summary>
    public string NewName { get; set; }

    /// <summary>
    /// Selected confidence mode.
    /// </summary>
    public GDConfidenceMode SelectedConfidence { get; set; }
}

/// <summary>
/// Preview dialog for rename operations with confidence level selection.
/// Shows references grouped by confidence level (Strict/Potential/NameMatch).
/// </summary>
internal partial class RenamePreviewDialog : Window
{
    // UI Components
    private VBoxContainer _mainLayout;
    private Label _titleLabel;
    private HSeparator _titleSeparator;

    // Name input
    private HBoxContainer _nameRow;
    private Label _nameLabel;
    private LineEdit _nameEdit;

    // Confidence selection
    private HBoxContainer _confidenceRow;
    private Label _confidenceLabel;
    private OptionButton _confidenceOption;

    // References display
    private HSeparator _referencesSeparator;
    private HBoxContainer _referencesHeader;
    private Label _referencesLabel;
    private HBoxContainer _selectButtonsRow;
    private Button _selectAllButton;
    private Button _deselectAllButton;

    // Reference groups
    private TabContainer _referenceTabs;
    private StyledReferencesTree _strictTree;
    private StyledReferencesTree _potentialTree;
    private StyledReferencesTree _nameMatchTree;

    // Pro message
    private PanelContainer _proMessagePanel;
    private Label _proMessageLabel;

    // Buttons
    private HSeparator _buttonsSeparator;
    private HBoxContainer _buttonsLayout;
    private Control _buttonsSpacer;
    private Button _cancelButton;
    private Button _applyButton;

    private TaskCompletionSource<RenamePreviewResult> _completion;
    private bool _isProLicensed;
    private string _oldName;

    // Reference lists
    private List<ReferenceItem> _strictRefs = new();
    private List<ReferenceItem> _potentialRefs = new();
    private List<ReferenceItem> _nameMatchRefs = new();

    // Constants
    private const int DialogWidth = 700;
    private const int DialogHeight = 550;

    // Colors for confidence levels
    private static readonly Color StrictColor = new Color(0.4f, 0.8f, 0.4f, 0.3f);    // Green
    private static readonly Color PotentialColor = new Color(0.9f, 0.8f, 0.2f, 0.3f); // Yellow
    private static readonly Color NameMatchColor = new Color(0.9f, 0.4f, 0.4f, 0.3f); // Red

    public RenamePreviewDialog()
    {
        Title = "Rename Preview";
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
        var marginContainer = new MarginContainer();
        marginContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        marginContainer.AddThemeConstantOverride("margin_left", 16);
        marginContainer.AddThemeConstantOverride("margin_right", 16);
        marginContainer.AddThemeConstantOverride("margin_top", 16);
        marginContainer.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(marginContainer);

        _mainLayout = new VBoxContainer();
        _mainLayout.AddThemeConstantOverride("separation", 10);
        marginContainer.AddChild(_mainLayout);

        // Title
        _titleLabel = new Label
        {
            Text = "Rename Symbol",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        _mainLayout.AddChild(_titleLabel);

        // Title separator
        _titleSeparator = new HSeparator();
        _mainLayout.AddChild(_titleSeparator);

        // Name input row
        _nameRow = new HBoxContainer();
        _nameRow.AddThemeConstantOverride("separation", 8);
        _mainLayout.AddChild(_nameRow);

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

        // Confidence selection row
        _confidenceRow = new HBoxContainer();
        _confidenceRow.AddThemeConstantOverride("separation", 8);
        _mainLayout.AddChild(_confidenceRow);

        _confidenceLabel = new Label
        {
            Text = "Confidence level:",
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
        };
        _confidenceRow.AddChild(_confidenceLabel);

        _confidenceOption = new OptionButton
        {
            CustomMinimumSize = new Vector2(200, 0)
        };
        _confidenceOption.AddItem("Strict (type-resolved only)", (int)GDConfidenceMode.Strict);
        _confidenceOption.AddItem("Potential (include duck-typed)", (int)GDConfidenceMode.Potential);
        _confidenceOption.AddItem("Name Match (include heuristic)", (int)GDConfidenceMode.NameMatch);
        _confidenceOption.Selected = 0;
        _confidenceRow.AddChild(_confidenceOption);

        // References separator
        _referencesSeparator = new HSeparator();
        _mainLayout.AddChild(_referencesSeparator);

        // References header
        _referencesHeader = new HBoxContainer();
        _referencesHeader.AddThemeConstantOverride("separation", 8);
        _mainLayout.AddChild(_referencesHeader);

        _referencesLabel = new Label
        {
            Text = "References to rename:",
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

        // Reference tabs
        _referenceTabs = new TabContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 250)
        };
        _mainLayout.AddChild(_referenceTabs);

        // Strict references tab
        _strictTree = new StyledReferencesTree(checkBoxesEnabled: true)
        {
            Name = "Strict",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _strictTree.SetHighlightColor(StrictColor);
        _referenceTabs.AddChild(_strictTree);

        // Potential references tab
        _potentialTree = new StyledReferencesTree(checkBoxesEnabled: true)
        {
            Name = "Potential",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _potentialTree.SetHighlightColor(PotentialColor);
        _referenceTabs.AddChild(_potentialTree);

        // Name match references tab
        _nameMatchTree = new StyledReferencesTree(checkBoxesEnabled: true)
        {
            Name = "Name Match",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _nameMatchTree.SetHighlightColor(NameMatchColor);
        _referenceTabs.AddChild(_nameMatchTree);

        // Pro message panel (hidden by default)
        _proMessagePanel = new PanelContainer
        {
            Visible = false
        };
        var proMessageStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.3f, 0.2f, 0.1f, 0.8f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        _proMessagePanel.AddThemeStyleboxOverride("panel", proMessageStyle);
        _mainLayout.AddChild(_proMessagePanel);

        _proMessageLabel = new Label
        {
            Text = "GDShrapt Pro required for Potential/NameMatch confidence",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _proMessageLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.8f, 0.4f));
        _proMessagePanel.AddChild(_proMessageLabel);

        // Buttons separator
        _buttonsSeparator = new HSeparator();
        _mainLayout.AddChild(_buttonsSeparator);

        // Buttons row
        _buttonsLayout = new HBoxContainer();
        _buttonsLayout.AddThemeConstantOverride("separation", 8);
        _mainLayout.AddChild(_buttonsLayout);

        // Spacer to push buttons to the right
        _buttonsSpacer = new Control
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _buttonsLayout.AddChild(_buttonsSpacer);

        // Cancel button
        _cancelButton = new Button
        {
            Text = "Cancel",
            CustomMinimumSize = new Vector2(80, 0)
        };
        _buttonsLayout.AddChild(_cancelButton);

        // Apply button
        _applyButton = new Button
        {
            Text = "Rename",
            CustomMinimumSize = new Vector2(80, 0)
        };
        _buttonsLayout.AddChild(_applyButton);

        // Apply initial size
        Size = new Vector2I(DialogWidth, DialogHeight);
    }

    private void ConnectSignals()
    {
        _nameEdit.TextSubmitted += _ => OnApply();
        _confidenceOption.ItemSelected += OnConfidenceChanged;
        _selectAllButton.Pressed += OnSelectAll;
        _deselectAllButton.Pressed += OnDeselectAll;
        _cancelButton.Pressed += OnCancelled;
        _applyButton.Pressed += OnApply;
        CloseRequested += OnCancelled;
    }

    private void OnConfidenceChanged(long index)
    {
        var confidence = (GDConfidenceMode)_confidenceOption.GetItemId((int)index);
        UpdateApplyButtonState(confidence);
        UpdateTabVisibility(confidence);
    }

    private void UpdateApplyButtonState(GDConfidenceMode confidence)
    {
        // Strict is always available, Potential/NameMatch require Pro
        var canApply = confidence == GDConfidenceMode.Strict || _isProLicensed;
        _applyButton.Disabled = !canApply;

        // Show Pro message if needed
        if (!_isProLicensed && confidence != GDConfidenceMode.Strict)
        {
            _proMessagePanel.Visible = true;
        }
        else
        {
            _proMessagePanel.Visible = false;
        }
    }

    private void UpdateTabVisibility(GDConfidenceMode confidence)
    {
        // Enable/disable tabs based on confidence level
        // In Base, Potential and NameMatch tabs are visible but with warning
        _referenceTabs.SetTabDisabled(1, !_isProLicensed && confidence != GDConfidenceMode.Potential);
        _referenceTabs.SetTabDisabled(2, !_isProLicensed && confidence != GDConfidenceMode.NameMatch);
    }

    private void OnSelectAll()
    {
        var currentTab = _referenceTabs.CurrentTab;
        var tree = currentTab switch
        {
            0 => _strictTree,
            1 => _potentialTree,
            2 => _nameMatchTree,
            _ => _strictTree
        };
        tree?.SelectAll();
    }

    private void OnDeselectAll()
    {
        var currentTab = _referenceTabs.CurrentTab;
        var tree = currentTab switch
        {
            0 => _strictTree,
            1 => _potentialTree,
            2 => _nameMatchTree,
            _ => _strictTree
        };
        tree?.DeselectAll();
    }

    private void OnCancelled()
    {
        _completion?.TrySetResult(new RenamePreviewResult
        {
            ShouldApply = false,
            Cancelled = true,
            NewName = null,
            SelectedConfidence = GDConfidenceMode.Strict
        });
        Hide();
    }

    private void OnApply()
    {
        var confidence = (GDConfidenceMode)_confidenceOption.GetItemId(_confidenceOption.Selected);

        // Check if can apply
        if (confidence != GDConfidenceMode.Strict && !_isProLicensed)
        {
            Logger.Warning("RenamePreviewDialog: Cannot apply non-strict rename without Pro license");
            return;
        }

        var newName = _nameEdit.Text?.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            Logger.Warning("RenamePreviewDialog: New name is empty");
            return;
        }

        _completion?.TrySetResult(new RenamePreviewResult
        {
            ShouldApply = true,
            Cancelled = false,
            NewName = newName,
            SelectedConfidence = confidence
        });
        Hide();
    }

    /// <summary>
    /// Sets the references for each confidence level.
    /// </summary>
    public void SetReferences(
        IEnumerable<ReferenceItem> strictRefs,
        IEnumerable<ReferenceItem> potentialRefs,
        IEnumerable<ReferenceItem> nameMatchRefs)
    {
        _strictRefs = strictRefs?.ToList() ?? new List<ReferenceItem>();
        _potentialRefs = potentialRefs?.ToList() ?? new List<ReferenceItem>();
        _nameMatchRefs = nameMatchRefs?.ToList() ?? new List<ReferenceItem>();

        // Update counts in tab titles
        _referenceTabs.SetTabTitle(0, $"Strict ({_strictRefs.Count})");
        _referenceTabs.SetTabTitle(1, $"Potential ({_potentialRefs.Count})");
        _referenceTabs.SetTabTitle(2, $"Name Match ({_nameMatchRefs.Count})");

        // Populate trees
        PopulateTree(_strictTree, _strictRefs);
        PopulateTree(_potentialTree, _potentialRefs);
        PopulateTree(_nameMatchTree, _nameMatchRefs);

        // Update total count
        var total = _strictRefs.Count + _potentialRefs.Count + _nameMatchRefs.Count;
        _referencesLabel.Text = $"References to rename ({total} total):";
    }

    private void PopulateTree(StyledReferencesTree tree, List<ReferenceItem> references)
    {
        tree.Clear();

        if (references == null || references.Count == 0)
            return;

        var root = tree.CreateItem();

        // Group by file
        var grouped = references
            .GroupBy(r => r.FilePath ?? "Unknown")
            .OrderBy(g => g.Key);

        foreach (var fileGroup in grouped)
        {
            var fileName = GetFileName(fileGroup.Key);
            var fileItem = tree.CreateFileItem(root, fileName, fileGroup.Count(), fileGroup.Key);

            foreach (var refItem in fileGroup.OrderBy(r => r.Line))
            {
                // Calculate highlight position if not set
                if (refItem.HighlightStart < 0 && !string.IsNullOrEmpty(_oldName))
                {
                    var idx = refItem.ContextLine?.IndexOf(_oldName, StringComparison.Ordinal) ?? -1;
                    if (idx >= 0)
                    {
                        refItem.HighlightStart = idx;
                        refItem.HighlightEnd = idx + _oldName.Length;
                    }
                }

                tree.CreateReferenceItem(fileItem, refItem);
            }

            fileItem.Collapsed = false;
        }
    }

    private static string GetFileName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "Unknown";

        var lastSlash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
        return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
    }

    /// <summary>
    /// Shows the rename preview dialog.
    /// </summary>
    /// <param name="oldName">Current symbol name</param>
    /// <param name="newName">Suggested new name</param>
    /// <param name="isProLicensed">Whether Pro features are available</param>
    public void ShowPreview(string oldName, string newName, bool isProLicensed)
    {
        _oldName = oldName;
        _isProLicensed = isProLicensed;

        Title = $"Rename \"{oldName}\"";
        _titleLabel.Text = $"Rename \"{oldName}\"";
        _nameEdit.Text = newName ?? oldName;

        // Reset confidence to Strict
        _confidenceOption.Selected = 0;
        UpdateApplyButtonState(GDConfidenceMode.Strict);

        // Disable Pro-only options if not licensed
        if (!isProLicensed)
        {
            _confidenceOption.SetItemDisabled(1, false); // Keep enabled but show warning
            _confidenceOption.SetItemDisabled(2, false);
        }
    }

    /// <summary>
    /// Shows the dialog and waits for user action.
    /// </summary>
    public Task<RenamePreviewResult> GetResultAsync()
    {
        _completion = new TaskCompletionSource<RenamePreviewResult>();

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

        return _completion.Task;
    }

    /// <summary>
    /// Convenience method to show preview and get result in one call.
    /// </summary>
    public Task<RenamePreviewResult> ShowForResult(
        string oldName,
        string newName,
        bool isProLicensed,
        IEnumerable<ReferenceItem> strictRefs,
        IEnumerable<ReferenceItem> potentialRefs = null,
        IEnumerable<ReferenceItem> nameMatchRefs = null)
    {
        ShowPreview(oldName, newName, isProLicensed);
        SetReferences(strictRefs, potentialRefs, nameMatchRefs);
        return GetResultAsync();
    }
}
