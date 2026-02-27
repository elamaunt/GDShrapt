using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

public class GDPrepareRenameParams : GDLspTextDocumentPositionParams
{
}

public class GDPrepareRenameResult
{
    [JsonPropertyName("range")]
    public GDLspRange Range { get; set; } = new();

    [JsonPropertyName("placeholder")]
    public string Placeholder { get; set; } = string.Empty;
}

public class GDRenameOptions
{
    [JsonPropertyName("prepareProvider")]
    public bool? PrepareProvider { get; set; }
}
