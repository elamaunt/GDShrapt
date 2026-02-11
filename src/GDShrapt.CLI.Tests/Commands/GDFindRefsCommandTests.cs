using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDFindRefsCommandTests
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
    public async Task ExecuteAsync_WithInvalidPath_ReturnsTwo()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("health", "/nonexistent/path", null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(2);
    }

    [TestMethod]
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
        result.Should().Be(0);
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
        var command = new GDFindRefsCommand("health", testProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("[");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithLineOption_FindsSymbolByPosition()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateTempProject(("test.gd", @"extends Node

var health: int = 100

func _ready() -> void:
    health = 50
"));
        var filePath = Path.Combine(_tempProjectPath, "test.gd");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand(null, _tempProjectPath, filePath, formatter, output, line: 3, column: 5);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("health");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithLineOption_NoSymbolAtPosition_ReturnsError()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateTempProject(("test.gd", @"extends Node

var health: int = 100

func _ready() -> void:
    health = 50
"));
        var filePath = Path.Combine(_tempProjectPath, "test.gd");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        // Line 4 is empty — no symbol at that position
        var command = new GDFindRefsCommand(null, _tempProjectPath, filePath, formatter, output, line: 4, column: 1);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(2);
        output.ToString().Should().Contain("No symbol found");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithLineButNoFile_ReturnsError()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateTempProject(("test.gd", @"extends Node
var health: int = 100
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand(null, _tempProjectPath, null, formatter, output, line: 2);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(2);
        output.ToString().Should().Contain("--file");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNoSymbolAndNoLine_ReturnsError()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateTempProject(("test.gd", @"extends Node
var health: int = 100
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand(null, _tempProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(2);
        output.ToString().Should().Contain("symbol name");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithLineOption_FindsCrossFileReferences()
    {
        // Arrange: player.gd has take_damage at 1-based line 9, enemy.gd calls it
        _tempProjectPath = TestProjectHelper.CreateMultiFileProject();
        var filePath = Path.Combine(_tempProjectPath, "player.gd");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand(null, _tempProjectPath, filePath, formatter, output, line: 9, column: 6);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("take_damage");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithSymbolNameAndNoLine_StillWorks()
    {
        // Arrange: regression test — name-based lookup still works
        _tempProjectPath = TestProjectHelper.CreateTempProject(("test.gd", @"extends Node

var health: int = 100

func _ready() -> void:
    health = 50
"));
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("health", _tempProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("health");
    }
}
