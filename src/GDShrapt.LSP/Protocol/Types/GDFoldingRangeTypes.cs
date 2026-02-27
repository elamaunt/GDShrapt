using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

public class GDFoldingRangeParams
{
    [JsonPropertyName("textDocument")]
    public GDLspTextDocumentIdentifier TextDocument { get; set; } = new();
}

public class GDFoldingRange
{
    [JsonPropertyName("startLine")]
    public int StartLine { get; set; }

    [JsonPropertyName("startCharacter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? StartCharacter { get; set; }

    [JsonPropertyName("endLine")]
    public int EndLine { get; set; }

    [JsonPropertyName("endCharacter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EndCharacter { get; set; }

    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Kind { get; set; }

    [JsonPropertyName("collapsedText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CollapsedText { get; set; }
}
