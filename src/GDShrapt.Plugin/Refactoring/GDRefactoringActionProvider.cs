using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// Registry and provider for refactoring actions.
/// Maintains a list of all available actions and filters them based on context.
/// </summary>
internal class GDRefactoringActionProvider
{
    private readonly List<IGDRefactoringAction> _actions = new();

    public GDRefactoringActionProvider()
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
    public void Register(IGDRefactoringAction action)
    {
        if (action != null && !_actions.Any(a => a.Id == action.Id))
        {
            _actions.Add(action);
            Logger.Debug($"GDRefactoringActionProvider: Registered action '{action.Id}'");
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
            Logger.Debug($"GDRefactoringActionProvider: Unregistered action '{actionId}'");
        }
    }

    /// <summary>
    /// Gets all registered actions.
    /// </summary>
    public IReadOnlyList<IGDRefactoringAction> AllActions => _actions.AsReadOnly();

    /// <summary>
    /// Gets all actions available in the given context, sorted by category and priority.
    /// </summary>
    public IEnumerable<IGDRefactoringAction> GetAvailableActions(GDPluginRefactoringContext context)
    {
        if (context == null)
            return Enumerable.Empty<IGDRefactoringAction>();

        return _actions
            .Where(a => IsActionAvailable(a, context))
            .OrderBy(a => (int)a.Category)
            .ThenBy(a => a.Priority);
    }

    /// <summary>
    /// Gets available actions for a specific category.
    /// </summary>
    public IEnumerable<IGDRefactoringAction> GetActionsForCategory(
        GDPluginRefactoringContext context,
        GDRefactoringCategory category)
    {
        return GetAvailableActions(context)
            .Where(a => a.Category == category);
    }

    /// <summary>
    /// Gets an action by its ID.
    /// </summary>
    public IGDRefactoringAction GetActionById(string actionId)
    {
        return _actions.FirstOrDefault(a => a.Id == actionId);
    }

    /// <summary>
    /// Gets all actions with a keyboard shortcut.
    /// </summary>
    public IEnumerable<IGDRefactoringAction> GetActionsWithShortcuts()
    {
        return _actions.Where(a => !string.IsNullOrEmpty(a.Shortcut));
    }

    /// <summary>
    /// Gets unique categories that have available actions.
    /// </summary>
    public IEnumerable<GDRefactoringCategory> GetAvailableCategories(GDPluginRefactoringContext context)
    {
        return GetAvailableActions(context)
            .Select(a => a.Category)
            .Distinct()
            .OrderBy(c => (int)c);
    }

    /// <summary>
    /// Checks if any actions are available in the context.
    /// </summary>
    public bool HasAvailableActions(GDPluginRefactoringContext context)
    {
        if (context == null)
            return false;

        return _actions.Any(a => IsActionAvailable(a, context));
    }

    private bool IsActionAvailable(IGDRefactoringAction action, GDPluginRefactoringContext context)
    {
        try
        {
            return action.IsAvailable(context);
        }
        catch (System.Exception ex)
        {
            Logger.Error($"GDRefactoringActionProvider: Error checking availability for '{action.Id}': {ex.Message}");
            return false;
        }
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
