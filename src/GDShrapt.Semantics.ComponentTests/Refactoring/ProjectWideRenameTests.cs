using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

[TestClass]
public class ProjectWideRenameTests
{
    private static GDRenameService Service => TestProjectFixture.ProjectModel.Services.Rename;

    private GDRenameResult PlanRename(string oldName, string sourceFile)
    {
        var script = TestProjectFixture.GetScript(sourceFile);
        Assert.IsNotNull(script, $"Script not found: {sourceFile}");

        var symbol = TestProjectFixture.ProjectModel
            .GetSemanticModel(script!)?.FindSymbol(oldName);

        if (symbol != null)
            return Service.PlanRename(symbol, oldName + "_renamed");

        // Fallback: class_name or string-based
        return Service.PlanRename(oldName, oldName + "_renamed", script!.FullPath);
    }

    private static string[] GetFiles(GDRenameResult result)
    {
        return result.StrictEdits
            .Select(e => System.IO.Path.GetFileName(e.FilePath ?? ""))
            .Distinct()
            .OrderBy(f => f)
            .ToArray();
    }

    #region class_name Rename Tests

    [TestMethod]
    public void PlanRename_ClassName_FindsExtendsDeclarations()
    {
        var result = PlanRename("BaseEntity", "base_entity.gd");

        result.Success.Should().BeTrue();
        var files = GetFiles(result);
        files.Should().Contain("enemy_entity.gd", "extends BaseEntity in enemy_entity.gd");
        files.Should().Contain("player_entity.gd", "extends BaseEntity in player_entity.gd");

        // Should have extends edits
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("enemy_entity.gd") && e.ConfidenceReason != null && e.ConfidenceReason.Contains("Extends"));
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("player_entity.gd") && e.ConfidenceReason != null && e.ConfidenceReason.Contains("Extends"));
    }

    [TestMethod]
    public void PlanRename_ClassName_FindsTypeAnnotations()
    {
        var result = PlanRename("BaseEntity", "base_entity.gd");

        result.Success.Should().BeTrue();

        // Should find type annotations (var x: BaseEntity)
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("enemy_entity.gd") &&
            e.ConfidenceReason != null && e.ConfidenceReason.Contains("TypeAnnotation"));
    }

    [TestMethod]
    public void PlanRename_ClassName_FindsTypeChecks()
    {
        var result = PlanRename("BaseEntity", "base_entity.gd");

        result.Success.Should().BeTrue();

        // Should find is-check (if obj is BaseEntity)
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("enemy_entity.gd") &&
            e.ConfidenceReason != null && e.ConfidenceReason.Contains("TypeCheck"));
    }

    [TestMethod]
    public void PlanRename_ClassName_IncludesDeclaration()
    {
        var result = PlanRename("BaseEntity", "base_entity.gd");

        result.Success.Should().BeTrue();

        // Should include the class_name declaration itself
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("base_entity.gd") &&
            e.ConfidenceReason != null && e.ConfidenceReason.Contains("class_name"));
    }

    #endregion

    #region Inherited Member Tests

    [TestMethod]
    public void PlanRename_InheritedVar_FindsDirectUsageInDerivedClass()
    {
        var result = PlanRename("current_health", "base_entity.gd");

        result.Success.Should().BeTrue();

        // Should find current_health usage in player_entity.gd
        var playerEdits = result.StrictEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("player_entity.gd"))
            .ToList();

        playerEdits.Should().NotBeEmpty("current_health is used directly in player_entity.gd");
        playerEdits.Count.Should().BeGreaterThanOrEqualTo(3, "player_entity.gd uses current_health multiple times");
    }

    [TestMethod]
    public void PlanRename_InheritedVar_FindsTransitiveInheritance()
    {
        var result = PlanRename("player_speed", "refactoring_targets.gd");

        result.Success.Should().BeTrue();

        // Should find player_speed in rename_test.gd (extends RefactoringTargets)
        var renameTestEdits = result.StrictEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("rename_test.gd"))
            .ToList();

        renameTestEdits.Should().NotBeEmpty("player_speed is used in rename_test.gd via inheritance");
        renameTestEdits.Count.Should().BeGreaterThanOrEqualTo(8, "rename_test.gd uses player_speed 9 times");
    }

    #endregion

    #region Method Override & Super Call Tests

    [TestMethod]
    public void PlanRename_Method_FindsOverrideDeclaration()
    {
        var result = PlanRename("take_damage", "base_entity.gd");

        result.Success.Should().BeTrue();

        // Should find edits in player_entity.gd (override declaration + references)
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("player_entity.gd"),
            "player_entity.gd overrides take_damage");

        // Should find edits in enemy_entity.gd (override declaration + references)
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("enemy_entity.gd"),
            "enemy_entity.gd overrides take_damage");
    }

    [TestMethod]
    public void PlanRename_Method_FindsSuperCalls()
    {
        var result = PlanRename("take_damage", "base_entity.gd");

        result.Success.Should().BeTrue();

        // Should find multiple edits in player_entity.gd (override + super call + references)
        var playerEdits = result.StrictEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("player_entity.gd"))
            .ToList();
        playerEdits.Count.Should().BeGreaterThanOrEqualTo(2,
            "player_entity.gd should have override declaration + super.take_damage() call");

        // Should find multiple edits in enemy_entity.gd (override + super call + references)
        var enemyEdits = result.StrictEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("enemy_entity.gd"))
            .ToList();
        enemyEdits.Count.Should().BeGreaterThanOrEqualTo(2,
            "enemy_entity.gd should have override declaration + super.take_damage() call");
    }

    #endregion

    #region Signal Tests

    [TestMethod]
    public void PlanRename_Signal_FindsInheritedEmits()
    {
        var result = PlanRename("health_changed", "base_entity.gd");

        result.Success.Should().BeTrue();

        // Should find health_changed.emit() in player_entity.gd
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("player_entity.gd"),
            "health_changed is emitted in player_entity.gd");
    }

    [TestMethod]
    public void PlanRename_Signal_FindsInheritedConnections()
    {
        var result = PlanRename("score_changed", "refactoring_targets.gd");

        result.Success.Should().BeTrue();

        // Should find score_changed usage in rename_test.gd
        var renameTestEdits = result.StrictEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("rename_test.gd"))
            .ToList();

        renameTestEdits.Should().NotBeEmpty("score_changed is used in rename_test.gd via inheritance");
        renameTestEdits.Count.Should().BeGreaterThanOrEqualTo(3, "rename_test.gd uses score_changed (emit + connect)");
    }

    #endregion

    #region Scope & Deduplication Tests

    [TestMethod]
    public void PlanRename_LocalVariable_StaysSingleFile()
    {
        var result = PlanRename("counter", "rename_test.gd");

        result.Success.Should().BeTrue();

        // All edits should be in rename_test.gd only
        var files = GetFiles(result);
        files.Should().HaveCount(1, "counter is a local variable, no cross-file edits");
        files[0].Should().Be("rename_test.gd");
    }

    [TestMethod]
    public void PlanRename_DuplicateEditsRemoved()
    {
        var result = PlanRename("calculate_score", "refactoring_targets.gd");

        result.Success.Should().BeTrue();

        // Each (file, line, column) position should appear exactly once
        var positions = result.StrictEdits
            .Select(e => $"{e.FilePath}|{e.Line}:{e.Column}")
            .ToList();

        positions.Should().OnlyHaveUniqueItems("duplicate edits at the same position should be removed");
    }

    #endregion

    #region Confidence & Integration Tests

    [TestMethod]
    public void PlanRename_CrossFile_HasCorrectConfidence()
    {
        var result = PlanRename("current_health", "base_entity.gd");

        result.Success.Should().BeTrue();

        // All cross-file edits for inherited members should be Strict
        var crossFileEdits = result.StrictEdits
            .Where(e => e.FilePath != null && !e.FilePath.EndsWith("base_entity.gd"))
            .ToList();

        crossFileEdits.Should().NotBeEmpty();
        crossFileEdits.Should().OnlyContain(e => e.Confidence == GDReferenceConfidence.Strict,
            "inherited member edits should have Strict confidence");
    }

    [TestMethod]
    public void PlanRename_ViaProjectSemanticModel_SameResults()
    {
        // Get result via ProjectModel.Services.Rename
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script);

        var modelService = TestProjectFixture.ProjectModel.Services.Rename;
        var symbol = TestProjectFixture.ProjectModel
            .GetSemanticModel(script!)?.FindSymbol("current_health");
        Assert.IsNotNull(symbol);

        var result1 = modelService.PlanRename(symbol!, "current_health_test");

        // Get result via direct construction with project model
        var directService = new GDRenameService(TestProjectFixture.Project, TestProjectFixture.ProjectModel);
        var result2 = directService.PlanRename(symbol!, "current_health_test");

        // Both should produce the same edits
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();

        result1.StrictEdits.Count.Should().Be(result2.StrictEdits.Count,
            "both paths should produce the same number of edits");

        var edits1 = result1.StrictEdits.Select(e => $"{e.FilePath}|{e.Line}:{e.Column}").OrderBy(x => x).ToList();
        var edits2 = result2.StrictEdits.Select(e => $"{e.FilePath}|{e.Line}:{e.Column}").OrderBy(x => x).ToList();

        edits1.Should().BeEquivalentTo(edits2, "both paths should produce identical edit positions");
    }

    [TestMethod]
    public void PlanRenameByName_TakeDamage_FindsAllAsStrict()
    {
        // String-based PlanRename (the CLI path) should find overrides and super calls as Strict
        var result = Service.PlanRename("take_damage", "take_damage_renamed");

        result.Success.Should().BeTrue();
        result.StrictEdits.Count.Should().BeGreaterThanOrEqualTo(5,
            "String-based PlanRename should find base definition, overrides, and super calls as Strict");

        // Should cover multiple files
        var files = result.StrictEdits
            .Where(e => e.FilePath != null)
            .Select(e => System.IO.Path.GetFileName(e.FilePath!))
            .Distinct()
            .OrderBy(f => f)
            .ToList();

        files.Count.Should().BeGreaterThanOrEqualTo(2,
            "take_damage edits should span multiple files (base + overrides)");
    }

    #endregion
}
