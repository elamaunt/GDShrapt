namespace GDShrapt.Plugin;

/// <summary>
/// A custom control that displays confidence level as colored dots (●●●○○).
/// Supports compact mode (single colored dot) for narrow layouts.
/// </summary>
internal partial class GDConfidenceBadge : Control
{
    private float _confidence;
    private bool _compactMode;
    private string _label;

    // Colors for confidence levels
    private static readonly Color HighColor = new(0.3f, 0.8f, 0.3f);    // Green
    private static readonly Color MediumColor = new(1.0f, 0.7f, 0.3f);  // Orange
    private static readonly Color LowColor = new(0.8f, 0.3f, 0.3f);     // Red
    private static readonly Color EmptyColor = new(0.3f, 0.3f, 0.3f);   // Gray

    // Dot drawing parameters
    private const float DotRadius = 4f;
    private const float DotSpacing = 10f;
    private const int TotalDots = 5;

    public GDConfidenceBadge()
    {
        CustomMinimumSize = new Vector2(DotSpacing * TotalDots, DotRadius * 3);
        MouseFilter = MouseFilterEnum.Pass;
    }

    /// <summary>
    /// Sets the confidence level to display.
    /// </summary>
    /// <param name="confidence">Confidence value from 0.0 to 1.0.</param>
    /// <param name="label">Optional label to show after dots (e.g., "High", "Medium").</param>
    public void SetConfidence(float confidence, string label = null)
    {
        _confidence = Math.Clamp(confidence, 0f, 1f);
        _label = label;
        UpdateMinimumSize();
        QueueRedraw();
    }

    /// <summary>
    /// Sets compact mode (single colored dot instead of 5 dots).
    /// </summary>
    public void SetCompactMode(bool compact)
    {
        _compactMode = compact;
        UpdateMinimumSize();
        QueueRedraw();
    }

    /// <summary>
    /// Gets whether compact mode is enabled.
    /// </summary>
    public bool IsCompactMode => _compactMode;

    /// <summary>
    /// Gets the current confidence value.
    /// </summary>
    public float Confidence => _confidence;

    private new void UpdateMinimumSize()
    {
        if (_compactMode)
        {
            // Compact mode: smaller single dot, minimize space usage in narrow layouts
            // Using 2*radius for width to fit single dot tightly
            CustomMinimumSize = new Vector2(DotRadius * 2 + 2, DotRadius * 2 + 2);
        }
        else
        {
            var width = DotSpacing * TotalDots;
            if (!string.IsNullOrEmpty(_label))
            {
                // Add space for label
                var font = ThemeDB.FallbackFont;
                var fontSize = 10;
                var labelSize = font.GetStringSize(_label, HorizontalAlignment.Left, -1, fontSize);
                width += labelSize.X + 6;
            }
            CustomMinimumSize = new Vector2(width, DotRadius * 3);
        }
    }

    public override void _Draw()
    {
        var color = GetConfidenceColor(_confidence);

        if (_compactMode)
        {
            DrawCompactMode(color);
        }
        else
        {
            DrawFullMode(color);
        }
    }

    private void DrawCompactMode(Color color)
    {
        // Draw a single colored circle
        var center = Size / 2;
        DrawCircle(center, DotRadius + 1, color);
    }

    private void DrawFullMode(Color color)
    {
        var filledDots = (int)Math.Round(_confidence * TotalDots);
        var y = Size.Y / 2;

        // Draw dots
        for (int i = 0; i < TotalDots; i++)
        {
            var x = DotRadius + i * DotSpacing;
            var dotColor = i < filledDots ? color : EmptyColor;
            DrawCircle(new Vector2(x, y), DotRadius, dotColor);
        }

        // Draw label if present
        if (!string.IsNullOrEmpty(_label))
        {
            var font = ThemeDB.FallbackFont;
            var fontSize = 10;
            var labelX = DotSpacing * TotalDots + 4;
            var labelY = y + fontSize / 3f;
            DrawString(font, new Vector2(labelX, labelY), _label, HorizontalAlignment.Left, -1, fontSize, color);
        }
    }

    /// <summary>
    /// Gets the color for a confidence level.
    /// </summary>
    public static Color GetConfidenceColor(float confidence)
    {
        return confidence switch
        {
            >= 0.8f => HighColor,
            >= 0.5f => MediumColor,
            _ => LowColor
        };
    }

    /// <summary>
    /// Gets the label for a confidence level.
    /// </summary>
    public static string GetConfidenceLabel(float confidence)
    {
        return confidence switch
        {
            >= 0.8f => "High",
            >= 0.5f => "Medium",
            _ => "Low"
        };
    }

    /// <summary>
    /// Gets a string representation of confidence as dots: ●●●○○
    /// </summary>
    public static string GetConfidenceDots(float confidence)
    {
        var filled = (int)Math.Round(confidence * TotalDots);
        return new string('●', filled) + new string('○', TotalDots - filled);
    }
}
