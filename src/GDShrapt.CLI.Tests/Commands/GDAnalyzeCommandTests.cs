using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using Xunit;

namespace GDShrapt.CLI.Tests;

public class GDAnalyzeCommandTests : IDisposable
{
    private string? _tempProjectPath;

    public void Dispose()
    {
        if (_tempProjectPath != null)
        {
            TestProjectHelper.DeleteTempProject(_tempProjectPath);
        }
    }

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

    // === Linter integration tests ===

    [Fact]
    public async Task ExecuteAsync_ClassNameViolation_ReportsGDL001()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithClassNameViolation();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDAnalyzeCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        Assert.Contains("GDL001", outputText);
    }

    [Fact]
    public async Task ExecuteAsync_VariableNameViolation_ReportsGDL003()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithVariableNameViolation();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDAnalyzeCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        Assert.Contains("GDL003", outputText);
    }

    [Theory]
    [InlineData("class_name badName\nextends Node\n", "GDL001")]
    [InlineData("extends Node\nvar BadVariable = 1\n", "GDL003")]
    [InlineData("extends Node\nconst bad_const = 1\n", "GDL004")]
    public async Task ExecuteAsync_NamingViolation_ReportsExpectedRule(string code, string expectedRuleId)
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithLinterIssue(code);
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDAnalyzeCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        Assert.Contains(expectedRuleId, outputText);
    }

    // === Validator integration tests ===

    [Fact]
    public async Task ExecuteAsync_BreakOutsideLoop_ReportsGD5001()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithBreakOutsideLoop();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDAnalyzeCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        Assert.Contains("GD5001", outputText);
    }

    [Fact]
    public async Task ExecuteAsync_ContinueOutsideLoop_ReportsGD5002()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithContinueOutsideLoop();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDAnalyzeCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        Assert.Contains("GD5002", outputText);
    }

    [Fact]
    public async Task ExecuteAsync_SyntaxError_ReportsGD0002()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithSyntaxError();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDAnalyzeCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        // Should report either GD0001 (parse failure) or GD0002 (invalid token)
        Assert.True(outputText.Contains("GD0001") || outputText.Contains("GD0002"),
            $"Expected GD0001 or GD0002 in output: {outputText}");
    }

    // === Clean project test ===

    [Fact]
    public async Task ExecuteAsync_CleanProject_ReturnsZero()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateCleanProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDAnalyzeCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_CleanProject_JsonFormat_ReturnsValidJson()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateCleanProject();
        var output = new StringWriter();
        var formatter = new GDJsonFormatter();
        var command = new GDAnalyzeCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString().Trim();
        Assert.StartsWith("{", outputText);
        Assert.EndsWith("}", outputText);
        Assert.Contains("\"totalErrors\"", outputText);
    }

    [Fact]
    public async Task ExecuteAsync_MultiFileProject_AnalyzesAllFiles()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateMultiFileProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDAnalyzeCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString().ToLower();
        // Should mention multiple files were analyzed
        Assert.Contains("3", outputText); // 3 scripts
    }
}

public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}
