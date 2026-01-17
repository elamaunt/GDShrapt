namespace GDShrapt.Abstractions;

/// <summary>
/// Fix descriptor for adding a type guard.
/// Generates: if variable is TypeName:
/// </summary>
public class GDTypeGuardFixDescriptor : GDFixDescriptor
{
    private string? _customTitle;

    /// <inheritdoc/>
    public override string Title => _customTitle ?? $"Add type guard: if {VariableName} is {TypeName}";

    /// <inheritdoc/>
    public override GDFixKind Kind => GDFixKind.AddTypeGuard;

    /// <summary>
    /// Name of the variable to guard.
    /// </summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>
    /// Type name to check against.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Line number of the statement to wrap (1-based).
    /// </summary>
    public int StatementLine { get; set; }

    /// <summary>
    /// Current indentation level (number of tabs/indent units).
    /// </summary>
    public int IndentLevel { get; set; }

    /// <summary>
    /// Sets a custom title for the fix.
    /// </summary>
    public GDTypeGuardFixDescriptor WithTitle(string title)
    {
        _customTitle = title;
        return this;
    }
}
