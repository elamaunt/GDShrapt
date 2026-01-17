namespace GDShrapt.Abstractions;

/// <summary>
/// Fix descriptor for suppressing a diagnostic with a comment.
/// Generates: # gd:ignore CODE
/// </summary>
public class GDSuppressionFixDescriptor : GDFixDescriptor
{
    /// <inheritdoc/>
    public override string Title => $"Suppress with # gd:ignore {DiagnosticCode}";

    /// <inheritdoc/>
    public override GDFixKind Kind => GDFixKind.Suppress;

    /// <summary>
    /// Target line number (1-based).
    /// </summary>
    public int TargetLine { get; set; }

    /// <summary>
    /// If true, add comment at end of line. If false, add on line above.
    /// </summary>
    public bool IsInline { get; set; }
}
