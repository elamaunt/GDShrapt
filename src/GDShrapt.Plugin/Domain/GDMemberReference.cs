using GDShrapt.Reader;

namespace GDShrapt.Plugin;

/// <summary>
/// Represents a reference to a class member for displaying in dialogs.
/// </summary>
internal class GDMemberReference
{
    /// <summary>
    /// The script containing this reference.
    /// </summary>
    public GDScriptMap? Script { get; set; }

    /// <summary>
    /// The identifier token of the reference.
    /// </summary>
    public GDIdentifier? Identifier { get; set; }

    /// <summary>
    /// The class member being referenced.
    /// </summary>
    public GDClassMember? Member { get; set; }
}
