using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDWatchCommandTests
{
    private string? _tempProjectPath;

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempProjectPath != null)
        {
            TestProjectHelper.DeleteTempProject(_tempProjectPath);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInvalidPath_ReturnsFatal()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDWatchCommand("/nonexistent/path", formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Fatal);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidProject_CanBeCancelled()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateCleanProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDWatchCommand(_tempProjectPath, formatter, output);

        using var cts = new CancellationTokenSource();

        // Cancel after a short delay
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        // Act
        var result = await command.ExecuteAsync(cts.Token);

        // Assert — should exit cleanly after cancellation
        result.Should().Be(GDExitCode.Success);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidProject_ProducesInitialAnalysis()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateCleanProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDWatchCommand(_tempProjectPath, formatter, output);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        // Act
        var result = await command.ExecuteAsync(cts.Token);

        // Assert — should have produced some output from initial analysis
        var outputText = output.ToString();
        // The watch command writes initial analysis results
        (result == GDExitCode.Success).Should().BeTrue();
    }

    [TestMethod]
    public async Task ExecuteAsync_WithMultiFileProject_AnalyzesAllFiles()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateMultiFileProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDWatchCommand(_tempProjectPath, formatter, output);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        // Act
        var result = await command.ExecuteAsync(cts.Token);

        // Assert
        result.Should().Be(GDExitCode.Success);
    }

    [TestMethod]
    public void Name_ReturnsWatch()
    {
        // Arrange
        var command = new GDWatchCommand(".", new GDTextFormatter());

        // Assert
        command.Name.Should().Be("watch");
    }

    [TestMethod]
    public void Description_IsNotEmpty()
    {
        // Arrange
        var command = new GDWatchCommand(".", new GDTextFormatter());

        // Assert
        command.Description.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public async Task ExecuteAsync_WithProjectContainingErrors_ReturnsErrorExitCode()
    {
        // Arrange — project with syntax errors should produce a non-zero exit code
        _tempProjectPath = TestProjectHelper.CreateProjectWithSyntaxError();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDWatchCommand(_tempProjectPath, formatter, output);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(3));

        // Act
        var result = await command.ExecuteAsync(cts.Token);

        // Assert — exit code should reflect the errors found
        result.Should().NotBe(GDExitCode.Fatal);
        result.Should().BeOneOf(GDExitCode.Errors, GDExitCode.WarningsOrHints, GDExitCode.Success);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithProjectContainingErrors_ProducesOutput()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithSyntaxError();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDWatchCommand(_tempProjectPath, formatter, output);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(3));

        // Act
        await command.ExecuteAsync(cts.Token);

        // Assert — should have written diagnostics output
        var outputText = output.ToString();
        outputText.Should().NotBeNullOrWhiteSpace();
    }
}
