namespace GDShrapt.Abstractions;

/// <summary>
/// Kind of symbol in the scope.
/// </summary>
public enum GDSymbolKind
{
    /// <summary>
    /// A variable declared with var.
    /// </summary>
    Variable,

    /// <summary>
    /// A constant declared with const.
    /// </summary>
    Constant,

    /// <summary>
    /// A function/method parameter.
    /// </summary>
    Parameter,

    /// <summary>
    /// A function or method.
    /// </summary>
    Method,

    /// <summary>
    /// A signal declaration.
    /// </summary>
    Signal,

    /// <summary>
    /// A class declaration.
    /// </summary>
    Class,

    /// <summary>
    /// An enum declaration.
    /// </summary>
    Enum,

    /// <summary>
    /// An enum value.
    /// </summary>
    EnumValue,

    /// <summary>
    /// A property (get/set).
    /// </summary>
    Property,

    /// <summary>
    /// A for loop iterator variable.
    /// </summary>
    Iterator
}
