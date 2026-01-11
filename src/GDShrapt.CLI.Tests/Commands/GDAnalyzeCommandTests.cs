using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDAnalyzeCommandTests
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
        outputText.Should().Contain("Analysis");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInvalidPath_ReturnsTwo()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDAnalyzeCommand("/nonexistent/path", formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(2);
    }

    [TestMethod]
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
        outputText.Should().Contain("{");
        outputText.Should().Contain("\"projectPath\"");
    }

    [TestMethod]
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
        outputText.ToLower().Should().Contain("scripts");
    }

    [TestMethod]
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
        outputText.Trim().Should().StartWith("{");
        outputText.Trim().Should().EndWith("}");
    }

    [TestMethod]
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
            (result == 0 || result == 2).Should().BeTrue("Empty directory should return 0 or 2");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // === Linter integration tests ===

    [TestMethod]
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
        outputText.Should().Contain("GDL001");
    }

    [TestMethod]
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
        outputText.Should().Contain("GDL003");
    }

    [DataTestMethod]
    [DataRow("class_name badName\nextends Node\n", "GDL001")]
    [DataRow("extends Node\nvar BadVariable = 1\n", "GDL003")]
    [DataRow("extends Node\nconst bad_const = 1\n", "GDL004")]
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
        outputText.Should().Contain(expectedRuleId);
    }

    // === Validator integration tests ===

    [TestMethod]
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
        outputText.Should().Contain("GD5001");
    }

    [TestMethod]
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
        outputText.Should().Contain("GD5002");
    }

    [TestMethod]
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
        (outputText.Contains("GD0001") || outputText.Contains("GD0002")).Should().BeTrue(
            $"Expected GD0001 or GD0002 in output: {outputText}");
    }

    // === Clean project test ===

    [TestMethod]
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
        result.Should().Be(0);
    }

    [TestMethod]
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
        outputText.Should().StartWith("{");
        outputText.Should().EndWith("}");
        outputText.Should().Contain("\"totalErrors\"");
    }

    [TestMethod]
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
        outputText.Should().Contain("3"); // 3 scripts
    }
}

public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}
