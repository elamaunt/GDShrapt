using System;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.LSP.Protocol;

/// <summary>
/// Abstraction for JSON-RPC transport layer.
/// Implementations can use stdio, sockets, or any other transport.
/// </summary>
public interface IGDJsonRpcTransport : IAsyncDisposable
{
    /// <summary>
    /// Starts the transport (begin reading messages).
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the transport.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Sends a request and waits for a response.
    /// </summary>
    Task<TResult?> SendRequestAsync<TParams, TResult>(string method, TParams parameters, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a notification (no response expected).
    /// </summary>
    Task SendNotificationAsync<TParams>(string method, TParams parameters);

    /// <summary>
    /// Sends a response to a request.
    /// </summary>
    Task SendResponseAsync(object? id, object? result);

    /// <summary>
    /// Sends an error response to a request.
    /// </summary>
    Task SendErrorAsync(object? id, int code, string message, object? data = null);

    /// <summary>
    /// Registers a handler for incoming requests.
    /// </summary>
    void OnRequest<TParams, TResult>(string method, Func<TParams, CancellationToken, Task<TResult?>> handler);

    /// <summary>
    /// Registers a handler for incoming notifications.
    /// </summary>
    void OnNotification<TParams>(string method, Func<TParams, Task> handler);
}
