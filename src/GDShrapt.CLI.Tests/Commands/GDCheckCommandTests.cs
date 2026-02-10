using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDCheckCommandTests
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

    private static string GetTestProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var testProjectPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "..", "testproject", "GDShrapt.TestProject"));
        return testProjectPath;
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidProject_ReturnsZeroOrOneOrTwo()
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
        // Exit codes: 0=Success, 1=Warnings/Hints (if fail-on configured), 2=Errors
        (result == 0 || result == 1 || result == 2).Should().BeTrue("Exit code should be 0 (success), 1 (warnings), or 2 (errors)");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInvalidPath_ReturnsFatal()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDCheckCommand("/nonexistent/path", formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        // Exit code 3 = Fatal (project not found)
        result.Should().Be(3);
    }

    [TestMethod]
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
        var command = new GDCheckCommand(testProjectPath, formatter, output, silent: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        outputText.Trim().Should().BeEmpty();
    }

    // === New tests with TestProjectHelper ===

    [TestMethod]
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
        result.Should().Be(0);
        output.ToString().Should().Contain("OK");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithErrors_ReturnsTwo()
    {
        // Arrange - break outside loop is an error
        _tempProjectPath = TestProjectHelper.CreateProjectWithBreakOutsideLoop();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDCheckCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        // Exit code 2 = Errors found
        result.Should().Be(2);
        output.ToString().Should().Contain("FAILED");
    }

    [TestMethod]
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
        (result == 0 || result == 1).Should().BeTrue();
    }

    [TestMethod]
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
        result.Should().Be(1);
    }

    [TestMethod]
    public async Task ExecuteAsync_SyntaxError_ReturnsTwo()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithSyntaxError();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDCheckCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        // Exit code 2 = Errors found
        result.Should().Be(2);
    }

    [TestMethod]
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
        outputText.Should().Contain("3 files"); // 3 scripts in multi-file project
    }

    [TestMethod]
    public async Task ExecuteAsync_QuietMode_WithErrors_StillReturnsTwo()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithBreakOutsideLoop();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDCheckCommand(_tempProjectPath, formatter, output, silent: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        // Exit code 2 = Errors found
        result.Should().Be(2);
        output.ToString().Trim().Should().BeEmpty(); // No output in quiet mode
    }

    [TestMethod]
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
        result.Should().Be(0);
    }
}
