using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDProgressReportingTests
{
    private static string BuildJsonRpcMessage(string json)
    {
        var bytes = Encoding.UTF8.GetByteCount(json);
        return $"Content-Length: {bytes}\r\n\r\n{json}";
    }

    [TestMethod]
    public async Task SendProgressAsync_Begin_SerializesCorrectly()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, new StringReader(""), output);

        await transport.SendNotificationAsync("$/progress", new GDProgressParams
        {
            Token = "test-token",
            Value = new GDWorkDoneProgressValue
            {
                Kind = "begin",
                Title = "Test Title",
                Percentage = 0
            }
        });

        var written = output.ToString();
        written.Should().Contain("$/progress");
        written.Should().Contain("test-token");
        written.Should().Contain("\"kind\":\"begin\"");
        written.Should().Contain("Test Title");
        written.Should().Contain("\"percentage\":0");
    }

    [TestMethod]
    public async Task SendProgressAsync_Report_SerializesCorrectly()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, new StringReader(""), output);

        await transport.SendNotificationAsync("$/progress", new GDProgressParams
        {
            Token = "test-token",
            Value = new GDWorkDoneProgressValue
            {
                Kind = "report",
                Message = "Building index...",
                Percentage = 60
            }
        });

        var written = output.ToString();
        written.Should().Contain("$/progress");
        written.Should().Contain("\"kind\":\"report\"");
        written.Should().Contain("Building index...");
        written.Should().Contain("\"percentage\":60");
    }

    [TestMethod]
    public async Task SendProgressAsync_End_SerializesCorrectly()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, new StringReader(""), output);

        await transport.SendNotificationAsync("$/progress", new GDProgressParams
        {
            Token = "test-token",
            Value = new GDWorkDoneProgressValue
            {
                Kind = "end",
                Message = "Analysis complete"
            }
        });

        var written = output.ToString();
        written.Should().Contain("$/progress");
        written.Should().Contain("\"kind\":\"end\"");
        written.Should().Contain("Analysis complete");
    }

    [TestMethod]
    public async Task WorkDoneProgressCreate_SerializesCorrectly()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, new StringReader(""), output);

        // Verify the request payload serializes correctly
        await transport.SendNotificationAsync("window/workDoneProgress/create", new GDWorkDoneProgressCreateParams
        {
            Token = "gdshrapt-analysis"
        });

        var written = output.ToString();
        written.Should().Contain("window/workDoneProgress/create");
        written.Should().Contain("gdshrapt-analysis");
    }

    [TestMethod]
    public async Task ProgressReport_NullFieldsOmitted()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, new StringReader(""), output);

        await transport.SendNotificationAsync("$/progress", new GDProgressParams
        {
            Token = "test-token",
            Value = new GDWorkDoneProgressValue
            {
                Kind = "end",
                Message = "Done"
            }
        });

        var written = output.ToString();
        // Null fields should be omitted (DefaultIgnoreCondition = WhenWritingNull)
        written.Should().NotContain("\"title\"");
        written.Should().NotContain("\"percentage\"");
        written.Should().NotContain("\"cancellable\"");
    }

    [TestMethod]
    public async Task Server_WithProject_SendsProgressNotifications()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var testProjectPath = GetTestProjectPath();
        var rootUri = new Uri(testProjectPath).AbsoluteUri;

        // Use anonymous pipes for controlled bidirectional communication
        await using var serverPipe = new AnonymousPipeServerStream(PipeDirection.Out);
        await using var clientPipe = new AnonymousPipeClientStream(PipeDirection.In, serverPipe.ClientSafePipeHandle);

        var pipeWriter = new StreamWriter(serverPipe, Encoding.UTF8) { AutoFlush = true };
        var pipeReader = new StreamReader(clientPipe, Encoding.UTF8);
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, pipeReader, output);

        // Build initialize with proper JSON serialization
        var initJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new { processId = (int?)null, capabilities = new { }, rootUri }
        });

        // Write initialize request
        await WriteJsonRpcAsync(pipeWriter, initJson);

        await using var server = new GDLanguageServer();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await server.InitializeAsync(transport, cts.Token);

        // Write initialized notification — triggers background analysis
        await WriteJsonRpcAsync(pipeWriter,
            "{\"jsonrpc\":\"2.0\",\"method\":\"initialized\",\"params\":{}}");

        // Wait for the server to send the window/workDoneProgress/create request.
        // Poll the output to ensure the request has been written before sending the response.
        var requestDeadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < requestDeadline)
        {
            if (output.ToString().Contains("window/workDoneProgress/create"))
                break;
            await Task.Delay(100);
        }

        // Respond to the progress create request (server's outgoing request ID = "1")
        await WriteJsonRpcAsync(pipeWriter,
            "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"result\":null}");

        // Wait for background analysis to complete (check output for "end" progress)
        var deadline = DateTime.UtcNow.AddSeconds(45);
        while (DateTime.UtcNow < deadline)
        {
            var current = output.ToString();
            if (current.Contains("\"kind\":\"end\""))
                break;
            await Task.Delay(200);
        }

        // Send shutdown + exit
        await WriteJsonRpcAsync(pipeWriter,
            "{\"jsonrpc\":\"2.0\",\"id\":99,\"method\":\"shutdown\",\"params\":{}}");
        await WriteJsonRpcAsync(pipeWriter,
            "{\"jsonrpc\":\"2.0\",\"method\":\"exit\",\"params\":{}}");

        try { await server.RunAsync(cts.Token); } catch (OperationCanceledException) { /* Expected — server intentionally cancelled */ }

        var written = output.ToString();

        // Verify progress create request was sent
        written.Should().Contain("window/workDoneProgress/create", "server should create progress token");
        written.Should().Contain("gdshrapt-analysis", "progress token should be 'gdshrapt-analysis'");

        // Verify progress notifications were sent
        written.Should().Contain("$/progress", "server should send progress notifications during analysis");
        written.Should().Contain("\"kind\":\"begin\"", "should send 'begin' progress");
        written.Should().Contain("GDShrapt: Analyzing project...", "begin should include title");
        written.Should().Contain("\"kind\":\"report\"", "should send 'report' progress");
        written.Should().Contain("\"kind\":\"end\"", "should send 'end' progress");
    }

    [TestMethod]
    public async Task Server_WithoutProject_NoProgressSent()
    {
        var serializer = new GDSystemTextJsonSerializer();

        // Initialize without rootUri — no project, no analysis, no progress
        var initRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"processId\":null,\"capabilities\":{}}}");
        var initializedNotification = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"method\":\"initialized\",\"params\":{}}");
        var shutdownRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":99,\"method\":\"shutdown\",\"params\":{}}");
        var exitNotification = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"method\":\"exit\",\"params\":{}}");

        var input = new StringReader(initRequest + initializedNotification + shutdownRequest + exitNotification);
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, input, output);

        await using var server = new GDLanguageServer();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await server.InitializeAsync(transport, cts.Token);

        try { await server.RunAsync(cts.Token); } catch (OperationCanceledException) { /* Expected — server intentionally cancelled */ }

        var written = output.ToString();

        // No progress should be sent when there's no project
        written.Should().NotContain("$/progress");
    }

    private static string GetTestProjectPath()
    {
        var baseDir = Directory.GetCurrentDirectory();
        var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    private static string EscapeJsonString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static async Task WriteJsonRpcAsync(StreamWriter writer, string json)
    {
        var bytes = Encoding.UTF8.GetByteCount(json);
        await writer.WriteAsync($"Content-Length: {bytes}\r\n\r\n{json}");
        await writer.FlushAsync();
    }
}
