using GDShrapt.Reader;

namespace GDShrapt.Plugin;

/// <summary>
/// Points to a specific code location (script + identifier).
/// Used for navigation and go-to-definition features.
/// </summary>
internal class GDCodePointer
{
    /// <summary>
    /// Reference to the script containing the declaration.
    /// </summary>
    public GDPluginScriptReference? ScriptReference { get; set; }

    /// <summary>
    /// The declaration identifier at this location.
    /// </summary>
    public GDIdentifier? DeclarationIdentifier { get; set; }
}
