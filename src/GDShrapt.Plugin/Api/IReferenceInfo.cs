using GDShrapt.Reader;

namespace GDShrapt.Plugin.Api;

/// <summary>
/// Information about a single reference to a symbol.
/// </summary>
public interface IReferenceInfo
{
    /// <summary>
    /// Path to the file containing the reference.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Line number (0-based).
    /// </summary>
    int Line { get; }

    /// <summary>
    /// Column number (0-based).
    /// </summary>
    int Column { get; }

    /// <summary>
    /// Context line of code.
    /// </summary>
    string ContextLine { get; }

    /// <summary>
    /// Kind of reference.
    /// </summary>
    ReferenceKind Kind { get; }

    /// <summary>
    /// The identifier node from AST.
    /// For advanced use cases requiring direct AST access.
    /// </summary>
    GDIdentifier? Identifier { get; }
}

/// <summary>
/// Kind of symbol reference.
/// </summary>
public enum ReferenceKind
{
    /// <summary>Symbol is being read.</summary>
    Read,
    /// <summary>Symbol is being written/assigned.</summary>
    Write,
    /// <summary>Symbol is being called as a function.</summary>
    Call,
    /// <summary>Symbol declaration.</summary>
    Declaration
}
