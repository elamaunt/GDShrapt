namespace GDShrapt.Semantics;

/// <summary>
/// Suggests how a union type parameter could be narrowed if
/// an overly-broad explicit annotation were updated.
/// </summary>
public class GDUnionNarrowingHint
{
    public string WiderType { get; init; } = "";
    public string NarrowType { get; init; } = "";
    public string SourceVariable { get; init; } = "";
    public string SourceFilePath { get; set; } = "";
    public int SourceLine { get; set; }
}
