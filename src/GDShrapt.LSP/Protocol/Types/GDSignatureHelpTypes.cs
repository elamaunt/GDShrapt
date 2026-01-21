using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

/// <summary>
/// SignatureHelp request parameters.
/// </summary>
public class GDSignatureHelpParams : GDLspTextDocumentPositionParams
{
    /// <summary>
    /// The signature help context.
    /// </summary>
    [JsonPropertyName("context")]
    public GDSignatureHelpContext? Context { get; set; }
}

/// <summary>
/// Additional information about the context in which a signature help request was triggered.
/// </summary>
public class GDSignatureHelpContext
{
    /// <summary>
    /// Action that caused signature help to be triggered.
    /// </summary>
    [JsonPropertyName("triggerKind")]
    public GDSignatureHelpTriggerKind TriggerKind { get; set; }

    /// <summary>
    /// Character that caused signature help to be triggered.
    /// </summary>
    [JsonPropertyName("triggerCharacter")]
    public string? TriggerCharacter { get; set; }

    /// <summary>
    /// True if signature help was already showing when it was triggered.
    /// </summary>
    [JsonPropertyName("isRetrigger")]
    public bool IsRetrigger { get; set; }

    /// <summary>
    /// The currently active SignatureHelp.
    /// </summary>
    [JsonPropertyName("activeSignatureHelp")]
    public GDLspSignatureHelp? ActiveSignatureHelp { get; set; }
}

/// <summary>
/// How a signature help was triggered.
/// </summary>
public enum GDSignatureHelpTriggerKind
{
    /// <summary>
    /// Signature help was invoked manually by the user or by a command.
    /// </summary>
    Invoked = 1,

    /// <summary>
    /// Signature help was triggered by a trigger character.
    /// </summary>
    TriggerCharacter = 2,

    /// <summary>
    /// Signature help was triggered by the cursor moving or by the document content changing.
    /// </summary>
    ContentChange = 3
}

/// <summary>
/// Signature help represents the signature of something callable.
/// </summary>
public class GDLspSignatureHelp
{
    /// <summary>
    /// One or more signatures.
    /// </summary>
    [JsonPropertyName("signatures")]
    public GDLspSignatureInformation[] Signatures { get; set; } = [];

    /// <summary>
    /// The active signature.
    /// </summary>
    [JsonPropertyName("activeSignature")]
    public int? ActiveSignature { get; set; }

    /// <summary>
    /// The active parameter of the active signature.
    /// </summary>
    [JsonPropertyName("activeParameter")]
    public int? ActiveParameter { get; set; }
}

/// <summary>
/// Represents the signature of something callable.
/// </summary>
public class GDLspSignatureInformation
{
    /// <summary>
    /// The label of this signature. Will be shown in the UI.
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The human-readable doc-comment of this signature.
    /// </summary>
    [JsonPropertyName("documentation")]
    public object? Documentation { get; set; } // string or GDLspMarkupContent

    /// <summary>
    /// The parameters of this signature.
    /// </summary>
    [JsonPropertyName("parameters")]
    public GDLspParameterInformation[]? Parameters { get; set; }

    /// <summary>
    /// The index of the active parameter.
    /// </summary>
    [JsonPropertyName("activeParameter")]
    public int? ActiveParameter { get; set; }
}

/// <summary>
/// Represents a parameter of a callable-signature.
/// </summary>
public class GDLspParameterInformation
{
    /// <summary>
    /// The label of this parameter information.
    /// Either a string or an inclusive start and exclusive end offset within its containing signature label.
    /// </summary>
    [JsonPropertyName("label")]
    public object Label { get; set; } = string.Empty; // string or [int, int]

    /// <summary>
    /// The human-readable doc-comment of this parameter.
    /// </summary>
    [JsonPropertyName("documentation")]
    public object? Documentation { get; set; } // string or GDLspMarkupContent
}

/// <summary>
/// Server capabilities for signature help.
/// </summary>
public class GDSignatureHelpOptions
{
    /// <summary>
    /// The characters that trigger signature help automatically.
    /// </summary>
    [JsonPropertyName("triggerCharacters")]
    public string[]? TriggerCharacters { get; set; }

    /// <summary>
    /// List of characters that re-trigger signature help.
    /// </summary>
    [JsonPropertyName("retriggerCharacters")]
    public string[]? RetriggerCharacters { get; set; }
}
