namespace GDShrapt.Abstractions;

/// <summary>
/// Type of dead code detected.
/// </summary>
public enum GDDeadCodeKind
{
    /// <summary>
    /// Unused variable (local or class-level).
    /// </summary>
    Variable,

    /// <summary>
    /// Unused function/method.
    /// </summary>
    Function,

    /// <summary>
    /// Unused signal (never emitted or connected).
    /// </summary>
    Signal,

    /// <summary>
    /// Unreachable code (after return, break, etc.).
    /// </summary>
    Unreachable,

    /// <summary>
    /// Unused parameter.
    /// </summary>
    Parameter,

    /// <summary>
    /// Unused constant.
    /// </summary>
    Constant,

    /// <summary>
    /// Unused enum value.
    /// </summary>
    EnumValue,

    /// <summary>
    /// Unused inner class.
    /// </summary>
    InnerClass
}
