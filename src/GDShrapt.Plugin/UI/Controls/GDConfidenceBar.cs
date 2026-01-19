namespace GDShrapt.Plugin;

/// <summary>
/// A compact progress bar showing type inference confidence level.
/// Displays a colored bar with percentage and optional label.
/// </summary>
internal partial class GDConfidenceBar : HBoxContainer
{
    // Confidence level colors
    private static readonly Color HighColor = new(0.3f, 0.8f, 0.3f);      // Green
    private static readonly Color MediumColor = new(1.0f, 0.7f, 0.3f);    // Orange
    private static readonly Color LowColor = new(0.8f, 0.3f, 0.3f);       // Red
    private static readonly Color BackgroundColor = new(0.2f, 0.2f, 0.2f);

    private ProgressBar _progressBar;
    private Label _percentLabel;
    private Label _levelLabel;

    private float _value;

    /// <summary>
    /// The confidence value (0.0 - 1.0).
    /// </summary>
    public float Value
    {
        get => _value;
        set
        {
            _value = Mathf.Clamp(value, 0f, 1f);
            UpdateDisplay();
        }
    }

    public GDConfidenceBar()
    {
        CreateUI();
    }

    /// <summary>
    /// Sets the confidence value and optional label.
    /// </summary>
    /// <param name="value">Confidence value (0.0 - 1.0).</param>
    /// <param name="label">Optional label like "High", "Medium", "Low".</param>
    public void SetConfidence(float value, string label = null)
    {
        Value = value;

        if (!string.IsNullOrEmpty(label))
        {
            _levelLabel.Text = label;
            _levelLabel.Visible = true;
        }
        else
        {
            _levelLabel.Visible = false;
        }
    }

    private void CreateUI()
    {
        AddThemeConstantOverride("separation", 6);
        CustomMinimumSize = new Vector2(100, 16);

        // Progress bar
        _progressBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 50,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(60, 12),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        // Style the progress bar
        var bgStyle = new StyleBoxFlat
        {
            BgColor = BackgroundColor,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2
        };
        _progressBar.AddThemeStyleboxOverride("background", bgStyle);

        var fillStyle = new StyleBoxFlat
        {
            BgColor = MediumColor,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2
        };
        _progressBar.AddThemeStyleboxOverride("fill", fillStyle);

        AddChild(_progressBar);

        // Percentage label
        _percentLabel = new Label
        {
            Text = "50%",
            CustomMinimumSize = new Vector2(35, 0),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _percentLabel.AddThemeFontSizeOverride("font_size", 10);
        _percentLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        AddChild(_percentLabel);

        // Level label (High/Medium/Low)
        _levelLabel = new Label
        {
            Text = "",
            Visible = false
        };
        _levelLabel.AddThemeFontSizeOverride("font_size", 10);
        AddChild(_levelLabel);

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_progressBar == null)
            return;

        // Update progress bar value
        _progressBar.Value = _value * 100;

        // Update percentage label
        _percentLabel.Text = $"{(int)(_value * 100)}%";

        // Update color based on confidence level
        var color = GetColorForConfidence(_value);
        _percentLabel.AddThemeColorOverride("font_color", color);
        _levelLabel.AddThemeColorOverride("font_color", color);

        // Update progress bar fill color
        var fillStyle = new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2
        };
        _progressBar.AddThemeStyleboxOverride("fill", fillStyle);
    }

    private Color GetColorForConfidence(float confidence)
    {
        return confidence switch
        {
            >= 0.8f => HighColor,
            >= 0.5f => MediumColor,
            _ => LowColor
        };
    }
}
