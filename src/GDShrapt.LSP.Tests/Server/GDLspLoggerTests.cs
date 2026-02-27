using System.IO;
using System.Threading.Tasks;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDLspLoggerTests
{
    private static (GDStdioJsonRpcTransport transport, StringWriter output) CreateTransport()
    {
        var serializer = new GDSystemTextJsonSerializer();
        var output = new StringWriter();
        var transport = new GDStdioJsonRpcTransport(serializer, new StringReader(""), output);
        return (transport, output);
    }

    [TestMethod]
    public async Task ErrorAsync_SendsWindowLogMessage_WithErrorType()
    {
        var (transport, output) = CreateTransport();
        var logger = new GDLspLogger(transport);

        await logger.ErrorAsync("Test error message");

        var written = output.ToString();
        written.Should().Contain("window/logMessage");
        written.Should().Contain("Test error message");
        written.Should().Contain("\"type\":1");
    }

    [TestMethod]
    public async Task WarningAsync_SendsCorrectType()
    {
        var (transport, output) = CreateTransport();
        var logger = new GDLspLogger(transport);

        await logger.WarningAsync("Test warning");

        var written = output.ToString();
        written.Should().Contain("\"type\":2");
    }

    [TestMethod]
    public async Task InfoAsync_SendsCorrectType()
    {
        var (transport, output) = CreateTransport();
        var logger = new GDLspLogger(transport);

        await logger.InfoAsync("Test info");

        var written = output.ToString();
        written.Should().Contain("\"type\":3");
    }

    [TestMethod]
    public async Task DebugAsync_SendsCorrectType()
    {
        var (transport, output) = CreateTransport();
        var logger = new GDLspLogger(transport);

        await logger.DebugAsync("Debug message");

        var written = output.ToString();
        written.Should().Contain("\"type\":4");
    }

    [TestMethod]
    public void Transport_LoggerProperty_CanBeSet()
    {
        var (transport, _) = CreateTransport();
        var logger = new GDLspLogger(transport);

        transport.Logger = logger;

        transport.Logger.Should().NotBeNull();
        transport.Logger.Should().BeSameAs(logger);
    }
}
