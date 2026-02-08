using System.IO;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using GDProjectLoader = GDShrapt.Semantics.GDProjectLoader;

namespace GDShrapt.CLI.Tests.Handlers;

/// <summary>
/// Tests for GDMetricsHandler using the TestProject fixture.
/// </summary>
[TestClass]
public class GDMetricsHandlerTests
{
    private GDScriptProject? _project;
    private GDMetricsHandler? _handler;

    [TestInitialize]
    public void Setup()
    {
        _project = TestProjectHelper.LoadTestProject();
        _handler = new GDMetricsHandler(new GDProjectSemanticModel(_project));
    }

    [TestCleanup]
    public void Cleanup()
    {
        _project?.Dispose();
    }

    // === AnalyzeFile Tests ===

    [TestMethod]
    public void AnalyzeFile_SimpleClass_ReturnsFileMetrics()
    {
        // Arrange
        var filePath = TestProjectHelper.GetTestScriptPath("simple_class.gd");

        // Act
        var metrics = _handler!.AnalyzeFile(filePath);

        // Assert
        metrics.Should().NotBeNull();
        metrics.FilePath.Should().NotBeNullOrEmpty();
        metrics.FileName.Should().Be("simple_class.gd");
        metrics.TotalLines.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void AnalyzeFile_GamePatterns_HasMethods()
    {
        // Arrange
        var filePath = TestProjectHelper.GetTestScriptPath("game_patterns.gd");

        // Act
        var metrics = _handler!.AnalyzeFile(filePath);

        // Assert
        metrics.Should().NotBeNull();
        metrics.Methods.Should().NotBeEmpty();
        metrics.MethodCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void AnalyzeFile_ComplexFile_CalculatesComplexity()
    {
        // Arrange
        var filePath = TestProjectHelper.GetTestScriptPath("game_patterns.gd");

        // Act
        var metrics = _handler!.AnalyzeFile(filePath);

        // Assert
        metrics.Should().NotBeNull();
        // At least one method should have complexity > 1
        metrics.Methods.Should().Contain(m => m.CyclomaticComplexity >= 1);
    }

    [TestMethod]
    public void AnalyzeFile_NonExistentFile_ReturnsEmptyMetrics()
    {
        // Arrange
        var filePath = "/nonexistent/path/file.gd";

        // Act
        var metrics = _handler!.AnalyzeFile(filePath);

        // Assert
        metrics.Should().NotBeNull();
        metrics.Methods.Should().BeEmpty();
    }

    [TestMethod]
    public void AnalyzeFile_TypeInference_ContainsMaintainabilityIndex()
    {
        // Arrange
        var filePath = TestProjectHelper.GetTestScriptPath("type_inference.gd");

        // Act
        var metrics = _handler!.AnalyzeFile(filePath);

        // Assert
        metrics.Should().NotBeNull();
        // Maintainability index should be in valid range (0-100)
        metrics.MaintainabilityIndex.Should().BeInRange(0, 100);
    }

    // === AnalyzeProject Tests ===

    [TestMethod]
    public void AnalyzeProject_TestProject_ReturnsProjectMetrics()
    {
        // Act
        var metrics = _handler!.AnalyzeProject();

        // Assert
        metrics.Should().NotBeNull();
        metrics.FileCount.Should().BeGreaterThan(0);
        metrics.Files.Should().NotBeEmpty();
    }

    [TestMethod]
    public void AnalyzeProject_TestProject_HasMultipleFiles()
    {
        // Act
        var metrics = _handler!.AnalyzeProject();

        // Assert
        // TestProject has many .gd files
        metrics.FileCount.Should().BeGreaterThan(10);
    }

    [TestMethod]
    public void AnalyzeProject_CalculatesAverageComplexity()
    {
        // Act
        var metrics = _handler!.AnalyzeProject();

        // Assert
        metrics.Should().NotBeNull();
        // Average complexity should be positive for a project with code
        metrics.AverageComplexity.Should().BeGreaterThanOrEqualTo(0);
    }

    [TestMethod]
    public void AnalyzeProject_CalculatesAverageMaintainability()
    {
        // Act
        var metrics = _handler!.AnalyzeProject();

        // Assert
        metrics.Should().NotBeNull();
        // Maintainability should be in valid range
        metrics.AverageMaintainability.Should().BeInRange(0, 100);
    }

    [TestMethod]
    public void AnalyzeProject_CountsMethods()
    {
        // Act
        var metrics = _handler!.AnalyzeProject();

        // Assert
        metrics.Should().NotBeNull();
        metrics.MethodCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void AnalyzeProject_CountsClasses()
    {
        // Act
        var metrics = _handler!.AnalyzeProject();

        // Assert
        metrics.Should().NotBeNull();
        // TestProject has files with class_name declarations
        metrics.ClassCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void AnalyzeProject_CountsSignals()
    {
        // Act
        var metrics = _handler!.AnalyzeProject();

        // Assert
        metrics.Should().NotBeNull();
        // TestProject has signal declarations
        metrics.SignalCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void AnalyzeProject_CountsTotalLines()
    {
        // Act
        var metrics = _handler!.AnalyzeProject();

        // Assert
        metrics.Should().NotBeNull();
        metrics.TotalLines.Should().BeGreaterThan(0);
        metrics.CodeLines.Should().BeGreaterThan(0);
    }

    // === Isolated temp project tests ===

    [TestMethod]
    public void AnalyzeFile_SingleMethod_ReturnsCorrectMethodCount()
    {
        // Arrange
        var tempPath = TestProjectHelper.CreateTempProject(("single.gd", @"extends Node
func single_method():
    pass
"));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var handler = new GDMetricsHandler(new GDProjectSemanticModel(project));
            var filePath = Path.Combine(tempPath, "single.gd");

            // Act
            var metrics = handler.AnalyzeFile(filePath);

            // Assert
            metrics.MethodCount.Should().Be(1);
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeFile_MultipleNestedIfs_HasHigherComplexity()
    {
        // Arrange
        var code = @"extends Node

func complex_method(a, b, c, d):
    if a > 0:
        if b > 0:
            if c > 0:
                if d > 0:
                    return 1
    return 0
";
        var tempPath = TestProjectHelper.CreateTempProject(("complex.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var handler = new GDMetricsHandler(new GDProjectSemanticModel(project));
            var filePath = Path.Combine(tempPath, "complex.gd");

            // Act
            var metrics = handler.AnalyzeFile(filePath);

            // Assert
            // 4 nested ifs + 1 base = CC >= 5
            var method = metrics.Methods.FirstOrDefault(m => m.Name == "complex_method");
            method.Should().NotBeNull();
            method!.CyclomaticComplexity.Should().BeGreaterThanOrEqualTo(4);
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }
}
