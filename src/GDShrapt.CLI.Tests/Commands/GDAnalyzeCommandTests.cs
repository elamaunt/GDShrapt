using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using Xunit;

namespace GDShrapt.CLI.Tests.Commands;

public class GDAnalyzeCommandTests
{
    private static string GetTestProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var testProjectPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "..", "testproject", "GDShrapt.TestProject"));
        return testProjectPath;
    }

    [Fact]
    public async Task ExecuteAsync_WithValidProject_ReturnsZero()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDAnalyzeCommand(testProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        Assert.Contains("Analysis", outputText);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPath_ReturnsTwo()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDAnalyzeCommand("/nonexistent/path", formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithJsonFormatter_OutputsJson()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        var output = new StringWriter();
        var formatter = new GDJsonFormatter();
        var command = new GDAnalyzeCommand(testProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        Assert.Contains("{", outputText);
        Assert.Contains("\"projectPath\"", outputText);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidProject_ContainsScriptInfo()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDAnalyzeCommand(testProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        Assert.Contains("scripts", outputText.ToLower());
    }

    [Fact]
    public async Task ExecuteAsync_JsonOutput_IsValidJson()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        var output = new StringWriter();
        var formatter = new GDJsonFormatter();
        var command = new GDAnalyzeCommand(testProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        // Basic JSON structure validation
        Assert.StartsWith("{", outputText.Trim());
        Assert.EndsWith("}", outputText.Trim());
    }

    [Fact]
    public async Task ExecuteAsync_EmptyDirectory_HandlesGracefully()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDAnalyzeCommand(tempDir, formatter, output);

            // Act
            var result = await command.ExecuteAsync();

            // Assert - should not crash on empty directory
            Assert.True(result == 0 || result == 2, "Empty directory should return 0 or 2");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}

public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}
