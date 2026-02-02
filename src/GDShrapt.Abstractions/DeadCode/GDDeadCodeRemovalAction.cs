namespace GDShrapt.Abstractions;

/// <summary>
/// Action to take when removing dead code (Pro feature).
/// </summary>
public enum GDDeadCodeRemovalAction
{
    /// <summary>
    /// Delete the dead code completely.
    /// </summary>
    Delete,

    /// <summary>
    /// Comment out the dead code (preserves for review).
    /// </summary>
    Comment,

    /// <summary>
    /// Add underscore prefix to mark as intentionally unused.
    /// Follows GDScript convention: _unused_var.
    /// </summary>
    AddUnderscorePrefix,

    /// <summary>
    /// Inline the function body at all call sites and remove the function.
    /// Pro feature with restrictions: no yield, no recursion, limited call sites.
    /// </summary>
    Inline
}
