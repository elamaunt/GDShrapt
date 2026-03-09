namespace GDShrapt.Abstractions;

/// <summary>
/// Lightweight record of a single assignment observation in flow analysis.
/// Accumulated in GDFlowVariableType.AssignmentHistory to preserve full assignment
/// history across SSA-style type replacements.
/// </summary>
public readonly struct GDFlowAssignmentRecord
{
    public GDSemanticType Type { get; }
    public GDTypeOriginKind Kind { get; }
    public GDTypeOriginConfidence Confidence { get; }
    public int Line { get; }
    public int Column { get; }

    public GDFlowAssignmentRecord(GDSemanticType type, GDTypeOriginKind kind, GDTypeOriginConfidence confidence, int line, int column)
    {
        Type = type;
        Kind = kind;
        Confidence = confidence;
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        return $"[{Confidence}] {Kind}: {Type.DisplayName} at {Line + 1}:{Column + 1}";
    }
}
