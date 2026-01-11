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
