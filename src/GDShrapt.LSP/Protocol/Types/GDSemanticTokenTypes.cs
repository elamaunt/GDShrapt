using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

/// <summary>
/// Parameters for textDocument/semanticTokens/full request.
/// </summary>
public class GDSemanticTokensParams
{
    [JsonPropertyName("textDocument")]
    public GDLspTextDocumentIdentifier TextDocument { get; set; } = new();
}

/// <summary>
/// Semantic tokens response.
/// </summary>
public class GDSemanticTokens
{
    [JsonPropertyName("data")]
    public int[] Data { get; set; } = [];
}

/// <summary>
/// Semantic tokens legend — defines token types and modifiers.
/// </summary>
public class GDSemanticTokensLegend
{
    [JsonPropertyName("tokenTypes")]
    public string[] TokenTypes { get; set; } = [];

    [JsonPropertyName("tokenModifiers")]
    public string[] TokenModifiers { get; set; } = [];
}

/// <summary>
/// Semantic tokens provider options for server capabilities.
/// </summary>
public class GDSemanticTokensOptions
{
    [JsonPropertyName("legend")]
    public GDSemanticTokensLegend Legend { get; set; } = new();

    [JsonPropertyName("full")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Full { get; set; }

    [JsonPropertyName("range")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Range { get; set; }
}
