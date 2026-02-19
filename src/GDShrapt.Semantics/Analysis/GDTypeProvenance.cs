namespace GDShrapt.Semantics;

/// <summary>
/// How the type of a call-site argument was determined.
/// </summary>
public enum GDTypeProvenance
{
    Unknown,
    ExplicitAnnotation,
    Inferred,
    Literal,
    ReturnType,
    MemberAccess
}
