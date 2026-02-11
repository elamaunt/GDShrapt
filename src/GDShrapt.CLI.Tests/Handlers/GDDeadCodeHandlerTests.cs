using System.IO;
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
}
