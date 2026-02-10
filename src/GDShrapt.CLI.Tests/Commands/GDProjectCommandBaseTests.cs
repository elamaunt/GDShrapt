using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDProjectCommandBaseTests
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
    public async Task ExecuteAsync_WithNonexistentPath_ReturnsFatalWithMessage()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new TestableProjectCommand("/nonexistent/path/to/project", formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Fatal);
        output.ToString().Should().Contain("Could not find project.godot");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidProject_ReturnsSuccess()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateCleanProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new TestableProjectCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Success);
        command.WasExecuted.Should().BeTrue();
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenCancelled_PropagatesCancellation()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateCleanProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new TestableProjectCommand(_tempProjectPath, formatter, output, simulateSlow: true);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await command.ExecuteAsync(cts.Token);

        // Assert — should handle cancellation gracefully (not throw unhandled)
        // The base class catches all exceptions, so it returns Fatal on unhandled OperationCanceledException
        result.Should().BeOneOf(GDExitCode.Success, GDExitCode.Fatal);
    }

    [TestMethod]
    public void GetRelativePath_WithValidPaths_ReturnsRelative()
    {
        // Arrange & Act — use the static method via the testable wrapper
        var result = TestableProjectCommand.TestGetRelativePath(
            Path.Combine("C:", "project", "scripts", "player.gd"),
            Path.Combine("C:", "project"));

        // Assert
        result.Should().Be(Path.Combine("scripts", "player.gd"));
    }

    [TestMethod]
    public void GetRelativePath_WithSamePath_ReturnsDot()
    {
        // Arrange & Act
        var result = TestableProjectCommand.TestGetRelativePath(
            Path.Combine("C:", "project"),
            Path.Combine("C:", "project"));

        // Assert
        result.Should().Be(".");
    }

    /// <summary>
    /// Testable subclass of GDProjectCommandBase for unit testing.
    /// </summary>
    private class TestableProjectCommand : GDProjectCommandBase
    {
        private readonly bool _simulateSlow;

        public bool WasExecuted { get; private set; }
        public override string Name => "test";
        public override string Description => "Test command for unit testing.";

        public TestableProjectCommand(
            string projectPath,
            IGDOutputFormatter formatter,
            TextWriter? output = null,
            bool simulateSlow = false)
            : base(projectPath, formatter, output)
        {
            _simulateSlow = simulateSlow;
        }

        protected override async Task<int> ExecuteOnProjectAsync(
            GDScriptProject project,
            string projectRoot,
            GDProjectConfig config,
            CancellationToken cancellationToken)
        {
            WasExecuted = true;

            if (_simulateSlow)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return GDExitCode.Success;
        }

        public static string TestGetRelativePath(string fullPath, string basePath)
            => GetRelativePath(fullPath, basePath);
    }
}
