using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Unit tests for GDCallSiteRegistry.
/// </summary>
[TestClass]
public class GDCallSiteRegistryTests
{
    #region Register and Retrieve Tests

    [TestMethod]
    public void Register_SingleCallSite_CanBeRetrieved()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        var entry = new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "_ready",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack");

        // Act
        registry.Register(entry);

        // Assert
        registry.Count.Should().Be(1);
        var callers = registry.GetCallersOf("Player", "attack");
        callers.Should().HaveCount(1);
        callers[0].Should().Be(entry);
    }

    [TestMethod]
    public void Register_MultipleCallSites_AllRetrievable()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        var entry1 = new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "_ready",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack");

        var entry2 = new GDCallSiteEntry(
            sourceFilePath: "C:/project/enemy.gd",
            sourceMethodName: "on_hit",
            line: 25,
            column: 8,
            targetClassName: "Player",
            targetMethodName: "attack");

        // Act
        registry.Register(entry1);
        registry.Register(entry2);

        // Assert
        registry.Count.Should().Be(2);
        var callers = registry.GetCallersOf("Player", "attack");
        callers.Should().HaveCount(2);
    }

    [TestMethod]
    public void GetCallersOf_NoCallSites_ReturnsEmpty()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Act
        var callers = registry.GetCallersOf("NonExistent", "method");

        // Assert
        callers.Should().BeEmpty();
    }

    [TestMethod]
    public void GetCallersOf_CaseInsensitive_FindsMatches()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        var entry = new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "_ready",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack");

        registry.Register(entry);

        // Act
        var callers = registry.GetCallersOf("PLAYER", "ATTACK");

        // Assert
        callers.Should().HaveCount(1);
    }

    #endregion

    #region Unregister File Tests

    [TestMethod]
    public void UnregisterFile_RemovesAllCallSitesFromFile()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        var entry1 = new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "method1",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack");

        var entry2 = new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "method2",
            line: 20,
            column: 5,
            targetClassName: "Enemy",
            targetMethodName: "die");

        var entry3 = new GDCallSiteEntry(
            sourceFilePath: "C:/project/other.gd",
            sourceMethodName: "method3",
            line: 5,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack");

        registry.Register(entry1);
        registry.Register(entry2);
        registry.Register(entry3);

        // Act
        var removedCount = registry.UnregisterFile("C:/project/game.gd");

        // Assert
        removedCount.Should().Be(2);
        registry.Count.Should().Be(1);
        registry.GetCallersOf("Player", "attack").Should().HaveCount(1);
        registry.GetCallersOf("Enemy", "die").Should().BeEmpty();
    }

    [TestMethod]
    public void UnregisterFile_NonExistentFile_ReturnsZero()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Act
        var removedCount = registry.UnregisterFile("C:/nonexistent.gd");

        // Assert
        removedCount.Should().Be(0);
    }

    [TestMethod]
    public void UnregisterFile_CaseInsensitivePath()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        var entry = new GDCallSiteEntry(
            sourceFilePath: "C:/Project/Game.gd",
            sourceMethodName: "_ready",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack");

        registry.Register(entry);

        // Act
        var removedCount = registry.UnregisterFile("c:/project/game.gd");

        // Assert
        removedCount.Should().Be(1);
        registry.Count.Should().Be(0);
    }

    #endregion

    #region Unregister Method Tests

    [TestMethod]
    public void UnregisterMethod_RemovesOnlyMethodCallSites()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        var entry1 = new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "method1",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack");

        var entry2 = new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "method2",
            line: 20,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack");

        registry.Register(entry1);
        registry.Register(entry2);

        // Act
        var removedCount = registry.UnregisterMethod("C:/project/game.gd", "method1");

        // Assert
        removedCount.Should().Be(1);
        registry.Count.Should().Be(1);
        registry.GetCallersOf("Player", "attack").Should().HaveCount(1);
    }

    [TestMethod]
    public void UnregisterMethod_NullMethodName_ReturnsZero()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Act
        var removedCount = registry.UnregisterMethod("C:/project/game.gd", null);

        // Assert
        removedCount.Should().Be(0);
    }

    #endregion

    #region GetCallSitesInFile Tests

    [TestMethod]
    public void GetCallSitesInFile_ReturnsAllFromFile()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        var entry1 = new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "method1",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack");

        var entry2 = new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "method2",
            line: 20,
            column: 5,
            targetClassName: "Enemy",
            targetMethodName: "die");

        registry.Register(entry1);
        registry.Register(entry2);

        // Act
        var callSites = registry.GetCallSitesInFile("C:/project/game.gd");

        // Assert
        callSites.Should().HaveCount(2);
    }

    [TestMethod]
    public void GetCallSitesInFile_NonExistentFile_ReturnsEmpty()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Act
        var callSites = registry.GetCallSitesInFile("C:/nonexistent.gd");

        // Assert
        callSites.Should().BeEmpty();
    }

    #endregion

    #region GetCallSitesInMethod Tests

    [TestMethod]
    public void GetCallSitesInMethod_ReturnsOnlyFromMethod()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        var entry1 = new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "method1",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack");

        var entry2 = new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "method2",
            line: 20,
            column: 5,
            targetClassName: "Enemy",
            targetMethodName: "die");

        registry.Register(entry1);
        registry.Register(entry2);

        // Act
        var callSites = registry.GetCallSitesInMethod("C:/project/game.gd", "method1");

        // Assert
        callSites.Should().HaveCount(1);
        callSites[0].TargetMethodName.Should().Be("attack");
    }

    #endregion

    #region GetFilesCallingClass Tests

    [TestMethod]
    public void GetFilesCallingClass_ReturnsAllCallerFiles()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        registry.Register(new GDCallSiteEntry(
            sourceFilePath: "C:/project/game1.gd",
            sourceMethodName: "_ready",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack"));

        registry.Register(new GDCallSiteEntry(
            sourceFilePath: "C:/project/game2.gd",
            sourceMethodName: "_ready",
            line: 15,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "jump"));

        registry.Register(new GDCallSiteEntry(
            sourceFilePath: "C:/project/enemy.gd",
            sourceMethodName: "on_hit",
            line: 20,
            column: 5,
            targetClassName: "Enemy",
            targetMethodName: "die"));

        // Act
        var files = registry.GetFilesCallingClass("Player");

        // Assert
        files.Should().HaveCount(2);
        files.Should().Contain("C:/project/game1.gd");
        files.Should().Contain("C:/project/game2.gd");
    }

    #endregion

    #region Clear Tests

    [TestMethod]
    public void Clear_RemovesAllCallSites()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        registry.Register(new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "_ready",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack"));

        // Act
        registry.Clear();

        // Assert
        registry.Count.Should().Be(0);
        registry.GetCallersOf("Player", "attack").Should().BeEmpty();
    }

    #endregion

    #region Helper Tests

    [TestMethod]
    public void HasCallSitesFromFile_ReturnsCorrectValue()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        registry.Register(new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "_ready",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack"));

        // Assert
        registry.HasCallSitesFromFile("C:/project/game.gd").Should().BeTrue();
        registry.HasCallSitesFromFile("C:/project/other.gd").Should().BeFalse();
    }

    [TestMethod]
    public void HasCallersOf_ReturnsCorrectValue()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        registry.Register(new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "_ready",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack"));

        // Assert
        registry.HasCallersOf("Player", "attack").Should().BeTrue();
        registry.HasCallersOf("Player", "jump").Should().BeFalse();
        registry.HasCallersOf("Enemy", "attack").Should().BeFalse();
    }

    [TestMethod]
    public void GetAllTargets_ReturnsAllUniqueTargets()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        registry.Register(new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "_ready",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack"));

        registry.Register(new GDCallSiteEntry(
            sourceFilePath: "C:/project/other.gd",
            sourceMethodName: "_ready",
            line: 10,
            column: 5,
            targetClassName: "Player",
            targetMethodName: "attack"));

        registry.Register(new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "_process",
            line: 20,
            column: 5,
            targetClassName: "Enemy",
            targetMethodName: "die"));

        // Act
        var targets = registry.GetAllTargets();

        // Assert
        targets.Should().HaveCount(2);
    }

    #endregion

    #region Confidence and DuckTyped Tests

    [TestMethod]
    public void Register_DuckTypedCallSite_PreservesFlag()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        var entry = new GDCallSiteEntry(
            sourceFilePath: "C:/project/game.gd",
            sourceMethodName: "_ready",
            line: 10,
            column: 5,
            targetClassName: "*",
            targetMethodName: "attack",
            confidence: GDReferenceConfidence.Potential,
            isDuckTyped: true);

        // Act
        registry.Register(entry);
        var callers = registry.GetCallersOf("*", "attack");

        // Assert
        callers.Should().HaveCount(1);
        callers[0].IsDuckTyped.Should().BeTrue();
        callers[0].Confidence.Should().Be(GDReferenceConfidence.Potential);
    }

    #endregion
}
