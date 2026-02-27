using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDShowMessageTests
{
    private static string BuildJsonRpcMessage(string json)
    {
        var bytes = Encoding.UTF8.GetByteCount(json);
        return $"Content-Length: {bytes}\r\n\r\n{json}";
    }

    [TestMethod]
    public async Task TryShowErrorAsync_SendsWindowShowMessage()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var initRequest = BuildJsonRpcMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"processId\":null,\"capabilities\":{}}}");

        var input = new StringReader(initRequest);
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, input, output);

        await using var server = new GDLanguageServer();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await server.InitializeAsync(transport, cts.Token);

        // Wait for initialize to be processed
        await Task.Delay(300);

        await server.TryShowErrorAsync("Test fatal error");

        var written = output.ToString();
        written.Should().Contain("window/showMessage");
        written.Should().Contain("Test fatal error");
        written.Should().Contain("\"type\":1");
    }

    [TestMethod]
    public async Task TryShowErrorAsync_BeforeInit_DoesNotCrash()
    {
        await using var server = new GDLanguageServer();

        Func<Task> act = () => server.TryShowErrorAsync("Error before init");

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task ShowMessage_TypeIsError_SerializesCorrectly()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, new StringReader(""), output);

        await transport.SendNotificationAsync("window/showMessage", new GDShowMessageParams
        {
            Type = GDLspMessageType.Error,
            Message = "Critical error test"
        });

        var written = output.ToString();
        written.Should().Contain("window/showMessage");
        written.Should().Contain("\"type\":1");
        written.Should().Contain("Critical error test");
    }
}
