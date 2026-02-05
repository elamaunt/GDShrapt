using GDShrapt.Reader;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Information about a reference to a symbol.
/// </summary>
public class ReferenceInfo
{
    /// <summary>
    /// The name of the symbol being referenced.
    /// </summary>
    public string SymbolName { get; set; } = string.Empty;

    /// <summary>
    /// The kind of reference (read, write, call, etc.).
    /// </summary>
    public ReferenceKind Kind { get; set; }

    /// <summary>
    /// The file containing the reference.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// The line number of the reference (1-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// The column number of the reference (1-based).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// The AST node of the reference.
    /// </summary>
    public GDNode? Node { get; set; }

    /// <summary>
    /// The containing scope (method name, class name, etc.).
    /// </summary>
    public string? ContainingScope { get; set; }

    public override string ToString()
    {
        return $"{SymbolName} ({Kind}) at {FilePath}:{Line}:{Column}";
    }
}
