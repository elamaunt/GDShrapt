using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDLspTraceTests
{
    private static string BuildJsonRpcMessage(string json)
    {
        var bytes = Encoding.UTF8.GetByteCount(json);
        return $"Content-Length: {bytes}\r\n\r\n{json}";
    }

    [TestMethod]
    public async Task Initialize_WithTraceMessages_RespondsSuccessfully()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var initRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"processId\":null,\"capabilities\":{},\"trace\":\"messages\"}}");
        var shutdownRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":99,\"method\":\"shutdown\",\"params\":{}}");
        var exitNotification = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"method\":\"exit\",\"params\":{}}");

        var input = new StringReader(initRequest + shutdownRequest + exitNotification);
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, input, output);

        await using var server = new GDLanguageServer();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await server.InitializeAsync(transport, cts.Token);

        try { await server.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        var written = output.ToString();
        written.Should().Contain("\"id\":1");
        written.Should().Contain("GDShrapt LSP");
    }

    [TestMethod]
    public async Task SetTrace_DoesNotCrash()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var initRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"processId\":null,\"capabilities\":{},\"trace\":\"off\"}}");
        var setTraceNotification = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"method\":\"$/setTrace\",\"params\":{\"value\":\"verbose\"}}");
        var shutdownRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":99,\"method\":\"shutdown\",\"params\":{}}");
        var exitNotification = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"method\":\"exit\",\"params\":{}}");

        var input = new StringReader(initRequest + setTraceNotification + shutdownRequest + exitNotification);
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, input, output);

        await using var server = new GDLanguageServer();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await server.InitializeAsync(transport, cts.Token);

        try { await server.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        var written = output.ToString();
        written.Should().Contain("\"id\":1");
    }

    [TestMethod]
    public async Task Initialize_WithTraceOff_DoesNotEmitLogTrace()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var initRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"processId\":null,\"capabilities\":{},\"trace\":\"off\"}}");
        var hoverRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"textDocument/hover\",\"params\":{\"textDocument\":{\"uri\":\"file:///test.gd\"},\"position\":{\"line\":0,\"character\":0}}}");
        var shutdownRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":99,\"method\":\"shutdown\",\"params\":{}}");
        var exitNotification = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"method\":\"exit\",\"params\":{}}");

        var input = new StringReader(initRequest + hoverRequest + shutdownRequest + exitNotification);
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, input, output);

        await using var server = new GDLanguageServer();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await server.InitializeAsync(transport, cts.Token);

        try { await server.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        var written = output.ToString();
        written.Should().NotContain("$/logTrace");
    }
}
