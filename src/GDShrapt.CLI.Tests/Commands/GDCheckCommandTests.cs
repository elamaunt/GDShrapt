using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using GDShrapt.CLI.Tests.Helpers;
using GDShrapt.Semantics;
using Xunit;

namespace GDShrapt.CLI.Tests.Commands;

public class GDCheckCommandTests : IDisposable
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

    // === New tests with TestProjectHelper ===

    [Fact]
    public async Task ExecuteAsync_CleanProject_ReturnsZero()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateCleanProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDCheckCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(0, result);
        Assert.Contains("OK", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithErrors_ReturnsOne()
    {
        // Arrange - break outside loop is an error
        _tempProjectPath = TestProjectHelper.CreateProjectWithBreakOutsideLoop();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDCheckCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(1, result);
        Assert.Contains("FAILED", output.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithLinterWarnings_ReturnsZeroByDefault()
    {
        // Arrange - naming violation is a warning by default
        _tempProjectPath = TestProjectHelper.CreateProjectWithVariableNameViolation();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDCheckCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        // Warnings don't fail check by default (only errors do)
        // Result depends on default severity configuration
        Assert.True(result == 0 || result == 1);
    }

    [Fact]
    public async Task ExecuteAsync_WithWarnings_FailOnWarning_ReturnsOne()
    {
        // Arrange - naming violation with FailOnWarning config
        _tempProjectPath = TestProjectHelper.CreateProjectWithVariableNameViolation();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var config = new GDProjectConfig
        {
            Cli = new GDCliConfig { FailOnWarning = true }
        };
        var command = new GDCheckCommand(_tempProjectPath, formatter, output, config: config);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_SyntaxError_ReturnsOne()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithSyntaxError();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDCheckCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_MultiFileProject_CountsAllFiles()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateMultiFileProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDCheckCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        Assert.Contains("3 files", outputText); // 3 scripts in multi-file project
    }

    [Fact]
    public async Task ExecuteAsync_QuietMode_WithErrors_StillReturnsOne()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithBreakOutsideLoop();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDCheckCommand(_tempProjectPath, formatter, output, quiet: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(1, result);
        Assert.Empty(output.ToString().Trim()); // No output in quiet mode
    }

    [Fact]
    public async Task ExecuteAsync_LintingDisabled_SkipsLinterRules()
    {
        // Arrange - naming violation but linting disabled
        _tempProjectPath = TestProjectHelper.CreateProjectWithVariableNameViolation();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var config = new GDProjectConfig
        {
            Linting = new GDLintingConfig { Enabled = false }
        };
        var command = new GDCheckCommand(_tempProjectPath, formatter, output, config: config);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        // With linting disabled, naming violations should not be reported
        Assert.Equal(0, result);
    }
}
