using Godot;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Universal preview dialog for refactoring operations.
/// Shows original vs result code and allows applying changes (if permitted).
/// </summary>
internal partial class RefactoringPreviewDialog : Window
{
    // UI Components
    private VBoxContainer _mainLayout;
    private Label _titleLabel;
    private HSeparator _titleSeparator;

    // Code preview panels
    private HBoxContainer _codeContainer;
    private VBoxContainer _originalPanel;
    private Label _originalLabel;
    private CodeEdit _originalCode;
    private VBoxContainer _resultPanel;
    private Label _resultLabel;
    private CodeEdit _resultCode;

    // Preview mode message panel
    private PanelContainer _proMessagePanel;
    private VBoxContainer _proMessageContainer;
    private HBoxContainer _proMessageHeader;
    private Label _previewModeBadge;
    private Label _proMessageLabel;
    private RichTextLabel _feedbackLink;

    // Buttons
    private HSeparator _buttonsSeparator;
    private HBoxContainer _buttonsLayout;
    private Control _buttonsSpacer;
    private Button _copyButton;
    private Button _cancelButton;
    private Button _applyButton;

    private TaskCompletionSource<RefactoringPreviewResult> _completion;
    private bool _canApply;

    // Constants
    private const int DialogWidth = 800;
    private const int DialogHeight = 500;

    public RefactoringPreviewDialog()
    {
        Title = "Refactoring Preview";
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
        _mainLayout.AddThemeConstantOverride("separation", 12);
        marginContainer.AddChild(_mainLayout);

        // Title
        _titleLabel = new Label
        {
            Text = "Preview Changes",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        _mainLayout.AddChild(_titleLabel);

        // Title separator
        _titleSeparator = new HSeparator();
        _mainLayout.AddChild(_titleSeparator);

        // Code container (side by side)
        _codeContainer = new HBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        _codeContainer.AddThemeConstantOverride("separation", 16);
        _mainLayout.AddChild(_codeContainer);

        // Original code panel
        _originalPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        _originalPanel.AddThemeConstantOverride("separation", 4);
        _codeContainer.AddChild(_originalPanel);

        _originalLabel = new Label
        {
            Text = "Original",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _originalLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        _originalPanel.AddChild(_originalLabel);

        _originalCode = CreateCodeEdit();
        _originalCode.Editable = false;
        _originalPanel.AddChild(_originalCode);

        // Result code panel
        _resultPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        _resultPanel.AddThemeConstantOverride("separation", 4);
        _codeContainer.AddChild(_resultPanel);

        _resultLabel = new Label
        {
            Text = "Result",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _resultLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.9f, 0.5f));
        _resultPanel.AddChild(_resultLabel);

        _resultCode = CreateCodeEdit();
        _resultCode.Editable = false;
        _resultPanel.AddChild(_resultCode);

        // Preview mode message panel (hidden by default)
        _proMessagePanel = new PanelContainer
        {
            Visible = false
        };
        var proMessageStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.25f, 0.3f, 0.9f),
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

        _proMessageContainer = new VBoxContainer();
        _proMessageContainer.AddThemeConstantOverride("separation", 4);
        _proMessagePanel.AddChild(_proMessageContainer);

        // Header with Preview Mode badge
        _proMessageHeader = new HBoxContainer();
        _proMessageHeader.AddThemeConstantOverride("separation", 8);
        _proMessageContainer.AddChild(_proMessageHeader);

        _previewModeBadge = new Label
        {
            Text = "Preview Mode",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _previewModeBadge.AddThemeColorOverride("font_color", new Color(0.4f, 0.7f, 1.0f));
        _previewModeBadge.AddThemeFontSizeOverride("font_size", 13);
        _proMessageHeader.AddChild(_previewModeBadge);

        _proMessageLabel = new Label
        {
            Text = "Apply is not available yet. Use Copy to paste the changes manually.",
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _proMessageLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        _proMessageLabel.AddThemeFontSizeOverride("font_size", 11);
        _proMessageContainer.AddChild(_proMessageLabel);

        // Feedback link
        _feedbackLink = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            SelectionEnabled = false,
            CustomMinimumSize = new Vector2(0, 20)
        };
        _feedbackLink.AddThemeFontSizeOverride("normal_font_size", 11);
        _feedbackLink.Text = "[url=https://github.com/elamaunt/GDShrapt/issues]Request this feature or report an issue[/url]";
        _feedbackLink.MetaClicked += OnFeedbackLinkClicked;
        _proMessageContainer.AddChild(_feedbackLink);

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

        // Copy button
        _copyButton = new Button
        {
            Text = "Copy Result",
            CustomMinimumSize = new Vector2(100, 0),
            TooltipText = "Copy the result code to clipboard"
        };
        _buttonsLayout.AddChild(_copyButton);

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
            Text = "Apply",
            CustomMinimumSize = new Vector2(80, 0)
        };
        _buttonsLayout.AddChild(_applyButton);

        // Apply initial size
        Size = new Vector2I(DialogWidth, DialogHeight);
    }

    private static CodeEdit CreateCodeEdit()
    {
        var codeEdit = new CodeEdit
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 200),
            GuttersDrawLineNumbers = true,
            MinimapDraw = false,
            ScrollFitContentHeight = true
        };

        // Apply GDScript-like syntax highlighting
        var highlighter = new CodeHighlighter();

        // Keywords
        highlighter.AddKeywordColor("if", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("elif", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("else", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("for", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("while", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("match", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("break", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("continue", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("pass", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("return", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("func", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("class", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("extends", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("var", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("const", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("signal", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("enum", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("static", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("and", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("or", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("not", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("in", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("is", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("as", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("self", new Color(0.9f, 0.6f, 0.4f));
        highlighter.AddKeywordColor("await", new Color(0.9f, 0.6f, 0.4f));

        // Literals
        highlighter.AddKeywordColor("true", new Color(0.6f, 0.8f, 1.0f));
        highlighter.AddKeywordColor("false", new Color(0.6f, 0.8f, 1.0f));
        highlighter.AddKeywordColor("null", new Color(0.6f, 0.8f, 1.0f));

        // Comments
        highlighter.AddColorRegion("#", "", new Color(0.5f, 0.5f, 0.5f));

        // Strings
        highlighter.AddColorRegion("\"", "\"", new Color(0.9f, 0.8f, 0.5f));
        highlighter.AddColorRegion("'", "'", new Color(0.9f, 0.8f, 0.5f));

        // Numbers
        highlighter.NumberColor = new Color(0.6f, 0.9f, 0.6f);

        // Symbols
        highlighter.SymbolColor = new Color(0.8f, 0.8f, 0.8f);

        // Function color
        highlighter.FunctionColor = new Color(0.4f, 0.7f, 1.0f);

        // Member variable color
        highlighter.MemberVariableColor = new Color(0.7f, 0.9f, 0.7f);

        codeEdit.SyntaxHighlighter = highlighter;

        return codeEdit;
    }

    private void ConnectSignals()
    {
        _copyButton.Pressed += OnCopyResult;
        _cancelButton.Pressed += OnCancelled;
        _applyButton.Pressed += OnApply;
        CloseRequested += OnCancelled;
    }

    private void OnCopyResult()
    {
        var resultText = _resultCode.Text;
        if (!string.IsNullOrEmpty(resultText))
        {
            DisplayServer.ClipboardSet(resultText);
            Logger.Info("RefactoringPreviewDialog: Result copied to clipboard");
        }
    }

    private void OnFeedbackLinkClicked(Variant meta)
    {
        var url = meta.AsString();
        if (!string.IsNullOrEmpty(url))
        {
            OS.ShellOpen(url);
        }
    }

    private void OnCancelled()
    {
        _completion?.TrySetResult(new RefactoringPreviewResult { ShouldApply = false, Cancelled = true });
        Hide();
    }

    private void OnApply()
    {
        if (_canApply)
        {
            _completion?.TrySetResult(new RefactoringPreviewResult { ShouldApply = true, Cancelled = false });
            Hide();
        }
    }

    /// <summary>
    /// Shows the preview dialog with original and result code.
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="originalCode">Original code before refactoring</param>
    /// <param name="resultCode">Result code after refactoring</param>
    /// <param name="canApply">Whether the Apply button should be enabled</param>
    /// <param name="applyButtonLabel">Custom label for the Apply button</param>
    /// <param name="proRequiredMessage">Message to show when Pro is required (null to hide)</param>
    public void ShowPreview(
        string title,
        string originalCode,
        string resultCode,
        bool canApply,
        string applyButtonLabel = "Apply",
        string proRequiredMessage = null)
    {
        _canApply = canApply;

        Title = title;
        _titleLabel.Text = title;
        _originalCode.Text = originalCode ?? string.Empty;
        _resultCode.Text = resultCode ?? string.Empty;

        _applyButton.Text = applyButtonLabel;
        _applyButton.Disabled = !canApply;

        // Show/hide Preview Mode panel
        if (!canApply)
        {
            // Use custom message if provided, otherwise default Preview Mode message
            _proMessageLabel.Text = !string.IsNullOrEmpty(proRequiredMessage)
                ? proRequiredMessage
                : "Apply is not available yet. Use Copy to paste the changes manually.";
            _proMessagePanel.Visible = true;
        }
        else
        {
            _proMessagePanel.Visible = false;
        }
    }

    /// <summary>
    /// Shows the dialog and waits for user action.
    /// </summary>
    public Task<RefactoringPreviewResult> GetResultAsync()
    {
        _completion = new TaskCompletionSource<RefactoringPreviewResult>();

        // Center on screen
        var screenSize = DisplayServer.ScreenGetSize();
        Position = new Vector2I(
            (screenSize.X - Size.X) / 2,
            (screenSize.Y - Size.Y) / 2
        );

        Popup();

        return _completion.Task;
    }

    /// <summary>
    /// Convenience method to show preview and get result in one call.
    /// </summary>
    public Task<RefactoringPreviewResult> ShowForResult(
        string title,
        string originalCode,
        string resultCode,
        bool canApply,
        string applyButtonLabel = "Apply",
        string proRequiredMessage = null)
    {
        ShowPreview(title, originalCode, resultCode, canApply, applyButtonLabel, proRequiredMessage);
        return GetResultAsync();
    }
}

/// <summary>
/// Result from the refactoring preview dialog.
/// </summary>
public class RefactoringPreviewResult
{
    /// <summary>
    /// True if user clicked Apply and can apply changes.
    /// </summary>
    public bool ShouldApply { get; set; }

    /// <summary>
    /// True if user cancelled the dialog.
    /// </summary>
    public bool Cancelled { get; set; }
}
