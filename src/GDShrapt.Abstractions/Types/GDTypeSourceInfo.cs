namespace GDShrapt.Abstractions;

/// <summary>
/// Source location information for a type defined in the project.
/// </summary>
public class GDTypeSourceInfo
{
    public string? FilePath { get; init; }
    public int Line { get; init; }
    public int StartColumn { get; init; }
    public int EndColumn { get; init; }
    public string TypeName { get; init; } = "";
}
