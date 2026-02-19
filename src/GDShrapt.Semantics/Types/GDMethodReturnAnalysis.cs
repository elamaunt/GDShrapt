using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Result of analyzing return types for a method.
/// Provides information about all return paths, their types, and consistency.
/// </summary>
public class GDMethodReturnAnalysis
{
    /// <summary>
    /// Declared return type (from -> annotation), null if none.
    /// </summary>
    public GDSemanticType? DeclaredReturnType { get; init; }

    /// <summary>
    /// All return paths in the method.
    /// </summary>
    public IReadOnlyList<GDReturnPathInfo> ReturnPaths { get; init; } = [];

    /// <summary>
    /// Whether any path is an implicit return (fallthrough without explicit return).
    /// </summary>
    public bool HasImplicitReturn => ReturnPaths.Any(r => r.IsImplicit);

    /// <summary>
    /// Whether return paths have inconsistent types (2+ non-null types that are not assignable to each other).
    /// </summary>
    public bool HasInconsistentTypes { get; init; }

    /// <summary>
    /// Union of all return types observed across all paths.
    /// </summary>
    public GDUnionType ReturnUnionType { get; init; } = new();
}

/// <summary>
/// Information about a single return path in a method.
/// </summary>
public class GDReturnPathInfo
{
    /// <summary>
    /// The inferred type of the return expression. Null for void return or implicit return.
    /// </summary>
    public GDSemanticType? InferredType { get; init; }

    /// <summary>
    /// Line number (0-based) of the return statement.
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Whether this is an implicit return (method ends without explicit return).
    /// </summary>
    public bool IsImplicit { get; init; }

    /// <summary>
    /// The branch context (e.g., "if", "else", "for loop > if").
    /// </summary>
    public string? BranchContext { get; init; }

    /// <summary>
    /// Whether the type inference is high confidence.
    /// </summary>
    public bool IsHighConfidence { get; init; }
}
