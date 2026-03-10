namespace GDShrapt.Abstractions;

/// <summary>
/// Confidence level of a reference.
/// </summary>
public enum GDReferenceConfidence
{
    /// <summary>
    /// Strict confidence - type is known and resolved.
    /// </summary>
    Strict,

    /// <summary>
    /// Union confidence - proven reference but variable has union type sharing with other types.
    /// Member exists in some but not all types of the union.
    /// </summary>
    Union,

    /// <summary>
    /// Potential confidence - duck-typed or type narrowed.
    /// </summary>
    Potential,

    /// <summary>
    /// Name match only - no type information available.
    /// </summary>
    NameMatch
}
