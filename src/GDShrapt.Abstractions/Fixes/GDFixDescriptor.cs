namespace GDShrapt.Abstractions;

/// <summary>
/// Abstract base class for code fix descriptors.
/// Describes the intent of a fix without implementation details.
/// Concrete implementations define specific fix types.
/// </summary>
public abstract class GDFixDescriptor
{
    /// <summary>
    /// Human-readable title for the fix (displayed in IDE quick-fix menu).
    /// </summary>
    public abstract string Title { get; }

    /// <summary>
    /// The kind of fix this descriptor represents.
    /// </summary>
    public abstract GDFixKind Kind { get; }

    /// <summary>
    /// The diagnostic code this fix addresses.
    /// </summary>
    public string DiagnosticCode { get; set; } = string.Empty;
}
