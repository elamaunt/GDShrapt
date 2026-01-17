namespace GDShrapt.Abstractions;

/// <summary>
/// Generic fix descriptor for text edits (insert, replace, or remove).
/// </summary>
public class GDTextEditFixDescriptor : GDFixDescriptor
{
    private string _title = string.Empty;
    private GDFixKind _kind = GDFixKind.InsertText;

    /// <inheritdoc/>
    public override string Title => _title;

    /// <inheritdoc/>
    public override GDFixKind Kind => _kind;

    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Start column (0-based).
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// End column (0-based). Same as StartColumn for pure insertion.
    /// </summary>
    public int EndColumn { get; set; }

    /// <summary>
    /// Text to insert. Empty string for removal.
    /// </summary>
    public string NewText { get; set; } = string.Empty;

    /// <summary>
    /// Sets the title for this fix.
    /// </summary>
    public GDTextEditFixDescriptor WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    /// Sets the kind for this fix.
    /// </summary>
    public GDTextEditFixDescriptor WithKind(GDFixKind kind)
    {
        _kind = kind;
        return this;
    }

    /// <summary>
    /// Creates an insertion fix descriptor.
    /// </summary>
    public static GDTextEditFixDescriptor Insert(string title, int line, int column, string text)
    {
        return new GDTextEditFixDescriptor
        {
            _title = title,
            _kind = GDFixKind.InsertText,
            Line = line,
            StartColumn = column,
            EndColumn = column,
            NewText = text
        };
    }

    /// <summary>
    /// Creates a removal fix descriptor.
    /// </summary>
    public static GDTextEditFixDescriptor Remove(string title, int line, int startColumn, int endColumn)
    {
        return new GDTextEditFixDescriptor
        {
            _title = title,
            _kind = GDFixKind.RemoveText,
            Line = line,
            StartColumn = startColumn,
            EndColumn = endColumn,
            NewText = string.Empty
        };
    }

    /// <summary>
    /// Creates a replacement fix descriptor.
    /// </summary>
    public static GDTextEditFixDescriptor Replace(string title, int line, int startColumn, int endColumn, string newText)
    {
        return new GDTextEditFixDescriptor
        {
            _title = title,
            _kind = GDFixKind.ReplaceIdentifier,
            Line = line,
            StartColumn = startColumn,
            EndColumn = endColumn,
            NewText = newText
        };
    }
}
