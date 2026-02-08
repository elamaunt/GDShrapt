using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a single overload of a polymorphic function with concrete parameter types.
/// </summary>
public class GDRuntimeFunctionOverload
{
    /// <summary>
    /// The concrete parameter types for this overload.
    /// </summary>
    public IReadOnlyList<GDRuntimeParameterInfo>? Parameters { get; set; }

    /// <summary>
    /// The return type for this overload.
    /// </summary>
    public string? ReturnType { get; set; }

    /// <summary>
    /// Role of return type for smart type inference.
    /// </summary>
    public string? ReturnTypeRole { get; set; }
}
