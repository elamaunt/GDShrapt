using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a naming conflict that would occur from a rename operation.
/// </summary>
public sealed class GDRenameConflict
{
    /// <summary>
    /// The name that causes the conflict.
    /// </summary>
    public string ConflictingName { get; }

    /// <summary>
    /// The existing symbol that conflicts with the new name.
    /// </summary>
    public GDSymbol? ConflictingSymbol { get; }

    /// <summary>
    /// The scope where the conflict occurs.
    /// </summary>
    public GDScope? Scope { get; }

    /// <summary>
    /// A description of the conflict.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The type of conflict.
    /// </summary>
    public GDRenameConflictType Type { get; }

    public GDRenameConflict(
        string conflictingName,
        string message,
        GDRenameConflictType type,
        GDSymbol? conflictingSymbol = null,
        GDScope? scope = null)
    {
        ConflictingName = conflictingName;
        Message = message;
        Type = type;
        ConflictingSymbol = conflictingSymbol;
        Scope = scope;
    }

    public override string ToString() => Message;
}

/// <summary>
/// Types of rename conflicts.
/// </summary>
public enum GDRenameConflictType
{
    /// <summary>
    /// A symbol with the new name already exists in the same scope.
    /// </summary>
    NameAlreadyExists,

    /// <summary>
    /// The new name would shadow an outer scope symbol.
    /// </summary>
    WouldShadow,

    /// <summary>
    /// The new name would be shadowed by an inner scope symbol.
    /// </summary>
    WouldBeShadowed,

    /// <summary>
    /// The new name is a reserved GDScript keyword.
    /// </summary>
    ReservedKeyword,

    /// <summary>
    /// The new name is a built-in type name.
    /// </summary>
    BuiltInType
}
