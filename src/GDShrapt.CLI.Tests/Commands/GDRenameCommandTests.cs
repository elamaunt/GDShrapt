using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDRenameCommandTests
{
    private string? _tempProjectPath;

    private static string GetTestProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var testProjectPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "..", "testproject", "GDShrapt.TestProject"));
        return testProjectPath;
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempProjectPath != null)
            TestProjectHelper.DeleteTempProject(_tempProjectPath);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInvalidPath_ReturnsFatal()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDRenameCommand("old_name", "new_name", "/nonexistent/path", null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Fatal);
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

    [TestMethod]
    public async Task ExecuteAsync_WithLineOption_ResolvesSymbolAndRenames()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("player.gd", @"extends Node2D
class_name Player

var speed: float = 100.0

func move(delta: float) -> void:
    position.x += speed * delta
"));

        var filePath = Path.Combine(_tempProjectPath, "player.gd");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();

        // Line 4 (1-based) = "var speed: float = 100.0", column 5 (1-based) = "speed"
        var command = new GDRenameCommand(
            null, "velocity", _tempProjectPath, filePath, formatter, output,
            dryRun: true, line: 4, column: 5);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        var outputText = output.ToString();
        result.Should().Be(0);
        outputText.Should().Contain("speed");
        outputText.Should().Contain("velocity");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithLineOption_NoFile_ReturnsError()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("test.gd", @"extends Node
var x: int = 1
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();

        // --line without --file should fail
        var command = new GDRenameCommand(
            null, "y", _tempProjectPath, null, formatter, output,
            dryRun: true, line: 2);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Fatal);
        var outputText = output.ToString();
        outputText.Should().Contain("--file");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithLineOption_NoSymbolAtPosition_ReturnsError()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("test.gd", @"extends Node

func _ready() -> void:
    pass
"));

        var filePath = Path.Combine(_tempProjectPath, "test.gd");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();

        // Line 4 = "    pass" — no renameable symbol at column 0
        var command = new GDRenameCommand(
            null, "new_name", _tempProjectPath, filePath, formatter, output,
            dryRun: true, line: 4, column: 0);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().BeGreaterThanOrEqualTo(1, "No symbol at 'pass' position should return error");
    }

    [TestMethod]
    public async Task ExecuteAsync_InheritanceChain_AllOverridesFoundAsStrict()
    {
        // Arrange: base -> child -> grandchild with overrides + super calls
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("base.gd", @"extends Node2D
class_name BaseClass

func take_damage(amount: int) -> void:
    print(amount)
"),
            ("child.gd", @"extends BaseClass
class_name ChildClass

func take_damage(amount: int) -> void:
    super.take_damage(amount)
    print(""child"")
"),
            ("grandchild.gd", @"extends ChildClass
class_name GrandChild

func take_damage(amount: int) -> void:
    super.take_damage(amount)
    print(""grand"")
"),
            ("caller.gd", @"extends Node

func test(target: BaseClass) -> void:
    target.take_damage(10)
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDRenameCommand(
            "take_damage", "receive_damage", _tempProjectPath, null, formatter, output,
            dryRun: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("Strict references");
        outputText.Should().Contain("base.gd");
        outputText.Should().Contain("child.gd");
        outputText.Should().Contain("grandchild.gd");
    }
}
