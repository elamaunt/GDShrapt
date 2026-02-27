using System.Threading.Tasks;

namespace GDShrapt.LSP;

/// <summary>
/// Sends structured log messages to the LSP client via window/logMessage.
/// </summary>
public class GDLspLogger
{
    private readonly IGDJsonRpcTransport _transport;

    public GDLspLogger(IGDJsonRpcTransport transport)
    {
        _transport = transport;
    }

    /// <summary>
    /// Sends a log message notification to the client.
    /// </summary>
    public Task LogAsync(GDLspMessageType type, string message)
    {
        return _transport.SendNotificationAsync("window/logMessage", new GDLogMessageParams
        {
            Type = type,
            Message = message
        });
    }

    public Task ErrorAsync(string message) => LogAsync(GDLspMessageType.Error, message);
    public Task WarningAsync(string message) => LogAsync(GDLspMessageType.Warning, message);
    public Task InfoAsync(string message) => LogAsync(GDLspMessageType.Info, message);
    public Task DebugAsync(string message) => LogAsync(GDLspMessageType.Log, message);
}
