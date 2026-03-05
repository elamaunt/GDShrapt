using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

/// <summary>
/// Parameters for textDocument/codeLens request.
/// </summary>
public class GDCodeLensParams
{
    /// <summary>
    /// The text document.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public GDLspTextDocumentIdentifier TextDocument { get; set; } = new();
}

/// <summary>
/// A code lens represents a command that should be shown along with source text.
/// </summary>
public class GDLspCodeLens
{
    /// <summary>
    /// The range in which this code lens is valid. Should only span a single line.
    /// </summary>
    [JsonPropertyName("range")]
    public GDLspRange Range { get; set; } = new();

    /// <summary>
    /// The command this code lens represents.
    /// </summary>
    [JsonPropertyName("command")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GDLspCommand? Command { get; set; }
}

/// <summary>
/// Code lens options.
/// </summary>
public class GDCodeLensOptions
{
    /// <summary>
    /// Code lens has a resolve provider as well.
    /// </summary>
    [JsonPropertyName("resolveProvider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ResolveProvider { get; set; }
}
