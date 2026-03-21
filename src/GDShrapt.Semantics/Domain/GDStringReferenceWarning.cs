using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Warning about a string reference that was statically resolved but cannot be auto-edited
/// (e.g. string built via concatenation).
/// </summary>
public sealed record GDStringReferenceWarning(
    string MemberName,
    GDNode Node,
    string Reason);
