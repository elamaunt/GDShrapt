using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.LSP;

/// <summary>
/// JSON-RPC transport over TCP socket.
/// Uses Content-Length header as per LSP specification.
/// </summary>
public class GDSocketJsonRpcTransport : IGDJsonRpcTransport
{
    private readonly IGDMessageSerializer _serializer;
    private readonly int _port;
    private readonly ConcurrentDictionary<string, RequestHandler> _requestHandlers = new();
    private readonly ConcurrentDictionary<string, NotificationHandler> _notificationHandlers = new();
    private readonly ConcurrentDictionary<object, TaskCompletionSource<JsonElement?>> _pendingRequests = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private int _requestId;
    private bool _disposed;

    /// <summary>
    /// Creates a new socket transport that listens on the specified port.
    /// </summary>
    /// <param name="serializer">The message serializer.</param>
    /// <param name="port">The TCP port to listen on.</param>
    public GDSocketJsonRpcTransport(IGDMessageSerializer serializer, int port)
    {
        _serializer = serializer;
        _port = port;
    }

    /// <summary>
    /// Starts the transport by listening for a client connection.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _listener = new TcpListener(IPAddress.Loopback, _port);
        _listener.Start();

        Console.Error.WriteLine($"LSP server listening on port {_port}...");

        try
        {
            _client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            Console.Error.WriteLine("Client connected.");

            _stream = _client.GetStream();
            _reader = new StreamReader(_stream, Encoding.UTF8);
            _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

            _readTask = Task.Run(() => ReadLoopAsync(_cts.Token), _cts.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancelled during accept, that's fine
        }
    }

    /// <summary>
    /// Stops the transport and closes all connections.
    /// </summary>
    public async Task StopAsync()
    {
        _cts?.Cancel();

        if (_readTask != null)
        {
            try
            {
                await _readTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _writer?.Dispose();
        _reader?.Dispose();
        _stream?.Dispose();
        _client?.Dispose();
        _listener?.Stop();
    }

    public async Task<TResult?> SendRequestAsync<TParams, TResult>(string method, TParams parameters, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _requestId).ToString();
        var tcs = new TaskCompletionSource<JsonElement?>();
        _pendingRequests[id] = tcs;

        try
        {
            var request = new GDJsonRpcRequest
            {
                Id = id,
                Method = method,
                Params = parameters != null ? JsonSerializer.SerializeToElement(parameters) : null
            };

            await WriteMessageAsync(request).ConfigureAwait(false);

            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
            var result = await tcs.Task.ConfigureAwait(false);

            if (result == null)
                return default;

            return result.Value.Deserialize<TResult>();
        }
        finally
        {
            _pendingRequests.TryRemove(id, out _);
        }
    }

    public Task SendNotificationAsync<TParams>(string method, TParams parameters)
    {
        var notification = new
        {
            jsonrpc = "2.0",
            method,
            @params = parameters
        };

        return WriteMessageAsync(notification);
    }

    public Task SendResponseAsync(object? id, object? result)
    {
        var response = new GDJsonRpcResponse
        {
            Id = id,
            Result = result
        };

        return WriteMessageAsync(response);
    }

    public Task SendErrorAsync(object? id, int code, string message, object? data = null)
    {
        var response = new GDJsonRpcResponse
        {
            Id = id,
            Error = new GDJsonRpcError
            {
                Code = code,
                Message = message,
                Data = data
            }
        };

        return WriteMessageAsync(response);
    }

    public void OnRequest<TParams, TResult>(string method, Func<TParams, CancellationToken, Task<TResult?>> handler)
    {
        _requestHandlers[method] = new RequestHandler(
            async (paramsJson, ct) =>
            {
                var @params = paramsJson != null
                    ? paramsJson.Value.Deserialize<TParams>()
                    : default;
                return await handler(@params!, ct).ConfigureAwait(false);
            });
    }

    public void OnNotification<TParams>(string method, Func<TParams, Task> handler)
    {
        _notificationHandlers[method] = new NotificationHandler(
            async (paramsJson) =>
            {
                var @params = paramsJson != null
                    ? paramsJson.Value.Deserialize<TParams>()
                    : default;
                await handler(@params!).ConfigureAwait(false);
            });
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var message = await ReadMessageAsync(cancellationToken).ConfigureAwait(false);
                if (message == null)
                    break;

                _ = ProcessMessageAsync(message, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException)
            {
                // Connection closed
                break;
            }
            catch (Exception)
            {
                // Log error but continue reading
            }
        }
    }

    private async Task<string?> ReadMessageAsync(CancellationToken cancellationToken)
    {
        if (_reader == null)
            return null;

        // Read headers
        var headers = new System.Collections.Generic.Dictionary<string, string>();
        string? line;

        while ((line = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            if (string.IsNullOrEmpty(line))
                break;

            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = line.Substring(0, colonIndex).Trim();
                var value = line.Substring(colonIndex + 1).Trim();
                headers[key] = value;
            }
        }

        if (line == null)
            return null;

        // Get content length
        if (!headers.TryGetValue("Content-Length", out var contentLengthStr) ||
            !int.TryParse(contentLengthStr, out var contentLength))
        {
            return null;
        }

        // Read content
        var buffer = new char[contentLength];
        var totalRead = 0;

        while (totalRead < contentLength)
        {
            var read = await _reader.ReadAsync(buffer, totalRead, contentLength - totalRead).ConfigureAwait(false);
            if (read == 0)
                return null;
            totalRead += read;
        }

        return new string(buffer);
    }

    private async Task ProcessMessageAsync(string json, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check if it's a response (has id but no method)
            if (root.TryGetProperty("id", out var idElement) && !root.TryGetProperty("method", out _))
            {
                await HandleResponseAsync(root, idElement).ConfigureAwait(false);
                return;
            }

            // It's a request or notification
            if (!root.TryGetProperty("method", out var methodElement))
                return;

            var method = methodElement.GetString() ?? string.Empty;
            JsonElement? paramsElement = root.TryGetProperty("params", out var pe) ? pe : null;

            // Check if it's a request (has id) or notification (no id)
            if (root.TryGetProperty("id", out idElement))
            {
                await HandleRequestAsync(method, paramsElement, idElement, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await HandleNotificationAsync(method, paramsElement).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing message: {ex.Message}");
        }
    }

    private Task HandleResponseAsync(JsonElement root, JsonElement idElement)
    {
        var id = idElement.ValueKind == JsonValueKind.Number
            ? idElement.GetInt32().ToString()
            : idElement.GetString() ?? string.Empty;

        if (_pendingRequests.TryRemove(id, out var tcs))
        {
            if (root.TryGetProperty("error", out var errorElement))
            {
                var message = errorElement.TryGetProperty("message", out var msgElement)
                    ? msgElement.GetString() ?? "Unknown error"
                    : "Unknown error";
                tcs.SetException(new Exception(message));
            }
            else if (root.TryGetProperty("result", out var resultElement))
            {
                tcs.SetResult(resultElement.Clone());
            }
            else
            {
                tcs.SetResult(null);
            }
        }

        return Task.CompletedTask;
    }

    private async Task HandleRequestAsync(string method, JsonElement? paramsElement, JsonElement idElement, CancellationToken cancellationToken)
    {
        var id = idElement.ValueKind == JsonValueKind.Number
            ? (object)idElement.GetInt32()
            : idElement.GetString() ?? string.Empty;

        if (!_requestHandlers.TryGetValue(method, out var handler))
        {
            await SendErrorAsync(id, GDJsonRpcError.MethodNotFound, $"Method not found: {method}").ConfigureAwait(false);
            return;
        }

        try
        {
            var result = await handler.Handler(paramsElement, cancellationToken).ConfigureAwait(false);
            await SendResponseAsync(id, result).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await SendErrorAsync(id, GDJsonRpcError.InternalError, ex.Message).ConfigureAwait(false);
        }
    }

    private async Task HandleNotificationAsync(string method, JsonElement? paramsElement)
    {
        if (!_notificationHandlers.TryGetValue(method, out var handler))
            return;

        try
        {
            await handler.Handler(paramsElement).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Notifications don't get responses
        }
    }

    private async Task WriteMessageAsync(object message)
    {
        if (_writer == null)
            return;

        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var json = _serializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);

            await _writer.WriteAsync($"Content-Length: {bytes.Length}\r\n\r\n").ConfigureAwait(false);
            await _writer.WriteAsync(json).ConfigureAwait(false);
            await _writer.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await StopAsync().ConfigureAwait(false);
        _cts?.Dispose();
        _writeLock.Dispose();
    }

    private class RequestHandler
    {
        public Func<JsonElement?, CancellationToken, Task<object?>> Handler { get; }

        public RequestHandler(Func<JsonElement?, CancellationToken, Task<object?>> handler)
        {
            Handler = handler;
        }
    }

    private class NotificationHandler
    {
        public Func<JsonElement?, Task> Handler { get; }

        public NotificationHandler(Func<JsonElement?, Task> handler)
        {
            Handler = handler;
        }
    }
}
