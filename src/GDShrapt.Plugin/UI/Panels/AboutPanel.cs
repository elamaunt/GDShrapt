namespace GDShrapt.Plugin;

/// <summary>
/// About panel displaying plugin information, version, and links.
/// </summary>
public partial class AboutPanel : Window
{
    private const string Version = "1.0.0";
    private const string Author = "elamaunt";
    private const string GitHubUrl = "https://github.com/elamaunt/GDShrapt.Plugin";
    private const string DonateUrl = "https://ko-fi.com/elamaunt";

    public override void _Ready()
    {
        Title = LocalizationManager.Tr(Strings.MenuAbout);
        Size = new Vector2I(400, 350);

        CreateUI();
    }

    private void CreateUI()
    {
        var mainContainer = new MarginContainer();
        mainContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        mainContainer.AddThemeConstantOverride("margin_left", 20);
        mainContainer.AddThemeConstantOverride("margin_right", 20);
        mainContainer.AddThemeConstantOverride("margin_top", 20);
        mainContainer.AddThemeConstantOverride("margin_bottom", 20);
        AddChild(mainContainer);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 15);
        mainContainer.AddChild(vbox);

        // Logo/Title
        var titleLabel = new Label
        {
            Text = "GDShrapt",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 32);
        vbox.AddChild(titleLabel);

        // Description
        var descLabel = new Label
        {
            Text = LocalizationManager.Tr(Strings.PluginDescription),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        vbox.AddChild(descLabel);

        // Separator
        vbox.AddChild(new HSeparator());

        // Version
        var versionLabel = new Label
        {
            Text = string.Format(LocalizationManager.Tr(Strings.AboutVersion), Version),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        vbox.AddChild(versionLabel);

        // Author
        var authorLabel = new Label
        {
            Text = string.Format(LocalizationManager.Tr(Strings.AboutAuthor), Author),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        vbox.AddChild(authorLabel);

        // Buttons container
        var buttonsContainer = new HBoxContainer();
        buttonsContainer.Alignment = BoxContainer.AlignmentMode.Center;
        buttonsContainer.AddThemeConstantOverride("separation", 20);
        vbox.AddChild(buttonsContainer);

        // GitHub button
        var githubButton = new Button
        {
            Text = LocalizationManager.Tr(Strings.AboutGithub),
            CustomMinimumSize = new Vector2(120, 35)
        };
        githubButton.Pressed += OnGitHubPressed;
        buttonsContainer.AddChild(githubButton);

        // Donate button
        var donateButton = new Button
        {
            Text = LocalizationManager.Tr(Strings.AboutDonate),
            CustomMinimumSize = new Vector2(150, 35)
        };
        donateButton.Pressed += OnDonatePressed;
        buttonsContainer.AddChild(donateButton);

        // Spacer
        vbox.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });

        // Close button
        var closeButton = new Button
        {
            Text = LocalizationManager.Tr(Strings.DialogOk),
            CustomMinimumSize = new Vector2(100, 35)
        };
        closeButton.Pressed += OnClosePressed;

        var closeContainer = new HBoxContainer();
        closeContainer.Alignment = BoxContainer.AlignmentMode.Center;
        closeContainer.AddChild(closeButton);
        vbox.AddChild(closeContainer);

        // Footer
        var footerLabel = new Label
        {
            Text = "Built with GDShrapt.Reader, GDShrapt.Formatter, GDShrapt.Linter, GDShrapt.Validator",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.7f, 0.7f, 0.7f)
        };
        footerLabel.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(footerLabel);
    }

    private void OnGitHubPressed()
    {
        OS.ShellOpen(GitHubUrl);
    }

    private void OnDonatePressed()
    {
        OS.ShellOpen(DonateUrl);
    }

    private void OnClosePressed()
    {
        Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            Hide();
            GetViewport().SetInputAsHandled();
        }
    }
}
