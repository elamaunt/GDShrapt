using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.LSP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDSocketJsonRpcTransportTests
{
    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [TestMethod]
    public void Constructor_CreatesInstance()
    {
        // Arrange
        var serializer = new GDSystemTextJsonSerializer();
        var port = 9999;

        // Act
        var transport = new GDSocketJsonRpcTransport(serializer, port);

        // Assert
        transport.Should().NotBeNull();
    }

    [TestMethod]
    public async Task StartAsync_ListensOnPort()
    {
        // Arrange
        var serializer = new GDSystemTextJsonSerializer();
        var port = GetAvailablePort();
        var transport = new GDSocketJsonRpcTransport(serializer, port);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act - start in background, then connect as client
        var startTask = transport.StartAsync(cts.Token);

        // Give server time to start listening
        await Task.Delay(100);

        // Try to connect
        using var client = new TcpClient();
        var connected = false;

        try
        {
            await client.ConnectAsync(IPAddress.Loopback, port);
            connected = true;
        }
        catch
        {
            // Connection might fail if server isn't ready yet
        }

        // Cleanup
        cts.Cancel();
        try
        {
            await startTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        await transport.DisposeAsync();

        // Assert
        connected.Should().BeTrue("Server should be accepting connections on the specified port");
    }

    [TestMethod]
    public async Task SendNotification_AfterClientConnects_WritesMessage()
    {
        // Arrange
        var serializer = new GDSystemTextJsonSerializer();
        var port = GetAvailablePort();
        var transport = new GDSocketJsonRpcTransport(serializer, port);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Start server
        var startTask = transport.StartAsync(cts.Token);

        // Give server time to start
        await Task.Delay(100);

        // Connect client
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);

        // Wait for server to accept connection
        await Task.Delay(200);

        // Act
        await transport.SendNotificationAsync("test/notification", new { param = "value" });

        // Read from client
        var stream = client.GetStream();
        var buffer = new byte[1024];
        stream.ReadTimeout = 2000;

        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        var received = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        // Cleanup
        cts.Cancel();
        await transport.DisposeAsync();

        // Assert
        received.Should().Contain("Content-Length:");
        received.Should().Contain("test/notification");
    }

    [TestMethod]
    public async Task Transport_ReceivesRequest_InvokesHandler()
    {
        // Arrange
        var serializer = new GDSystemTextJsonSerializer();
        var port = GetAvailablePort();
        var transport = new GDSocketJsonRpcTransport(serializer, port);

        var handlerInvoked = false;
        transport.OnRequest<TestParams, TestResult>("test/request", async (p, ct) =>
        {
            handlerInvoked = true;
            return new TestResult { Value = p?.Input ?? "none" };
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Start server
        _ = transport.StartAsync(cts.Token);

        await Task.Delay(100);

        // Connect client and send request
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);

        var stream = client.GetStream();
        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        var request = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"test/request\",\"params\":{\"input\":\"hello\"}}";
        var message = $"Content-Length: {Encoding.UTF8.GetByteCount(request)}\r\n\r\n{request}";

        await writer.WriteAsync(message);

        // Give time for handler to be invoked
        await Task.Delay(500);

        // Cleanup
        cts.Cancel();
        await transport.DisposeAsync();

        // Assert
        handlerInvoked.Should().BeTrue("Handler should be invoked when receiving a request");
    }

    [TestMethod]
    public async Task StopAsync_ClosesConnection()
    {
        // Arrange
        var serializer = new GDSystemTextJsonSerializer();
        var port = GetAvailablePort();
        var transport = new GDSocketJsonRpcTransport(serializer, port);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Start server
        _ = transport.StartAsync(cts.Token);
        await Task.Delay(100);

        // Connect client
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        await Task.Delay(100);

        // Act
        await transport.StopAsync();
        await transport.DisposeAsync();

        // Assert - trying to connect again should fail
        using var newClient = new TcpClient();
        Func<Task> connectAction = async () =>
        {
            await newClient.ConnectAsync(IPAddress.Loopback, port);
        };

        await connectAction.Should().ThrowAsync<SocketException>();
    }

    private class TestParams
    {
        public string? Input { get; set; }
    }

    private class TestResult
    {
        public string? Value { get; set; }
    }
}
