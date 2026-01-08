namespace GDShrapt.Semantics;

/// <summary>
/// Kinds of type declarations in GDScript.
/// </summary>
public enum GDTypeKind
{
    /// <summary>
    /// A top-level class (class_name declaration).
    /// </summary>
    Class,

    /// <summary>
    /// An inner class (class keyword inside a script).
    /// </summary>
    InnerClass,

    /// <summary>
    /// An enum declaration.
    /// </summary>
    Enum
}
