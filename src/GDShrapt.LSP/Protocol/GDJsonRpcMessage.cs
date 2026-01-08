using System.Text.Json;
using System.Text.Json.Serialization;

namespace GDShrapt.LSP.Protocol;

/// <summary>
/// Base JSON-RPC message.
/// </summary>
public class GDJsonRpcMessage
{
    /// <summary>
    /// JSON-RPC version, always "2.0".
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
}

/// <summary>
/// JSON-RPC request message.
/// </summary>
public class GDJsonRpcRequest : GDJsonRpcMessage
{
    /// <summary>
    /// The request id.
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id { get; set; }

    /// <summary>
    /// The method to be invoked.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// The method's params.
    /// </summary>
    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }
}

/// <summary>
/// JSON-RPC notification message (request without id).
/// </summary>
public class GDJsonRpcNotification : GDJsonRpcMessage
{
    /// <summary>
    /// The method to be invoked.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// The method's params.
    /// </summary>
    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }
}

/// <summary>
/// JSON-RPC response message.
/// </summary>
public class GDJsonRpcResponse : GDJsonRpcMessage
{
    /// <summary>
    /// The request id.
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id { get; set; }

    /// <summary>
    /// The result of a request.
    /// </summary>
    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; set; }

    /// <summary>
    /// The error object in case a request fails.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GDJsonRpcError? Error { get; set; }
}

/// <summary>
/// JSON-RPC error object.
/// </summary>
public class GDJsonRpcError
{
    /// <summary>
    /// A number indicating the error type.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// A string providing a short description of the error.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional information about the error.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }

    // Standard error codes
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
    public const int InternalError = -32603;
    public const int ServerNotInitialized = -32002;
    public const int UnknownErrorCode = -32001;

    // LSP specific error codes
    public const int RequestCancelled = -32800;
    public const int ContentModified = -32801;
}
