namespace GDShrapt.Semantics.Tests.Helpers;

/// <summary>
/// Type of reference to a symbol.
/// </summary>
public enum ReferenceKind
{
    /// <summary>
    /// Declaration of a symbol (var x, func foo, signal bar).
    /// </summary>
    Declaration,

    /// <summary>
    /// Read access to a variable/member (x in "print(x)").
    /// </summary>
    Read,

    /// <summary>
    /// Write access to a variable/member (x in "x = 1" or "x += 1").
    /// </summary>
    Write,

    /// <summary>
    /// Method/function call (foo in "foo()").
    /// </summary>
    Call,

    /// <summary>
    /// Signal emission (signal.emit()).
    /// </summary>
    SignalEmit,

    /// <summary>
    /// Signal connection (signal.connect()).
    /// </summary>
    SignalConnect,

    /// <summary>
    /// Type annotation (x: MyType).
    /// </summary>
    TypeAnnotation,

    /// <summary>
    /// Type check (x is MyType).
    /// </summary>
    TypeCheck,

    /// <summary>
    /// Base class reference (extends MyClass).
    /// </summary>
    Extends,

    /// <summary>
    /// super() call reference.
    /// </summary>
    SuperCall,

    /// <summary>
    /// Unknown reference type.
    /// </summary>
    Unknown
}
