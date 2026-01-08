using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a pointer to a specific code location (identifier declaration).
/// </summary>
public class GDCodePointer
{
    /// <summary>
    /// Reference to the script containing the declaration.
    /// </summary>
    public GDScriptReference? ScriptReference { get; set; }

    /// <summary>
    /// The identifier at the declaration site.
    /// </summary>
    public GDIdentifier? DeclarationIdentifier { get; set; }

    /// <summary>
    /// Line number (1-based) of the declaration.
    /// </summary>
    public int Line => DeclarationIdentifier?.StartLine ?? 0;

    /// <summary>
    /// Column number (0-based) of the declaration.
    /// </summary>
    public int Column => DeclarationIdentifier?.StartColumn ?? 0;

    public override string ToString()
    {
        var location = DeclarationIdentifier != null
            ? $":{DeclarationIdentifier.StartLine}:{DeclarationIdentifier.StartColumn}"
            : "";
        return $"{ScriptReference}{location}";
    }
}
