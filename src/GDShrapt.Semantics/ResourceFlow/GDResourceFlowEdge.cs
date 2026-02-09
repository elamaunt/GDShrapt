using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

public class GDResourceFlowEdge
{
    public string ConsumerPath { get; init; } = "";
    public string ResourcePath { get; init; } = "";
    public GDResourceReferenceSource Source { get; init; }
    public GDTypeConfidence Confidence { get; init; }
    public string? SourceFile { get; init; }
    public int LineNumber { get; init; }
    public string? NodePath { get; init; }
    public string? PropertyName { get; init; }
    public string? VariableName { get; init; }
    public bool IsConditional { get; init; }
}
