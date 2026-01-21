using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

/// <summary>
/// Parameters for textDocument/inlayHint request.
/// </summary>
public class GDInlayHintParams
{
    /// <summary>
    /// The text document.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public GDLspTextDocumentIdentifier TextDocument { get; set; } = new();

    /// <summary>
    /// The visible document range for which inlay hints should be computed.
    /// </summary>
    [JsonPropertyName("range")]
    public GDLspRange Range { get; set; } = new();
}

/// <summary>
/// Inlay hint information.
/// </summary>
public class GDLspInlayHint
{
    /// <summary>
    /// The position of this hint.
    /// </summary>
    [JsonPropertyName("position")]
    public GDLspPosition Position { get; set; } = new();

    /// <summary>
    /// The label of this hint. A human readable string.
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The kind of this hint. Can be omitted in which case the client
    /// should fall back to a reasonable default.
    /// </summary>
    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Kind { get; set; }

    /// <summary>
    /// Render padding before the hint.
    /// </summary>
    [JsonPropertyName("paddingLeft")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PaddingLeft { get; set; }

    /// <summary>
    /// Render padding after the hint.
    /// </summary>
    [JsonPropertyName("paddingRight")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PaddingRight { get; set; }

    /// <summary>
    /// Optional tooltip when hovering over the hint.
    /// </summary>
    [JsonPropertyName("tooltip")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tooltip { get; set; }
}

/// <summary>
/// Inlay hint kinds.
/// </summary>
public static class GDInlayHintKind
{
    /// <summary>
    /// An inlay hint that is for a type annotation.
    /// </summary>
    public const int Type = 1;

    /// <summary>
    /// An inlay hint that is for a parameter.
    /// </summary>
    public const int Parameter = 2;
}

/// <summary>
/// Inlay hint options.
/// </summary>
public class GDInlayHintOptions
{
    /// <summary>
    /// The server provides support to resolve additional information for an inlay hint item.
    /// </summary>
    [JsonPropertyName("resolveProvider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ResolveProvider { get; set; }
}
