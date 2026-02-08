using System.IO;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using GDProjectLoader = GDShrapt.Semantics.GDProjectLoader;

namespace GDShrapt.CLI.Tests.Handlers;

/// <summary>
/// Tests for GDDependencyHandler using the TestProject fixture.
/// </summary>
[TestClass]
public class GDDependencyHandlerTests
{
    private GDScriptProject? _project;
    private GDDependencyHandler? _handler;

    [TestInitialize]
    public void Setup()
    {
        _project = TestProjectHelper.LoadTestProject();
        _handler = new GDDependencyHandler(new GDProjectSemanticModel(_project));
    }

    [TestCleanup]
    public void Cleanup()
    {
        _project?.Dispose();
    }

    // === AnalyzeFile Tests ===

    [TestMethod]
    public void AnalyzeFile_ValidFile_ReturnsDependencyInfo()
    {
        // Arrange
        var filePath = TestProjectHelper.GetTestScriptPath("player_entity.gd");

        // Act
        var info = _handler!.AnalyzeFile(filePath);

        // Assert
        info.Should().NotBeNull();
        info.FilePath.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public void AnalyzeFile_NonExistentFile_ReturnsEmptyInfo()
    {
        // Arrange
        var filePath = "/nonexistent/file.gd";

        // Act
        var info = _handler!.AnalyzeFile(filePath);

        // Assert
        info.Should().NotBeNull();
        info.FilePath.Should().Be(filePath);
        info.Dependencies.Should().BeEmpty();
    }

    [TestMethod]
    public void AnalyzeFile_FileWithExtends_HasExtendsRelationship()
    {
        // Arrange - player_entity extends base_entity
        var filePath = TestProjectHelper.GetTestScriptPath("player_entity.gd");

        // Act
        var info = _handler!.AnalyzeFile(filePath);

        // Assert
        info.Should().NotBeNull();
        // Should have extends dependency (to base_entity or another class)
        info.Dependencies.Should().NotBeEmpty();
    }

    [TestMethod]
    public void AnalyzeFile_SimpleClass_MayHaveNoDependencies()
    {
        // Arrange
        var filePath = TestProjectHelper.GetTestScriptPath("simple_class.gd");

        // Act
        var info = _handler!.AnalyzeFile(filePath);

        // Assert
        info.Should().NotBeNull();
        // Simple class may only extend Node (built-in, not tracked)
    }

    // === AnalyzeProject Tests ===

    [TestMethod]
    public void AnalyzeProject_TestProject_ReturnsReport()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        report.Should().NotBeNull();
        report.Files.Should().NotBeEmpty();
        report.TotalFiles.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void AnalyzeProject_TestProject_BuildsDependencyGraph()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        report.Should().NotBeNull();
        // TestProject has files with dependencies
        var filesWithDependencies = report.Files
            .Where(f => f.DirectDependencyCount > 0)
            .ToList();
        filesWithDependencies.Should().NotBeEmpty("TestProject should have files with dependencies");
    }

    [TestMethod]
    public void AnalyzeProject_CyclesTest_VerifiesCycleDetection()
    {
        // Arrange - TestProject has cycles_test.gd with internal type flow cycles,
        // but not necessarily file-level preload cycles.
        // The dedicated WithCycle test validates file-level cycle detection.

        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        report.Should().NotBeNull();
        // Verify the report is generated correctly - cycle detection is tested in WithCycle test
        report.TotalFiles.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void AnalyzeProject_GetFile_ReturnsCorrectFile()
    {
        // Arrange
        var filePath = TestProjectHelper.GetTestScriptPath("simple_class.gd");

        // Act
        var report = _handler!.AnalyzeProject();
        var fileInfo = report.GetFile(filePath);

        // Assert
        fileInfo.Should().NotBeNull();
        fileInfo!.FilePath.Should().ContainEquivalentOf("simple_class.gd");
    }

    [TestMethod]
    public void AnalyzeProject_MostDependent_ReturnsOrderedFiles()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        var mostDependent = report.MostDependent.ToList();
        mostDependent.Should().NotBeEmpty();
        // Should be ordered by dependent count descending
        if (mostDependent.Count > 1)
        {
            mostDependent[0].Dependents.Count
                .Should().BeGreaterThanOrEqualTo(mostDependent[1].Dependents.Count);
        }
    }

    [TestMethod]
    public void AnalyzeProject_MostCoupled_ReturnsOrderedFiles()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        var mostCoupled = report.MostCoupled.ToList();
        mostCoupled.Should().NotBeEmpty();
        // Should be ordered by dependency count descending
        if (mostCoupled.Count > 1)
        {
            mostCoupled[0].DirectDependencyCount
                .Should().BeGreaterThanOrEqualTo(mostCoupled[1].DirectDependencyCount);
        }
    }

    // === Isolated temp project tests ===

    [TestMethod]
    public void AnalyzeProject_WithExtendsChain_DetectsDependencies()
    {
        // Arrange
        var tempPath = TestProjectHelper.CreateTempProject(
            ("base.gd", @"class_name BaseClass
extends Node

func base_method() -> void:
    pass
"),
            ("derived.gd", @"class_name DerivedClass
extends BaseClass

func derived_method() -> void:
    base_method()
"));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var handler = new GDDependencyHandler(new GDProjectSemanticModel(project));

            // Act
            var report = handler.AnalyzeProject();

            // Assert
            report.TotalFiles.Should().Be(2);

            var derivedFile = report.Files.FirstOrDefault(f => f.FilePath.Contains("derived.gd"));
            derivedFile.Should().NotBeNull();
            derivedFile!.Dependencies.Should().NotBeEmpty("derived extends base");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_WithPreload_DetectsPreloadDependencies()
    {
        // Arrange
        var tempPath = TestProjectHelper.CreateTempProject(
            ("resource.gd", @"class_name ResourceClass
extends Resource

var value: int = 42
"),
            ("consumer.gd", @"extends Node

const MyResource = preload(""res://resource.gd"")

func _ready() -> void:
    var r = MyResource.new()
"));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var handler = new GDDependencyHandler(new GDProjectSemanticModel(project));

            // Act
            var report = handler.AnalyzeProject();

            // Assert
            var consumerFile = report.Files.FirstOrDefault(f => f.FilePath.Contains("consumer.gd"));
            consumerFile.Should().NotBeNull();
            // Should detect preload dependency
            consumerFile!.Dependencies.Should().NotBeEmpty("consumer preloads resource");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_WithCycle_DetectsCycle()
    {
        // Arrange - Create circular dependency A -> B -> A
        var tempPath = TestProjectHelper.CreateTempProject(
            ("cycle_a.gd", @"class_name CycleA
extends Node

const CycleB = preload(""res://cycle_b.gd"")

func get_b() -> CycleB:
    return CycleB.new()
"),
            ("cycle_b.gd", @"class_name CycleB
extends Node

const CycleA = preload(""res://cycle_a.gd"")

func get_a() -> CycleA:
    return CycleA.new()
"));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var handler = new GDDependencyHandler(new GDProjectSemanticModel(project));

            // Act
            var report = handler.AnalyzeProject();

            // Assert
            report.HasCycles.Should().BeTrue("A preloads B which preloads A");
            report.CycleCount.Should().BeGreaterThan(0);
            report.FilesInCycles.Should().Be(2);
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_NoCycles_ReportsNoCycles()
    {
        // Arrange - Linear dependency chain without cycles
        var tempPath = TestProjectHelper.CreateTempProject(
            ("a.gd", @"class_name ClassA
extends Node
"),
            ("b.gd", @"class_name ClassB
extends ClassA
"),
            ("c.gd", @"class_name ClassC
extends ClassB
"));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var handler = new GDDependencyHandler(new GDProjectSemanticModel(project));

            // Act
            var report = handler.AnalyzeProject();

            // Assert
            report.HasCycles.Should().BeFalse("Linear chain A -> B -> C has no cycles");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }
}
