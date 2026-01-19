using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// Popup menu for quick actions (Ctrl+.).
/// Shows available refactoring actions based on cursor position and selection.
/// </summary>
internal partial class QuickActionsPopup : PopupMenu
{
    private GDRefactoringActionProvider _provider;
    private GDPluginRefactoringContext _currentContext;
    private List<IGDRefactoringAction> _currentActions = new();

    public QuickActionsPopup()
    {
        IdPressed += OnItemPressed;
    }

    /// <summary>
    /// Sets the action provider.
    /// </summary>
    public void SetProvider(GDRefactoringActionProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Shows the popup with available actions for the given context.
    /// </summary>
    public void ShowActions(GDPluginRefactoringContext context, Vector2 position)
    {
        _currentContext = context;
        Clear();

        if (_provider == null)
        {
            Logger.Info("QuickActionsPopup: No provider set");
            AddItem("No actions available", -1);
            SetItemDisabled(0, true);
            ShowAtPosition(position);
            return;
        }

        _currentActions = _provider.GetAvailableActions(context).ToList();

        if (!_currentActions.Any())
        {
            Logger.Info("QuickActionsPopup: No actions available for context");
            AddItem("No actions available", -1);
            SetItemDisabled(0, true);
        }
        else
        {
            Logger.Info($"QuickActionsPopup: Found {_currentActions.Count} available actions");
            PopulateMenu();
        }

        ShowAtPosition(position);
    }

    private void PopulateMenu()
    {
        GDRefactoringCategory? lastCategory = null;
        var index = 0;

        foreach (var action in _currentActions)
        {
            // Add separator between categories
            if (lastCategory != null && lastCategory != action.Category)
            {
                AddSeparator();
            }

            // Build display text with shortcut
            var text = action.DisplayName;
            if (!string.IsNullOrEmpty(action.Shortcut))
            {
                text += $"  ({action.Shortcut})";
            }

            AddItem(text, index);

            // Set icon based on category
            var icon = GetCategoryIcon(action.Category);
            if (icon != null)
            {
                SetItemIcon(index, icon);
            }

            lastCategory = action.Category;
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
        if (index < 0 || index >= _currentActions.Count)
        {
            Logger.Info($"QuickActionsPopup: Invalid index {index}");
            return;
        }

        var action = _currentActions[index];
        Logger.Info($"QuickActionsPopup: Executing action '{action.Id}'");

        // Execute the action asynchronously
        _ = ExecuteActionAsync(action);
    }

    private async System.Threading.Tasks.Task ExecuteActionAsync(IGDRefactoringAction action)
    {
        try
        {
            await action.ExecuteAsync(_currentContext);
        }
        catch (Exception ex)
        {
            Logger.Error($"QuickActionsPopup: Error executing action '{action.Id}': {ex.Message}");
        }
    }

    private Texture2D GetCategoryIcon(GDRefactoringCategory category)
    {
        // Return null for now - icons can be added later
        // Could use EditorInterface.GetEditorTheme() to get themed icons
        return null;
    }

    /// <summary>
    /// Shows the popup with a "No actions available" message.
    /// Used when context cannot be built.
    /// </summary>
    public void ShowNoActionsMessage(Vector2 position)
    {
        Clear();
        _currentContext = null;
        _currentActions.Clear();

        Logger.Info("QuickActionsPopup: Showing no actions message");
        AddItem("No actions available", -1);
        SetItemDisabled(0, true);
        ShowAtPosition(position);
    }

    /// <summary>
    /// Gets the display name for a category.
    /// </summary>
    public static string GetCategoryDisplayName(GDRefactoringCategory category)
    {
        return category switch
        {
            GDRefactoringCategory.Extract => "Extract",
            GDRefactoringCategory.Generate => "Generate",
            GDRefactoringCategory.Convert => "Convert",
            GDRefactoringCategory.Surround => "Surround With",
            GDRefactoringCategory.Inline => "Inline",
            GDRefactoringCategory.Move => "Move",
            GDRefactoringCategory.Organize => "Organize",
            GDRefactoringCategory.QuickFix => "Quick Fix",
            _ => category.ToString()
        };
    }
}
