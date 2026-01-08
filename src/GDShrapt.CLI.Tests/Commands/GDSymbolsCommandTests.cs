using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using Xunit;

namespace GDShrapt.CLI.Tests.Commands;

public class GDSymbolsCommandTests
{
    private static string GetTestProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var testProjectPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "..", "testproject", "GDShrapt.TestProject"));
        return testProjectPath;
    }

    [Fact]
    public async Task ExecuteAsync_WithValidFile_ReturnsZero()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        var scriptPath = Path.Combine(testProjectPath, "test_scripts", "base_entity.gd");
        if (!File.Exists(scriptPath))
        {
            throw new SkipException($"Test script not found at: {scriptPath}");
        }

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDSymbolsCommand(scriptPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(0, result);
        var outputText = output.ToString();
        Assert.NotEmpty(outputText);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonexistentFile_ReturnsTwo()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDSymbolsCommand("/nonexistent/file.gd", formatter, output);

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

        var scriptPath = Path.Combine(testProjectPath, "test_scripts", "base_entity.gd");
        if (!File.Exists(scriptPath))
        {
            throw new SkipException($"Test script not found at: {scriptPath}");
        }

        var output = new StringWriter();
        var formatter = new GDJsonFormatter();
        var command = new GDSymbolsCommand(scriptPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(0, result);
        var outputText = output.ToString();
        Assert.Contains("[", outputText);
    }
}
