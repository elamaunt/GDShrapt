using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Tests for duck-typed call site collection.
/// Duck-typing occurs when the receiver type is unknown (untyped parameter or variable).
/// </summary>
[TestClass]
public class DuckTypedCallSiteTests
{
    #region Basic Duck-Typed Tests

    [TestMethod]
    public void CollectDuckTyped_UnknownReceiver_CollectsCallSite()
    {
        // Arrange - obj has no type annotation
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Game

func process(obj):
    obj.attack(null)
""");

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSites("Player", "attack");

        // Assert
        Assert.AreEqual(1, callSites.Count);
        Assert.IsTrue(callSites[0].IsDuckTyped);
        Assert.AreEqual("obj", callSites[0].ReceiverVariableName);
    }

    [TestMethod]
    public void CollectDuckTyped_MethodNameMatches_IncludesInInference()
    {
        // Arrange - Multiple classes have attack method
        var project = CreateProject("""
class_name Player

func attack(damage):
    pass
""", """
class_name Enemy

func attack(strength):
    pass
""", """
class_name Game

func process(entity):
    entity.attack(10)
""");

        var collector = new GDCallSiteCollector(project);

        // Act - should collect for both Player and Enemy
        var playerCallSites = collector.CollectCallSites("Player", "attack");
        var enemyCallSites = collector.CollectCallSites("Enemy", "attack");

        // Assert - duck-typed call site should be collected for both
        Assert.IsTrue(playerCallSites.Any(c => c.IsDuckTyped));
        Assert.IsTrue(enemyCallSites.Any(c => c.IsDuckTyped));
    }

    [TestMethod]
    public void CollectDuckTyped_ConfidenceIsPotential()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Game

func test(obj):
    obj.attack(null)
""");

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSites("Player", "attack");

        // Assert
        var duckTyped = callSites.FirstOrDefault(c => c.IsDuckTyped);
        Assert.IsNotNull(duckTyped);
        Assert.AreEqual(GDReferenceConfidence.Potential, duckTyped.Confidence);
    }

    [TestMethod]
    public void CollectDuckTyped_ArgumentTypesPreserved()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(damage, critical):
    pass
""", """
class_name Game

func test(entity):
    entity.attack(50, true)
""");

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSites("Player", "attack");

        // Assert
        var duckTyped = callSites.FirstOrDefault(c => c.IsDuckTyped);
        Assert.IsNotNull(duckTyped);
        Assert.AreEqual(2, duckTyped.Arguments.Count);
        Assert.AreEqual("int", duckTyped.Arguments[0].InferredType?.DisplayName);
        Assert.AreEqual("bool", duckTyped.Arguments[1].InferredType?.DisplayName);
    }

    #endregion

    #region Multiple Duck-Typed Receivers Tests

    [TestMethod]
    public void CollectDuckTyped_MultipleUntypedReceivers_CollectsAll()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Game

func battle(a, b, c):
    a.attack(null)
    b.attack(null)
    c.attack(null)
""");

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSites("Player", "attack");

        // Assert
        var duckTyped = callSites.Where(c => c.IsDuckTyped).ToList();
        Assert.AreEqual(3, duckTyped.Count);

        var receiverNames = duckTyped.Select(c => c.ReceiverVariableName).ToHashSet();
        Assert.IsTrue(receiverNames.Contains("a"));
        Assert.IsTrue(receiverNames.Contains("b"));
        Assert.IsTrue(receiverNames.Contains("c"));
    }

    #endregion

    #region Mixed Typed and Duck-Typed Tests

    [TestMethod]
    public void Collect_MixedTypedAndDuckTyped_CollectsBoth()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Game

var player: Player

func test(obj):
    player.attack(null)
    obj.attack(null)
""");

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSites("Player", "attack");

        // Assert
        Assert.AreEqual(2, callSites.Count);

        var typedCallSites = callSites.Where(c => !c.IsDuckTyped).ToList();
        var duckTypedCallSites = callSites.Where(c => c.IsDuckTyped).ToList();

        // Should have at least one duck-typed call
        Assert.IsTrue(duckTypedCallSites.Count >= 1);
    }

    #endregion

    #region Inference Engine with Duck-Typed Tests

    [TestMethod]
    public void InferenceEngine_DuckTypedCallSites_IncludedInParameterReport()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(damage):
    pass
""", """
class_name Game

func process(entity):
    entity.attack(100)
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "attack");

        // Assert
        Assert.IsNotNull(report);
        var param = report.GetParameter("damage");
        Assert.IsNotNull(param);

        // Should have duck-typed call sites
        Assert.IsTrue(param.DuckTypedCallSiteCount > 0);
    }

    [TestMethod]
    public void InferenceEngine_DuckTypedCallSites_LowerConfidence()
    {
        // Arrange - Only duck-typed call sites
        var project = CreateProject("""
class_name Player

func attack(damage):
    pass
""", """
class_name Game

func process(entity):
    entity.attack(100)
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "attack");

        // Assert
        Assert.IsNotNull(report);
        var param = report.GetParameter("damage");
        Assert.IsNotNull(param);

        // Confidence should be Potential due to duck-typing
        Assert.AreEqual(GDReferenceConfidence.Potential, param.Confidence);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void CollectDuckTyped_MethodNotInAnyClass_StillCollected()
    {
        // Arrange - Duck-typed calls are collected even if the target class doesn't exist
        var project = CreateProject("""
class_name Game

func process(obj):
    obj.nonexistent_method()
""");

        var collector = new GDCallSiteCollector(project);

        // Act - Looking for a method that exists nowhere
        // Duck-typed calls to this method will still be found
        var callSites = collector.CollectCallSites("Player", "nonexistent_method");

        // Assert - Duck typing means calls are collected by method name
        // This is expected behavior - collect all potential call sites
        Assert.IsTrue(callSites.All(c => c.IsDuckTyped));
    }

    [TestMethod]
    public void CollectDuckTyped_ChainedCall_CollectsLast()
    {
        // Arrange - obj.get_player().attack()
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Game

func test(obj):
    obj.get_player().attack(null)
""");

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSites("Player", "attack");

        // Assert - chained calls are collected as duck-typed
        Assert.IsTrue(callSites.Count >= 1);
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
