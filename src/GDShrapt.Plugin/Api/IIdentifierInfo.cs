namespace GDShrapt.Plugin.Api;

/// <summary>
/// Information about an identifier usage in code.
/// </summary>
public interface IIdentifierInfo
{
    /// <summary>
    /// The identifier name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Line number (0-based).
    /// </summary>
    int Line { get; }

    /// <summary>
    /// Column number (0-based).
    /// </summary>
    int Column { get; }

    /// <summary>
    /// Inferred type if available.
    /// </summary>
    string? InferredType { get; }

    /// <summary>
    /// Whether this is a declaration.
    /// </summary>
    bool IsDeclaration { get; }
}
