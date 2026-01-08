using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a reference to a class member (method, variable, signal, etc.).
/// </summary>
public class GDMemberReference
{
    /// <summary>
    /// Reference to the script containing this member.
    /// </summary>
    public GDScriptReference? ScriptReference { get; set; }

    /// <summary>
    /// The identifier token referencing the member.
    /// </summary>
    public GDIdentifier? Identifier { get; set; }

    /// <summary>
    /// The class member declaration (if resolved).
    /// </summary>
    public GDClassMember? Member { get; set; }

    /// <summary>
    /// Line number of the reference.
    /// </summary>
    public int Line => Identifier?.StartLine ?? 0;

    /// <summary>
    /// Column number of the reference.
    /// </summary>
    public int Column => Identifier?.StartColumn ?? 0;

    public override string ToString()
    {
        var name = Identifier?.Sequence ?? Member?.ToString() ?? "(unknown)";
        return $"{name} at {ScriptReference}:{Line}:{Column}";
    }
}
