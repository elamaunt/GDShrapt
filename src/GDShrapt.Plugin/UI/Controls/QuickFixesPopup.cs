namespace GDShrapt.Plugin;

/// <summary>
/// Popup menu for quick fixes (Ctrl+.).
/// Shows available code fixes for diagnostics at the cursor position.
/// </summary>
internal partial class QuickFixesPopup : PopupMenu
{
    private GDQuickFixHandler? _handler;
    private GDScriptFile? _currentScript;
    private List<GDQuickFixItem> _currentFixes = new();
    private TextEdit? _textEdit;
    private string? _currentSourceCode;

    /// <summary>
    /// Event fired when a fix is applied and the source code is modified.
    /// </summary>
    public event Action<string>? FixApplied;

    public QuickFixesPopup()
    {
        IdPressed += OnItemPressed;
    }

    /// <summary>
    /// Sets the quick fix handler.
    /// </summary>
    public void SetHandler(GDQuickFixHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Sets the text edit control for applying fixes.
    /// </summary>
    public void SetTextEdit(TextEdit textEdit)
    {
        _textEdit = textEdit;
    }

    /// <summary>
    /// Shows the popup with available fixes at the cursor position.
    /// </summary>
    /// <param name="script">The script reference.</param>
    /// <param name="line">Cursor line (0-based).</param>
    /// <param name="column">Cursor column (0-based).</param>
    /// <param name="sourceCode">Current source code.</param>
    /// <param name="position">Screen position for the popup.</param>
    public void ShowFixes(GDScriptFile script, int line, int column, string sourceCode, Vector2 position)
    {
        _currentScript = script;
        _currentSourceCode = sourceCode;
        Clear();

        if (_handler == null)
        {
            Logger.Info("QuickFixesPopup: No handler set");
            AddItem("No fixes available", -1);
            SetItemDisabled(0, true);
            ShowAtPosition(position);
            return;
        }

        // Get fixes at cursor position first, fall back to line if none found
        _currentFixes = _handler.GetFixesAtPosition(script, line, column).ToList();

        if (_currentFixes.Count == 0)
        {
            _currentFixes = _handler.GetFixesOnLine(script, line).ToList();
        }

        if (_currentFixes.Count == 0)
        {
            Logger.Info("QuickFixesPopup: No fixes available at cursor");
            AddItem("No fixes available", -1);
            SetItemDisabled(0, true);
        }
        else
        {
            Logger.Info($"QuickFixesPopup: Found {_currentFixes.Count} available fixes");
            PopulateMenu();
        }

        ShowAtPosition(position);
    }

    /// <summary>
    /// Shows all fixes for the current script.
    /// </summary>
    public void ShowAllFixes(GDScriptFile script, string sourceCode, Vector2 position)
    {
        _currentScript = script;
        _currentSourceCode = sourceCode;
        Clear();

        if (_handler == null)
        {
            AddItem("No fixes available", -1);
            SetItemDisabled(0, true);
            ShowAtPosition(position);
            return;
        }

        _currentFixes = _handler.GetAllFixes(script).ToList();

        if (_currentFixes.Count == 0)
        {
            AddItem("No fixes available", -1);
            SetItemDisabled(0, true);
        }
        else
        {
            PopulateMenu();

            // Add "Fix All" option if there are multiple fixes
            if (_currentFixes.Count > 1)
            {
                AddSeparator();
                AddItem($"Fix All ({_currentFixes.Count} issues)", -2);
            }
        }

        ShowAtPosition(position);
    }

    private void PopulateMenu()
    {
        GDDiagnosticSeverity? lastSeverity = null;
        var index = 0;

        foreach (var fixItem in _currentFixes)
        {
            // Add separator between severity levels
            if (lastSeverity != null && lastSeverity != fixItem.GDPluginDiagnostic.Severity)
            {
                AddSeparator();
            }

            // Build display text with rule ID and location
            var text = $"{GetSeverityPrefix(fixItem.GDPluginDiagnostic.Severity)} {fixItem.DisplayTitle}";

            // Add location hint for multi-line view
            if (_currentFixes.Select(f => f.GDPluginDiagnostic.StartLine).Distinct().Count() > 1)
            {
                text += $" (line {fixItem.GDPluginDiagnostic.StartLine})";
            }

            AddItem(text, index);

            // Set icon based on severity
            var icon = GetSeverityIcon(fixItem.GDPluginDiagnostic.Severity);
            if (icon != null)
            {
                SetItemIcon(index, icon);
            }

            lastSeverity = fixItem.GDPluginDiagnostic.Severity;
            index++;
        }
    }

    private void ShowAtPosition(Vector2 position)
    {
        Position = (Vector2I)position;
        Popup();

        // Ensure popup is visible on screen
        var screenSize = GetTree().Root.GetVisibleRect().Size;
        var popupSize = Size;

        var newPos = Position;
        if (newPos.X + popupSize.X > screenSize.X)
            newPos = new Vector2I((int)(screenSize.X - popupSize.X - 10), newPos.Y);
        if (newPos.Y + popupSize.Y > screenSize.Y)
            newPos = new Vector2I(newPos.X, (int)(screenSize.Y - popupSize.Y - 10));
        if (newPos.X < 0) newPos = new Vector2I(10, newPos.Y);
        if (newPos.Y < 0) newPos = new Vector2I(newPos.X, 10);

        Position = newPos;
    }

    private void OnItemPressed(long id)
    {
        var index = (int)id;

        // Handle "Fix All" option
        if (index == -2)
        {
            ApplyAllFixes();
            return;
        }

        if (index < 0 || index >= _currentFixes.Count)
        {
            Logger.Info($"QuickFixesPopup: Invalid index {index}");
            return;
        }

        var fixItem = _currentFixes[index];
        Logger.Info($"QuickFixesPopup: Applying fix '{fixItem.DisplayTitle}'");

        ApplyFix(fixItem);
    }

    private void ApplyFix(GDQuickFixItem fixItem)
    {
        if (_handler == null || _currentSourceCode == null)
            return;

        try
        {
            var newCode = _handler.ApplyFix(fixItem.Fix, _currentSourceCode);

            if (newCode != _currentSourceCode)
            {
                UpdateTextEdit(newCode);
                Logger.Info($"QuickFixesPopup: Fix applied successfully");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"QuickFixesPopup: Error applying fix: {ex.Message}");
        }
    }

    private void ApplyAllFixes()
    {
        if (_handler == null || _currentSourceCode == null)
            return;

        try
        {
            var newCode = _handler.ApplyFixes(_currentFixes, _currentSourceCode);

            if (newCode != _currentSourceCode)
            {
                UpdateTextEdit(newCode);
                Logger.Info($"QuickFixesPopup: Applied {_currentFixes.Count} fixes");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"QuickFixesPopup: Error applying fixes: {ex.Message}");
        }
    }

    private void UpdateTextEdit(string newCode)
    {
        if (_textEdit == null)
        {
            // Fallback: fire event for external handling
            FixApplied?.Invoke(newCode);
            return;
        }

        // Remember cursor position
        var cursorLine = _textEdit.GetCaretLine();
        var cursorCol = _textEdit.GetCaretColumn();

        // Update text
        _textEdit.Text = newCode;
        _currentSourceCode = newCode;

        // Restore cursor position (clamped to valid range)
        var lineCount = _textEdit.GetLineCount();
        cursorLine = Math.Min(cursorLine, lineCount - 1);
        var lineLength = _textEdit.GetLine(cursorLine).Length;
        cursorCol = Math.Min(cursorCol, lineLength);

        _textEdit.SetCaretLine(cursorLine);
        _textEdit.SetCaretColumn(cursorCol);

        // Fire event
        FixApplied?.Invoke(newCode);
    }

    private static string GetSeverityPrefix(GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDDiagnosticSeverity.Error => "[E]",
            GDDiagnosticSeverity.Warning => "[W]",
            GDDiagnosticSeverity.Info => "[I]",
            GDDiagnosticSeverity.Hint => "[H]",
            _ => "[ ]"
        };
    }

    private Texture2D? GetSeverityIcon(GDDiagnosticSeverity severity)
    {
        // Try to get themed icons from editor
        try
        {
            var editorInterface = EditorInterface.Singleton;
            var theme = editorInterface?.GetEditorTheme();
            if (theme == null)
                return null;

            var iconName = severity switch
            {
                GDDiagnosticSeverity.Error => "StatusError",
                GDDiagnosticSeverity.Warning => "StatusWarning",
                GDDiagnosticSeverity.Info => "Popup",
                GDDiagnosticSeverity.Hint => "Info",
                _ => null
            };

            if (iconName != null && theme.HasIcon(iconName, "EditorIcons"))
            {
                return theme.GetIcon(iconName, "EditorIcons");
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"QuickFixesPopup: Could not get icon: {ex.Message}");
        }

        return null;
    }
}
