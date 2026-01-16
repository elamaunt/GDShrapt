namespace GDShrapt.Abstractions;

/// <summary>
/// Type of scope in the scope stack.
/// </summary>
public enum GDScopeType
{
    /// <summary>
    /// Global/file scope.
    /// </summary>
    Global,

    /// <summary>
    /// Class scope.
    /// </summary>
    Class,

    /// <summary>
    /// Method/function scope.
    /// </summary>
    Method,

    /// <summary>
    /// For loop scope.
    /// </summary>
    ForLoop,

    /// <summary>
    /// While loop scope.
    /// </summary>
    WhileLoop,

    /// <summary>
    /// If/elif/else block scope.
    /// </summary>
    Conditional,

    /// <summary>
    /// Match statement scope.
    /// </summary>
    Match,

    /// <summary>
    /// Lambda/anonymous function scope.
    /// </summary>
    Lambda,

    /// <summary>
    /// Generic block scope.
    /// </summary>
    Block
}
