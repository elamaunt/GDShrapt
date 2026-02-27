using System;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.LSP;

/// <summary>
/// Interface for the GDScript language server.
/// </summary>
public interface IGDLanguageServer : IAsyncDisposable
{
    /// <summary>
    /// Initializes the server with the given transport.
    /// </summary>
    Task InitializeAsync(IGDJsonRpcTransport transport, CancellationToken cancellationToken);

    /// <summary>
    /// Runs the server until shutdown is requested.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets whether the server has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets whether shutdown has been requested.
    /// </summary>
    bool IsShuttingDown { get; }

    /// <summary>
    /// Attempts to send a critical error message to the client via window/showMessage.
    /// Safe to call even before initialization — will not throw.
    /// </summary>
    Task TryShowErrorAsync(string message);
}
