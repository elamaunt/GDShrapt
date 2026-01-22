namespace GDShrapt.CLI.Core;

/// <summary>
/// Types of edges in the type flow graph.
/// </summary>
public enum GDTypeFlowEdgeKind
{
    /// <summary>
    /// Normal type flow (type inference propagation).
    /// </summary>
    TypeFlow,

    /// <summary>
    /// Assignment edge (value assigned to variable).
    /// </summary>
    Assignment,

    /// <summary>
    /// Union member edge (one type in a union).
    /// </summary>
    UnionMember,

    /// <summary>
    /// Duck type constraint edge (method/property requirement).
    /// </summary>
    DuckConstraint,

    /// <summary>
    /// Return value edge (method return type).
    /// </summary>
    Return
}
