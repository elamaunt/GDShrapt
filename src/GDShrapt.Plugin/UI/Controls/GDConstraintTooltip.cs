using GDShrapt.CLI.Core;

namespace GDShrapt.Plugin;

/// <summary>
/// Tooltip control for displaying duck type constraints.
/// Shows required methods, properties, and signals when hovering over constraint edges.
/// </summary>
internal partial class GDConstraintTooltip : PanelContainer
{
    private VBoxContainer _contentContainer;
    private Label _titleLabel;
    private Label _methodsLabel;
    private Label _propertiesLabel;
    private Label _signalsLabel;

    public GDConstraintTooltip()
    {
        CreateUI();
        Visible = false;
    }

    private void CreateUI()
    {
        // Style
        var styleBox = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.18f),
            BorderColor = new Color(1.0f, 0.85f, 0.3f), // Yellow for duck type
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        AddThemeStyleboxOverride("panel", styleBox);

        CustomMinimumSize = new Vector2(180, 50);
        MouseFilter = MouseFilterEnum.Ignore;

        _contentContainer = new VBoxContainer();
        _contentContainer.AddThemeConstantOverride("separation", 4);
        AddChild(_contentContainer);

        // Title
        _titleLabel = new Label
        {
            Text = "Duck Type Constraints"
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 11);
        _titleLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.3f));
        _contentContainer.AddChild(_titleLabel);

        // Methods section
        _methodsLabel = new Label
        {
            Text = "",
            Visible = false
        };
        _methodsLabel.AddThemeFontSizeOverride("font_size", 10);
        _methodsLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.9f));
        _contentContainer.AddChild(_methodsLabel);

        // Properties section
        _propertiesLabel = new Label
        {
            Text = "",
            Visible = false
        };
        _propertiesLabel.AddThemeFontSizeOverride("font_size", 10);
        _propertiesLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.9f, 0.7f));
        _contentContainer.AddChild(_propertiesLabel);

        // Signals section
        _signalsLabel = new Label
        {
            Text = "",
            Visible = false
        };
        _signalsLabel.AddThemeFontSizeOverride("font_size", 10);
        _signalsLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.8f, 0.7f));
        _contentContainer.AddChild(_signalsLabel);
    }

    /// <summary>
    /// Shows the tooltip for the specified edge at the given position.
    /// </summary>
    public void ShowForEdge(GDTypeFlowEdge edge, Vector2 position)
    {
        if (edge?.Constraints == null || !edge.Constraints.HasRequirements)
        {
            Hide();
            return;
        }

        UpdateContent(edge.Constraints);
        Position = position + new Vector2(10, 10); // Offset from cursor
        Show();
    }

    /// <summary>
    /// Shows the tooltip for the specified duck type constraints.
    /// </summary>
    public void ShowForConstraints(GDEdgeConstraints constraints, Vector2 position)
    {
        if (constraints == null || !constraints.HasRequirements)
        {
            Hide();
            return;
        }

        UpdateContent(constraints);
        Position = position + new Vector2(10, 10);
        Show();
    }

    /// <summary>
    /// Updates the tooltip content based on constraints.
    /// </summary>
    private void UpdateContent(GDEdgeConstraints constraints)
    {
        // Methods
        if (constraints.RequiredMethods.Count > 0)
        {
            var methods = constraints.RequiredMethods.Keys
                .Select(m => $"  {m}()")
                .ToList();
            _methodsLabel.Text = "Methods:\n" + string.Join("\n", methods.Take(5));
            if (methods.Count > 5)
                _methodsLabel.Text += $"\n  +{methods.Count - 5} more...";
            _methodsLabel.Visible = true;
        }
        else
        {
            _methodsLabel.Visible = false;
        }

        // Properties
        if (constraints.RequiredProperties.Count > 0)
        {
            var props = constraints.RequiredProperties
                .Select(kv => kv.Value != null ? $"  .{kv.Key}: {kv.Value}" : $"  .{kv.Key}")
                .ToList();
            _propertiesLabel.Text = "Properties:\n" + string.Join("\n", props.Take(5));
            if (props.Count > 5)
                _propertiesLabel.Text += $"\n  +{props.Count - 5} more...";
            _propertiesLabel.Visible = true;
        }
        else
        {
            _propertiesLabel.Visible = false;
        }

        // Signals
        if (constraints.RequiredSignals.Count > 0)
        {
            var signals = constraints.RequiredSignals
                .Select(s => $"  signal {s}")
                .ToList();
            _signalsLabel.Text = "Signals:\n" + string.Join("\n", signals.Take(3));
            if (signals.Count > 3)
                _signalsLabel.Text += $"\n  +{signals.Count - 3} more...";
            _signalsLabel.Visible = true;
        }
        else
        {
            _signalsLabel.Visible = false;
        }
    }

    /// <summary>
    /// Hides the tooltip.
    /// </summary>
    public new void Hide()
    {
        Visible = false;
    }

    /// <summary>
    /// Shows the tooltip.
    /// </summary>
    public new void Show()
    {
        Visible = true;
    }
}
