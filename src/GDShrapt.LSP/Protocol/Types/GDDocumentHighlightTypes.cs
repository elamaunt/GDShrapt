using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

public class GDDocumentHighlightParams : GDLspTextDocumentPositionParams
{
}

public enum GDDocumentHighlightKind
{
    Text = 1,
    Read = 2,
    Write = 3
}

public class GDDocumentHighlight
{
    [JsonPropertyName("range")]
    public GDLspRange Range { get; set; } = new();

    [JsonPropertyName("kind")]
    public GDDocumentHighlightKind? Kind { get; set; }
}
