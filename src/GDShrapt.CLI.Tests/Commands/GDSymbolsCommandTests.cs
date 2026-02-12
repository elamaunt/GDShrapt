using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDSymbolsCommandTests
{
    private static string GetTestProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var testProjectPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "..", "testproject", "GDShrapt.TestProject"));
        return testProjectPath;
    }

    [TestMethod]
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
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonexistentFile_ReturnsFatal()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDSymbolsCommand("/nonexistent/file.gd", formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Fatal);
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
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("[");
    }

    [TestMethod]
    public async Task ExecuteAsync_InheritedSymbols_NotShownInChildFile()
    {
        // Arrange: parent defines members, child extends it
        var tempPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

var max_health: int = 100
var move_speed: float = 5.0

func heal(amount: int) -> void:
    max_health += amount
"),
            ("enemy.gd", @"class_name Enemy
extends Entity

var damage: int = 10

func attack() -> void:
    heal(damage)
"));

        try
        {
            var scriptPath = Path.Combine(tempPath, "enemy.gd");
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDSymbolsCommand(scriptPath, formatter, output, projectPath: tempPath);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            result.Should().Be(GDExitCode.Success);
            var outputText = output.ToString();

            // enemy.gd's own symbols should be present
            outputText.Should().Contain("damage");
            outputText.Should().Contain("attack");

            // Inherited symbols from Entity should NOT appear
            outputText.Should().NotContain("max_health");
            outputText.Should().NotContain("move_speed");
            outputText.Should().NotContain("heal");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithProjectOption_Works()
    {
        // Arrange
        var tempPath = TestProjectHelper.CreateTempProject(
            ("player.gd", @"class_name Player
extends Node

var health: int = 100

func take_damage(amount: int) -> void:
    health -= amount
"));

        try
        {
            var scriptPath = Path.Combine(tempPath, "player.gd");
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDSymbolsCommand(scriptPath, formatter, output, projectPath: tempPath);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            result.Should().Be(GDExitCode.Success);
            var outputText = output.ToString();
            outputText.Should().Contain("health");
            outputText.Should().Contain("take_damage");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_FileOutsideProject_ReturnsFatal()
    {
        // Arrange: create two separate temp dirs
        var projectPath = TestProjectHelper.CreateTempProject(
            ("game.gd", @"extends Node
func _ready() -> void:
    pass
"));
        var outsidePath = Path.Combine(Path.GetTempPath(), "gdshrapt_outside_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outsidePath);
        var outsideFile = Path.Combine(outsidePath, "rogue.gd");
        File.WriteAllText(outsideFile, "extends Node\n");

        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDSymbolsCommand(outsideFile, formatter, output, projectPath: projectPath);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            result.Should().Be(GDExitCode.Fatal);
            output.ToString().Should().Contain("outside the project");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(projectPath);
            TestProjectHelper.DeleteTempProject(outsidePath);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_AllSymbolKinds_CorrectlyLabeled()
    {
        // Arrange: script with various symbol kinds
        var tempPath = TestProjectHelper.CreateTempProject(
            ("kinds.gd", @"class_name Kinds
extends Node

enum Direction { UP, DOWN, LEFT, RIGHT }

const MAX_SPEED: float = 100.0

var position: Vector2 = Vector2.ZERO

signal moved(new_pos: Vector2)

func update(delta: float) -> void:
    for i in range(10):
        pass
    position += Vector2.ONE * delta
"));

        try
        {
            var scriptPath = Path.Combine(tempPath, "kinds.gd");
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDSymbolsCommand(scriptPath, formatter, output, projectPath: tempPath);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            result.Should().Be(GDExitCode.Success);
            var outputText = output.ToString();

            // All kinds should be present and not "unknown"
            outputText.Should().NotContain("unknown");
            outputText.Should().Contain("class");
            outputText.Should().Contain("enum");
            outputText.Should().Contain("constant");
            outputText.Should().Contain("variable");
            outputText.Should().Contain("signal");
            outputText.Should().Contain("method");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }
}
