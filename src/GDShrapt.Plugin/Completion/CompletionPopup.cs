using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// Popup window for code completion suggestions.
/// Provides keyboard navigation and filtering.
/// </summary>
internal partial class CompletionPopup : PopupPanel
{
    private ItemList _itemList;
    private List<CompletionItem> _allItems = new();
    private List<CompletionItem> _filteredItems = new();
    private string _currentFilter = "";
    private TextEdit? _textEdit;
    private int _wordStartColumn;
    private int _triggerLine;

    /// <summary>
    /// Event fired when a completion item is selected.
    /// </summary>
    public event Action<CompletionItem>? ItemSelected;

    /// <summary>
    /// Event fired when the popup is closed without selection.
    /// </summary>
    public event Action? Cancelled;

    public CompletionPopup()
    {
        // Create the item list
        _itemList = new ItemList
        {
            AllowReselect = false,
            SelectMode = ItemList.SelectModeEnum.Single,
            CustomMinimumSize = new Vector2(300, 200),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        AddChild(_itemList);

        // Connect signals
        _itemList.ItemActivated += OnItemActivated;
        PopupHide += OnPopupHide;

        // Configure popup
        Transient = true;
        Exclusive = false;
        Unresizable = false;
        MinSize = new Vector2I(250, 150);
    }

    /// <summary>
    /// Sets the text edit control for inserting completions.
    /// </summary>
    public void SetTextEdit(TextEdit textEdit)
    {
        _textEdit = textEdit;
    }

    /// <summary>
    /// Shows the completion popup with the given items.
    /// </summary>
    public void ShowCompletions(
        IReadOnlyList<CompletionItem> items,
        Vector2 position,
        int wordStartColumn,
        int triggerLine,
        string initialFilter = "")
    {
        _allItems = items.ToList();
        _wordStartColumn = wordStartColumn;
        _triggerLine = triggerLine;
        _currentFilter = initialFilter;

        ApplyFilter();

        if (_filteredItems.Count == 0)
        {
            Logger.Debug("CompletionPopup: No matching completions");
            return;
        }

        // Position and show
        Position = (Vector2I)position;
        Popup();

        // Ensure visibility on screen
        EnsureOnScreen();

        // Select first item
        if (_filteredItems.Count > 0)
        {
            _itemList.Select(0);
            _itemList.EnsureCurrentIsVisible();
        }

        // Grab focus for keyboard input
        _itemList.GrabFocus();
    }

    /// <summary>
    /// Updates the filter and refreshes the list.
    /// </summary>
    public void UpdateFilter(string filter)
    {
        _currentFilter = filter;
        ApplyFilter();

        if (_filteredItems.Count == 0)
        {
            Hide();
            return;
        }

        // Keep first item selected
        if (_filteredItems.Count > 0)
        {
            _itemList.Select(0);
            _itemList.EnsureCurrentIsVisible();
        }
    }

    /// <summary>
    /// Handles keyboard input for navigation.
    /// </summary>
    public bool HandleKeyInput(InputEventKey keyEvent)
    {
        if (!Visible)
            return false;

        var keycode = keyEvent.Keycode;

        switch (keycode)
        {
            case Key.Up:
                MoveSelection(-1);
                return true;

            case Key.Down:
                MoveSelection(1);
                return true;

            case Key.Pageup:
                MoveSelection(-10);
                return true;

            case Key.Pagedown:
                MoveSelection(10);
                return true;

            case Key.Home:
                if (_filteredItems.Count > 0)
                {
                    _itemList.Select(0);
                    _itemList.EnsureCurrentIsVisible();
                }
                return true;

            case Key.End:
                if (_filteredItems.Count > 0)
                {
                    _itemList.Select(_filteredItems.Count - 1);
                    _itemList.EnsureCurrentIsVisible();
                }
                return true;

            case Key.Enter:
            case Key.KpEnter:
            case Key.Tab:
                AcceptSelected();
                return true;

            case Key.Escape:
                Hide();
                Cancelled?.Invoke();
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the currently selected item.
    /// </summary>
    public CompletionItem? GetSelectedItem()
    {
        var selected = _itemList.GetSelectedItems();
        if (selected.Length == 0)
            return null;

        var index = selected[0];
        if (index < 0 || index >= _filteredItems.Count)
            return null;

        return _filteredItems[index];
    }

    /// <summary>
    /// Accepts the currently selected item.
    /// </summary>
    public void AcceptSelected()
    {
        var item = GetSelectedItem();
        if (item != null)
        {
            InsertCompletion(item);
            ItemSelected?.Invoke(item);
        }

        Hide();
    }

    private void ApplyFilter()
    {
        _itemList.Clear();

        if (string.IsNullOrEmpty(_currentFilter))
        {
            _filteredItems = _allItems.ToList();
        }
        else
        {
            var lowerFilter = _currentFilter.ToLowerInvariant();
            _filteredItems = _allItems
                .Where(item =>
                {
                    var lowerLabel = item.Label.ToLowerInvariant();
                    return lowerLabel.StartsWith(lowerFilter) ||
                           lowerLabel.Contains(lowerFilter) ||
                           MatchesFuzzy(lowerLabel, lowerFilter);
                })
                .OrderBy(item =>
                {
                    var lowerLabel = item.Label.ToLowerInvariant();
                    // Exact prefix match first
                    if (lowerLabel.StartsWith(lowerFilter))
                        return 0;
                    // Contains match second
                    if (lowerLabel.Contains(lowerFilter))
                        return 1;
                    // Fuzzy match last
                    return 2;
                })
                .ThenBy(item => item.SortPriority)
                .ThenBy(item => item.Label)
                .ToList();
        }

        foreach (var item in _filteredItems)
        {
            var text = FormatItemText(item);
            var index = _itemList.AddItem(text);

            // Set icon based on kind
            var icon = GetKindIcon(item.Kind);
            if (icon != null)
            {
                _itemList.SetItemIcon(index, icon);
            }

            // Set tooltip with details
            if (!string.IsNullOrEmpty(item.Documentation))
            {
                _itemList.SetItemTooltip(index, item.Documentation);
            }
            else if (!string.IsNullOrEmpty(item.Detail))
            {
                _itemList.SetItemTooltip(index, item.Detail);
            }
        }
    }

    private string FormatItemText(CompletionItem item)
    {
        var text = item.Label;

        // Add type hint for variables/properties
        if (!string.IsNullOrEmpty(item.TypeName) && item.Kind != CompletionItemKind.Keyword)
        {
            text += $"  : {item.TypeName}";
        }

        // Add detail for methods
        if (!string.IsNullOrEmpty(item.Detail) && item.Kind == CompletionItemKind.Method)
        {
            text += $"  {item.Detail}";
        }

        return text;
    }

    private static bool MatchesFuzzy(string text, string pattern)
    {
        // Simple fuzzy matching: all characters of pattern appear in order in text
        int patternIndex = 0;
        for (int i = 0; i < text.Length && patternIndex < pattern.Length; i++)
        {
            if (text[i] == pattern[patternIndex])
                patternIndex++;
        }
        return patternIndex == pattern.Length;
    }

    private void MoveSelection(int delta)
    {
        if (_filteredItems.Count == 0)
            return;

        var selected = _itemList.GetSelectedItems();
        var currentIndex = selected.Length > 0 ? selected[0] : 0;
        var newIndex = Math.Clamp(currentIndex + delta, 0, _filteredItems.Count - 1);

        _itemList.Select(newIndex);
        _itemList.EnsureCurrentIsVisible();
    }

    private void InsertCompletion(CompletionItem item)
    {
        if (_textEdit == null)
            return;

        // Get current cursor position
        var cursorLine = _textEdit.GetCaretLine();
        var cursorColumn = _textEdit.GetCaretColumn();

        // Only insert if we're still on the same line
        if (cursorLine != _triggerLine)
        {
            Logger.Debug("CompletionPopup: Cursor moved to different line, skipping insertion");
            return;
        }

        // Calculate the text to replace (from word start to cursor)
        var lineText = _textEdit.GetLine(cursorLine);
        var replaceStart = _wordStartColumn;
        var replaceEnd = cursorColumn;

        // Build the insert text
        var insertText = item.InsertText ?? item.Label;

        // Remove the current word and insert completion
        _textEdit.BeginComplexOperation();
        try
        {
            // Select and delete the partial word
            _textEdit.Select(cursorLine, replaceStart, cursorLine, replaceEnd);
            _textEdit.DeleteSelection();

            // Insert the completion
            _textEdit.InsertTextAtCaret(insertText);

            // Handle method completions - position cursor inside parentheses
            if (item.Kind == CompletionItemKind.Method && insertText.EndsWith("()"))
            {
                var newColumn = _textEdit.GetCaretColumn();
                _textEdit.SetCaretColumn(newColumn - 1); // Position inside ()
            }
        }
        finally
        {
            _textEdit.EndComplexOperation();
        }

        Logger.Debug($"CompletionPopup: Inserted '{insertText}' at line {cursorLine}, column {replaceStart}");
    }

    private void OnItemActivated(long index)
    {
        if (index >= 0 && index < _filteredItems.Count)
        {
            var item = _filteredItems[(int)index];
            InsertCompletion(item);
            ItemSelected?.Invoke(item);
            Hide();
        }
    }

    private void OnPopupHide()
    {
        _currentFilter = "";
        _allItems.Clear();
        _filteredItems.Clear();
    }

    private void EnsureOnScreen()
    {
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

    private Texture2D? GetKindIcon(CompletionItemKind kind)
    {
        try
        {
            var editorInterface = EditorInterface.Singleton;
            var theme = editorInterface?.GetEditorTheme();
            if (theme == null)
                return null;

            var iconName = kind switch
            {
                CompletionItemKind.Method => "MemberMethod",
                CompletionItemKind.Property => "MemberProperty",
                CompletionItemKind.Variable => "MemberProperty",
                CompletionItemKind.Event => "MemberSignal",
                CompletionItemKind.Constant => "MemberConstant",
                CompletionItemKind.Class => "Object",
                CompletionItemKind.Enum => "Enum",
                CompletionItemKind.EnumMember => "MemberConstant",
                CompletionItemKind.Keyword => "Shortcut",
                CompletionItemKind.Snippet => "Script",
                _ => null
            };

            if (iconName != null && theme.HasIcon(iconName, "EditorIcons"))
            {
                return theme.GetIcon(iconName, "EditorIcons");
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"CompletionPopup: Could not get icon: {ex.Message}");
        }

        return null;
    }
}
