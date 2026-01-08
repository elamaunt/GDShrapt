using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using Xunit;

namespace GDShrapt.CLI.Tests.Commands;

public class GDFindRefsCommandTests
{
    private static string GetTestProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var testProjectPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "..", "testproject", "GDShrapt.TestProject"));
        return testProjectPath;
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPath_ReturnsTwo()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("health", "/nonexistent/path", null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidSymbol_ReturnsZero()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("health", testProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(0, result);
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
        var command = new GDFindRefsCommand("health", testProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(0, result);
        var outputText = output.ToString();
        Assert.Contains("[", outputText);
    }
}
