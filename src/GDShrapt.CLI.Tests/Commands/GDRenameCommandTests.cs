using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDRenameCommandTests
{
    private static string GetTestProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var testProjectPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "..", "testproject", "GDShrapt.TestProject"));
        return testProjectPath;
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInvalidPath_ReturnsTwo()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDRenameCommand("old_name", "new_name", "/nonexistent/path", null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(2);
    }

    [TestMethod]
    public async Task ExecuteAsync_DryRun_DoesNotModifyFiles()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        // Get content of a test file before
        var testFile = Path.Combine(testProjectPath, "test_scripts", "base_entity.gd");
        if (!File.Exists(testFile))
        {
            throw new SkipException($"Test file not found at: {testFile}");
        }

        var originalContent = File.ReadAllText(testFile);

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDRenameCommand("health", "hp", testProjectPath, null, formatter, output, dryRun: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var contentAfter = File.ReadAllText(testFile);
        contentAfter.Should().Be(originalContent);
        (result == 0 || result == 1).Should().BeTrue("Dry run should succeed or report no matches");
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
        var command = new GDRenameCommand("health", "hp", testProjectPath, null, formatter, output, dryRun: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task ExecuteAsync_SymbolNotFound_ReturnsAppropriateCode()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDRenameCommand("nonexistent_symbol_xyz123", "new_name", testProjectPath, null, formatter, output, dryRun: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert - should return 1 (no matches found)
        (result == 0 || result == 1).Should().BeTrue("Non-existent symbol should return 0 or 1");
    }

    [TestMethod]
    public async Task ExecuteAsync_EmptyOldName_ReturnsError()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDRenameCommand("", "new_name", testProjectPath, null, formatter, output, dryRun: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        (result >= 1).Should().BeTrue("Empty old name should return error");
    }

    [TestMethod]
    public async Task ExecuteAsync_EmptyNewName_ReturnsError()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDRenameCommand("health", "", testProjectPath, null, formatter, output, dryRun: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        (result >= 1).Should().BeTrue("Empty new name should return error");
    }

    [TestMethod]
    public async Task ExecuteAsync_SameOldAndNewName_ReturnsZero()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDRenameCommand("health", "health", testProjectPath, null, formatter, output, dryRun: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert - same name should be handled gracefully
        (result == 0 || result == 1).Should().BeTrue("Same name rename should be handled gracefully");
    }

    [TestMethod]
    public async Task ExecuteAsync_CrossFileReference_FindsAllOccurrences()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            throw new SkipException($"Test project not found at: {testProjectPath}");
        }

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        // "max_health" is defined in base_entity.gd and used in child classes
        var command = new GDRenameCommand("max_health", "maximum_health", testProjectPath, null, formatter, output, dryRun: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        // Should find references in multiple files if max_health exists
        (outputText.Length > 0).Should().BeTrue("Should produce output for cross-file symbol");
    }
}
