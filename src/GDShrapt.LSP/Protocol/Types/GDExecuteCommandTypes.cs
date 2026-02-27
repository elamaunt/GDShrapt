using System.Text.Json;
using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

/// <summary>
/// Execute command options for server capabilities.
/// </summary>
public class GDExecuteCommandOptions
{
    /// <summary>
    /// The commands the server supports.
    /// </summary>
    [JsonPropertyName("commands")]
    public string[] Commands { get; set; } = [];
}

/// <summary>
/// Execute command request parameters.
/// </summary>
public class GDExecuteCommandParams
{
    /// <summary>
    /// The identifier of the actual command handler.
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Arguments that the command should be invoked with.
    /// </summary>
    [JsonPropertyName("arguments")]
    public JsonElement[]? Arguments { get; set; }
}
