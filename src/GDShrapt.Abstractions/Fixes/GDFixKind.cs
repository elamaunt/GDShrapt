namespace GDShrapt.Abstractions;

/// <summary>
/// Kinds of code fixes available for diagnostics.
/// </summary>
public enum GDFixKind
{
    /// <summary>
    /// Suppress diagnostic with # gd:ignore CODE comment.
    /// </summary>
    Suppress,

    /// <summary>
    /// Add type guard: if obj is Type:
    /// </summary>
    AddTypeGuard,

    /// <summary>
    /// Add method existence guard: if obj.has_method("name"):
    /// </summary>
    AddMethodGuard,

    /// <summary>
    /// Fix a typo in identifier name.
    /// </summary>
    FixTypo,

    /// <summary>
    /// Declare a missing variable.
    /// </summary>
    DeclareVariable,

    /// <summary>
    /// Generic identifier replacement.
    /// </summary>
    ReplaceIdentifier,

    /// <summary>
    /// Insert text at position.
    /// </summary>
    InsertText,

    /// <summary>
    /// Remove text range.
    /// </summary>
    RemoveText
}
