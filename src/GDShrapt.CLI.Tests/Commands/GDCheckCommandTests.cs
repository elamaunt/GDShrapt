using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using Xunit;

namespace GDShrapt.CLI.Tests.Commands;

public class GDCheckCommandTests
{
    private static string GetTestProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var testProjectPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "..", "testproject", "GDShrapt.TestProject"));
        return testProjectPath;
    }

    [Fact]
    public async Task ExecuteAsync_WithValidProject_ReturnsZeroOrOne()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDCheckCommand(testProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.True(result == 0 || result == 1, "Exit code should be 0 (success) or 1 (errors found)");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPath_ReturnsTwo()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDCheckCommand("/nonexistent/path", formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task ExecuteAsync_QuietMode_NoOutput()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDCheckCommand(testProjectPath, formatter, output, quiet: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        Assert.Empty(outputText.Trim());
    }
}
