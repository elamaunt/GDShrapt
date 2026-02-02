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
            IncludeFunctions = true
        };

        // Act
        var report = _handler!.AnalyzeFile(filePath, options);

        // Assert
        // Base handler should only return Strict confidence items
        report.Items.Should().OnlyContain(item => item.Confidence == GDReferenceConfidence.Strict);
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
            IncludeSignals = true
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
            IncludeUnreachable = false // Disable unreachable to test only variables
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
            IncludeUnreachable = false // Disable unreachable to test only functions
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
            IncludeUnreachable = false // Disable unreachable to test only signals
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
}
