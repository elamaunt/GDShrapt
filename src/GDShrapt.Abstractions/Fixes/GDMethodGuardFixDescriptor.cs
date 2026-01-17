namespace GDShrapt.Abstractions;

/// <summary>
/// Fix descriptor for adding a method existence guard.
/// Generates: if variable.has_method("methodName"):
/// </summary>
public class GDMethodGuardFixDescriptor : GDFixDescriptor
{
    private string? _customTitle;

    /// <inheritdoc/>
    public override string Title => _customTitle ?? $"Add guard: if {VariableName}.has_method(\"{MethodName}\")";

    /// <inheritdoc/>
    public override GDFixKind Kind => GDFixKind.AddMethodGuard;

    /// <summary>
    /// Name of the variable to guard.
    /// </summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>
    /// Method name to check for.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

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
    public GDMethodGuardFixDescriptor WithTitle(string title)
    {
        _customTitle = title;
        return this;
    }
}
