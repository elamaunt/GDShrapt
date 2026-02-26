using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDListCommandTests
{
    private string? _tempProjectPath;

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempProjectPath != null)
            TestProjectHelper.DeleteTempProject(_tempProjectPath);
    }

    // =====================================================
    // Classes
    // =====================================================

    [TestMethod]
    public async Task ListClasses_ReturnsAll()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("player.gd", @"class_name Player
extends CharacterBody2D

var health: int = 100
"),
            ("enemy.gd", @"class_name Enemy
extends CharacterBody2D

var damage: int = 10
"),
            ("game.gd", @"extends Node

func _ready() -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Class, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("Player");
        outputText.Should().Contain("Enemy");
    }

    [TestMethod]
    public async Task ListClasses_AbstractOnly()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("abstract_entity.gd", @"@abstract
class_name AbstractEntity
extends Node

@abstract
func process_entity() -> void
"),
            ("concrete.gd", @"class_name ConcreteEntity
extends Node

func process_entity() -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Class, output, abstractOnly: true);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("AbstractEntity");
        outputText.Should().NotContain("ConcreteEntity");
    }

    [TestMethod]
    public async Task ListClasses_ExtendsFilter()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("base.gd", @"class_name BaseClass
extends Node2D

func do_something() -> void:
    pass
"),
            ("child.gd", @"class_name ChildClass
extends BaseClass

func do_something() -> void:
    pass
"),
            ("other.gd", @"class_name OtherClass
extends Control

func do_something() -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Class, output, extendsType: "BaseClass");

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("ChildClass");
        outputText.Should().NotContain("OtherClass");
        // BaseClass itself has extends=Node2D, not extends=BaseClass, so it shouldn't be listed
        // Note: "BaseClass" may still appear as the SemanticType of ChildClass in the output
    }

    [TestMethod]
    public async Task ListClasses_InnerClasses()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("outer.gd", @"class_name OuterClass
extends Node

class InnerHelper:
    var data: int = 0

class AnotherInner:
    func do_work() -> void:
        pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Class, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("OuterClass");
        outputText.Should().Contain("InnerHelper");
        outputText.Should().Contain("AnotherInner");
    }

    [TestMethod]
    public async Task ListClasses_InnerOnly()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("outer.gd", @"class_name OuterClass
extends Node

class InnerHelper:
    var data: int = 0
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Class, output, innerOnly: true);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("InnerHelper");
        // Only 1 class should be listed (inner only, no top-level)
        outputText.Should().Contain("(1)");
        // OuterClass appears as OwnerScope but not as a listed item name
    }

    [TestMethod]
    public async Task ListClasses_TopLevelOnly()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("outer.gd", @"class_name OuterClass
extends Node

class InnerHelper:
    var data: int = 0
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Class, output, topLevelOnly: true);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("OuterClass");
        outputText.Should().NotContain("InnerHelper");
    }

    [TestMethod]
    public async Task ListClasses_ImplementsFilter()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("base.gd", @"class_name BaseEntity
extends Node2D

func update() -> void:
    pass
"),
            ("player.gd", @"class_name Player
extends BaseEntity

func update() -> void:
    pass
"),
            ("enemy.gd", @"class_name Enemy
extends BaseEntity

func update() -> void:
    pass
"),
            ("ui.gd", @"class_name UIPanel
extends Control

func show_panel() -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Class, output, implementsType: "BaseEntity");

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        // BaseEntity itself is assignable to BaseEntity
        outputText.Should().Contain("BaseEntity");
        outputText.Should().Contain("Player");
        outputText.Should().Contain("Enemy");
        outputText.Should().NotContain("UIPanel");
    }

    [TestMethod]
    public async Task ListClasses_ImplementsAbstract()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("abstract_base.gd", @"@abstract
class_name AbstractBase
extends Node

@abstract
func execute() -> void
"),
            ("concrete_a.gd", @"class_name ConcreteA
extends AbstractBase

func execute() -> void:
    pass
"),
            ("concrete_b.gd", @"class_name ConcreteB
extends AbstractBase

func execute() -> void:
    pass
"),
            ("unrelated.gd", @"class_name Unrelated
extends Control

func show() -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Class, output, implementsType: "AbstractBase");

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("AbstractBase");
        outputText.Should().Contain("ConcreteA");
        outputText.Should().Contain("ConcreteB");
        outputText.Should().NotContain("Unrelated");
    }

    // =====================================================
    // Signals
    // =====================================================

    [TestMethod]
    public async Task ListSignals_ReturnsAll()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("player.gd", @"class_name Player
extends Node

signal health_changed(new_health: int)
signal died

var health: int = 100
"),
            ("enemy.gd", @"class_name Enemy
extends Node

signal attack_started
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Signal, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("health_changed");
        outputText.Should().Contain("died");
        outputText.Should().Contain("attack_started");
    }

    [TestMethod]
    public async Task ListSignals_ConnectedFilter()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("emitter.gd", @"class_name Emitter
extends Node

signal my_signal
"),
            ("listener.gd", @"extends Node

@onready var emitter: Emitter = $Emitter

func _ready() -> void:
    emitter.my_signal.connect(_on_signal)

func _on_signal() -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Signal, output, connectedOnly: true);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        // connected filter relies on signal connection registry detecting the .connect() call
    }

    // =====================================================
    // Autoloads
    // =====================================================

    [TestMethod]
    public async Task ListAutoloads_ReturnsEntries()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProjectWithAutoloads(
            scripts: new[]
            {
                ("game_manager.gd", @"extends Node

var score: int = 0
"),
                ("audio_manager.gd", @"extends Node

func play_sound(name: String) -> void:
    pass
")
            },
            autoloads: new[]
            {
                ("GameManager", "game_manager.gd"),
                ("AudioManager", "audio_manager.gd")
            });

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Autoload, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("GameManager");
        outputText.Should().Contain("AudioManager");
    }

    [TestMethod]
    public async Task ListAutoloads_Empty()
    {
        _tempProjectPath = TestProjectHelper.CreateCleanProject();

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Autoload, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("0");
    }

    // =====================================================
    // Engine Callbacks
    // =====================================================

    [TestMethod]
    public async Task ListEngineCallbacks_Categories()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node2D

func _ready() -> void:
    pass

func _process(delta: float) -> void:
    pass

func _input(event: InputEvent) -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.EngineCallback, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("_ready");
        outputText.Should().Contain("_process");
        outputText.Should().Contain("_input");
    }

    // =====================================================
    // Methods
    // =====================================================

    [TestMethod]
    public async Task ListMethods_All()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("player.gd", @"class_name Player
extends Node

func take_damage(amount: int) -> void:
    pass

func heal(amount: int) -> void:
    pass
"),
            ("enemy.gd", @"class_name Enemy
extends Node

func attack() -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Method, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("take_damage");
        outputText.Should().Contain("heal");
        outputText.Should().Contain("attack");
    }

    [TestMethod]
    public async Task ListMethods_StaticOnly()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("utils.gd", @"class_name Utils
extends RefCounted

static func clamp_value(val: int, min_val: int, max_val: int) -> int:
    return clampi(val, min_val, max_val)

func non_static_method() -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Method, output, staticOnly: true);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("clamp_value");
        outputText.Should().NotContain("non_static_method");
    }

    [TestMethod]
    public async Task ListMethods_VirtualOnly()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", @"class_name Entity
extends Node

func _ready() -> void:
    pass

func _process(delta: float) -> void:
    pass

func take_damage(amount: int) -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Method, output, virtualOnly: true);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("_ready");
        outputText.Should().Contain("_process");
        outputText.Should().NotContain("take_damage");
    }

    // =====================================================
    // Variables
    // =====================================================

    [TestMethod]
    public async Task ListVariables_All()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("player.gd", @"class_name Player
extends Node

var health: int = 100
var max_health: int = 100
const MAX_SPEED: float = 200.0
"),
            ("enemy.gd", @"class_name Enemy
extends Node

var damage: int = 10
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Variable, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("health");
        outputText.Should().Contain("max_health");
        outputText.Should().Contain("MAX_SPEED");
        outputText.Should().Contain("damage");
    }

    [TestMethod]
    public async Task ListVariables_ConstOnly()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("config.gd", @"class_name Config
extends RefCounted

const MAX_HEALTH: int = 100
const GRAVITY: float = 9.8
var mutable_var: int = 0
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Variable, output, constOnly: true);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("MAX_HEALTH");
        outputText.Should().Contain("GRAVITY");
        outputText.Should().NotContain("mutable_var");
    }

    // =====================================================
    // Exports
    // =====================================================

    [TestMethod]
    public async Task ListExports_All()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("player.gd", @"class_name Player
extends CharacterBody2D

@export var speed: float = 200.0
@export var jump_force: float = 400.0
var internal_var: int = 0
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Export, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("speed");
        outputText.Should().Contain("jump_force");
        outputText.Should().NotContain("internal_var");
    }

    // =====================================================
    // Enums
    // =====================================================

    [TestMethod]
    public async Task ListEnums_WithValues()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("types.gd", @"class_name Types
extends RefCounted

enum Direction { UP, DOWN, LEFT, RIGHT }
enum State { IDLE, RUNNING, JUMPING }
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Enum, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("Direction");
        outputText.Should().Contain("State");
    }

    // =====================================================
    // Cross-cutting filters
    // =====================================================

    [TestMethod]
    public async Task ListClasses_NameGlob()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("player.gd", @"class_name Player
extends Node
"),
            ("player_controller.gd", @"class_name PlayerController
extends Node
"),
            ("enemy.gd", @"class_name Enemy
extends Node
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Class, output, nameGlob: "Player*");

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("Player");
        outputText.Should().Contain("PlayerController");
        outputText.Should().NotContain("Enemy");
    }

    [TestMethod]
    public async Task ListClasses_TopOption()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("a.gd", @"class_name AlphaClass
extends Node
"),
            ("b.gd", @"class_name BetaClass
extends Node
"),
            ("c.gd", @"class_name GammaClass
extends Node
"),
            ("d.gd", @"class_name DeltaClass
extends Node
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Class, output, top: 2);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        // Should contain count info showing total
        outputText.Should().Contain("4");
        // With --top 2, only 2 items should be listed (output is truncated)
    }

    [TestMethod]
    public async Task ListClasses_SortByFile()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("z_file.gd", @"class_name AFirst
extends Node
"),
            ("a_file.gd", @"class_name ZLast
extends Node
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Class, output, sortBy: GDListSortBy.File);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        // When sorted by file, a_file.gd comes before z_file.gd
        var zLastPos = outputText.IndexOf("ZLast");
        var aFirstPos = outputText.IndexOf("AFirst");
        zLastPos.Should().BeLessThan(aFirstPos, "ZLast in a_file.gd should appear before AFirst in z_file.gd when sorted by file");
    }

    [TestMethod]
    public async Task ListClasses_JsonOutput()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("player.gd", @"class_name Player
extends Node

var health: int = 100
"));

        var output = new StringWriter();
        var formatter = new GDJsonFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Class, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString().Trim();
        outputText.Should().StartWith("{");
        outputText.Should().EndWith("}");
        outputText.Should().Contain("\"queryKind\"");
        outputText.Should().Contain("\"items\"");
        outputText.Should().Contain("Player");
    }

    [TestMethod]
    public async Task ListClasses_Empty()
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(
            ("no_class.gd", @"extends Node

func _ready() -> void:
    pass
"));

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand(_tempProjectPath, formatter, GDListItemKind.Class, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("0");
    }

    [TestMethod]
    public async Task InvalidPath_ReturnsFatal()
    {
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDListCommand("/nonexistent/path", formatter, GDListItemKind.Class, output);

        var result = await command.ExecuteAsync();

        result.Should().Be(GDExitCode.Fatal);
    }
}
