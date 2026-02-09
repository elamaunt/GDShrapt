namespace GDShrapt.Semantics;

public class GDResourceNode
{
    public string ResourcePath { get; init; } = "";
    public string ResourceType { get; init; } = "Resource";
    public GDResourceCategory Category { get; init; }
}
