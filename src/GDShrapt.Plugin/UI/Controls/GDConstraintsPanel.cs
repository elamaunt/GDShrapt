using GDShrapt.CLI.Core;

namespace GDShrapt.Plugin;

/// <summary>
/// Panel for displaying type constraints (Duck types and Union types).
/// Collapsible section that shows detailed constraint information.
/// </summary>
internal partial class GDConstraintsPanel : PanelContainer
{
    private VBoxContainer _content;
    private Button _toggleButton;
    private VBoxContainer _detailsContainer;
    private bool _collapsed = true;

    // Colors
    private static readonly Color WarningColor = new(1.0f, 0.85f, 0.4f);
    private static readonly Color HintColor = new(0.5f, 0.5f, 0.5f);
    private static readonly Color MethodColor = new(0.6f, 0.8f, 1.0f);
    private static readonly Color PropertyColor = new(0.8f, 0.7f, 1.0f);

    /// <summary>
    /// Fired when user wants to add an interface for duck type.
    /// </summary>
    public event Action<GDTypeFlowNode> AddInterfaceRequested;

    /// <summary>
    /// Fired when user wants to narrow a union type.
    /// </summary>
    public event Action<GDTypeFlowNode> NarrowTypeRequested;

    /// <summary>
    /// Fired when user wants to add a type guard.
    /// </summary>
    public event Action<GDTypeFlowNode> AddTypeGuardRequested;

    // Current node being displayed
    private GDTypeFlowNode _currentNode;

    /// <summary>
    /// Controls whether Pro action buttons are shown.
    /// Since single-file operations with preview are allowed in Base,
    /// we show these buttons but they will show a preview dialog.
    /// </summary>
    public bool ShowProActions { get; set; } = true;

    public GDConstraintsPanel()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ShrinkBegin;  // Don't expand, prevent pushing ActionBar off screen

        // Panel styling
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.14f, 0.12f),
            BorderWidthTop = 1,
            BorderColor = new Color(0.3f, 0.28f, 0.2f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        AddThemeStyleboxOverride("panel", style);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 4);
        AddChild(_content);

        // Toggle button (header)
        _toggleButton = new Button
        {
            Text = "▶ Constraints",
            Flat = true,
            Alignment = HorizontalAlignment.Left
        };
        _toggleButton.AddThemeFontSizeOverride("font_size", 12);
        _toggleButton.AddThemeColorOverride("font_color", WarningColor);
        _toggleButton.Pressed += OnTogglePressed;
        _content.AddChild(_toggleButton);

        // Details container (collapsible)
        _detailsContainer = new VBoxContainer();
        _detailsContainer.AddThemeConstantOverride("separation", 4);
        _detailsContainer.Visible = false;
        _content.AddChild(_detailsContainer);
    }

    /// <summary>
    /// Sets the constraints to display for the given node.
    /// Auto-expands when constraints are critical (low confidence, many union types, etc.)
    /// </summary>
    public void SetConstraints(GDTypeFlowNode node)
    {
        _currentNode = node;
        ClearDetails();

        bool hasConstraints = false;

        // Duck type constraints
        if (node?.HasDuckConstraints == true && node.DuckTypeInfo != null)
        {
            AddDuckTypeSection(node.DuckTypeInfo);
            hasConstraints = true;
        }

        // Union type info
        if (node?.IsUnionType == true && node.UnionTypeInfo != null)
        {
            AddUnionTypeSection(node.UnionTypeInfo, node.UnionSources);
            hasConstraints = true;
        }

        // Update visibility
        Visible = hasConstraints;

        if (hasConstraints)
        {
            var constraintCount = GetConstraintCount(node);

            // Auto-expand when constraints are critical for understanding the type
            bool shouldAutoExpand = ShouldAutoExpand(node, constraintCount);
            if (shouldAutoExpand && _collapsed)
            {
                _collapsed = false;
                _detailsContainer.Visible = true;
            }

            _toggleButton.Text = _collapsed
                ? $"▶ Constraints ({constraintCount})"
                : $"▼ Constraints ({constraintCount})";
        }
    }

    /// <summary>
    /// Determines if constraints panel should auto-expand based on data criticality.
    /// </summary>
    private bool ShouldAutoExpand(GDTypeFlowNode node, int constraintCount)
    {
        if (node == null)
            return false;

        // Low confidence - developer needs to see what constraints exist
        if (node.Confidence < 0.5f)
            return true;

        // Multiple union types - critical for understanding variable behavior
        if (node.IsUnionType && node.UnionTypeInfo?.Types.Count > 2)
            return true;

        // Many duck constraints - important to show
        if (node.HasDuckConstraints && constraintCount > 3)
            return true;

        return false;
    }

    /// <summary>
    /// Clears the panel.
    /// </summary>
    public void Clear()
    {
        _currentNode = null;
        ClearDetails();
        Visible = false;
    }

    private void ClearDetails()
    {
        foreach (var child in _detailsContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    private int GetConstraintCount(GDTypeFlowNode node)
    {
        int count = 0;

        if (node?.DuckTypeInfo != null)
        {
            count += node.DuckTypeInfo.RequiredMethods.Count;
            count += node.DuckTypeInfo.RequiredProperties.Count;
        }

        if (node?.UnionTypeInfo != null)
        {
            count += node.UnionTypeInfo.Types.Count;
        }

        return count;
    }

    private void AddDuckTypeSection(GDDuckType duckType)
    {
        // Section header
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 4);

        var warningIcon = new Label { Text = "⚠" };
        warningIcon.AddThemeColorOverride("font_color", WarningColor);
        header.AddChild(warningIcon);

        var headerLabel = new Label { Text = "Duck Type Constraints" };
        headerLabel.AddThemeFontSizeOverride("font_size", 11);
        headerLabel.AddThemeColorOverride("font_color", WarningColor);
        header.AddChild(headerLabel);

        _detailsContainer.AddChild(header);

        // Required methods
        if (duckType.RequiredMethods.Count > 0)
        {
            var methodsLabel = new Label
            {
                Text = "  Required methods:"
            };
            methodsLabel.AddThemeFontSizeOverride("font_size", 10);
            methodsLabel.AddThemeColorOverride("font_color", HintColor);
            _detailsContainer.AddChild(methodsLabel);

            foreach (var method in duckType.RequiredMethods.Take(5))
            {
                var methodRow = new HBoxContainer();
                methodRow.AddThemeConstantOverride("separation", 4);

                var bullet = new Label { Text = "    •" };
                bullet.AddThemeColorOverride("font_color", HintColor);
                methodRow.AddChild(bullet);

                var methodName = new Label { Text = $"{method.Key}()" };
                methodName.AddThemeFontSizeOverride("font_size", 10);
                methodName.AddThemeColorOverride("font_color", MethodColor);
                methodRow.AddChild(methodName);

                // method.Value is parameter count (-1 means unknown)
                if (method.Value >= 0)
                {
                    var paramInfo = new Label { Text = $" ({method.Value} params)" };
                    paramInfo.AddThemeFontSizeOverride("font_size", 10);
                    paramInfo.AddThemeColorOverride("font_color", HintColor);
                    methodRow.AddChild(paramInfo);
                }

                _detailsContainer.AddChild(methodRow);
            }

            if (duckType.RequiredMethods.Count > 5)
            {
                var moreLabel = new Label { Text = $"    ... and {duckType.RequiredMethods.Count - 5} more" };
                moreLabel.AddThemeFontSizeOverride("font_size", 10);
                moreLabel.AddThemeColorOverride("font_color", HintColor);
                _detailsContainer.AddChild(moreLabel);
            }
        }

        // Required properties
        if (duckType.RequiredProperties.Count > 0)
        {
            var propsLabel = new Label
            {
                Text = "  Required properties:"
            };
            propsLabel.AddThemeFontSizeOverride("font_size", 10);
            propsLabel.AddThemeColorOverride("font_color", HintColor);
            _detailsContainer.AddChild(propsLabel);

            foreach (var prop in duckType.RequiredProperties.Take(5))
            {
                var propRow = new HBoxContainer();
                propRow.AddThemeConstantOverride("separation", 4);

                var bullet = new Label { Text = "    •" };
                bullet.AddThemeColorOverride("font_color", HintColor);
                propRow.AddChild(bullet);

                var propName = new Label { Text = $".{prop.Key}" };
                propName.AddThemeFontSizeOverride("font_size", 10);
                propName.AddThemeColorOverride("font_color", PropertyColor);
                propRow.AddChild(propName);

                if (prop.Value != null)
                {
                    var propType = new Label { Text = $": {prop.Value.DisplayName}" };
                    propType.AddThemeFontSizeOverride("font_size", 10);
                    propType.AddThemeColorOverride("font_color", HintColor);
                    propRow.AddChild(propType);
                }

                _detailsContainer.AddChild(propRow);
            }

            if (duckType.RequiredProperties.Count > 5)
            {
                var moreLabel = new Label { Text = $"    ... and {duckType.RequiredProperties.Count - 5} more" };
                moreLabel.AddThemeFontSizeOverride("font_size", 10);
                moreLabel.AddThemeColorOverride("font_color", HintColor);
                _detailsContainer.AddChild(moreLabel);
            }
        }

        // Action buttons (Pro feature - hidden in Base per Rule 12)
        if (ShowProActions)
        {
            var actions = new HBoxContainer();
            actions.AddThemeConstantOverride("separation", 8);

            var addInterfaceBtn = new Button
            {
                Text = "Add interface",
                Flat = true,
                TooltipText = "Create an interface from these constraints"
            };
            addInterfaceBtn.AddThemeFontSizeOverride("font_size", 10);
            addInterfaceBtn.Pressed += () => AddInterfaceRequested?.Invoke(_currentNode);
            actions.AddChild(addInterfaceBtn);

            _detailsContainer.AddChild(actions);
        }

        // Spacer
        var spacer = new Control { CustomMinimumSize = new Vector2(0, 4) };
        _detailsContainer.AddChild(spacer);
    }

    private void AddUnionTypeSection(GDUnionType unionType, List<GDTypeFlowNode> sources)
    {
        // Section header
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 4);

        var warningIcon = new Label { Text = "⚠" };
        warningIcon.AddThemeColorOverride("font_color", WarningColor);
        header.AddChild(warningIcon);

        var headerLabel = new Label { Text = $"Union Type ({unionType.Types.Count} types)" };
        headerLabel.AddThemeFontSizeOverride("font_size", 11);
        headerLabel.AddThemeColorOverride("font_color", WarningColor);
        header.AddChild(headerLabel);

        _detailsContainer.AddChild(header);

        // List each type with source
        var sourcesList = sources ?? new List<GDTypeFlowNode>();
        var typesList = unionType.Types.ToList();

        for (int i = 0; i < Math.Min(typesList.Count, 5); i++)
        {
            var type = typesList[i];
            var source = i < sourcesList.Count ? sourcesList[i] : null;

            var typeRow = new HBoxContainer();
            typeRow.AddThemeConstantOverride("separation", 4);

            var bullet = new Label { Text = "  •" };
            bullet.AddThemeColorOverride("font_color", HintColor);
            typeRow.AddChild(bullet);

            var typeLabel = new Label { Text = type.DisplayName };
            typeLabel.AddThemeFontSizeOverride("font_size", 10);
            typeLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.76f, 0.65f));
            typeRow.AddChild(typeLabel);

            if (source != null)
            {
                var sourceHint = GetSourceHint(source);
                var sourceLabel = new Label { Text = $" ← {sourceHint}" };
                sourceLabel.AddThemeFontSizeOverride("font_size", 10);
                sourceLabel.AddThemeColorOverride("font_color", HintColor);
                typeRow.AddChild(sourceLabel);
            }

            _detailsContainer.AddChild(typeRow);
        }

        if (unionType.Types.Count > 5)
        {
            var moreLabel = new Label { Text = $"  ... and {unionType.Types.Count - 5} more types" };
            moreLabel.AddThemeFontSizeOverride("font_size", 10);
            moreLabel.AddThemeColorOverride("font_color", HintColor);
            _detailsContainer.AddChild(moreLabel);
        }

        // Common base type if available
        if (unionType.CommonBaseType != null)
        {
            var baseLabel = new Label { Text = $"  Common base: {unionType.CommonBaseType.DisplayName}" };
            baseLabel.AddThemeFontSizeOverride("font_size", 10);
            baseLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.8f));
            _detailsContainer.AddChild(baseLabel);
        }

        // Action buttons (Pro feature - hidden in Base per Rule 12)
        if (ShowProActions)
        {
            var actions = new HBoxContainer();
            actions.AddThemeConstantOverride("separation", 8);

            var narrowBtn = new Button
            {
                Text = "Narrow type",
                Flat = true,
                TooltipText = "Add explicit type annotation to narrow the type"
            };
            narrowBtn.AddThemeFontSizeOverride("font_size", 10);
            narrowBtn.Pressed += () => NarrowTypeRequested?.Invoke(_currentNode);
            actions.AddChild(narrowBtn);

            var guardBtn = new Button
            {
                Text = "Add type guard",
                Flat = true,
                TooltipText = "Add a type check to narrow the type at runtime"
            };
            guardBtn.AddThemeFontSizeOverride("font_size", 10);
            guardBtn.Pressed += () => AddTypeGuardRequested?.Invoke(_currentNode);
            actions.AddChild(guardBtn);

            _detailsContainer.AddChild(actions);
        }
    }

    private string GetSourceHint(GDTypeFlowNode source)
    {
        if (source == null)
            return "";

        var location = source.Location != null && source.Location.IsValid
            ? $"line {source.Location.StartLine + 1}"
            : "";

        return source.Kind switch
        {
            GDTypeFlowNodeKind.Literal => $"literal {location}".Trim(),
            GDTypeFlowNodeKind.MethodCall => $"method call {location}".Trim(),
            GDTypeFlowNodeKind.Assignment => $"assignment {location}".Trim(),
            GDTypeFlowNodeKind.Parameter => "parameter",
            _ => source.Description ?? location
        };
    }

    private void OnTogglePressed()
    {
        _collapsed = !_collapsed;
        _detailsContainer.Visible = !_collapsed;

        var constraintCount = GetConstraintCount(_currentNode);
        _toggleButton.Text = _collapsed
            ? $"▶ Constraints ({constraintCount})"
            : $"▼ Constraints ({constraintCount})";
    }

    /// <summary>
    /// Sets compact mode for space-constrained layouts.
    /// In compact mode, constraints are always collapsed.
    /// </summary>
    public void SetCompactMode(bool compact)
    {
        if (compact)
        {
            // In compact mode, always keep collapsed
            _collapsed = true;
            _detailsContainer.Visible = false;
        }
        // When exiting compact mode, we don't auto-expand - let user decide
        // Just update the toggle button text to reflect current state
        var constraintCount = GetConstraintCount(_currentNode);
        _toggleButton.Text = _collapsed
            ? $"▶ Constraints ({constraintCount})"
            : $"▼ Constraints ({constraintCount})";
    }
}
