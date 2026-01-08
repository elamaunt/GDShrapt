using GDShrapt.Plugin.Refactoring.Actions.Convert;
using GDShrapt.Plugin.Refactoring.Actions.Extract;
using GDShrapt.Plugin.Refactoring.Actions.Generate;
using GDShrapt.Plugin.Refactoring.Actions.Move;
using GDShrapt.Plugin.Refactoring.Actions.Organize;
using GDShrapt.Plugin.Refactoring.Actions.Surround;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin.Refactoring;

/// <summary>
/// Registry and provider for refactoring actions.
/// Maintains a list of all available actions and filters them based on context.
/// </summary>
internal class RefactoringActionProvider
{
    private readonly List<IRefactoringAction> _actions = new();

    public RefactoringActionProvider()
    {
        RegisterDefaultActions();
    }

    private void RegisterDefaultActions()
    {
        // Extract actions
        Register(new ExtractConstantAction());
        Register(new ExtractVariableAction());

        // Generate actions
        Register(new GenerateGetterSetterAction());

        // Convert actions
        Register(new InvertConditionAction());
        Register(new ConvertForToWhileAction());

        // Surround actions
        Register(new SurroundWithIfAction());

        // Move actions
        Register(new MoveGetNodeToOnreadyAction());

        // Organize actions
        Register(new AddTypeAnnotationAction());
    }

    /// <summary>
    /// Registers a refactoring action.
    /// </summary>
    public void Register(IRefactoringAction action)
    {
        if (action != null && !_actions.Any(a => a.Id == action.Id))
        {
            _actions.Add(action);
            Logger.Debug($"RefactoringActionProvider: Registered action '{action.Id}'");
        }
    }

    /// <summary>
    /// Unregisters a refactoring action by ID.
    /// </summary>
    public void Unregister(string actionId)
    {
        var removed = _actions.RemoveAll(a => a.Id == actionId);
        if (removed > 0)
        {
            Logger.Debug($"RefactoringActionProvider: Unregistered action '{actionId}'");
        }
    }

    /// <summary>
    /// Gets all registered actions.
    /// </summary>
    public IReadOnlyList<IRefactoringAction> AllActions => _actions.AsReadOnly();

    /// <summary>
    /// Gets all actions available in the given context, sorted by category and priority.
    /// </summary>
    public IEnumerable<IRefactoringAction> GetAvailableActions(RefactoringContext context)
    {
        if (context == null)
            return Enumerable.Empty<IRefactoringAction>();

        return _actions
            .Where(a => IsActionAvailable(a, context))
            .OrderBy(a => (int)a.Category)
            .ThenBy(a => a.Priority);
    }

    /// <summary>
    /// Gets available actions for a specific category.
    /// </summary>
    public IEnumerable<IRefactoringAction> GetActionsForCategory(
        RefactoringContext context,
        RefactoringCategory category)
    {
        return GetAvailableActions(context)
            .Where(a => a.Category == category);
    }

    /// <summary>
    /// Gets an action by its ID.
    /// </summary>
    public IRefactoringAction GetActionById(string actionId)
    {
        return _actions.FirstOrDefault(a => a.Id == actionId);
    }

    /// <summary>
    /// Gets all actions with a keyboard shortcut.
    /// </summary>
    public IEnumerable<IRefactoringAction> GetActionsWithShortcuts()
    {
        return _actions.Where(a => !string.IsNullOrEmpty(a.Shortcut));
    }

    /// <summary>
    /// Gets unique categories that have available actions.
    /// </summary>
    public IEnumerable<RefactoringCategory> GetAvailableCategories(RefactoringContext context)
    {
        return GetAvailableActions(context)
            .Select(a => a.Category)
            .Distinct()
            .OrderBy(c => (int)c);
    }

    /// <summary>
    /// Checks if any actions are available in the context.
    /// </summary>
    public bool HasAvailableActions(RefactoringContext context)
    {
        if (context == null)
            return false;

        return _actions.Any(a => IsActionAvailable(a, context));
    }

    private bool IsActionAvailable(IRefactoringAction action, RefactoringContext context)
    {
        try
        {
            return action.IsAvailable(context);
        }
        catch (System.Exception ex)
        {
            Logger.Error($"RefactoringActionProvider: Error checking availability for '{action.Id}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the display name for a category.
    /// </summary>
    public static string GetCategoryDisplayName(RefactoringCategory category)
    {
        return category switch
        {
            RefactoringCategory.Extract => "Extract",
            RefactoringCategory.Generate => "Generate",
            RefactoringCategory.Convert => "Convert",
            RefactoringCategory.Surround => "Surround With",
            RefactoringCategory.Inline => "Inline",
            RefactoringCategory.Move => "Move",
            RefactoringCategory.Organize => "Organize",
            RefactoringCategory.QuickFix => "Quick Fix",
            _ => category.ToString()
        };
    }
}
