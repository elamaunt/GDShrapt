namespace GDShrapt.Plugin;

/// <summary>
/// Visual block representing a node in the type flow graph.
/// Supports different visual states: focused, source (inflow), target (outflow), inactive.
/// </summary>
internal partial class GDTypeFlowBlock : PanelContainer
{
    // Theme colors for different states - Focused block is more prominent
    private static readonly Color FocusedBorder = new(0.5f, 0.8f, 1.0f);      // Bright Blue
    private static readonly Color SourceBorder = new(0.5f, 0.8f, 0.5f);       // Green
    private static readonly Color TargetBorder = new(1.0f, 0.7f, 0.4f);       // Orange
    private static readonly Color InactiveBorder = new(0.3f, 0.3f, 0.3f);     // Gray

    private static readonly Color FocusedBg = new(0.18f, 0.25f, 0.35f);       // Brighter blue bg
    private static readonly Color SourceBg = new(0.12f, 0.18f, 0.12f);
    private static readonly Color TargetBg = new(0.18f, 0.15f, 0.1f);
    private static readonly Color InactiveBg = new(0.1f, 0.1f, 0.1f);

    // UI elements
    private VBoxContainer _contentContainer;
    private HBoxContainer _headerRow;
    private TextureRect _iconRect;
    private RichTextLabel _labelText;
    private Label _typeText;
    private Label _sourceTypeLabel;
    private GDConfidenceBar _confidenceBar;
    private Label _descriptionLabel;
    private HBoxContainer _indicatorsRow;
    private Label _unionIndicator;
    private Label _duckIndicator;
    private Label _sourceObjectLabel;

    /// <summary>
    /// The type flow node this block represents.
    /// </summary>
    public GDTypeFlowNode Node { get; private set; }

    /// <summary>
    /// Whether this block is the currently focused node.
    /// </summary>
    public bool IsFocused { get; set; }

    /// <summary>
    /// Whether this block is a source (inflow) of the focused node.
    /// </summary>
    public bool IsSource { get; set; }

    /// <summary>
    /// Whether this block is a target (outflow) of the focused node.
    /// </summary>
    public bool IsTarget { get; set; }

    /// <summary>
    /// Event fired when the block is clicked.
    /// </summary>
    public event Action<GDTypeFlowBlock> Clicked;

    /// <summary>
    /// Event fired when the block is double-clicked.
    /// </summary>
    public event Action<GDTypeFlowBlock> DoubleClicked;

    /// <summary>
    /// Event fired when the label (identifier) is clicked.
    /// Used to change focus to this node.
    /// </summary>
    public event Action<GDTypeFlowBlock> LabelClicked;

    public GDTypeFlowBlock()
    {
        CreateUI();
    }

    /// <summary>
    /// Sets the node data and updates the visual representation.
    /// </summary>
    public void SetNode(GDTypeFlowNode node)
    {
        Node = node;

        if (node == null)
        {
            Visible = false;
            return;
        }

        Visible = true;

        // Update label with underline to indicate clickability
        var labelText = node.Label ?? "Unknown";
        _labelText.Text = $"[u]{labelText}[/u]";

        // Update type with SourceType if available
        var typeDisplay = BuildTypeDisplay(node);
        _typeText.Text = typeDisplay;
        _typeText.AddThemeColorOverride("font_color", node.GetConfidenceColor());

        // Update source type label (shown for method calls, indexers, property access)
        UpdateSourceTypeDisplay(node);

        // Update confidence bar
        _confidenceBar?.SetConfidence(node.Confidence, GetConfidenceLabel(node.Confidence));

        // Update description
        if (!string.IsNullOrEmpty(node.Description))
        {
            _descriptionLabel.Text = node.Description;
            _descriptionLabel.Visible = true;
        }
        else
        {
            _descriptionLabel.Visible = false;
        }

        // Update icon if available
        UpdateIcon(node);

        // Update union type indicator
        if (node.IsUnionType && node.UnionTypeInfo != null && node.UnionTypeInfo.Types.Count > 1)
        {
            _unionIndicator.Text = $"+{node.UnionTypeInfo.Types.Count} types";
            _unionIndicator.Visible = true;
        }
        else
        {
            _unionIndicator.Visible = false;
        }

        // Update duck type indicator
        if (node.HasDuckConstraints && node.DuckTypeInfo != null && node.DuckTypeInfo.HasRequirements)
        {
            _duckIndicator.Visible = true;
        }
        else
        {
            _duckIndicator.Visible = false;
        }

        // Show/hide indicators row
        _indicatorsRow.Visible = _unionIndicator.Visible || _duckIndicator.Visible || _sourceObjectLabel.Visible;

        UpdateVisualState();
    }

    /// <summary>
    /// Updates the visual state based on IsFocused, IsSource, IsTarget.
    /// </summary>
    public void UpdateVisualState()
    {
        var styleBox = new StyleBoxFlat();

        if (IsFocused)
        {
            styleBox.BorderColor = FocusedBorder;
            styleBox.BorderWidthBottom = styleBox.BorderWidthTop = 4;
            styleBox.BorderWidthLeft = styleBox.BorderWidthRight = 4;
            styleBox.BgColor = FocusedBg;
            // Add shadow effect for focused block
            styleBox.ShadowColor = new Color(0.3f, 0.6f, 1.0f, 0.4f);
            styleBox.ShadowSize = 8;
            Modulate = Colors.White;
            // Make focused block slightly larger
            CustomMinimumSize = new Vector2(170, 75);
        }
        else if (IsSource)
        {
            styleBox.BorderColor = SourceBorder;
            styleBox.BorderWidthBottom = 2;
            styleBox.BorderWidthTop = styleBox.BorderWidthLeft = styleBox.BorderWidthRight = 1;
            styleBox.BgColor = SourceBg;
            Modulate = Colors.White;
            CustomMinimumSize = new Vector2(150, 60);
        }
        else if (IsTarget)
        {
            styleBox.BorderColor = TargetBorder;
            styleBox.BorderWidthTop = 2;
            styleBox.BorderWidthBottom = styleBox.BorderWidthLeft = styleBox.BorderWidthRight = 1;
            styleBox.BgColor = TargetBg;
            Modulate = Colors.White;
            CustomMinimumSize = new Vector2(150, 60);
        }
        else
        {
            styleBox.BorderColor = InactiveBorder;
            styleBox.BorderWidthBottom = styleBox.BorderWidthTop = 1;
            styleBox.BorderWidthLeft = styleBox.BorderWidthRight = 1;
            styleBox.BgColor = InactiveBg;
            Modulate = new Color(0.7f, 0.7f, 0.7f); // Dimmed
            CustomMinimumSize = new Vector2(150, 60);
        }

        styleBox.CornerRadiusTopLeft = styleBox.CornerRadiusTopRight = 4;
        styleBox.CornerRadiusBottomLeft = styleBox.CornerRadiusBottomRight = 4;
        styleBox.ContentMarginLeft = styleBox.ContentMarginRight = 8;
        styleBox.ContentMarginTop = styleBox.ContentMarginBottom = 6;

        AddThemeStyleboxOverride("panel", styleBox);
    }

    private void CreateUI()
    {
        CustomMinimumSize = new Vector2(150, 60);
        MouseFilter = MouseFilterEnum.Stop;

        _contentContainer = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass  // Pass clicks through to parent
        };
        _contentContainer.AddThemeConstantOverride("separation", 2);
        AddChild(_contentContainer);

        // Header row: icon + label
        _headerRow = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass
        };
        _headerRow.AddThemeConstantOverride("separation", 6);

        _iconRect = new TextureRect
        {
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(16, 16),
            MouseFilter = MouseFilterEnum.Pass
        };
        _headerRow.AddChild(_iconRect);

        _labelText = new RichTextLabel
        {
            Text = "[u]Symbol[/u]",
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(0, 20),
            MouseFilter = MouseFilterEnum.Stop  // Label captures clicks for focus change
        };
        _labelText.AddThemeFontSizeOverride("normal_font_size", 13);
        _labelText.AddThemeColorOverride("default_color", new Color(0.7f, 0.85f, 1.0f)); // Clickable look
        _labelText.GuiInput += OnLabelGuiInput;
        _headerRow.AddChild(_labelText);

        _contentContainer.AddChild(_headerRow);

        // Type row
        _typeText = new Label
        {
            Text = "Type",
            MouseFilter = MouseFilterEnum.Pass
        };
        _typeText.AddThemeFontSizeOverride("font_size", 12);
        _contentContainer.AddChild(_typeText);

        // Source type label (for method calls, indexers, property access)
        _sourceTypeLabel = new Label
        {
            Text = "",
            Visible = false,
            MouseFilter = MouseFilterEnum.Pass
        };
        _sourceTypeLabel.AddThemeFontSizeOverride("font_size", 10);
        _sourceTypeLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.7f, 0.9f)); // Light blue
        _contentContainer.AddChild(_sourceTypeLabel);

        // Confidence bar (compact, only for focused blocks)
        _confidenceBar = new GDConfidenceBar();
        _confidenceBar.Visible = false; // Show only when focused
        _confidenceBar.MouseFilter = MouseFilterEnum.Pass;
        _contentContainer.AddChild(_confidenceBar);

        // Description (secondary info)
        _descriptionLabel = new Label
        {
            Text = "",
            Visible = false,
            MouseFilter = MouseFilterEnum.Pass
        };
        _descriptionLabel.AddThemeFontSizeOverride("font_size", 10);
        _descriptionLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        _contentContainer.AddChild(_descriptionLabel);

        // Indicators row (union type, duck type)
        _indicatorsRow = new HBoxContainer
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Pass
        };
        _indicatorsRow.AddThemeConstantOverride("separation", 8);

        // Union type indicator
        _unionIndicator = new Label
        {
            Text = "+3 types",
            Visible = false,
            MouseFilter = MouseFilterEnum.Pass
        };
        _unionIndicator.AddThemeFontSizeOverride("font_size", 9);
        _unionIndicator.AddThemeColorOverride("font_color", new Color(0.7f, 0.5f, 0.9f)); // Purple
        _indicatorsRow.AddChild(_unionIndicator);

        // Duck type indicator
        _duckIndicator = new Label
        {
            Text = "duck",
            Visible = false,
            MouseFilter = MouseFilterEnum.Pass
        };
        _duckIndicator.AddThemeFontSizeOverride("font_size", 9);
        _duckIndicator.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.3f)); // Yellow
        _indicatorsRow.AddChild(_duckIndicator);

        // Source object indicator (for non-focused blocks)
        _sourceObjectLabel = new Label
        {
            Text = "",
            Visible = false,
            MouseFilter = MouseFilterEnum.Pass
        };
        _sourceObjectLabel.AddThemeFontSizeOverride("font_size", 9);
        _sourceObjectLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 0.7f)); // Light green
        _indicatorsRow.AddChild(_sourceObjectLabel);

        _contentContainer.AddChild(_indicatorsRow);

        // Initial state
        UpdateVisualState();
    }

    /// <summary>
    /// Handles input on the label for focus change.
    /// </summary>
    private void OnLabelGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            LabelClicked?.Invoke(this);
            AcceptEvent();
        }
    }

    private void UpdateIcon(GDTypeFlowNode node)
    {
        // Try to get icon from Godot theme
        var iconName = node.GetIconName();
        try
        {
            var icon = GetThemeIcon(iconName, "EditorIcons");
            if (icon != null)
            {
                _iconRect.Texture = icon;
                _iconRect.Visible = true;
                return;
            }
        }
        catch
        {
            // Icon not found, ignore
        }

        _iconRect.Visible = false;
    }

    private string GetConfidenceLabel(float confidence)
    {
        return confidence switch
        {
            >= 0.8f => "High",
            >= 0.5f => "Medium",
            _ => "Low"
        };
    }

    /// <summary>
    /// Builds the type display string, including SourceType for calls/indexers.
    /// </summary>
    private string BuildTypeDisplay(GDTypeFlowNode node)
    {
        var resultType = node.Type ?? "Variant";

        // For method calls, indexers, and property access - show "ResultType" only
        // SourceType is shown separately in source type label
        if (node.Kind == GDTypeFlowNodeKind.MethodCall ||
            node.Kind == GDTypeFlowNodeKind.IndexerAccess ||
            node.Kind == GDTypeFlowNodeKind.PropertyAccess)
        {
            // Show return type prominently
            return $"→ {resultType}";
        }

        // For type checks and null checks - always bool
        if (node.Kind == GDTypeFlowNodeKind.TypeCheck ||
            node.Kind == GDTypeFlowNodeKind.NullCheck ||
            node.Kind == GDTypeFlowNodeKind.Comparison)
        {
            return "→ bool";
        }

        return resultType;
    }

    /// <summary>
    /// Updates the source type display for method calls, indexers, etc.
    /// </summary>
    private void UpdateSourceTypeDisplay(GDTypeFlowNode node)
    {
        // Show source object and type for applicable kinds
        var showSourceInfo = node.Kind == GDTypeFlowNodeKind.MethodCall ||
                             node.Kind == GDTypeFlowNodeKind.IndexerAccess ||
                             node.Kind == GDTypeFlowNodeKind.PropertyAccess;

        if (showSourceInfo && !string.IsNullOrEmpty(node.SourceObjectName))
        {
            var sourceInfo = node.SourceObjectName;
            if (!string.IsNullOrEmpty(node.SourceType) && node.SourceType != "Variant")
            {
                sourceInfo = $"{node.SourceObjectName}: {node.SourceType}";
            }
            _sourceTypeLabel.Text = $"on {sourceInfo}";
            _sourceTypeLabel.Visible = true;
        }
        else
        {
            _sourceTypeLabel.Visible = false;
        }

        // Show source object indicator in indicators row
        if (!string.IsNullOrEmpty(node.SourceObjectName))
        {
            _sourceObjectLabel.Text = node.SourceObjectName;
            _sourceObjectLabel.Visible = true;
        }
        else
        {
            _sourceObjectLabel.Visible = false;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.DoubleClick)
            {
                DoubleClicked?.Invoke(this);
            }
            else
            {
                Clicked?.Invoke(this);
            }
            AcceptEvent();
        }
    }

    /// <summary>
    /// Sets whether this block should show detailed info (confidence bar, etc.).
    /// Used for the focused block which shows more details.
    /// </summary>
    public void SetShowDetails(bool show)
    {
        _confidenceBar.Visible = show;
    }
}
