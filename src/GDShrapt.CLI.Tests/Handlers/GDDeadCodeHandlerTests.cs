using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using GDProjectLoader = GDShrapt.Semantics.GDProjectLoader;

namespace GDShrapt.CLI.Tests.Handlers;

/// <summary>
/// Tests for GDDeadCodeHandler using the TestProject fixture.
/// Base handler enforces Strict confidence only.
/// </summary>
[TestClass]
public class GDDeadCodeHandlerTests
{
    private GDScriptProject? _project;
    private GDProjectSemanticModel? _projectModel;
    private GDDeadCodeHandler? _handler;

    [TestInitialize]
    public void Setup()
    {
        _project = TestProjectHelper.LoadTestProject();
        _projectModel = new GDProjectSemanticModel(_project);
        _handler = new GDDeadCodeHandler(_projectModel);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _project?.Dispose();
    }

    // === AnalyzeFile Tests ===

    [TestMethod]
    public void AnalyzeFile_ValidFile_ReturnsDeadCodeReport()
    {
        // Arrange
        var filePath = TestProjectHelper.GetTestScriptPath("simple_class.gd");
        var options = new GDDeadCodeOptions
        {
            IncludeVariables = true,
            IncludeFunctions = true,
            IncludeSignals = true
        };

        // Act
        var report = _handler!.AnalyzeFile(filePath, options);

        // Assert
        report.Should().NotBeNull();
    }

    [TestMethod]
    public void AnalyzeFile_NonExistentFile_ReturnsEmptyReport()
    {
        // Arrange
        var filePath = "/nonexistent/file.gd";
        var options = new GDDeadCodeOptions();

        // Act
        var report = _handler!.AnalyzeFile(filePath, options);

        // Assert
        report.Should().NotBeNull();
        report.Items.Should().BeEmpty();
    }

    [TestMethod]
    public void AnalyzeFile_EnforcesStrictConfidence()
    {
        // Arrange
        var filePath = TestProjectHelper.GetTestScriptPath("simple_class.gd");
        var options = new GDDeadCodeOptions
        {
            MaxConfidence = GDReferenceConfidence.NameMatch, // Request high confidence
            IncludeVariables = true,
            IncludeFunctions = true,
            IncludeSignals = false,
            IncludeUnreachable = false,
            IncludeConstants = false,
            IncludeEnumValues = false,
            IncludeInnerClasses = false,
            IncludeParameters = false
        };

        // Act
        var report = _handler!.AnalyzeFile(filePath, options);

        // Assert
        // Base handler should only return Strict confidence items
        if (report.HasItems)
        {
            report.Items.Should().OnlyContain(item => item.Confidence == GDReferenceConfidence.Strict);
        }
    }

    // === AnalyzeProject Tests ===

    [TestMethod]
    public void AnalyzeProject_TestProject_ReturnsReport()
    {
        // Arrange
        var options = new GDDeadCodeOptions
        {
            IncludeVariables = true,
            IncludeFunctions = true,
            IncludeSignals = true
        };

        // Act
        var report = _handler!.AnalyzeProject(options);

        // Assert
        report.Should().NotBeNull();
    }

    [TestMethod]
    public void AnalyzeProject_EnforcesStrictConfidence()
    {
        // Arrange
        var options = new GDDeadCodeOptions
        {
            MaxConfidence = GDReferenceConfidence.Potential, // Request Potential
            IncludeVariables = true,
            IncludeFunctions = true,
            IncludeSignals = true,
            IncludeUnreachable = false,
            IncludeConstants = false,
            IncludeEnumValues = false,
            IncludeInnerClasses = false,
            IncludeParameters = false
        };

        // Act
        var report = _handler!.AnalyzeProject(options);

        // Assert
        // Base handler enforces Strict confidence only
        if (report.HasItems)
        {
            report.Items.Should().OnlyContain(item => item.Confidence == GDReferenceConfidence.Strict);
        }
    }

    [TestMethod]
    public void AnalyzeProject_IncludeVariables_FindsVariables()
    {
        // Arrange
        var options = new GDDeadCodeOptions
        {
            IncludeVariables = true,
            IncludeFunctions = false,
            IncludeSignals = false,
            IncludeUnreachable = false,
            IncludeConstants = false,
            IncludeEnumValues = false,
            IncludeInnerClasses = false,
            IncludeParameters = false
        };

        // Act
        var report = _handler!.AnalyzeProject(options);

        // Assert
        // If any dead code found, should only be variables
        if (report.HasItems)
        {
            report.Items.Should().OnlyContain(item => item.Kind == GDDeadCodeKind.Variable);
        }
    }

    [TestMethod]
    public void AnalyzeProject_IncludeFunctions_FindsFunctions()
    {
        // Arrange
        var options = new GDDeadCodeOptions
        {
            IncludeVariables = false,
            IncludeFunctions = true,
            IncludeSignals = false,
            IncludeUnreachable = false,
            IncludeConstants = false,
            IncludeEnumValues = false,
            IncludeInnerClasses = false,
            IncludeParameters = false
        };

        // Act
        var report = _handler!.AnalyzeProject(options);

        // Assert
        if (report.HasItems)
        {
            report.Items.Should().OnlyContain(item => item.Kind == GDDeadCodeKind.Function);
        }
    }

    [TestMethod]
    public void AnalyzeProject_IncludeSignals_FindsSignals()
    {
        // Arrange
        var options = new GDDeadCodeOptions
        {
            IncludeVariables = false,
            IncludeFunctions = false,
            IncludeSignals = true,
            IncludeUnreachable = false,
            IncludeConstants = false,
            IncludeEnumValues = false,
            IncludeInnerClasses = false,
            IncludeParameters = false
        };

        // Act
        var report = _handler!.AnalyzeProject(options);

        // Assert
        if (report.HasItems)
        {
            report.Items.Should().OnlyContain(item => item.Kind == GDDeadCodeKind.Signal);
        }
    }

    // === Isolated temp project tests with guaranteed dead code ===

    [TestMethod]
    public void AnalyzeProject_WithUnusedVariable_FindsDeadVariable()
    {
        // Arrange
        var code = @"extends Node
class_name DeadCodeTest

var used_var: int = 0
var unused_var: String = ""never used""

func _ready() -> void:
    used_var = 10
";
        var tempPath = TestProjectHelper.CreateTempProject(("dead_code.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = true,
                IncludeFunctions = false,
                IncludeSignals = false
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert
            report.UnusedVariables.Should().BeGreaterThan(0);
            report.Items.Should().Contain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "unused_var");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_WithUnusedFunction_FindsDeadFunction()
    {
        // Arrange
        var code = @"extends Node
class_name DeadFunctionTest

func _ready() -> void:
    used_function()

func used_function() -> void:
    print(""used"")

func unused_function() -> void:
    print(""never called"")
";
        var tempPath = TestProjectHelper.CreateTempProject(("dead_func.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = true,
                IncludeSignals = false,
                IncludePrivate = false // Skip underscore-prefixed
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert
            report.UnusedFunctions.Should().BeGreaterThan(0);
            report.Items.Should().Contain(item =>
                item.Kind == GDDeadCodeKind.Function &&
                item.Name == "unused_function");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_WithUnusedSignal_FindsDeadSignal()
    {
        // Arrange
        var code = @"extends Node
class_name DeadSignalTest

signal used_signal
signal unused_signal

func _ready() -> void:
    emit_signal(""used_signal"")
";
        var tempPath = TestProjectHelper.CreateTempProject(("dead_signal.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = false,
                IncludeSignals = true
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert
            report.UnusedSignals.Should().BeGreaterThan(0);
            report.Items.Should().Contain(item =>
                item.Kind == GDDeadCodeKind.Signal &&
                item.Name == "unused_signal");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_AllOptions_FindsAllKinds()
    {
        // Arrange
        var code = @"extends Node
class_name AllDeadCodeTest

var unused_var = 0
signal unused_signal

func _ready() -> void:
    pass

func unused_function() -> void:
    print(""never called"")
";
        var tempPath = TestProjectHelper.CreateTempProject(("all_dead.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = true,
                IncludeFunctions = true,
                IncludeSignals = true,
                IncludePrivate = false
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert
            report.TotalCount.Should().BeGreaterThan(0);
            report.Items.Should().Contain(item => item.Kind == GDDeadCodeKind.Variable);
            report.Items.Should().Contain(item => item.Kind == GDDeadCodeKind.Signal);
            report.Items.Should().Contain(item => item.Kind == GDDeadCodeKind.Function);
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_ReportGroupsByFile()
    {
        // Arrange
        var options = new GDDeadCodeOptions
        {
            IncludeVariables = true,
            IncludeFunctions = true,
            IncludeSignals = true
        };

        // Act
        var report = _handler!.AnalyzeProject(options);

        // Assert
        if (report.HasItems)
        {
            var byFile = report.ByFile.ToList();
            byFile.Should().NotBeEmpty();
            byFile.Should().OnlyContain(g => !string.IsNullOrEmpty(g.Key));
        }
    }

    // === GDScript 4.x signal connect / cross-file emit tests ===

    [TestMethod]
    public void AnalyzeProject_GDScript4SignalConnect_NoFalsePositive()
    {
        // Arrange — Events autoload with signal, enemy connects via GDScript 4.x syntax
        var eventsCode = @"extends Node
class_name Events

signal enemy_killed
";
        var enemyCode = @"extends Node

func _ready() -> void:
    Events.enemy_killed.connect(_on_enemy_killed)

func _on_enemy_killed() -> void:
    pass
";
        var tempPath = TestProjectHelper.CreateTempProjectWithAutoloads(
            new[] { ("events.gd", eventsCode), ("enemy.gd", enemyCode) },
            new[] { ("Events", "events.gd") });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = false,
                IncludeSignals = true
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — enemy_killed should NOT be reported as dead
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Signal &&
                item.Name == "enemy_killed");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_GDScript4LocalSignalConnect_NoFalsePositive()
    {
        // Arrange — local signal connected via GDScript 4.x syntax within same file
        var code = @"extends Node

signal my_signal

func _ready() -> void:
    my_signal.connect(_on_signal)

func _on_signal() -> void:
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("local_signal.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = false,
                IncludeSignals = true
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — my_signal should NOT be reported as dead
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Signal &&
                item.Name == "my_signal");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_TrulyUnusedSignal_StillReported()
    {
        // Arrange — signal that is never connected or emitted
        var code = @"extends Node
class_name UnusedSignalTest

signal unused_signal

func _ready() -> void:
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("unused_signal.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = false,
                IncludeSignals = true
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — unused_signal SHOULD be reported as dead
            report.Items.Should().Contain(item =>
                item.Kind == GDDeadCodeKind.Signal &&
                item.Name == "unused_signal");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_CrossFileSignalEmit_NoFalsePositive()
    {
        // Arrange — signal emitted from another file via Events.wave_completed.emit()
        var eventsCode = @"extends Node
class_name Events

signal wave_completed
";
        var gameCode = @"extends Node

func _on_wave_done() -> void:
    Events.wave_completed.emit()
";
        var tempPath = TestProjectHelper.CreateTempProjectWithAutoloads(
            new[] { ("events.gd", eventsCode), ("game.gd", gameCode) },
            new[] { ("Events", "events.gd") });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = false,
                IncludeSignals = true
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — wave_completed should NOT be reported as dead
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Signal &&
                item.Name == "wave_completed");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // === Cross-file autoload / static method / variable tests ===

    [TestMethod]
    public void AnalyzeProject_AutoloadMethodCalledCrossFile_NoFalsePositive()
    {
        // Arrange — GameManager autoload with start_game(), called from main.gd
        var gameManagerCode = @"extends Node
class_name GameManager

func start_game() -> void:
    pass
";
        var mainCode = @"extends Node

func _ready() -> void:
    GameManager.start_game()
";
        var tempPath = TestProjectHelper.CreateTempProjectWithAutoloads(
            new[] { ("game_manager.gd", gameManagerCode), ("main.gd", mainCode) },
            new[] { ("GameManager", "game_manager.gd") });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = true,
                IncludeSignals = false,
                IncludePrivate = false
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — start_game should NOT be reported as dead
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Function &&
                item.Name == "start_game");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_AutoloadVariableReadCrossFile_NoFalsePositive()
    {
        // Arrange — GameManager autoload with selected_tower_type, read from ui.gd
        var gameManagerCode = @"extends Node
class_name GameManager

var selected_tower_type: int = 0
";
        var uiCode = @"extends Node

func _ready() -> void:
    var t = GameManager.selected_tower_type
    print(t)
";
        var tempPath = TestProjectHelper.CreateTempProjectWithAutoloads(
            new[] { ("game_manager.gd", gameManagerCode), ("ui.gd", uiCode) },
            new[] { ("GameManager", "game_manager.gd") });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = true,
                IncludeFunctions = false,
                IncludeSignals = false
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — selected_tower_type should NOT be reported as dead
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "selected_tower_type");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_StaticMethodCalledCrossFile_NoFalsePositive()
    {
        // Arrange — Constants class with static method, called from enemy.gd
        var constantsCode = @"extends RefCounted
class_name Constants

static func get_enemy_data(enemy_type: int) -> Dictionary:
    return {}
";
        var enemyCode = @"extends Node

func _ready() -> void:
    var data = Constants.get_enemy_data(1)
    print(data)
";
        var tempPath = TestProjectHelper.CreateTempProject(
            ("constants.gd", constantsCode), ("enemy.gd", enemyCode));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = true,
                IncludeSignals = false,
                IncludePrivate = false
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — get_enemy_data should NOT be reported as dead
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Function &&
                item.Name == "get_enemy_data");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_TrulyUnusedMethod_StillReported()
    {
        // Arrange — method that is never called from anywhere
        var code = @"extends Node
class_name UnusedMethodTest

func _ready() -> void:
    pass

func never_called() -> void:
    print(""nobody calls me"")
";
        var tempPath = TestProjectHelper.CreateTempProject(("unused_method.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = true,
                IncludeSignals = false,
                IncludePrivate = false
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — never_called SHOULD be reported as dead
            report.Items.Should().Contain(item =>
                item.Kind == GDDeadCodeKind.Function &&
                item.Name == "never_called");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // === Subdirectory autoload / realistic scenario tests ===

    [TestMethod]
    public void AnalyzeProject_AutoloadMethodInSubdirectory_NoFalsePositive()
    {
        // Arrange — realistic scenario: autoload in subdirectory with non-static method
        var eventsCode = @"extends Node
class_name Events

signal gold_changed(new_gold: int, delta: int)
";
        var gameManagerCode = @"extends Node
class_name GameManager

var current_gold: int = 100

func can_afford(amount: int) -> bool:
    return current_gold >= amount

func spend_gold(amount: int) -> bool:
    if can_afford(amount):
        var old_gold: int = current_gold
        current_gold -= amount
        Events.gold_changed.emit(current_gold, current_gold - old_gold)
        return true
    return false
";
        var towerPlacementCode = @"extends Node

var selected_cost: int = 50

func _try_place_tower() -> void:
    if not GameManager.spend_gold(selected_cost):
        return
    print(""tower placed"")
";
        var tempPath = TestProjectHelper.CreateTempProjectWithAutoloads(
            new[]
            {
                ("src/autoload/events.gd", eventsCode),
                ("src/autoload/game_manager.gd", gameManagerCode),
                ("src/systems/tower_placement.gd", towerPlacementCode)
            },
            new[]
            {
                ("Events", "src/autoload/events.gd"),
                ("GameManager", "src/autoload/game_manager.gd")
            });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = true,
                IncludeSignals = false,
                IncludePrivate = false
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — spend_gold should NOT be reported as dead (called cross-file)
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Function &&
                item.Name == "spend_gold",
                "spend_gold is called from tower_placement.gd via GameManager.spend_gold()");

            // can_afford should NOT be dead either (called from spend_gold within same file)
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Function &&
                item.Name == "can_afford",
                "can_afford is called from spend_gold within the same file");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_AutoloadMethodUsedInCondition_NoFalsePositive()
    {
        // Arrange — method return value used in if/not condition
        var gameManagerCode = @"extends Node
class_name GameManager

func spend_gold(cost: int) -> bool:
    return cost > 0
";
        var callerCode = @"extends Node

func _ready() -> void:
    if not GameManager.spend_gold(10):
        return
    print(""spent"")
";
        var tempPath = TestProjectHelper.CreateTempProjectWithAutoloads(
            new[] { ("game_manager.gd", gameManagerCode), ("caller.gd", callerCode) },
            new[] { ("GameManager", "game_manager.gd") });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = true,
                IncludeSignals = false,
                IncludePrivate = false
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — spend_gold should NOT be reported as dead
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Function &&
                item.Name == "spend_gold",
                "spend_gold is called via 'if not GameManager.spend_gold(10):'");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // === Autoload without class_name tests ===

    [TestMethod]
    public void AnalyzeProject_AutoloadWithoutClassName_NoFalsePositive()
    {
        // Arrange — autoload registered as "GameManager" but script has NO class_name
        // This is the real-world pattern: file.TypeName = "game_manager" (from filename)
        // but code calls GameManager.spend_gold() using the autoload name
        var gameManagerCode = @"extends Node

var selected_tower_type: Variant = null

func start_game() -> void:
    pass

func reset_game() -> void:
    pass

func spend_gold(amount: int) -> bool:
    return amount > 0
";
        var callerCode = @"extends Node

func _ready() -> void:
    GameManager.start_game()

func _on_button() -> void:
    if not GameManager.spend_gold(50):
        return
    print(""spent"")

func _on_reset() -> void:
    GameManager.reset_game()
";
        var uiCode = @"extends Node

func _process(_delta: float) -> void:
    var t = GameManager.selected_tower_type
    print(t)
";
        var tempPath = TestProjectHelper.CreateTempProjectWithAutoloads(
            new[]
            {
                ("src/autoload/game_manager.gd", gameManagerCode),
                ("src/ui/caller.gd", callerCode),
                ("src/ui/tower_ui.gd", uiCode)
            },
            new[]
            {
                ("GameManager", "src/autoload/game_manager.gd")
            });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = true,
                IncludeFunctions = true,
                IncludeSignals = false,
                IncludePrivate = false
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — none of these should be reported as dead
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Function &&
                item.Name == "start_game",
                "start_game is called cross-file via GameManager.start_game()");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Function &&
                item.Name == "spend_gold",
                "spend_gold is called cross-file via GameManager.spend_gold()");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Function &&
                item.Name == "reset_game",
                "reset_game is called cross-file via GameManager.reset_game()");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "selected_tower_type",
                "selected_tower_type is read cross-file via GameManager.selected_tower_type");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_AutoloadSignalWithoutClassName_NoFalsePositive()
    {
        // Arrange — Events autoload without class_name, signals used cross-file
        var eventsCode = @"extends Node

signal enemy_killed
signal gold_changed(new_gold: int, delta: int)
";
        var gameCode = @"extends Node

func _ready() -> void:
    Events.enemy_killed.connect(_on_enemy_killed)

func _on_enemy_killed() -> void:
    pass

func _add_gold(amount: int) -> void:
    Events.gold_changed.emit(100, amount)
";
        var tempPath = TestProjectHelper.CreateTempProjectWithAutoloads(
            new[]
            {
                ("src/autoload/events.gd", eventsCode),
                ("src/game.gd", gameCode)
            },
            new[]
            {
                ("Events", "src/autoload/events.gd")
            });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = false,
                IncludeSignals = true
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — signals should NOT be reported as dead
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Signal &&
                item.Name == "enemy_killed",
                "enemy_killed is connected cross-file via Events.enemy_killed.connect()");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Signal &&
                item.Name == "gold_changed",
                "gold_changed is emitted cross-file via Events.gold_changed.emit()");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // === Inherited method/variable call tests ===

    [TestMethod]
    public void AnalyzeProject_InheritedMethodCalledInChild_NoFalsePositive()
    {
        // Arrange — entity.gd defines heal(), enemy_tank.gd calls bare heal() via inheritance
        var entityCode = @"extends Node2D
class_name Entity

var max_health: int = 100
var _current_health: int = 100

func heal(amount: int) -> void:
    _current_health = mini(_current_health + amount, max_health)
";
        var enemyBaseCode = @"extends Entity
class_name EnemyBase

func take_damage(amount: int) -> void:
    _current_health -= amount
";
        var enemyTankCode = @"extends EnemyBase

func _ability_heal() -> void:
    heal(max_health / 4)
";
        var tempPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", entityCode),
            ("enemy_base.gd", enemyBaseCode),
            ("enemy_tank.gd", enemyTankCode));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = true,
                IncludeSignals = false,
                IncludePrivate = false
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — heal should NOT be reported as dead
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Function &&
                item.Name == "heal",
                "heal is called from enemy_tank.gd via inheritance (bare heal() call)");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_InheritedVariableReadInChild_NoFalsePositive()
    {
        // Arrange — entity.gd defines max_health, enemy.gd reads it via inheritance
        var entityCode = @"extends Node2D
class_name Entity

var max_health: int = 100
";
        var enemyCode = @"extends Entity

func get_health_display() -> String:
    return str(max_health)
";
        var tempPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", entityCode),
            ("enemy.gd", enemyCode));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = true,
                IncludeFunctions = false,
                IncludeSignals = false
            };

            // Act
            var report = handler.AnalyzeProject(options);

            // Assert — max_health should NOT be reported as dead
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "max_health",
                "max_health is read from enemy.gd via inheritance (bare max_health access)");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // === Override method / polymorphic call tests ===

    [TestMethod]
    public void AnalyzeProject_OverrideMethodCalledViaBaseType_NoFalsePositive()
    {
        var entityCode = @"extends Node2D
class_name Entity

var _health: int = 100

func take_damage(amount: int) -> void:
    _health -= amount

func is_alive() -> bool:
    return _health > 0

func get_health() -> int:
    return _health
";
        var enemyBasicCode = @"extends Entity
class_name EnemyBasic

func take_damage(amount: int) -> void:
    _health -= amount * 2
";
        var enemyFastCode = @"extends Entity
class_name EnemyFast

func take_damage(amount: int) -> void:
    _health -= amount
";
        var towerCode = @"extends Node2D

var target: Entity

func _process(_delta: float) -> void:
    if target != null:
        target.take_damage(10)
        if target.is_alive():
            print(target.get_health())
";
        var tempPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", entityCode),
            ("enemy_basic.gd", enemyBasicCode),
            ("enemy_fast.gd", enemyFastCode),
            ("tower.gd", towerCode));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = true,
                IncludeSignals = false,
                IncludePrivate = false
            };

            var report = handler.AnalyzeProject(options);

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Function && item.Name == "take_damage",
                "take_damage is called on Entity type, all overrides are used");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Function && item.Name == "is_alive",
                "is_alive is called on Entity type");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Function && item.Name == "get_health",
                "get_health is called on Entity type");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_DeepOverrideChain_NoFalsePositive()
    {
        var entityCode = @"extends Node2D
class_name Entity

func take_damage(amount: int) -> void:
    pass
";
        var enemyBaseCode = @"extends Entity
class_name EnemyBase

func take_damage(amount: int) -> void:
    pass
";
        var enemyTankCode = @"extends EnemyBase
class_name EnemyTank

func take_damage(amount: int) -> void:
    pass
";
        var towerCode = @"extends Node2D

func shoot(target: Entity) -> void:
    target.take_damage(10)
";
        var tempPath = TestProjectHelper.CreateTempProject(
            ("entity.gd", entityCode),
            ("enemy_base.gd", enemyBaseCode),
            ("enemy_tank.gd", enemyTankCode),
            ("tower.gd", towerCode));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = true,
                IncludeSignals = false,
                IncludePrivate = false
            };

            var report = handler.AnalyzeProject(options);

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Function && item.Name == "take_damage",
                "All take_damage overrides should be recognized as used through the 3-level chain");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_UnusedOverride_StillReported()
    {
        var baseCode = @"extends Node
class_name BaseClass

func never_called() -> void:
    pass
";
        var childCode = @"extends BaseClass
class_name ChildClass

func never_called() -> void:
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(
            ("base_class.gd", baseCode),
            ("child_class.gd", childCode));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = true,
                IncludeSignals = false,
                IncludePrivate = false
            };

            var report = handler.AnalyzeProject(options);

            report.Items.Should().Contain(item =>
                item.Kind == GDDeadCodeKind.Function && item.Name == "never_called",
                "Both base and child never_called should be reported as dead");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // === Constants cross-file access tests ===

    [TestMethod]
    public void AnalyzeProject_ConstantUsedCrossFile_NoFalsePositive()
    {
        var constantsCode = @"extends RefCounted
class_name Constants

const MAX_WAVES: int = 10
const STARTING_GOLD: int = 100
const UNUSED_CONST: int = 999
";
        var gameCode = @"extends Node

func _ready() -> void:
    print(Constants.MAX_WAVES)
    var gold = Constants.STARTING_GOLD
    print(gold)
";
        var tempPath = TestProjectHelper.CreateTempProject(
            ("constants.gd", constantsCode),
            ("game.gd", gameCode));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = false,
                IncludeSignals = false,
                IncludeConstants = true
            };

            var report = handler.AnalyzeProject(options);

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Constant && item.Name == "MAX_WAVES",
                "MAX_WAVES is used cross-file via Constants.MAX_WAVES");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Constant && item.Name == "STARTING_GOLD",
                "STARTING_GOLD is used cross-file via Constants.STARTING_GOLD");

            report.Items.Should().Contain(item =>
                item.Kind == GDDeadCodeKind.Constant && item.Name == "UNUSED_CONST");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_ConstantUsedViaAutoload_NoFalsePositive()
    {
        var constantsCode = @"extends Node

const MAX_WAVES: int = 10
";
        var gameCode = @"extends Node

func _ready() -> void:
    print(Constants.MAX_WAVES)
";
        var tempPath = TestProjectHelper.CreateTempProjectWithAutoloads(
            new[] { ("constants.gd", constantsCode), ("game.gd", gameCode) },
            new[] { ("Constants", "constants.gd") });

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = false,
                IncludeSignals = false,
                IncludeConstants = true
            };

            var report = handler.AnalyzeProject(options);

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Constant && item.Name == "MAX_WAVES",
                "MAX_WAVES is used cross-file via Constants.MAX_WAVES autoload");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // === Chained property access tests ===

    [TestMethod]
    public void AnalyzeProject_VariableReadViaTwoLevelPropertyChain_NoFalsePositive()
    {
        // class A: has member variable
        var regexCode = @"extends RefCounted
class_name CompilerRegEx

var INLINE_RANDOM_REGEX: RegEx = RegEx.create_from_string(""\[\[(?<options>.*?)\]\]"")
";
        // class B: has typed property pointing to A
        var compilationCode = @"extends RefCounted
class_name Compilation

var regex: CompilerRegEx = CompilerRegEx.new()
";
        // class C: accesses A's member through B's property
        var userCode = @"extends Node

var compilation: Compilation = Compilation.new()

func _ready() -> void:
    var results = compilation.regex.INLINE_RANDOM_REGEX.search_all(""test"")
    print(results)
";
        var tempPath = TestProjectHelper.CreateTempProject(
            ("compiler_regex.gd", regexCode),
            ("compilation.gd", compilationCode),
            ("user.gd", userCode));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = true,
                IncludeFunctions = false,
                IncludeSignals = false
            };

            var report = handler.AnalyzeProject(options);

            // INLINE_RANDOM_REGEX should NOT be dead — it's read via chain
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "INLINE_RANDOM_REGEX",
                "INLINE_RANDOM_REGEX is read via property chain: compilation.regex.INLINE_RANDOM_REGEX");

            // regex should NOT be dead — it's read via compilation.regex
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "regex",
                "regex is read via: compilation.regex");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_VariableReadViaChainInForLoop_NoFalsePositive()
    {
        // Reproduces the exact user scenario: for loop iterating over chained property access
        var regexCode = @"extends RefCounted
class_name DMCompilerRegEx

var IMPORT_REGEX: RegEx = RegEx.create_from_string(""import \""(?<path>[^\""]+)\""  as (?<prefix>[a-zA-Z_]+)"")
var INLINE_RANDOM_REGEX: RegEx = RegEx.create_from_string(""\[\[(?<options>.*?)\]\]"")
";
        var compilationCode = @"extends RefCounted
class_name DMCompilation

var file_path: String
var imported_paths: PackedStringArray = []
var regex: DMCompilerRegEx = DMCompilerRegEx.new()
";
        var compilerCode = @"extends RefCounted

var compilation: DMCompilation = DMCompilation.new()

func resolve_random(text: String) -> String:
    for found: RegExMatch in compilation.regex.INLINE_RANDOM_REGEX.search_all(text):
        var options: PackedStringArray = found.get_string(&""options"").split(&""|"")
        text = text.replace(&""[[%s]]"" % found.get_string(&""options""), options[randi_range(0, options.size() - 1)])
    return text
";
        var tempPath = TestProjectHelper.CreateTempProject(
            ("compiler_regex.gd", regexCode),
            ("compilation.gd", compilationCode),
            ("compiler.gd", compilerCode));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = true,
                IncludeFunctions = false,
                IncludeSignals = false
            };

            var report = handler.AnalyzeProject(options);

            // INLINE_RANDOM_REGEX should NOT be dead
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "INLINE_RANDOM_REGEX",
                "INLINE_RANDOM_REGEX is read via: compilation.regex.INLINE_RANDOM_REGEX.search_all()");

            // regex should NOT be dead
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "regex",
                "regex is read via: compilation.regex");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_VariableReadViaChainWithLocalVar_NoFalsePositive()
    {
        // Chain access through a local variable (no class-level annotation)
        var regexCode = @"extends RefCounted
class_name CompilerRegEx

var MY_PATTERN: RegEx = RegEx.create_from_string(""test"")
";
        var compilationCode = @"extends RefCounted
class_name Compilation

var regex: CompilerRegEx = CompilerRegEx.new()
";
        var userCode = @"extends Node

func process_text(text: String) -> void:
    var comp: Compilation = Compilation.new()
    var result = comp.regex.MY_PATTERN.search(text)
    if result:
        print(result.get_string())
";
        var tempPath = TestProjectHelper.CreateTempProject(
            ("compiler_regex.gd", regexCode),
            ("compilation.gd", compilationCode),
            ("user.gd", userCode));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = true,
                IncludeFunctions = false,
                IncludeSignals = false
            };

            var report = handler.AnalyzeProject(options);

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "MY_PATTERN",
                "MY_PATTERN is read via local variable chain: comp.regex.MY_PATTERN");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_VariableReadViaChainWithManyMembers_NoFalsePositive()
    {
        // Reproduces the real scenario more faithfully:
        // - DMCompilerRegEx has many RegEx members (not just one)
        // - DMCompilation has regex property + many other properties
        // - Access happens in a separate file via local variable in a function
        // - The accessing file has no class_name
        var regexCode = @"extends RefCounted
class_name DMCompilerRegEx

var IMPORT_REGEX: RegEx = RegEx.create_from_string(""import"")
var USING_REGEX: RegEx = RegEx.create_from_string(""using"")
var INDENT_REGEX: RegEx = RegEx.create_from_string(""indent"")
var CONDITION_REGEX: RegEx = RegEx.create_from_string(""if|elif"")
var GOTO_REGEX: RegEx = RegEx.create_from_string(""goto"")
var INLINE_RANDOM_REGEX: RegEx = RegEx.create_from_string(""\[\[(?<options>.*?)\]\]"")
var INLINE_CONDITIONALS_REGEX: RegEx = RegEx.create_from_string(""\[if (?<condition>.+?)\]"")
var IMAGE_TAGS_REGEX: RegEx = RegEx.create_from_string(""\[img.*?\](?<path>.+?)\[\/img\]"")
var TAGS_REGEX: RegEx = RegEx.create_from_string(""\[#(?<tags>.*?)\]"")
";
        var compilationCode = @"extends RefCounted
class_name DMCompilation

var file_path: String
var imported_paths: PackedStringArray = []
var using_states: PackedStringArray = []
var labels: Dictionary = {}
var first_label: String = """"
var character_names: PackedStringArray = []
var errors: Array[Dictionary] = []
var lines: Dictionary = {}
var data: Dictionary = {}
var processor: Node = null
var regex: DMCompilerRegEx = DMCompilerRegEx.new()
";
        // No class_name — just extends Node
        var managerCode = @"extends Node

func resolve_random_text(text: String) -> String:
    var compilation: DMCompilation = DMCompilation.new()
    for found: RegExMatch in compilation.regex.INLINE_RANDOM_REGEX.search_all(text):
        var options: PackedStringArray = found.get_string(&""options"").split(&""|"")
        text = text.replace(&""[[%s]]"" % found.get_string(&""options""), options[0])
    var conditionals: Array[RegExMatch] = compilation.regex.INLINE_CONDITIONALS_REGEX.search_all(text)
    for c: RegExMatch in conditionals:
        print(c)
    var tags: Array[RegExMatch] = compilation.regex.IMAGE_TAGS_REGEX.search_all(text)
    for t: RegExMatch in tags:
        print(t)
    return text
";
        var tempPath = TestProjectHelper.CreateTempProject(
            ("compiler_regex.gd", regexCode),
            ("compilation.gd", compilationCode),
            ("manager.gd", managerCode));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = true,
                IncludeFunctions = false,
                IncludeSignals = false
            };

            var report = handler.AnalyzeProject(options);

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "INLINE_RANDOM_REGEX",
                "INLINE_RANDOM_REGEX is read via: compilation.regex.INLINE_RANDOM_REGEX");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "INLINE_CONDITIONALS_REGEX",
                "INLINE_CONDITIONALS_REGEX is read via: compilation.regex.INLINE_CONDITIONALS_REGEX");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "IMAGE_TAGS_REGEX",
                "IMAGE_TAGS_REGEX is read via: compilation.regex.IMAGE_TAGS_REGEX");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "regex",
                "regex is read via: compilation.regex");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_DtoVariableReadCrossFile_NoFalsePositive()
    {
        // DTO class with properties read via typed local variable from another file
        // Reproduces DialogueLine pattern: class has many public vars, other files read them
        var dtoCode = @"extends RefCounted
class_name DialogueLine

var id: String
var next_id: String = """"
var character: String = """"
var text: String = """"
var translation_key: String = """"
var responses: Array = []

func _init(data: Dictionary = {}) -> void:
    if data.size() > 0:
        id = data.id
        next_id = data.next_id
        character = data.character
        text = data.text
";
        var managerCode = @"extends Node

func get_line() -> DialogueLine:
    return DialogueLine.new({id = ""1"", next_id = ""2"", character = ""NPC"", text = ""Hello""})

func process() -> void:
    var line: DialogueLine = get_line()
    print(line.next_id)
    print(line.character)
    print(line.text)
    print(line.translation_key)
    for r in line.responses:
        print(r)
";
        var tempPath = TestProjectHelper.CreateTempProject(
            ("dialogue_line.gd", dtoCode),
            ("manager.gd", managerCode));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = true,
                IncludeFunctions = false,
                IncludeSignals = false
            };

            var report = handler.AnalyzeProject(options);

            // All these properties are read cross-file via typed local variable
            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "next_id",
                "next_id is read via: line.next_id");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "character",
                "character is read via: line.character");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "text",
                "text is read via: line.text");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "translation_key",
                "translation_key is read via: line.translation_key");

            report.Items.Should().NotContain(item =>
                item.Kind == GDDeadCodeKind.Variable &&
                item.Name == "responses",
                "responses is read via: line.responses");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // === Reason Code Tests ===

    [TestMethod]
    public void AnalyzeProject_Items_HaveReasonCodes()
    {
        var code = @"extends Node
class_name ReasonCodeTest

var unused_var: int = 0
const UNUSED_CONST = 42
signal unused_signal

func unused_function():
    pass

func _ready():
    return
    print(""unreachable"")
";
        var tempPath = TestProjectHelper.CreateTempProject(("dead.gd", code));
        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeVariables = true,
                IncludeFunctions = true,
                IncludeSignals = true,
                IncludeUnreachable = true,
                IncludeConstants = true,
                IncludePrivate = false
            };
            var report = handler.AnalyzeProject(options);

            report.Items.Should().NotBeEmpty("should find dead code in test script");

            // Verify specific reason codes per kind
            var variables = report.Items.Where(i => i.Kind == GDDeadCodeKind.Variable).ToList();
            variables.Should().OnlyContain(i => i.ReasonCode == GDDeadCodeReasonCode.VNR,
                "unused variables should have VNR reason code");

            var functions = report.Items.Where(i => i.Kind == GDDeadCodeKind.Function).ToList();
            functions.Should().OnlyContain(i => i.ReasonCode == GDDeadCodeReasonCode.FNC,
                "unused functions should have FNC reason code");

            var signals = report.Items.Where(i => i.Kind == GDDeadCodeKind.Signal).ToList();
            signals.Should().OnlyContain(i => i.ReasonCode == GDDeadCodeReasonCode.SNE,
                "unused signals should have SNE reason code");

            var constants = report.Items.Where(i => i.Kind == GDDeadCodeKind.Constant).ToList();
            constants.Should().OnlyContain(i => i.ReasonCode == GDDeadCodeReasonCode.CNU,
                "unused constants should have CNU reason code");

            var unreachable = report.Items.Where(i => i.Kind == GDDeadCodeKind.Unreachable).ToList();
            unreachable.Should().OnlyContain(i => i.ReasonCode == GDDeadCodeReasonCode.UCR,
                "unreachable code should have UCR reason code");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // === @export/@onready confidence downgrade tests ===

    [TestMethod]
    public void AnalyzeProject_ExportVariable_NotReportedAsStrictDeadCode()
    {
        var code = @"extends Node

@export var speed: float = 10.0
var truly_unused: int = 0

func _ready():
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("player.gd", code));
        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                MaxConfidence = GDReferenceConfidence.Strict,
                IncludeVariables = true
            };
            var report = handler.AnalyzeProject(options);

            // Base handler = Strict only → @export vars excluded
            report.Items.Should().NotContain(i => i.Name == "speed",
                "@export variables should not appear in Strict-only results");

            // truly_unused should still appear
            report.Items.Should().Contain(i =>
                i.Name == "truly_unused" && i.Confidence == GDReferenceConfidence.Strict);
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_OnreadyVariable_NotReportedAsStrictDeadCode()
    {
        var code = @"extends Node

@onready var label = $Label
var truly_unused: int = 0

func _ready():
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("ui.gd", code));
        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                MaxConfidence = GDReferenceConfidence.Strict,
                IncludeVariables = true
            };
            var report = handler.AnalyzeProject(options);

            report.Items.Should().NotContain(i => i.Name == "label",
                "@onready variables should not appear in Strict-only results");
            report.Items.Should().Contain(i => i.Name == "truly_unused");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_ExportOnready_MarkedWithCorrectReasonCodeAndFlag()
    {
        var code = @"extends Node

@export var speed: float = 10.0
@onready var label = $Label
@export_range(0, 100) var health: int = 100
var normal_unused: int = 0
";
        var tempPath = TestProjectHelper.CreateTempProject(("node.gd", code));
        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var service = projectModel.DeadCode;
            var options = new GDDeadCodeOptions
            {
                MaxConfidence = GDReferenceConfidence.Potential,
                IncludeVariables = true
            };
            var report = service.AnalyzeProject(options);

            // @export → VEX, Potential, IsExportedOrOnready=true
            var speedItem = report.Items.FirstOrDefault(i => i.Name == "speed");
            speedItem.Should().NotBeNull("@export var speed should be reported at Potential confidence");
            speedItem!.ReasonCode.Should().Be(GDDeadCodeReasonCode.VEX);
            speedItem.Confidence.Should().Be(GDReferenceConfidence.Potential);
            speedItem.IsExportedOrOnready.Should().BeTrue();

            // @onready → VOR, Potential, IsExportedOrOnready=true
            var labelItem = report.Items.FirstOrDefault(i => i.Name == "label");
            labelItem.Should().NotBeNull("@onready var label should be reported at Potential confidence");
            labelItem!.ReasonCode.Should().Be(GDDeadCodeReasonCode.VOR);
            labelItem.Confidence.Should().Be(GDReferenceConfidence.Potential);
            labelItem.IsExportedOrOnready.Should().BeTrue();

            // @export_range → VEX, Potential
            var healthItem = report.Items.FirstOrDefault(i => i.Name == "health");
            healthItem.Should().NotBeNull("@export_range var health should be reported at Potential confidence");
            healthItem!.ReasonCode.Should().Be(GDDeadCodeReasonCode.VEX);

            // normal → VNR, Strict, IsExportedOrOnready=false
            var normalItem = report.Items.FirstOrDefault(i => i.Name == "normal_unused");
            normalItem.Should().NotBeNull();
            normalItem!.ReasonCode.Should().Be(GDDeadCodeReasonCode.VNR);
            normalItem.IsExportedOrOnready.Should().BeFalse();
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // === Test file exclusion ===

    [TestMethod]
    public void AnalyzeProject_ExcludeTests_SkipsTestFiles()
    {
        // Test the ShouldSkipFile logic directly on the options object
        var options = new GDDeadCodeOptions
        {
            ExcludeTestFiles = true
        };

        // Should skip files matching test patterns
        options.ShouldSkipFile("tests/test_player.gd").Should().BeTrue(
            "'tests/' prefix matches TestPathPatterns");
        options.ShouldSkipFile("test_player.gd").Should().BeTrue(
            "'test_' prefix matches TestPathPatterns");
        options.ShouldSkipFile("unit_test.gd").Should().BeTrue(
            "'unit_test.gd' matches '_test.gd' suffix pattern");
        options.ShouldSkipFile("player.gd").Should().BeFalse(
            "'player.gd' does not match any pattern");
        options.ShouldSkipFile("src/game/player_test.gd").Should().BeTrue(
            "'_test.gd' suffix matches TestPathPatterns");

        // When disabled, nothing is skipped
        options.ExcludeTestFiles = false;
        options.ShouldSkipFile("tests/test_player.gd").Should().BeFalse(
            "ExcludeTestFiles=false means nothing is skipped");
    }

    // === Report metadata tests ===

    [TestMethod]
    public void AnalyzeProject_Report_HasMetadata()
    {
        var code = @"extends Node
var unused: int = 0
";
        var tempPath = TestProjectHelper.CreateTempProject(("main.gd", code));
        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var report = handler.AnalyzeProject(new GDDeadCodeOptions());

            report.FilesAnalyzed.Should().BeGreaterThan(0);
            report.VirtualMethodsSkipped.Should().BeGreaterThan(0,
                "should count Godot virtual methods in skip list");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // === Report helper tests ===

    [TestMethod]
    public void Report_ByConfidence_GroupsCorrectly()
    {
        var items = new List<GDDeadCodeItem>
        {
            new(GDDeadCodeKind.Variable, "a", "f.gd") { Confidence = GDReferenceConfidence.Strict },
            new(GDDeadCodeKind.Variable, "b", "f.gd") { Confidence = GDReferenceConfidence.Potential },
            new(GDDeadCodeKind.Variable, "c", "f.gd") { Confidence = GDReferenceConfidence.Strict },
        };
        var report = new GDDeadCodeReport(items);

        var groups = report.ByConfidence.ToList();
        groups.Should().HaveCount(2);
        groups.First(g => g.Key == GDReferenceConfidence.Strict).Count().Should().Be(2);
        groups.First(g => g.Key == GDReferenceConfidence.Potential).Count().Should().Be(1);
    }

    [TestMethod]
    public void Report_TopOffenders_ReturnsCorrectOrder()
    {
        var items = new List<GDDeadCodeItem>
        {
            new(GDDeadCodeKind.Variable, "a", "big.gd"),
            new(GDDeadCodeKind.Variable, "b", "big.gd"),
            new(GDDeadCodeKind.Variable, "c", "big.gd"),
            new(GDDeadCodeKind.Variable, "d", "small.gd"),
        };
        var report = new GDDeadCodeReport(items);

        var top = report.TopOffenders(2);
        top.Should().HaveCount(2);
        top[0].FilePath.Should().Be("big.gd");
        top[0].Count.Should().Be(3);
        top[1].FilePath.Should().Be("small.gd");
        top[1].Count.Should().Be(1);
    }

    // === Output helper tests ===

    [TestMethod]
    public void OutputHelper_GetReasonCodeLabel_AllCodesHaveLabels()
    {
        foreach (GDDeadCodeReasonCode code in Enum.GetValues(typeof(GDDeadCodeReasonCode)))
        {
            var label = GDDeadCodeOutputHelper.GetReasonCodeLabel(code);
            label.Should().NotBeNullOrEmpty($"reason code {code} must have a label");
        }
    }

    [TestMethod]
    public void OutputHelper_FormatSummaryLine_ContainsCorrectCounts()
    {
        var items = new List<GDDeadCodeItem>
        {
            new(GDDeadCodeKind.Variable, "a", "f.gd"),
            new(GDDeadCodeKind.Variable, "b", "f.gd"),
            new(GDDeadCodeKind.Function, "c", "f.gd"),
        };
        var report = new GDDeadCodeReport(items);

        var line = GDDeadCodeOutputHelper.FormatSummaryLine(report);

        line.Should().Contain("dead-code:");
        line.Should().Contain("3 items");
        line.Should().Contain("2 var");
        line.Should().Contain("1 func");
    }

    // === Output formatting tests ===

    [TestMethod]
    public void WriteDeadCodeOutput_ContainsReasonCodes()
    {
        var code = @"extends Node
var unused: int = 0
func unused_func():
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("test.gd", code));
        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var options = new GDDeadCodeCommandOptions();
            var cmd = new GDDeadCodeCommand(tempPath, formatter, output, options: options);
            cmd.ExecuteAsync().GetAwaiter().GetResult();

            var text = output.ToString();
            text.Should().Contain("[VNR]", "output should contain reason code for unused variable");
            text.Should().Contain("[FNC]", "output should contain reason code for unused function");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void WriteDeadCodeOutput_ContainsTopFilesSection()
    {
        var code = @"extends Node
var a: int = 0
var b: int = 0
";
        var tempPath = TestProjectHelper.CreateTempProject(("test.gd", code));
        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var cmd = new GDDeadCodeCommand(tempPath, formatter, output);
            cmd.ExecuteAsync().GetAwaiter().GetResult();

            var text = output.ToString();
            text.Should().Contain("Top files:");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void WriteDeadCodeOutput_ContainsByKindSummary()
    {
        var code = @"extends Node
var unused: int = 0
func unused_func():
    pass
signal unused_signal
";
        var tempPath = TestProjectHelper.CreateTempProject(("test.gd", code));
        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var cmd = new GDDeadCodeCommand(tempPath, formatter, output);
            cmd.ExecuteAsync().GetAwaiter().GetResult();

            var text = output.ToString();
            text.Should().Contain("By kind:");
            text.Should().Contain("Variable:");
            text.Should().Contain("Function:");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void WriteDeadCodeOutput_ContainsLegend()
    {
        var code = @"extends Node
var unused: int = 0
";
        var tempPath = TestProjectHelper.CreateTempProject(("test.gd", code));
        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var cmd = new GDDeadCodeCommand(tempPath, formatter, output);
            cmd.ExecuteAsync().GetAwaiter().GetResult();

            var text = output.ToString();
            text.Should().Contain("Legend:");
            text.Should().Contain("VNR");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void WriteDeadCodeOutput_Legend_OnlyShowsPresentCodes()
    {
        var code = @"extends Node
var unused: int = 0
";
        var tempPath = TestProjectHelper.CreateTempProject(("test.gd", code));
        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var cmd = new GDDeadCodeCommand(tempPath, formatter, output);
            cmd.ExecuteAsync().GetAwaiter().GetResult();

            var text = output.ToString();
            text.Should().Contain("VNR");
            // FNC and SNE should not be in the legend if no functions/signals are dead
            text.Should().NotContain("FNC", "no functions are dead, FNC should not appear in legend");
            text.Should().NotContain("SNE", "no signals are dead, SNE should not appear in legend");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void WriteDeadCodeOutput_QuietMode_OutputsSingleLine()
    {
        var code = @"extends Node
var unused: int = 0
func unused_func():
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("test.gd", code));
        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var options = new GDDeadCodeCommandOptions { Quiet = true };
            var cmd = new GDDeadCodeCommand(tempPath, formatter, output, options: options);
            cmd.ExecuteAsync().GetAwaiter().GetResult();

            var text = output.ToString();
            var lines = text.Trim().Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

            lines.Where(l => l.StartsWith("dead-code:")).Should().HaveCount(1,
                "quiet mode should output exactly one summary line starting with 'dead-code:'");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // === Evidence collection test ===

    [TestMethod]
    public void AnalyzeProject_CollectEvidence_PopulatesEvidenceField()
    {
        var code = @"extends Node
func unused_func():
    pass
func _ready():
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("test.gd", code));
        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            using var projectModel = new GDProjectSemanticModel(project);
            var handler = new GDDeadCodeHandler(projectModel);
            var options = new GDDeadCodeOptions
            {
                IncludeFunctions = true,
                IncludePrivate = false,
                CollectEvidence = true
            };
            var report = handler.AnalyzeProject(options);

            var funcItem = report.Items.FirstOrDefault(i => i.Name == "unused_func");
            funcItem.Should().NotBeNull();
            funcItem!.Evidence.Should().NotBeNull("evidence should be collected when CollectEvidence=true");
            funcItem.Evidence!.CallSitesScanned.Should().BeGreaterThanOrEqualTo(0);
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }
}
