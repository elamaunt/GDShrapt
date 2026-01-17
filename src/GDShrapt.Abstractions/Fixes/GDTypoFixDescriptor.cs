namespace GDShrapt.Abstractions;

/// <summary>
/// Fix descriptor for correcting a typo in an identifier.
/// Replaces misspelled name with suggested correction.
/// </summary>
public class GDTypoFixDescriptor : GDFixDescriptor
{
    private string? _customTitle;

    /// <inheritdoc/>
    public override string Title => _customTitle ?? $"Did you mean '{SuggestedName}'?";

    /// <inheritdoc/>
    public override GDFixKind Kind => GDFixKind.FixTypo;

    /// <summary>
    /// The original (misspelled) identifier name.
    /// </summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// The suggested correct name.
    /// </summary>
    public string SuggestedName { get; set; } = string.Empty;

    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Start column (0-based).
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// End column (0-based).
    /// </summary>
    public int EndColumn { get; set; }

    /// <summary>
    /// Sets a custom title for the fix.
    /// </summary>
    public GDTypoFixDescriptor WithTitle(string title)
    {
        _customTitle = title;
        return this;
    }
}
