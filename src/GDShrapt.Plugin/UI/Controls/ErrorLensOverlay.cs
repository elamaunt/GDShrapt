using Godot;

namespace GDShrapt.Plugin;

/// <summary>
/// Overlay control that displays inline errors at the end of lines (Error Lens style).
/// Shows parser errors and diagnostics directly in the editor.
/// </summary>
internal partial class ErrorLensOverlay : Control
{
    private TextEdit _textEdit;
    private GDScriptFile _ScriptFile;

    private readonly List<ErrorInfo> _errors = new();
    private readonly List<Diagnostic> _lintDiagnostics = new();
    private bool _needsRefresh = true;
    private double _refreshTimer = 0;
    private const double RefreshDelay = 0.5; // Debounce delay in seconds

    // Colors for different error severities
    private static readonly Color ErrorColor = new(1.0f, 0.3f, 0.3f, 0.9f);      // Red
    private static readonly Color ErrorBgColor = new(1.0f, 0.2f, 0.2f, 0.1f);    // Light red background
    private static readonly Color WarningColor = new(1.0f, 0.8f, 0.2f, 0.9f);    // Yellow
    private static readonly Color WarningBgColor = new(1.0f, 0.8f, 0.2f, 0.1f);  // Light yellow background
    private static readonly Color InfoColor = new(0.3f, 0.7f, 1.0f, 0.9f);       // Blue
    private static readonly Color InfoBgColor = new(0.3f, 0.7f, 1.0f, 0.1f);     // Light blue background
    private static readonly Color HintColor = new(0.5f, 0.5f, 0.5f, 0.8f);       // Gray for hints
    private static readonly Color HintBgColor = new(0.5f, 0.5f, 0.5f, 0.05f);    // Light gray background

    public override void _Ready()
    {
        // Transparent, doesn't block mouse input
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>
    /// Attaches the overlay to a TextEdit control.
    /// </summary>
    public void AttachToEditor(TextEdit textEdit)
    {
        if (_textEdit != null)
        {
            Detach();
        }

        _textEdit = textEdit;

        if (_textEdit != null)
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            _textEdit.TextChanged += OnTextChanged;
            _needsRefresh = true;
        }
    }

    /// <summary>
    /// Sets the current script being edited.
    /// </summary>
    public void SetScript(GDScriptFile ScriptFile)
    {
        _ScriptFile = ScriptFile;
        _needsRefresh = true;
        QueueRedraw();
    }

    /// <summary>
    /// Detaches from the current TextEdit.
    /// </summary>
    public void Detach()
    {
        if (_textEdit != null)
        {
            _textEdit.TextChanged -= OnTextChanged;
            _textEdit = null;
        }

        _ScriptFile = null;
        _errors.Clear();
        QueueRedraw();
    }

    private void OnTextChanged()
    {
        _needsRefresh = true;
        _refreshTimer = RefreshDelay;
    }

    public override void _Process(double delta)
    {
        if (_textEdit == null || !IsVisibleInTree())
            return;

        // Debounced refresh
        if (_refreshTimer > 0)
        {
            _refreshTimer -= delta;
            if (_refreshTimer <= 0)
            {
                RefreshErrors();
            }
        }

        // Always redraw to handle scroll changes
        QueueRedraw();
    }

    private void RefreshErrors()
    {
        _errors.Clear();

        if (_ScriptFile?.Class == null)
        {
            _needsRefresh = false;
            QueueRedraw();
            return;
        }

        // Collect parser errors from the script
        CollectParserErrors();

        _needsRefresh = false;
        QueueRedraw();
    }

    private void CollectParserErrors()
    {
        // Walk the AST and find invalid tokens or nodes
        if (_ScriptFile?.Class == null)
            return;

        foreach (var token in _ScriptFile.Class.AllTokens)
        {
            // Check for invalid tokens
            if (token is GDInvalidToken invalidToken)
            {
                _errors.Add(new ErrorInfo
                {
                    Line = invalidToken.StartLine,
                    Message = $"Syntax error: unexpected token '{invalidToken}'",
                    Severity = ErrorSeverity.Error
                });
            }
        }

        // Add any parser-level errors if available
        // GDShrapt.Reader may have collected errors during parsing
        // For now we rely on invalid tokens as indicators
    }

    /// <summary>
    /// Adds a diagnostic error to display.
    /// </summary>
    public void AddError(int line, string message, ErrorSeverity severity = ErrorSeverity.Error)
    {
        _errors.Add(new ErrorInfo
        {
            Line = line,
            Message = message,
            Severity = severity
        });
        QueueRedraw();
    }

    /// <summary>
    /// Clears all displayed errors.
    /// </summary>
    public void ClearErrors()
    {
        _errors.Clear();
        _lintDiagnostics.Clear();
        QueueRedraw();
    }

    /// <summary>
    /// Sets lint diagnostics from DiagnosticService.
    /// </summary>
    public void SetDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        _lintDiagnostics.Clear();
        _lintDiagnostics.AddRange(diagnostics);
        QueueRedraw();
    }

    /// <summary>
    /// Gets combined list of all errors (parser + lint).
    /// </summary>
    private IEnumerable<ErrorInfo> GetAllErrors()
    {
        // Return parser errors first
        foreach (var error in _errors)
        {
            yield return error;
        }

        // Then lint diagnostics (converted to ErrorInfo)
        foreach (var diag in _lintDiagnostics)
        {
            yield return new ErrorInfo
            {
                Line = diag.StartLine,
                Message = $"[{diag.RuleId}] {diag.Message}",
                Severity = diag.Severity switch
                {
                    GDDiagnosticSeverity.Error => ErrorSeverity.Error,
                    GDDiagnosticSeverity.Warning => ErrorSeverity.Warning,
                    GDDiagnosticSeverity.Info => ErrorSeverity.Info,
                    GDDiagnosticSeverity.Hint => ErrorSeverity.Hint,
                    _ => ErrorSeverity.Info
                }
            };
        }
    }

    public override void _Draw()
    {
        var allErrors = GetAllErrors().ToList();
        if (_textEdit == null || allErrors.Count == 0)
            return;

        var font = ThemeDB.FallbackFont;
        var fontSize = 12;

        // Get visible line range
        int firstVisibleLine = _textEdit.GetFirstVisibleLine();
        int lastVisibleLine = firstVisibleLine + _textEdit.GetVisibleLineCount() + 1;

        // Get the line height
        float lineHeight = _textEdit.GetLineHeight();

        // Get text edit size for background rectangles
        float textEditWidth = _textEdit.Size.X;

        // Group errors by line to show only one per line (highest severity)
        var errorsByLine = allErrors
            .GroupBy(e => e.Line)
            .Select(g => g.OrderByDescending(e => e.Severity).First())
            .ToList();

        foreach (var error in errorsByLine)
        {
            int line = error.Line;

            // Skip if not visible
            if (line < firstVisibleLine || line > lastVisibleLine)
                continue;

            // Calculate Y position for this line
            // line is 0-indexed, firstVisibleLine is also 0-indexed
            float yPos = (line - firstVisibleLine) * lineHeight;

            // Get colors based on severity
            Color textColor = error.Severity switch
            {
                ErrorSeverity.Error => ErrorColor,
                ErrorSeverity.Warning => WarningColor,
                ErrorSeverity.Info => InfoColor,
                ErrorSeverity.Hint => HintColor,
                _ => ErrorColor
            };

            Color bgColor = error.Severity switch
            {
                ErrorSeverity.Error => ErrorBgColor,
                ErrorSeverity.Warning => WarningBgColor,
                ErrorSeverity.Info => InfoBgColor,
                ErrorSeverity.Hint => HintBgColor,
                _ => ErrorBgColor
            };

            // Draw background highlight for the entire line
            var lineRect = new Rect2(0, yPos, textEditWidth, lineHeight);
            DrawRect(lineRect, bgColor);

            // Get the line text to calculate where to place the error message
            string lineText = "";
            if (line >= 0 && line < _textEdit.GetLineCount())
            {
                lineText = _textEdit.GetLine(line);
            }

            // Calculate X position - after the line text (with some padding)
            float lineTextWidth = font.GetStringSize(lineText, HorizontalAlignment.Left, -1, fontSize).X;
            float gutterWidth = 60; // Approximate gutter width for line numbers

            // Position the error after the code, with some padding
            float xPos = gutterWidth + lineTextWidth + 24;

            // Ensure minimum X position
            xPos = Math.Max(xPos, 200);

            // Format: icon + message
            string icon = error.Severity switch
            {
                ErrorSeverity.Error => "●",
                ErrorSeverity.Warning => "▲",
                ErrorSeverity.Info => "ℹ",
                ErrorSeverity.Hint => "○",
                _ => "●"
            };

            string displayText = $"{icon} {error.Message}";

            // Truncate if too long
            float maxWidth = textEditWidth - xPos - 20;
            if (maxWidth > 0)
            {
                var textSize = font.GetStringSize(displayText, HorizontalAlignment.Left, -1, fontSize);
                if (textSize.X > maxWidth)
                {
                    // Truncate with ellipsis
                    while (displayText.Length > 10 && font.GetStringSize(displayText + "...", HorizontalAlignment.Left, -1, fontSize).X > maxWidth)
                    {
                        displayText = displayText.Substring(0, displayText.Length - 1);
                    }
                    displayText += "...";
                }
            }

            // Draw the error text
            float textYPos = yPos + (lineHeight + fontSize) / 2;
            DrawString(font, new Vector2(xPos, textYPos), displayText, HorizontalAlignment.Left, -1, fontSize, textColor);
        }
    }

    /// <summary>
    /// Forces a refresh of error checking.
    /// </summary>
    public void ForceRefresh()
    {
        _needsRefresh = true;
        _refreshTimer = 0;
        RefreshErrors();
    }

    private struct ErrorInfo
    {
        public int Line;
        public string Message;
        public ErrorSeverity Severity;
    }
}

/// <summary>
/// Severity level for diagnostic messages.
/// </summary>
internal enum ErrorSeverity
{
    Hint = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}
