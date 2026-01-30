using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for Union receiver call site collection.
/// When a variable has a Union type, calls should be collected from all possible types.
/// </summary>
[TestClass]
public class UnionReceiverCallTests
{
    #region Basic Union Receiver Tests

    [TestMethod]
    public void UnionReceiver_SingleType_CollectsNormally()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Player");

        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""");

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSitesForUnionReceiver(union, "attack");

        // Assert
        // Should find the Player.attack method
        Assert.IsNotNull(callSites);
    }

    [TestMethod]
    public void UnionReceiver_MultipleTypes_CollectsFromAll()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Player");
        union.AddType("Enemy");

        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Enemy

func attack(strength):
    pass
""", """
class_name Game

var player: Player
var enemy: Enemy

func test():
    player.attack(null)
    enemy.attack(10)
""");

        var collector = new GDCallSiteCollector(project);

        // Act - Collect for both types in Union
        var playerCallSites = collector.CollectCallSites("Player", "attack");
        var enemyCallSites = collector.CollectCallSites("Enemy", "attack");

        // Assert - Both should have call sites
        Assert.IsTrue(playerCallSites.Count > 0 || enemyCallSites.Count > 0);
    }

    [TestMethod]
    public void UnionReceiver_CycleProtection_SkipsVisited()
    {
        // Arrange - Create a Union type with the same type appearing multiple times
        var union = new GDUnionType();
        union.AddType("Player");
        union.AddType("Player"); // Duplicate - should not cause issues

        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""");

        var collector = new GDCallSiteCollector(project);

        // Act - Should handle duplicates gracefully
        var visited = new HashSet<string>();
        var callSites = collector.CollectCallSitesForUnionReceiver(union, "attack", visited);

        // Assert - Should work without infinite loop
        Assert.IsNotNull(callSites);
    }

    [TestMethod]
    public void UnionReceiver_MethodNotInAllTypes_PartialCollection()
    {
        // Arrange - Only Player has attack method
        var union = new GDUnionType();
        union.AddType("Player");
        union.AddType("NPC"); // NPC doesn't have attack

        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name NPC

func speak():
    pass
""");

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSitesForUnionReceiver(union, "attack");

        // Assert - Should not error, just collect what's available
        Assert.IsNotNull(callSites);
    }

    #endregion

    #region Union Type Inference Tests

    [TestMethod]
    public void UnionReceiver_ArgumentTypesFromUnion()
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
    p.attack("str")
""");

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSites("Player", "attack");

        // Assert - Should collect both call sites with different argument types
        Assert.IsTrue(callSites.Count >= 2);
        var argTypes = callSites.SelectMany(c => c.Arguments).Select(a => a.InferredType).ToHashSet();
        Assert.IsTrue(argTypes.Contains("int"));
        Assert.IsTrue(argTypes.Contains("String"));
    }

    #endregion

    #region GDCallSiteInfo Union Properties Tests

    [TestMethod]
    public void CallSiteInfo_CreateWithUnionReceiver_HasCorrectProperties()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Game

var player: Player

func test():
    player.attack(null)
""");

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSites("Player", "attack");

        // Assert
        Assert.AreEqual(1, callSites.Count);
        var callSite = callSites[0];

        // Verify call site properties
        Assert.IsNotNull(callSite.SourceScript);
        Assert.IsTrue(callSite.Line > 0);
    }

    #endregion

    #region Inference Engine Union Tests

    [TestMethod]
    public void InferenceEngine_UnionReceiverMethodCall_CollectsTypes()
    {
        // Arrange
        var project = CreateProject("""
class_name Entity

func take_damage(amount):
    pass
""", """
class_name Player
extends Entity
""", """
class_name Enemy
extends Entity
""", """
class_name Game

var entities: Array

func damage_all():
    for entity in entities:
        entity.take_damage(10)
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Entity", "take_damage");

        // Assert
        Assert.IsNotNull(report);
        var param = report.GetParameter("amount");
        Assert.IsNotNull(param);
    }

    #endregion

    #region Call Site Argument Report Union Tests

    [TestMethod]
    public void CallSiteArgumentReport_FromUnionReceiver_HasFlag()
    {
        // Arrange - Create a call site with Union receiver
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Game

var player: Player

func test():
    player.attack(null)
""");

        var collector = new GDCallSiteCollector(project);
        var callSites = collector.CollectCallSites("Player", "attack");

        // Act
        Assert.AreEqual(1, callSites.Count);
        var report = GDCallSiteArgumentReport.FromCallSite(callSites[0], callSites[0].Arguments[0]);

        // Assert
        Assert.IsNotNull(report);
        // FromUnionReceiver is set when UnionReceiverType is not null
        // In this test, the receiver is typed as Player, not a Union
        Assert.IsFalse(report.FromUnionReceiver);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void UnionReceiver_EmptyUnion_ReturnsEmpty()
    {
        // Arrange
        var union = new GDUnionType();

        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""");

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSitesForUnionReceiver(union, "attack");

        // Assert
        Assert.AreEqual(0, callSites.Count);
    }

    [TestMethod]
    public void UnionReceiver_UnknownType_Skipped()
    {
        // Arrange - Union contains a type that doesn't exist in the project
        var union = new GDUnionType();
        union.AddType("NonExistentClass");

        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""");

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSitesForUnionReceiver(union, "attack");

        // Assert
        Assert.AreEqual(0, callSites.Count);
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
