using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDDepsCommandTests
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

    private string CreateMultiFileProject()
    {
        return TestProjectHelper.CreateTempProject(
            ("base.gd", @"class_name BaseClass
extends Node

var health: int = 100

func take_damage(amount: int) -> void:
    health -= amount
"),
            ("derived.gd", @"class_name DerivedClass
extends BaseClass

var armor: int = 50

func take_damage(amount: int) -> void:
    var reduced = amount - armor
    super.take_damage(reduced)
"),
            ("consumer.gd", @"extends Node

const Base = preload(""res://base.gd"")
const Derived = preload(""res://derived.gd"")

func _ready() -> void:
    var b = Base.new()
    var d = Derived.new()
"),
            ("standalone.gd", @"extends Node

func _ready() -> void:
    pass
"));
    }

    [TestMethod]
    public async Task ExecuteAsync_DefaultGraphAll_ShowsAllSections()
    {
        _tempProjectPath = CreateMultiFileProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDDepsCommand(_tempProjectPath, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var text = output.ToString();
        text.Should().Contain("Dependency Analysis:");
        text.Should().Contain("Fan-in");
        text.Should().Contain("Fan-out");
        text.Should().Contain("Coupling");
    }

    [TestMethod]
    public async Task ExecuteAsync_GraphCode_ShowsOnlyCodeSections()
    {
        _tempProjectPath = CreateMultiFileProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDDepsOptions { GraphLayer = GDDepsGraphLayer.Code };
        var command = new GDDepsCommand(_tempProjectPath, formatter, output, options: options);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var text = output.ToString();
        text.Should().Contain("Fan-in");
        text.Should().NotContain("Scene Hubs");
        text.Should().NotContain("Signal Hubs");
    }

    [TestMethod]
    public async Task ExecuteAsync_GraphScenes_ShowsOnlySceneSections()
    {
        _tempProjectPath = CreateMultiFileProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDDepsOptions { GraphLayer = GDDepsGraphLayer.Scenes };
        var command = new GDDepsCommand(_tempProjectPath, formatter, output, options: options);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var text = output.ToString();
        // No scene files in this project, so scene section should be empty
        text.Should().NotContain("Fan-in");
        text.Should().NotContain("Signal Hubs");
    }

    [TestMethod]
    public async Task ExecuteAsync_GraphSignals_ShowsOnlySignalSections()
    {
        _tempProjectPath = CreateMultiFileProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDDepsOptions { GraphLayer = GDDepsGraphLayer.Signals };
        var command = new GDDepsCommand(_tempProjectPath, formatter, output, options: options);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var text = output.ToString();
        text.Should().NotContain("Fan-in");
        text.Should().NotContain("Scene Hubs");
    }

    [TestMethod]
    public async Task ExecuteAsync_Explain_ShowsEdgeTypes()
    {
        _tempProjectPath = CreateMultiFileProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDDepsOptions { Explain = true };
        var command = new GDDepsCommand(_tempProjectPath, formatter, output, options: options);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var text = output.ToString();
        // Explain mode should show edge kinds
        (text.Contains("preload") || text.Contains("extends")).Should().BeTrue("Should contain edge type labels");
    }

    [TestMethod]
    public async Task ExecuteAsync_TopN_LimitsEntries()
    {
        _tempProjectPath = CreateMultiFileProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDDepsOptions { TopN = 1 };
        var command = new GDDepsCommand(_tempProjectPath, formatter, output, options: options);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var text = output.ToString();
        text.Should().Contain("Top 1");
    }

    [TestMethod]
    public async Task ExecuteAsync_GroupByDir_ShowsDirectories()
    {
        // Create a project with files in different directories
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entities/base.gd", @"class_name BaseEntity
extends Node

var health: int = 100
"),
            ("entities/player.gd", @"class_name Player
extends BaseEntity

var speed: float = 5.0
"),
            ("utils/helpers.gd", @"extends Node

const Base = preload(""res://entities/base.gd"")

func create_entity() -> void:
    var b = Base.new()
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDDepsOptions { GroupByDir = "dir", GroupDepth = 1 };
        var command = new GDDepsCommand(_tempProjectPath, formatter, output, options: options);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var text = output.ToString();
        text.Should().Contain("Module Dependencies");
    }

    [TestMethod]
    public async Task ExecuteAsync_Dir_RestrictsScope()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("scripts/a.gd", @"class_name ClassA
extends Node
"),
            ("scripts/b.gd", @"class_name ClassB
extends ClassA
"),
            ("addons/plugin.gd", @"extends Node

func _ready() -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDDepsOptions { Dir = "scripts" };
        var command = new GDDepsCommand(_tempProjectPath, formatter, output, options: options);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var text = output.ToString();
        text.Should().NotContain("plugin.gd");
    }

    [TestMethod]
    public async Task ExecuteAsync_ExcludeAddons_FiltersAddons()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("main.gd", @"extends Node

func _ready() -> void:
    pass
"),
            ("addons/myplugin/plugin.gd", @"extends Node

func _ready() -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDDepsOptions { ExcludeAddons = true };
        var command = new GDDepsCommand(_tempProjectPath, formatter, output, options: options);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var text = output.ToString();
        text.Should().NotContain("plugin.gd");
    }

    [TestMethod]
    public async Task ExecuteAsync_FailOnCycles_WithCycles_ReturnsWarning()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("cycle_a.gd", @"class_name CycleA
extends Node

const CycleB = preload(""res://cycle_b.gd"")
"),
            ("cycle_b.gd", @"class_name CycleB
extends Node

const CycleA = preload(""res://cycle_a.gd"")
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDDepsOptions { FailOnCycles = true };
        var command = new GDDepsCommand(_tempProjectPath, formatter, output, options: options);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.WarningsOrHints);
    }

    [TestMethod]
    public async Task ExecuteAsync_FailOnCycles_NoCycles_ReturnsSuccess()
    {
        _tempProjectPath = CreateMultiFileProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDDepsOptions { FailOnCycles = true };
        var command = new GDDepsCommand(_tempProjectPath, formatter, output, options: options);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
    }

    [TestMethod]
    public async Task ExecuteAsync_FileMode_ShowsDependencies()
    {
        _tempProjectPath = CreateMultiFileProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDDepsOptions { FilePath = "consumer.gd" };
        var command = new GDDepsCommand(_tempProjectPath, formatter, output, options: options);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var text = output.ToString();
        text.Should().Contain("Dependencies for:");
        text.Should().Contain("consumer.gd");
    }

    [TestMethod]
    public async Task ExecuteAsync_FileExplain_ShowsTypedEdges()
    {
        _tempProjectPath = CreateMultiFileProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDDepsOptions { FilePath = "consumer.gd", Explain = true };
        var command = new GDDepsCommand(_tempProjectPath, formatter, output, options: options);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var text = output.ToString();
        text.Should().Contain("Outgoing edges");
        text.Should().Contain("preload");
    }

    [TestMethod]
    public async Task ExecuteAsync_InvalidPath_ReturnsFatal()
    {
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDDepsCommand("/nonexistent/path", formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Fatal);
    }

    [TestMethod]
    public async Task ExecuteAsync_EmptyProject_NoErrors()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("empty.gd", @"extends Node
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDDepsCommand(_tempProjectPath, formatter, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
    }
}
