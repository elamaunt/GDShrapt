namespace GDShrapt.CLI.Core;

/// <summary>
/// Types of nodes in the type flow graph.
/// </summary>
public enum GDTypeFlowNodeKind
{
    /// <summary>
    /// A function parameter.
    /// </summary>
    Parameter,

    /// <summary>
    /// A local variable within a function.
    /// </summary>
    LocalVariable,

    /// <summary>
    /// A class member variable (field).
    /// </summary>
    MemberVariable,

    /// <summary>
    /// A method or function call expression.
    /// </summary>
    MethodCall,

    /// <summary>
    /// A return value from a method.
    /// </summary>
    ReturnValue,

    /// <summary>
    /// An assignment expression.
    /// </summary>
    Assignment,

    /// <summary>
    /// An explicit type annotation.
    /// </summary>
    TypeAnnotation,

    /// <summary>
    /// A member inherited from a base class.
    /// </summary>
    InheritedMember,

    /// <summary>
    /// A built-in Godot type.
    /// </summary>
    BuiltinType,

    /// <summary>
    /// A literal value (string, int, etc.).
    /// </summary>
    Literal,

    /// <summary>
    /// An indexer access expression (e.g., result["key"], array[i]).
    /// </summary>
    IndexerAccess,

    /// <summary>
    /// A property access expression (e.g., result.property, not a method call).
    /// </summary>
    PropertyAccess,

    /// <summary>
    /// A type check expression (e.g., x is Dictionary).
    /// </summary>
    TypeCheck,

    /// <summary>
    /// A null check expression (e.g., x == null, x != null).
    /// </summary>
    NullCheck,

    /// <summary>
    /// A comparison expression (e.g., x == y, x != y, x &lt; y) that is not a null check.
    /// </summary>
    Comparison,

    /// <summary>
    /// Unknown or generic source.
    /// </summary>
    Unknown
}
