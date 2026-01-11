using Godot;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Generic dialog for entering a name (constant, variable, etc.).
/// </summary>
internal partial class NameInputDialog : ConfirmationDialog
{
    private LineEdit _nameEdit;
    private Label _previewLabel;
    private Label _errorLabel;
    private TaskCompletionSource<string> _completion;
    private static readonly Regex ValidIdentifierRegex = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    // GDScript reserved keywords
    private static readonly string[] ReservedKeywords =
    {
        "if", "elif", "else", "for", "while", "match", "break", "continue",
        "pass", "return", "class", "class_name", "extends", "is", "as",
        "self", "signal", "func", "static", "const", "enum", "var",
        "onready", "export", "setget", "tool", "yield", "assert", "preload",
        "await", "in", "not", "and", "or", "true", "false", "null",
        "PI", "TAU", "INF", "NAN", "super"
    };

    public NameInputDialog()
    {
        GetCancelButton().Pressed += OnCancelled;
        GetOkButton().Pressed += OnOk;
        Canceled += OnCancelled;
        Confirmed += OnOk;

        var vbox = new VBoxContainer();
        AddChild(vbox);

        _nameEdit = new LineEdit
        {
            CustomMinimumSize = new Vector2(280, 0)
        };
        _nameEdit.TextSubmitted += _ => OnOk();
        _nameEdit.TextChanged += OnNameChanged;
        vbox.AddChild(_nameEdit);

        _errorLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Visible = false
        };
        _errorLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.4f, 0.4f));
        vbox.AddChild(_errorLabel);

        _previewLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _previewLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        vbox.AddChild(_previewLabel);

        Size = new Vector2I(320, 140);
    }

    private void OnNameChanged(string newText)
    {
        ValidateInput(newText);
        UpdatePreview();
    }

    private void ValidateInput(string text)
    {
        var (isValid, errorMessage) = ValidateName(text);

        _errorLabel.Text = errorMessage ?? string.Empty;
        _errorLabel.Visible = !string.IsNullOrEmpty(errorMessage);

        // Update OK button state
        GetOkButton().Disabled = !isValid;

        // Update input field color
        if (isValid)
        {
            _nameEdit.RemoveThemeColorOverride("font_color");
        }
        else
        {
            _nameEdit.AddThemeColorOverride("font_color", new Color(1.0f, 0.6f, 0.6f));
        }
    }

    /// <summary>
    /// Validates a GDScript identifier name.
    /// </summary>
    private (bool isValid, string errorMessage) ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Name cannot be empty");

        name = name.Trim();

        // Check for valid identifier format
        if (!ValidIdentifierRegex.IsMatch(name))
        {
            if (char.IsDigit(name[0]))
                return (false, "Name cannot start with a digit");

            return (false, "Name contains invalid characters");
        }

        // Check for reserved keywords
        var lowerName = name.ToLowerInvariant();
        foreach (var keyword in ReservedKeywords)
        {
            if (lowerName == keyword.ToLowerInvariant())
                return (false, $"'{keyword}' is a reserved keyword");
        }

        // Check length
        if (name.Length > 100)
            return (false, "Name is too long (max 100 characters)");

        return (true, null);
    }

    private void UpdatePreview()
    {
        // Override in subclasses if needed
    }

    private void OnOk()
    {
        var name = _nameEdit.Text?.Trim();

        var (isValid, _) = ValidateName(name);
        if (!isValid)
        {
            // Don't close if invalid
            return;
        }

        if (!string.IsNullOrEmpty(name))
        {
            Logger.Info($"NameInputDialog: Confirmed with '{name}'");
            _completion?.TrySetResult(name);
            Hide();
        }
    }

    private void OnCancelled()
    {
        Logger.Info("NameInputDialog: Cancelled");
        _completion?.TrySetResult(null);
    }

    /// <summary>
    /// Shows the dialog and returns the entered name, or null if cancelled.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="defaultName">Default name to show.</param>
    /// <param name="preview">Optional preview text.</param>
    public Task<string> ShowForResult(string title, string defaultName, string preview = null)
    {
        _completion = new TaskCompletionSource<string>();

        Title = title;
        _nameEdit.Text = defaultName;
        _previewLabel.Text = preview ?? string.Empty;
        _previewLabel.Visible = !string.IsNullOrEmpty(preview);

        // Validate initial value
        ValidateInput(defaultName ?? string.Empty);

        Popup();

        _nameEdit.SelectAll();
        _nameEdit.GrabFocus();
        _nameEdit.CaretColumn = defaultName?.Length ?? 0;

        return _completion.Task;
    }
}
