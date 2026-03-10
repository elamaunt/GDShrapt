using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.LSP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDJsonRpcTransportTests
{
    private static string BuildJsonRpcMessage(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        return $"Content-Length: {bytes.Length}\r\n\r\n{json}";
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Transport_CancelRequest_CancelsRunningHandler()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var output = new StringWriter();

        var request = BuildJsonRpcMessage("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"test/slow\",\"params\":{}}");
        var cancelNotification = BuildJsonRpcMessage("{\"jsonrpc\":\"2.0\",\"method\":\"$/cancelRequest\",\"params\":{\"id\":1}}");
        var input = new StringReader(request + cancelNotification);

        await using var transport = new GDStdioJsonRpcTransport(serializer, input, output);

        var handlerCancelled = new TaskCompletionSource<bool>();

        transport.OnRequest<object, object>("test/slow", async (_, ct) =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                handlerCancelled.TrySetResult(false);
                return null;
            }
            catch (OperationCanceledException)
            {
                handlerCancelled.TrySetResult(true);
                throw;
            }
        });

        await transport.StartAsync(CancellationToken.None);

        var wasCancelled = await handlerCancelled.Task;
        wasCancelled.Should().BeTrue("handler should have been cancelled by $/cancelRequest");

        await Task.Delay(100);
        var written = output.ToString();
        written.Should().Contain("-32800", "response should contain RequestCancelled error code");
    }

    [TestMethod]
    public void Serialize_InitializeResult_ProducesValidJson()
    {
        // Arrange
        var serializer = new GDSystemTextJsonSerializer();
        var result = new GDJsonRpcResponse
        {
            Id = "1",
            Result = new { test = "value" }
        };

        // Act
        var json = serializer.Serialize(result);

        // Assert
        json.Should().Contain("\"jsonrpc\":\"2.0\"");
        json.Should().Contain("\"id\":\"1\"");
        json.Should().Contain("\"test\":\"value\"");
    }

    [TestMethod]
    public async Task Transport_SendNotification_WritesContentLengthHeader()
    {
        // Arrange
        var serializer = new GDSystemTextJsonSerializer();
        var output = new StringWriter();
        var input = new StringReader("");

        var transport = new GDStdioJsonRpcTransport(serializer, input, output);

        // Act
        await transport.SendNotificationAsync("test/method", new { param = "value" });

        // Assert
        var written = output.ToString();
        written.Should().Contain("Content-Length:");
        written.Should().Contain("test/method");
    }

    [TestMethod]
    public async Task Transport_SendResponse_WritesValidJson()
    {
        // Arrange
        var serializer = new GDSystemTextJsonSerializer();
        var output = new StringWriter();
        var input = new StringReader("");

        var transport = new GDStdioJsonRpcTransport(serializer, input, output);

        // Act
        await transport.SendResponseAsync("123", new { result = "success" });

        // Assert
        var written = output.ToString();
        written.Should().Contain("\"id\":\"123\"");
        written.Should().Contain("result");
    }

    [TestMethod]
    public async Task Transport_SendError_WritesErrorResponse()
    {
        // Arrange
        var serializer = new GDSystemTextJsonSerializer();
        var output = new StringWriter();
        var input = new StringReader("");

        var transport = new GDStdioJsonRpcTransport(serializer, input, output);

        // Act
        await transport.SendErrorAsync("456", -32600, "Invalid Request");

        // Assert
        var written = output.ToString();
        written.Should().Contain("\"id\":\"456\"");
        written.Should().Contain("\"error\"");
        written.Should().Contain("-32600");
        written.Should().Contain("Invalid Request");
    }
}
