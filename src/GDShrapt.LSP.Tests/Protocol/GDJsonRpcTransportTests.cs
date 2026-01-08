using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.LSP.Protocol;
using GDShrapt.LSP.Transport.Serialization;
using GDShrapt.LSP.Transport.Stdio;
using Xunit;

namespace GDShrapt.LSP.Tests.Protocol;

public class GDJsonRpcTransportTests
{
    [Fact]
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
        Assert.Contains("\"jsonrpc\":\"2.0\"", json);
        Assert.Contains("\"id\":\"1\"", json);
        Assert.Contains("\"test\":\"value\"", json);
    }

    [Fact]
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
        Assert.Contains("Content-Length:", written);
        Assert.Contains("test/method", written);
    }

    [Fact]
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
        Assert.Contains("\"id\":\"123\"", written);
        Assert.Contains("result", written);
    }

    [Fact]
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
        Assert.Contains("\"id\":\"456\"", written);
        Assert.Contains("\"error\"", written);
        Assert.Contains("-32600", written);
        Assert.Contains("Invalid Request", written);
    }
}
