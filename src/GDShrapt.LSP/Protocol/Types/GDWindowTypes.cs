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

/// <summary>
/// Parameters for window/workDoneProgress/create request.
/// </summary>
public class GDWorkDoneProgressCreateParams
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Parameters for $/progress notification.
/// </summary>
public class GDProgressParams
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public GDWorkDoneProgressValue Value { get; set; } = new();
}

/// <summary>
/// Value payload for WorkDoneProgress notifications.
/// </summary>
public class GDWorkDoneProgressValue
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "begin";

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("percentage")]
    public int? Percentage { get; set; }

    [JsonPropertyName("cancellable")]
    public bool? Cancellable { get; set; }
}
