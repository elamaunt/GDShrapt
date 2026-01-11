using Godot;

namespace GDShrapt.Plugin;

/// <summary>
/// Ko-fi style support button with rounded corners and warm orange color.
/// </summary>
internal partial class SupportButton : Button
{
    private const string DonateUrl = "https://ko-fi.com/elamaunt";
    private static readonly Color OrangeColor = new("#FF813F");
    private static readonly Color OrangeHoverColor = new("#FF9A5F");
    private static readonly Color OrangePressedColor = new("#E5732F");

    public SupportButton()
    {
        Text = LocalizationManager.Tr(Strings.SupportButton);
        CustomMinimumSize = new Vector2(100, 26);

        // Push to right side
        SizeFlagsHorizontal = SizeFlags.ShrinkEnd;

        SetupStyle();

        Pressed += OnPressed;
    }

    private void SetupStyle()
    {
        // Normal state
        var normalStyle = new StyleBoxFlat
        {
            BgColor = OrangeColor,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        AddThemeStyleboxOverride("normal", normalStyle);

        // Hover state
        var hoverStyle = new StyleBoxFlat
        {
            BgColor = OrangeHoverColor,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        AddThemeStyleboxOverride("hover", hoverStyle);

        // Pressed state
        var pressedStyle = new StyleBoxFlat
        {
            BgColor = OrangePressedColor,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        AddThemeStyleboxOverride("pressed", pressedStyle);

        // White text color
        AddThemeColorOverride("font_color", Colors.White);
        AddThemeColorOverride("font_hover_color", Colors.White);
        AddThemeColorOverride("font_pressed_color", Colors.White);
        AddThemeColorOverride("font_focus_color", Colors.White);
    }

    private void OnPressed()
    {
        OS.ShellOpen(DonateUrl);
    }
}
