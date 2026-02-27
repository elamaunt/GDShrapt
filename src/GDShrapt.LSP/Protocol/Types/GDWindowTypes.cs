using System.Text.Json.Serialization;

namespace GDShrapt.LSP;

/// <summary>
/// LSP MessageType values for window/logMessage and window/showMessage.
/// </summary>
public enum GDLspMessageType
{
    Error = 1,
    Warning = 2,
    Info = 3,
    Log = 4
}

/// <summary>
/// LSP trace level for $/logTrace support.
/// </summary>
public enum GDLspTraceLevel
{
    Off,
    Messages,
    Verbose
}

/// <summary>
/// Parameters for window/logMessage notification.
/// </summary>
public class GDLogMessageParams
{
    [JsonPropertyName("type")]
    public GDLspMessageType Type { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Parameters for window/showMessage notification.
/// </summary>
public class GDShowMessageParams
{
    [JsonPropertyName("type")]
    public GDLspMessageType Type { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Parameters for $/logTrace notification.
/// </summary>
public class GDLogTraceParams
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("verbose")]
    public string? Verbose { get; set; }
}

/// <summary>
/// Parameters for $/setTrace notification.
/// </summary>
public class GDSetTraceParams
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = "off";
}
