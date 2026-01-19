using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Interface for refactoring actions that can be displayed in context menus
/// and quick action popups based on cursor position and selection.
/// </summary>
internal interface IGDRefactoringAction
{
    /// <summary>
    /// Unique identifier for the action.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name shown in menus.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Category for grouping in menus.
    /// </summary>
    GDRefactoringCategory Category { get; }

    /// <summary>
    /// Keyboard shortcut (e.g., "Ctrl+Alt+M"). Null if no shortcut.
    /// </summary>
    string Shortcut { get; }

    /// <summary>
    /// Sort priority within category (lower = higher in list).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Checks if the action is available in the current context.
    /// </summary>
    /// <param name="context">The current refactoring context.</param>
    /// <returns>True if the action can be executed.</returns>
    bool IsAvailable(GDPluginRefactoringContext context);

    /// <summary>
    /// Executes the refactoring action.
    /// </summary>
    /// <param name="context">The current refactoring context.</param>
    Task ExecuteAsync(GDPluginRefactoringContext context);
}

/// <summary>
/// Categories for grouping refactoring actions in menus.
/// </summary>
internal enum GDRefactoringCategory
{
    /// <summary>
    /// Extract code into new constructs (method, constant, variable, class).
    /// </summary>
    Extract = 0,

    /// <summary>
    /// Generate new code (getter/setter, constructor, override).
    /// </summary>
    Generate = 1,

    /// <summary>
    /// Convert between equivalent constructs (if↔match, for↔while).
    /// </summary>
    Convert = 2,

    /// <summary>
    /// Surround selection with construct (if, for, try/catch).
    /// </summary>
    Surround = 3,

    /// <summary>
    /// Inline code (variable, method).
    /// </summary>
    Inline = 4,

    /// <summary>
    /// Move code (get_node→@onready, to new file).
    /// </summary>
    Move = 5,

    /// <summary>
    /// Organize code (sort members, add/remove type annotations).
    /// </summary>
    Organize = 6,

    /// <summary>
    /// Quick fixes for issues.
    /// </summary>
    QuickFix = 7
}
