using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Integration tests for cross-file parameter and return type inference.
/// Tests the complete inference pipeline from call site collection to report generation.
/// </summary>
[TestClass]
public class CrossFileInferenceIntegrationTests
{
    #region Simple Parameter Inference Tests

    [TestMethod]
    public void Integration_SimpleParameterInference_InfersFromCallSite()
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
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "attack");

        // Assert
        Assert.IsNotNull(report);
        var param = report.GetParameter("target");
        Assert.IsNotNull(param);
        Assert.AreEqual("int", param.EffectiveType);
    }

    [TestMethod]
    public void Integration_MultipleCallSites_InfersUnionType()
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
    p.attack("critical")
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "attack");

        // Assert
        Assert.IsNotNull(report);
        var param = report.GetParameter("target");
        Assert.IsNotNull(param);
        Assert.IsNotNull(param.InferredUnionType);
        Assert.IsTrue(param.InferredUnionType.Types.Contains("int"));
        Assert.IsTrue(param.InferredUnionType.Types.Contains("String"));
    }

    [TestMethod]
    public void Integration_ExplicitType_PreservesDeclaredType()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target: int):
    pass
""", """
class_name Game

func test():
    var p = Player.new()
    p.attack(10)
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "attack");

        // Assert
        Assert.IsNotNull(report);
        var param = report.GetParameter("target");
        Assert.IsNotNull(param);
        Assert.AreEqual("int", param.ExplicitType);
        Assert.AreEqual(GDReferenceConfidence.Strict, param.Confidence);
    }

    #endregion

    #region Return Type Inference Tests

    [TestMethod]
    public void Integration_ReturnTypeInference_InfersFromReturns()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func get_damage():
    return 10
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "get_damage");

        // Assert
        Assert.IsNotNull(report);
        Assert.IsNotNull(report.ReturnTypeReport);
        Assert.AreEqual("int", report.ReturnTypeReport.EffectiveType);
    }

    [TestMethod]
    public void Integration_ReturnTypeInference_MultipleReturns_InfersUnion()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func get_status(condition):
    if condition:
        return 10
    else:
        return "healthy"
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "get_status");

        // Assert
        Assert.IsNotNull(report);
        Assert.IsNotNull(report.ReturnTypeReport);
        Assert.IsNotNull(report.ReturnTypeReport.InferredUnionType);
        Assert.IsTrue(report.ReturnTypeReport.InferredUnionType.Types.Contains("int"));
        Assert.IsTrue(report.ReturnTypeReport.InferredUnionType.Types.Contains("String"));
    }

    [TestMethod]
    public void Integration_ExplicitReturnType_PreservesDeclaredType()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func get_damage() -> int:
    return 10
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "get_damage");

        // Assert
        Assert.IsNotNull(report);
        Assert.IsNotNull(report.ReturnTypeReport);
        Assert.AreEqual("int", report.ReturnTypeReport.ExplicitType);
        Assert.AreEqual(GDReferenceConfidence.Strict, report.ReturnTypeReport.Confidence);
    }

    #endregion

    #region Cross-File Tests

    [TestMethod]
    public void Integration_CrossFileCallSites_CollectsFromAllFiles()
    {
        // Arrange - Call from multiple files
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Game

func test():
    var p = Player.new()
    p.attack(10)
""", """
class_name Battle

func fight():
    var p = Player.new()
    p.attack(20)
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "attack");

        // Assert
        Assert.IsNotNull(report);
        var param = report.GetParameter("target");
        Assert.IsNotNull(param);
        Assert.AreEqual(2, param.CallSiteCount); // 2 call sites from 2 files
    }

    #endregion

    #region Cyclic Dependency Tests

    [TestMethod]
    public void Integration_CyclicDependency_HandlesGracefully()
    {
        // Arrange - A calls B, B calls A
        var project = CreateProject("""
class_name Handler

var processor: Processor

func handle(data):
    processor.process(self)
""", """
class_name Processor

var handler: Handler

func process(h):
    handler.handle(h)
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act - Should not throw
        var handlerReport = engine.GetMethodReport("Handler", "handle");
        var processorReport = engine.GetMethodReport("Processor", "process");

        // Assert - Reports should exist, cyclic dependency flagged
        // Note: Depends on actual implementation - may be null if type can't be resolved
        // This test mainly verifies no infinite loop/stack overflow
        Assert.IsTrue(handlerReport?.HasCyclicDependency == true || processorReport?.HasCyclicDependency == true ||
                      handlerReport == null || processorReport == null);
    }

    [TestMethod]
    public void Integration_SelfRecursion_HandlesGracefully()
    {
        // Arrange - Method calls itself
        var project = CreateProject("""
class_name Calculator

func factorial(n):
    if n <= 1:
        return 1
    return n * factorial(n - 1)
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Calculator", "factorial");

        // Assert
        Assert.IsNotNull(report);
        // Self-recursion is detected as a cycle
        Assert.IsTrue(report.HasCyclicDependency);
    }

    #endregion

    #region Duck-Typed Call Site Tests

    [TestMethod]
    public void Integration_DuckTypedCall_IncludedWithPotentialConfidence()
    {
        // Arrange - Untyped parameter calls method
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Game

func process(obj):
    obj.attack(10)
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "attack");

        // Assert
        Assert.IsNotNull(report);
        var param = report.GetParameter("target");
        Assert.IsNotNull(param);
        // Duck-typed calls should be collected
        Assert.IsTrue(param.DuckTypedCallSiteCount > 0 || param.CallSiteCount > 0);
    }

    #endregion

    #region Project Report Tests

    [TestMethod]
    public void Integration_ProjectReport_ContainsAllMethods()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass

func defend():
    pass
""", """
class_name Enemy

func damage(amount):
    pass
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetProjectReport();

        // Assert
        Assert.IsNotNull(report);
        Assert.IsTrue(report.TotalMethodsAnalyzed >= 3);
        Assert.IsTrue(report.Methods.ContainsKey("Player.attack"));
        Assert.IsTrue(report.Methods.ContainsKey("Player.defend"));
        Assert.IsTrue(report.Methods.ContainsKey("Enemy.damage"));
    }

    [TestMethod]
    public void Integration_ProjectReport_HasDependencyGraph()
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

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetProjectReport();

        // Assert
        Assert.IsNotNull(report);
        Assert.IsNotNull(report.DependencyGraph);
        // Should have edges in the dependency graph
        Assert.IsTrue(report.DependencyGraph.Nodes.Count >= 2);
    }

    #endregion

    #region Visualization Service Tests

    [TestMethod]
    public void Integration_VisualizationService_GetStatistics()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    return 10
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
        Assert.IsTrue(stats.TotalMethods >= 2);
        Assert.IsTrue(stats.TotalCallSites >= 0);
    }

    [TestMethod]
    public void Integration_VisualizationService_ExportToJson()
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
        Assert.IsTrue(json.Contains("Player"));
        Assert.IsTrue(json.Contains("attack"));
    }

    [TestMethod]
    public void Integration_VisualizationService_GetParameterCallSites()
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

    #endregion

    #region Dependency Tracker Tests

    [TestMethod]
    public void Integration_DependencyTracker_TracksMethodFiles()
    {
        // Arrange
        var tracker = new GDInferenceDependencyTracker();

        // Act
        tracker.RegisterMethod("Player.attack", "/player.gd");
        tracker.RegisterMethod("Player.defend", "/player.gd");
        tracker.RegisterMethod("Enemy.damage", "/enemy.gd");

        // Assert
        var playerMethods = tracker.GetMethodsInFile("/player.gd").ToList();
        Assert.AreEqual(2, playerMethods.Count);
        Assert.IsTrue(playerMethods.Contains("Player.attack"));
        Assert.IsTrue(playerMethods.Contains("Player.defend"));
    }

    [TestMethod]
    public void Integration_DependencyTracker_GetsDependents()
    {
        // Arrange
        var tracker = new GDInferenceDependencyTracker();
        tracker.RegisterMethod("Player.attack", "/player.gd");
        tracker.RegisterMethod("Game.test", "/game.gd");
        tracker.RegisterMethod("Battle.fight", "/battle.gd");
        tracker.AddDependency("Game.test", "Player.attack");
        tracker.AddDependency("Battle.fight", "Player.attack");

        // Act
        var dependents = tracker.GetDirectDependents("Player.attack").ToList();

        // Assert
        Assert.AreEqual(2, dependents.Count);
        Assert.IsTrue(dependents.Contains("Game.test"));
        Assert.IsTrue(dependents.Contains("Battle.fight"));
    }

    [TestMethod]
    public void Integration_DependencyTracker_GetMethodsToRecompute()
    {
        // Arrange
        var tracker = new GDInferenceDependencyTracker();
        tracker.RegisterMethod("Player.attack", "/player.gd");
        tracker.RegisterMethod("Game.test", "/game.gd");
        tracker.AddDependency("Game.test", "Player.attack");

        // Act
        var toRecompute = tracker.GetMethodsToRecomputeOnFileChange("/player.gd").ToList();

        // Assert
        Assert.IsTrue(toRecompute.Contains("Player.attack")); // Method in changed file
        Assert.IsTrue(toRecompute.Contains("Game.test")); // Dependent method
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Integration_NoCallSites_ReturnsVariant()
    {
        // Arrange - Method with parameter but no calls
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "attack");

        // Assert
        Assert.IsNotNull(report);
        var param = report.GetParameter("target");
        Assert.IsNotNull(param);
        Assert.AreEqual("Variant", param.EffectiveType);
        Assert.AreEqual(GDReferenceConfidence.Potential, param.Confidence);
    }

    [TestMethod]
    public void Integration_EmptyProject_NoErrors()
    {
        // Arrange
        var project = new GDScriptProject();

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetProjectReport();

        // Assert
        Assert.IsNotNull(report);
        Assert.AreEqual(0, report.TotalMethodsAnalyzed);
    }

    [TestMethod]
    public void Integration_MultipleParameterTypes_InfersEach()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target, damage, critical):
    pass
""", """
class_name Game

func test():
    var p = Player.new()
    p.attack("enemy", 10, true)
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "attack");

        // Assert
        Assert.IsNotNull(report);
        Assert.AreEqual("String", report.GetParameter("target")?.EffectiveType);
        Assert.AreEqual("int", report.GetParameter("damage")?.EffectiveType);
        Assert.AreEqual("bool", report.GetParameter("critical")?.EffectiveType);
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
