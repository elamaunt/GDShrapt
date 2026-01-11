using Godot;
using System;
using System.Collections.Generic;

namespace GDShrapt.Plugin;

/// <summary>
/// Floating notification panel that shows diagnostic summary.
/// Can be expanded/collapsed, positioned in corner of editor.
/// </summary>
internal partial class NotificationPanel : Control
{
    // UI Elements
    private PanelContainer _container;
    private VBoxContainer _mainLayout;
    private HBoxContainer _headerRow;
    private Label _headerLabel;
    private Button _expandButton;
    private VBoxContainer _detailsContainer;
    private Label _errorsLabel;
    private Label _warningsLabel;
    private Label _hintsLabel;
    private HBoxContainer _buttonsRow;
    private Button _fixFormattingButton;
    private Button _showAllButton;

    // State
    private bool _isExpanded = false;
    private bool _hasBeenShownExpanded = false;
    private DiagnosticSummary _currentSummary = DiagnosticSummary.Empty;
    private readonly HashSet<string> _shownFiles = new();

    // Constants
    private const float AnimationDuration = 0.15f;
    private const int CollapsedWidth = 200;
    private const int ExpandedWidth = 280;
    private const int CollapsedHeight = 32;
    private const int ExpandedHeight = 140;
    private const int CornerOffset = 16;

    // Colors - lighter background for better visibility
    private static readonly Color ErrorColor = new(1.0f, 0.4f, 0.4f);
    private static readonly Color WarningColor = new(1.0f, 0.8f, 0.2f);
    private static readonly Color HintColor = new(0.6f, 0.8f, 1.0f);
    private static readonly Color BackgroundColor = new(0.22f, 0.22f, 0.25f, 0.92f);
    private static readonly Color BorderColor = new(0.4f, 0.4f, 0.45f);

    // Events
    public event Action? FormatCodeRequested;
    public event Action? ShowAllProblemsRequested;
    public event Action? Dismissed;

    public override void _Ready()
    {
        // Allow mouse events to pass through the control itself
        // but the container will still receive events
        MouseFilter = MouseFilterEnum.Ignore;
        CreateUI();
        UpdateVisibility();
    }

    private void CreateUI()
    {
        // Main container with styling
        _container = new PanelContainer();
        AddChild(_container);

        // Style the container
        var styleBox = new StyleBoxFlat
        {
            BgColor = BackgroundColor,
            BorderColor = BorderColor,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginBottom = 8,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8
        };
        _container.AddThemeStyleboxOverride("panel", styleBox);

        // Main layout
        _mainLayout = new VBoxContainer();
        _mainLayout.AddThemeConstantOverride("separation", 6);
        _container.AddChild(_mainLayout);

        // Header row (always visible)
        _headerRow = new HBoxContainer();
        _headerRow.AddThemeConstantOverride("separation", 8);
        _mainLayout.AddChild(_headerRow);

        // Expand/collapse button
        _expandButton = new Button
        {
            Text = "▶",
            Flat = true,
            CustomMinimumSize = new Vector2(20, 20)
        };
        _expandButton.Pressed += OnExpandPressed;
        _headerRow.AddChild(_expandButton);

        // Header label
        _headerLabel = new Label
        {
            Text = "No problems",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _headerLabel.AddThemeFontSizeOverride("font_size", 14);
        _headerRow.AddChild(_headerLabel);

        // Close button
        var closeButton = new Button
        {
            Text = "×",
            Flat = true,
            CustomMinimumSize = new Vector2(20, 20)
        };
        closeButton.Pressed += OnClosePressed;
        _headerRow.AddChild(closeButton);

        // Details container (shown when expanded)
        _detailsContainer = new VBoxContainer();
        _detailsContainer.AddThemeConstantOverride("separation", 4);
        _detailsContainer.Visible = false;
        _mainLayout.AddChild(_detailsContainer);

        // Separator
        _detailsContainer.AddChild(new HSeparator());

        // Error/Warning/Hint counts
        _errorsLabel = CreateCountLabel("Errors: 0", ErrorColor);
        _detailsContainer.AddChild(_errorsLabel);

        _warningsLabel = CreateCountLabel("Warnings: 0", WarningColor);
        _detailsContainer.AddChild(_warningsLabel);

        _hintsLabel = CreateCountLabel("Hints: 0", HintColor);
        _detailsContainer.AddChild(_hintsLabel);

        // Buttons row
        _buttonsRow = new HBoxContainer();
        _buttonsRow.AddThemeConstantOverride("separation", 8);
        _detailsContainer.AddChild(_buttonsRow);

        // Fix formatting button
        _fixFormattingButton = new Button
        {
            Text = "Fix Formatting",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _fixFormattingButton.Pressed += OnFixFormattingPressed;
        _buttonsRow.AddChild(_fixFormattingButton);

        // Show all button
        _showAllButton = new Button
        {
            Text = "Show All",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _showAllButton.Pressed += OnShowAllPressed;
        _buttonsRow.AddChild(_showAllButton);

        // Initial size
        UpdateSize();
    }

    private Label CreateCountLabel(string text, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    /// <summary>
    /// Updates the panel with new diagnostic summary.
    /// </summary>
    public void UpdateSummary(DiagnosticSummary summary, string? scriptPath = null)
    {
        _currentSummary = summary;

        // Update header text
        if (summary.HasIssues)
        {
            var parts = new List<string>();
            if (summary.ErrorCount > 0)
                parts.Add($"{summary.ErrorCount} error{(summary.ErrorCount > 1 ? "s" : "")}");
            if (summary.WarningCount > 0)
                parts.Add($"{summary.WarningCount} warn{(summary.WarningCount > 1 ? "s" : "")}");

            _headerLabel.Text = string.Join(", ", parts);

            if (summary.ErrorCount > 0)
                _headerLabel.AddThemeColorOverride("font_color", ErrorColor);
            else if (summary.WarningCount > 0)
                _headerLabel.AddThemeColorOverride("font_color", WarningColor);
            else
                _headerLabel.RemoveThemeColorOverride("font_color");
        }
        else
        {
            _headerLabel.Text = "No problems";
            _headerLabel.RemoveThemeColorOverride("font_color");
        }

        // Update detail labels
        _errorsLabel.Text = $"Errors: {summary.ErrorCount}";
        _warningsLabel.Text = $"Warnings: {summary.WarningCount}";
        _hintsLabel.Text = $"Hints: {summary.HintCount}";

        // Enable/disable fix button
        _fixFormattingButton.Disabled = !summary.HasFormattingIssues;

        // Show expanded for first time if there are problems
        if (scriptPath != null && summary.HasIssues && !_shownFiles.Contains(scriptPath))
        {
            _shownFiles.Add(scriptPath);
            ShowExpanded();
        }

        UpdateVisibility();
    }

    /// <summary>
    /// Shows the panel in expanded state.
    /// </summary>
    public void ShowExpanded()
    {
        _isExpanded = true;
        _hasBeenShownExpanded = true;
        UpdateExpandState();
        Show();
    }

    /// <summary>
    /// Shows the panel in collapsed state.
    /// </summary>
    public void ShowCollapsed()
    {
        _isExpanded = false;
        UpdateExpandState();
        Show();
    }

    /// <summary>
    /// Positions the panel in the specified corner.
    /// </summary>
    public void PositionInCorner(Control parent, Corner corner = Corner.TopRight)
    {
        if (parent == null)
            return;

        // Ensure UI is created
        if (_container == null)
            CreateUI();

        var parentSize = parent.Size;
        var panelSize = _container?.Size ?? new Vector2(CollapsedWidth, CollapsedHeight);

        Vector2 position = corner switch
        {
            Corner.TopLeft => new Vector2(CornerOffset, CornerOffset),
            Corner.TopRight => new Vector2(parentSize.X - panelSize.X - CornerOffset, CornerOffset),
            Corner.BottomLeft => new Vector2(CornerOffset, parentSize.Y - panelSize.Y - CornerOffset),
            Corner.BottomRight => new Vector2(parentSize.X - panelSize.X - CornerOffset, parentSize.Y - panelSize.Y - CornerOffset),
            _ => new Vector2(parentSize.X - panelSize.X - CornerOffset, CornerOffset)
        };

        Position = position;
    }

    /// <summary>
    /// Resets the "shown expanded" state for all files.
    /// </summary>
    public void ResetShownState()
    {
        _shownFiles.Clear();
    }

    private void UpdateVisibility()
    {
        // Hide if no issues
        Visible = _currentSummary.HasIssues;
    }

    private void UpdateExpandState()
    {
        _detailsContainer.Visible = _isExpanded;
        _expandButton.Text = _isExpanded ? "▼" : "▶";
        UpdateSize();
    }

    private void UpdateSize()
    {
        _container.CustomMinimumSize = _isExpanded
            ? new Vector2(ExpandedWidth, ExpandedHeight)
            : new Vector2(CollapsedWidth, CollapsedHeight);
    }

    private void OnExpandPressed()
    {
        _isExpanded = !_isExpanded;
        UpdateExpandState();
    }

    private void OnClosePressed()
    {
        Hide();
        Dismissed?.Invoke();
    }

    private void OnFixFormattingPressed()
    {
        FormatCodeRequested?.Invoke();
    }

    private void OnShowAllPressed()
    {
        ShowAllProblemsRequested?.Invoke();
    }

    /// <summary>
    /// Corner positions for the panel.
    /// </summary>
    public enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}
