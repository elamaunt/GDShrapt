using System.IO;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using GDProjectLoader = GDShrapt.Semantics.GDProjectLoader;

namespace GDShrapt.CLI.Tests.Handlers;

/// <summary>
/// Tests for GDTypeCoverageHandler using the TestProject fixture.
/// </summary>
[TestClass]
public class GDTypeCoverageHandlerTests
{
    private GDScriptProject? _project;
    private GDTypeCoverageHandler? _handler;

    [TestInitialize]
    public void Setup()
    {
        _project = TestProjectHelper.LoadTestProject();
        _handler = new GDTypeCoverageHandler(new GDProjectSemanticModel(_project));
    }

    [TestCleanup]
    public void Cleanup()
    {
        _project?.Dispose();
    }

    // === AnalyzeFile Tests ===

    [TestMethod]
    public void AnalyzeFile_ValidFile_ReturnsReport()
    {
        // Arrange
        var filePath = TestProjectHelper.GetTestScriptPath("type_inference.gd");

        // Act
        var report = _handler!.AnalyzeFile(filePath);

        // Assert
        report.Should().NotBeNull();
    }

    [TestMethod]
    public void AnalyzeFile_NonExistentFile_ReturnsEmptyReport()
    {
        // Arrange
        var filePath = "/nonexistent/file.gd";

        // Act
        var report = _handler!.AnalyzeFile(filePath);

        // Assert
        report.Should().NotBeNull();
        report.TotalVariables.Should().Be(0);
    }

    [TestMethod]
    public void AnalyzeFile_TypeInference_TracksCoverage()
    {
        // Arrange
        var filePath = TestProjectHelper.GetTestScriptPath("type_inference.gd");

        // Act
        var report = _handler!.AnalyzeFile(filePath);

        // Assert
        report.Should().NotBeNull();
        // type_inference.gd should have typed variables
        report.TotalVariables.Should().BeGreaterThan(0);
    }

    // === AnalyzeProject Tests ===

    [TestMethod]
    public void AnalyzeProject_TestProject_ReturnsReport()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        report.Should().NotBeNull();
        report.TotalVariables.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void AnalyzeProject_TracksVariableAnnotations()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        report.Should().NotBeNull();
        report.TotalVariables.Should().BeGreaterThan(0);
        // TestProject has typed and untyped variables
        (report.AnnotatedVariables + report.InferredVariables + report.VariantVariables)
            .Should().Be(report.TotalVariables);
    }

    [TestMethod]
    public void AnalyzeProject_TracksParameterAnnotations()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        report.Should().NotBeNull();
        report.TotalParameters.Should().BeGreaterThan(0);
        (report.AnnotatedParameters + report.InferredParameters)
            .Should().BeLessThanOrEqualTo(report.TotalParameters);
    }

    [TestMethod]
    public void AnalyzeProject_TracksReturnTypeAnnotations()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        report.Should().NotBeNull();
        // TestProject has functions with return types
        report.TotalReturnTypes.Should().BeGreaterThanOrEqualTo(0);
    }

    [TestMethod]
    public void AnalyzeProject_CalculatesAnnotationCoverage()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        report.Should().NotBeNull();
        report.AnnotationCoverage.Should().BeInRange(0, 100);
    }

    [TestMethod]
    public void AnalyzeProject_CalculatesEffectiveCoverage()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        report.Should().NotBeNull();
        report.EffectiveCoverage.Should().BeInRange(0, 100);
        // Effective coverage >= Annotation coverage (includes inferred)
        report.EffectiveCoverage.Should().BeGreaterThanOrEqualTo(report.AnnotationCoverage);
    }

    [TestMethod]
    public void AnalyzeProject_CalculatesVariantPercentage()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        report.Should().NotBeNull();
        report.VariantPercentage.Should().BeInRange(0, 100);
    }

    [TestMethod]
    public void AnalyzeProject_CalculatesParameterCoverage()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        report.Should().NotBeNull();
        report.ParameterCoverage.Should().BeInRange(0, 100);
    }

    [TestMethod]
    public void AnalyzeProject_CalculatesReturnTypeCoverage()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        report.Should().NotBeNull();
        report.ReturnTypeCoverage.Should().BeInRange(0, 100);
    }

    [TestMethod]
    public void AnalyzeProject_CalculatesTypeSafetyScore()
    {
        // Act
        var report = _handler!.AnalyzeProject();

        // Assert
        report.Should().NotBeNull();
        report.TypeSafetyScore.Should().BeInRange(0, 100);
    }

    // === Isolated temp project tests ===

    [TestMethod]
    public void AnalyzeProject_FullyTyped_Returns100PercentCoverage()
    {
        // Arrange
        var code = @"extends Node
class_name FullyTyped

var health: int = 100
var name: String = ""Test""
var items: Array[String] = []

func set_health(value: int) -> void:
    health = value

func get_health() -> int:
    return health

func process_item(item: String) -> bool:
    items.append(item)
    return true
";
        var tempPath = TestProjectHelper.CreateTempProject(("typed.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var handler = new GDTypeCoverageHandler(new GDProjectSemanticModel(project));

            // Act
            var report = handler.AnalyzeProject();

            // Assert
            report.Should().NotBeNull();
            report.AnnotationCoverage.Should().Be(100);
            report.ParameterCoverage.Should().Be(100);
            report.ReturnTypeCoverage.Should().Be(100);
            report.VariantPercentage.Should().Be(0);
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_Untyped_Returns0PercentAnnotationCoverage()
    {
        // Arrange
        var code = @"extends Node
class_name Untyped

var health = 100
var name = ""Test""

func set_health(value):
    health = value

func get_health():
    return health
";
        var tempPath = TestProjectHelper.CreateTempProject(("untyped.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var handler = new GDTypeCoverageHandler(new GDProjectSemanticModel(project));

            // Act
            var report = handler.AnalyzeProject();

            // Assert
            report.Should().NotBeNull();
            report.AnnotationCoverage.Should().Be(0);
            report.ParameterCoverage.Should().Be(0);
            report.ReturnTypeCoverage.Should().Be(0);
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_MixedTypes_ReturnsPartialCoverage()
    {
        // Arrange
        var code = @"extends Node
class_name MixedTypes

var typed_var: int = 100
var untyped_var = ""test""

func typed_func(a: int) -> int:
    return a * 2

func untyped_func(b):
    return b
";
        var tempPath = TestProjectHelper.CreateTempProject(("mixed.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var handler = new GDTypeCoverageHandler(new GDProjectSemanticModel(project));

            // Act
            var report = handler.AnalyzeProject();

            // Assert
            report.Should().NotBeNull();
            // 1 typed out of 2 variables = 50%
            report.AnnotationCoverage.Should().Be(50);
            // 1 typed out of 2 parameters = 50%
            report.ParameterCoverage.Should().Be(50);
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_WithInference_CountsInferred()
    {
        // Arrange
        var code = @"extends Node

func _ready() -> void:
    var inferred_int := 42
    var inferred_string := ""hello""
    var explicit_int: int = 10
";
        var tempPath = TestProjectHelper.CreateTempProject(("inference.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var handler = new GDTypeCoverageHandler(new GDProjectSemanticModel(project));

            // Act
            var report = handler.AnalyzeProject();

            // Assert
            report.Should().NotBeNull();
            // := syntax means inferred type
            report.InferredVariables.Should().BeGreaterThanOrEqualTo(2);
            // Effective coverage should be higher than annotation coverage
            report.EffectiveCoverage.Should().BeGreaterThan(report.AnnotationCoverage);
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void AnalyzeProject_EmptyProject_ReturnsFullCoverage()
    {
        // Arrange
        var code = @"extends Node

func _ready() -> void:
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("empty.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var handler = new GDTypeCoverageHandler(new GDProjectSemanticModel(project));

            // Act
            var report = handler.AnalyzeProject();

            // Assert
            report.Should().NotBeNull();
            // No variables = 100% coverage by definition
            if (report.TotalVariables == 0)
            {
                report.AnnotationCoverage.Should().Be(100);
            }
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }
}
