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
        result.Should().Be(GDExitCode.Fatal);
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
        result.Should().Be(GDExitCode.Fatal);
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

    [TestMethod]
    public async Task ExecuteAsync_OverriddenMethod_ShowsOverrMarker()
    {
        // Arrange: entity.gd defines take_damage, enemy.gd overrides it
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

func take_damage(amount: int) -> void:
    pass
"),
            ("enemy.gd", @"class_name Enemy
extends Entity

func take_damage(amount: int) -> void:
    print(amount)
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("[def]");
        outputText.Should().Contain("[override]");
    }

    [TestMethod]
    public async Task ExecuteAsync_NonOverriddenMethod_ShowsDeclNotOverr()
    {
        // Arrange: single class, no inheritance
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("player.gd", @"class_name Player
extends Node

func attack() -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("attack", _tempProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("[def]");
        outputText.Should().NotContain("[override]");
    }

    [TestMethod]
    public async Task ExecuteAsync_DeepInheritanceOverride_ShowsOverrMarker()
    {
        // Arrange: A -> B -> C, method defined in A, overridden in C
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("base_entity.gd", @"class_name BaseEntity
extends Node

func process_turn() -> void:
    pass
"),
            ("entity.gd", @"class_name Entity
extends BaseEntity
"),
            ("enemy.gd", @"class_name Enemy
extends Entity

func process_turn() -> void:
    print(""enemy turn"")
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("process_turn", _tempProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        // base_entity.gd has [def], enemy.gd has [override]
        outputText.Should().Contain("[def]");
        outputText.Should().Contain("[override]");
    }

    [TestMethod]
    public async Task ExecuteAsync_SuperCall_ShowsReadBaseMarker()
    {
        // Arrange: entity.gd defines take_damage, enemy.gd overrides and calls super
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

func take_damage(amount: int) -> void:
    pass
"),
            ("enemy.gd", @"class_name Enemy
extends Entity

func take_damage(amount: int) -> void:
    super.take_damage(amount)
    print(amount)
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("take_damage", _tempProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("[def]");
        outputText.Should().Contain("[override]");
        outputText.Should().Contain("[call-super]");
    }

    [TestMethod]
    public async Task ExecuteAsync_WriteReference_ShowsWriteMarker()
    {
        // Arrange: health is declared, then assigned in _ready
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("test.gd", @"extends Node

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
        var outputText = output.ToString();
        outputText.Should().Contain("[def]");
        outputText.Should().Contain("[write]");
    }

    [TestMethod]
    public async Task ExecuteAsync_ReadReference_ShowsReadMarker()
    {
        // Arrange: health is declared, then read in a print call
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("test.gd", @"extends Node

var health: int = 100

func _ready() -> void:
    print(health)
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("health", _tempProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("[def]");
        outputText.Should().Contain("[call]");
        outputText.Should().NotContain("[write]");
    }

    [TestMethod]
    public async Task ExecuteAsync_MixedReadWrite_ShowsBothMarkers()
    {
        // Arrange: health is declared, written, and read
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("test.gd", @"extends Node

var health: int = 100

func _ready() -> void:
    health = 50
    print(health)
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("health", _tempProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("[def]");
        outputText.Should().Contain("[write]");
        outputText.Should().Contain("[call]");
    }

    [TestMethod]
    public async Task ExecuteAsync_SameNameDifferentFiles_GroupsSeparately()
    {
        // Arrange: 'data' declared independently in two files
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("enemy.gd", @"extends Node

var data: Dictionary = {}

func _ready() -> void:
    data[""speed""] = 10
"),
            ("tower.gd", @"extends Node

var data: Dictionary = {}

func _ready() -> void:
    print(data)
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("data", _tempProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        // Should have two separate [def] markers — one per file
        var declCount = outputText.Split("[def]").Length - 1;
        declCount.Should().Be(2, "each file's 'data' is an independent declaration");
    }

    [TestMethod]
    public async Task ExecuteAsync_InheritedVariableUsedInChild_ShowsWriteBaseMarker()
    {
        // Arrange: 'target' declared in base, written in child (no local declaration)
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("projectile_base.gd", @"extends Node2D
class_name ProjectileBase

var target: Node2D = null

func _ready() -> void:
    print(target)
"),
            ("projectile_aoe.gd", @"extends ProjectileBase

func fire(node: Node2D) -> void:
    target = node
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("target", _tempProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        // Root group: ProjectileBase with declaration
        outputText.Should().Contain("[def]");
        // Child inherited usage: [write] marker
        outputText.Should().Contain("[write]");
    }

    [TestMethod]
    public async Task ExecuteAsync_InheritedVariableReadInChild_ShowsReadBaseMarker()
    {
        // Arrange: 'max_health' declared in base, read in child
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"extends Node
class_name Entity

var max_health: int = 100
"),
            ("enemy.gd", @"extends Entity

func show_info() -> void:
    print(max_health)
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFindRefsCommand("max_health", _tempProjectPath, null, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("[def]");
        outputText.Should().Contain("[call]");
    }
}
