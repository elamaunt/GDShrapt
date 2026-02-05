using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Unit tests for GDInferenceVisualizationService.
/// Tests the API for plugin visualization.
/// </summary>
[TestClass]
public class InferenceVisualizationServiceTests
{
    #region Project Report Tests

    [TestMethod]
    public void GetProjectReport_ReturnsReport()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    return 10
""");
        var service = new GDInferenceVisualizationService(project);

        // Act
        var report = service.GetProjectReport();

        // Assert
        Assert.IsNotNull(report);
        Assert.IsTrue(report.Methods.Count >= 1);
    }

    [TestMethod]
    public void GetMethodReport_ReturnsMethodInfo()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    return 10
""");
        var service = new GDInferenceVisualizationService(project);

        // Act
        var report = service.GetMethodReport("Player", "attack");

        // Assert
        Assert.IsNotNull(report);
        Assert.AreEqual("Player", report.ClassName);
        Assert.AreEqual("attack", report.MethodName);
    }

    [TestMethod]
    public void GetMethodReport_NonexistentMethod_ReturnsNull()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""");
        var service = new GDInferenceVisualizationService(project);

        // Act
        var report = service.GetMethodReport("Player", "nonexistent");

        // Assert
        Assert.IsNull(report);
    }

    #endregion

    #region Call Site Information Tests

    [TestMethod]
    public void GetParameterCallSites_ReturnsCallSites()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Game

func test():
    var p = Player.new()
    p.attack(10)
    p.attack(20)
""");
        var service = new GDInferenceVisualizationService(project);

        // Act
        var callSites = service.GetParameterCallSites("Player", "attack", "target").ToList();

        // Assert
        Assert.AreEqual(2, callSites.Count);
    }

    [TestMethod]
    public void GetMethodCallSites_ReturnsUniqueCallLocations()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(a, b):
    pass
""", """
class_name Game

func test():
    var p = Player.new()
    p.attack(1, 2)
    p.attack(3, 4)
""");
        var service = new GDInferenceVisualizationService(project);

        // Act
        var callSites = service.GetMethodCallSites("Player", "attack").ToList();

        // Assert
        // Should have 2 unique call locations (each call is counted once)
        Assert.AreEqual(2, callSites.Count);
    }

    #endregion

    #region Dependency Graph Tests

    [TestMethod]
    public void GetDependencyGraph_ReturnsGraph()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

var enemy: Enemy

func attack():
    enemy.take_damage(10)
""", """
class_name Enemy

func take_damage(amount):
    pass
""");
        var service = new GDInferenceVisualizationService(project);

        // Act
        var graph = service.GetDependencyGraph();

        // Assert
        Assert.IsNotNull(graph);
        Assert.IsTrue(graph.Nodes.Count >= 2);
    }

    [TestMethod]
    public void GetMethodDependencies_ReturnsOutgoingEdges()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

var enemy: Enemy

func attack():
    enemy.take_damage(10)
""", """
class_name Enemy

func take_damage(amount):
    pass
""");
        var service = new GDInferenceVisualizationService(project);

        // Act
        var dependencies = service.GetMethodDependencies("Player.attack").ToList();

        // Assert
        // Player.attack calls Enemy.take_damage
        Assert.IsTrue(dependencies.Contains("Enemy.take_damage"));
    }

    [TestMethod]
    public void GetMethodDependents_ReturnsIncomingEdges()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

var enemy: Enemy

func attack():
    enemy.take_damage(10)
""", """
class_name Enemy

func take_damage(amount):
    pass
""");
        var service = new GDInferenceVisualizationService(project);

        // Act
        var dependents = service.GetMethodDependents("Enemy.take_damage").ToList();

        // Assert
        // Player.attack depends on Enemy.take_damage
        Assert.IsTrue(dependents.Contains("Player.attack"));
    }

    [TestMethod]
    public void GetCyclesContainingMethod_ReturnsCycles()
    {
        // Arrange - Create a cycle
        var project = CreateProject("""
class_name A

func foo():
    foo()
""");
        var service = new GDInferenceVisualizationService(project);

        // Act
        var cycles = service.GetCyclesContainingMethod("A.foo").ToList();

        // Assert
        Assert.IsTrue(cycles.Count >= 1);
        Assert.IsTrue(cycles.Any(c => c.Contains("A.foo")));
    }

    #endregion

    #region Statistics Tests

    [TestMethod]
    public void GetStatistics_ReturnsStats()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    return 10

func defend():
    return 5
""", """
class_name Game

func test():
    var p = Player.new()
    p.attack(10)
""");
        var service = new GDInferenceVisualizationService(project);

        // Act
        var stats = service.GetStatistics();

        // Assert
        Assert.IsTrue(stats.TotalMethods >= 3);
        Assert.IsTrue(stats.MethodsWithInferredParams >= 1);
        Assert.IsTrue(stats.MethodsWithInferredReturn >= 1);
    }

    [TestMethod]
    public void GetStatistics_EmptyProject_ReturnsZeros()
    {
        // Arrange
        var project = new GDScriptProject();
        var service = new GDInferenceVisualizationService(project);

        // Act
        var stats = service.GetStatistics();

        // Assert
        Assert.AreEqual(0, stats.TotalMethods);
        Assert.AreEqual(0, stats.TotalCallSites);
    }

    #endregion

    #region Export Tests

    [TestMethod]
    public void ExportToJson_ReturnsValidJson()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    return 10
""");
        var service = new GDInferenceVisualizationService(project);

        // Act
        var json = service.ExportToJson();

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.StartsWith("{"));
        Assert.IsTrue(json.EndsWith("}"));
        Assert.IsTrue(json.Contains("Player"));
        Assert.IsTrue(json.Contains("attack"));
    }

    [TestMethod]
    public void ExportMethodToJson_ReturnsMethodJson()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    return 10
""");
        var service = new GDInferenceVisualizationService(project);

        // Act
        var json = service.ExportMethodToJson("Player", "attack");

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("attack"));
        Assert.IsTrue(json.Contains("target"));
    }

    [TestMethod]
    public void ExportMethodToJson_NonexistentMethod_ReturnsNull()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""");
        var service = new GDInferenceVisualizationService(project);

        // Act
        var json = service.ExportMethodToJson("Player", "nonexistent");

        // Assert
        Assert.IsNull(json);
    }

    #endregion

    #region Cache Tests

    [TestMethod]
    public void InvalidateCache_ForcesRegeneration()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""");
        var service = new GDInferenceVisualizationService(project);

        // First call generates cache
        var report1 = service.GetProjectReport();
        Assert.IsNotNull(report1);

        // Act - Invalidate cache
        service.InvalidateCache();

        // Second call should regenerate
        var report2 = service.GetProjectReport();

        // Assert
        Assert.IsNotNull(report2);
        // Reports should have same content but may be different objects
        Assert.AreEqual(report1.TotalMethodsAnalyzed, report2.TotalMethodsAnalyzed);
    }

    #endregion

    #region Helper Methods

    private static GDScriptProject CreateProject(params string[] scripts)
    {
        var project = new GDScriptProject(scripts);
        project.AnalyzeAll();
        return project;
    }

    #endregion
}
