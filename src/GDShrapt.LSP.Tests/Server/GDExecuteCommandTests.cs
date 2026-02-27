using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDExecuteCommandTests
{
    private static string BuildJsonRpcMessage(string json)
    {
        var bytes = Encoding.UTF8.GetByteCount(json);
        return $"Content-Length: {bytes}\r\n\r\n{json}";
    }

    [TestMethod]
    public async Task ExecuteCommand_ServerStatus_ReturnsVersion()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var initRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"processId\":null,\"capabilities\":{}}}");
        var executeRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"workspace/executeCommand\",\"params\":{\"command\":\"gdshrapt.serverStatus\"}}");
        var shutdownRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":99,\"method\":\"shutdown\",\"params\":{}}");
        var exitNotification = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"method\":\"exit\",\"params\":{}}");

        var input = new StringReader(initRequest + executeRequest + shutdownRequest + exitNotification);
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, input, output);

        await using var server = new GDLanguageServer();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await server.InitializeAsync(transport, cts.Token);

        try { await server.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        var written = output.ToString();
        written.Should().Contain("\"id\":2");
        written.Should().Contain("version");
    }

    [TestMethod]
    public async Task ExecuteCommand_UnknownCommand_ReturnsNullResult()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var initRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"processId\":null,\"capabilities\":{}}}");
        var executeRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"workspace/executeCommand\",\"params\":{\"command\":\"unknown.command\"}}");
        var shutdownRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":99,\"method\":\"shutdown\",\"params\":{}}");
        var exitNotification = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"method\":\"exit\",\"params\":{}}");

        var input = new StringReader(initRequest + executeRequest + shutdownRequest + exitNotification);
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, input, output);

        await using var server = new GDLanguageServer();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await server.InitializeAsync(transport, cts.Token);

        try { await server.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        var written = output.ToString();
        written.Should().Contain("\"id\":2");
    }

    [TestMethod]
    public async Task Initialize_IncludesExecuteCommandProvider()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var initRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"processId\":null,\"capabilities\":{}}}");
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
        written.Should().Contain("executeCommandProvider");
        written.Should().Contain("gdshrapt.serverStatus");
    }
}
