using System.Linq;
using System.Runtime.InteropServices;
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

    [TestMethod]
    public void PlanRename_Method_OverrideAndSuperCallPositionsPointToIdentifier()
    {
        var result = PlanRename("take_damage", "base_entity.gd");

        result.Success.Should().BeTrue();

        // Override declaration: "func take_damage(..." — column must point to "take_damage", not "func"
        var overrideEdit = result.StrictEdits
            .FirstOrDefault(e => e.FilePath != null && e.FilePath.EndsWith("enemy_entity.gd") &&
                                 e.ConfidenceReason != null && e.ConfidenceReason.Contains("Method override"));
        overrideEdit.Should().NotBeNull("enemy_entity.gd should have override edit");
        overrideEdit!.Column.Should().Be(6,
            "override edit column should point to 'take_damage' identifier (col 6), not 'func' keyword (col 1)");

        // Super call: "\tsuper.take_damage(..." — column must point to "take_damage", not "super"
        var superEdit = result.StrictEdits
            .FirstOrDefault(e => e.FilePath != null && e.FilePath.EndsWith("enemy_entity.gd") &&
                                 e.ConfidenceReason != null && e.ConfidenceReason.Contains("super."));
        superEdit.Should().NotBeNull("enemy_entity.gd should have super call edit");
        superEdit!.Column.Should().Be(8,
            "super call edit column should point to 'take_damage' identifier (col 8), not start of call expression");
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

    #region Bug Fix Tests — Same-Name Different Class, Duck-Typed, has_method, .tscn

    [TestMethod]
    public void PlanRenameByName_SameNameDifferentClass_NotInStrictEdits()
    {
        // Bug 1: PlanRename(string) treats same-named methods on unrelated classes as same symbol
        // UnrelatedClass.take_damage() should NOT appear in strict edits when renaming BaseEntity.take_damage
        var result = Service.PlanRename("take_damage", "take_damage_renamed");

        result.Success.Should().BeTrue();

        // UnrelatedClass.take_damage should NOT be in strict edits
        var unrelatedEdits = result.StrictEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("unrelated_class.gd"))
            .ToList();

        unrelatedEdits.Should().BeEmpty(
            "UnrelatedClass.take_damage is on an unrelated type (not in BaseEntity hierarchy) " +
            "and should not be in strict edits");

        // BaseEntity hierarchy files SHOULD still be in strict edits
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("base_entity.gd"),
            "base_entity.gd defines take_damage");
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("enemy_entity.gd"),
            "enemy_entity.gd overrides take_damage");
    }

    [TestMethod]
    public void PlanRenameByName_DuckTypedMemberAccess_InPotentialEdits()
    {
        // Bug 2: Duck-typed member access (entity.take_damage on untyped var) not found
        var result = Service.PlanRename("take_damage", "take_damage_renamed");

        result.Success.Should().BeTrue();

        // duck_caller.gd: entity.take_damage(5) should be in potential edits
        var allEdits = result.StrictEdits.Concat(result.PotentialEdits).ToList();
        var duckCallerEdits = allEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("duck_caller.gd"))
            .ToList();

        duckCallerEdits.Should().NotBeEmpty(
            "duck_caller.gd calls entity.take_damage(5) via duck-typing on untyped array; " +
            "should appear in edits (at least as Potential)");

        // The duck-typed call should be Potential confidence
        var potentialDuckEdits = result.PotentialEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("duck_caller.gd"))
            .ToList();

        potentialDuckEdits.Should().NotBeEmpty(
            "duck-typed entity.take_damage() call should have Potential confidence");
    }

    [TestMethod]
    public void PlanRenameByName_HasMethodString_InPotentialEdits()
    {
        // Bug 3: has_method("take_damage") string literals not tracked as references
        var result = Service.PlanRename("take_damage", "take_damage_renamed");

        result.Success.Should().BeTrue();

        // duck_caller.gd: has_method("take_damage") should be in potential edits
        var allEdits = result.StrictEdits.Concat(result.PotentialEdits).ToList();
        var hasMethodEdits = allEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("duck_caller.gd") &&
                        e.OldText == "take_damage")
            .ToList();

        // Should have at least 2 edits in duck_caller.gd:
        // 1. has_method("take_damage") string literal
        // 2. entity.take_damage(5) duck-typed call
        hasMethodEdits.Count.Should().BeGreaterThanOrEqualTo(2,
            "duck_caller.gd should have both has_method string and duck-typed call edits");
    }

    [TestMethod]
    public void PlanRenameByName_TscnConnection_Found()
    {
        // Bug 4: .tscn [connection method="take_damage"] not scanned
        var result = Service.PlanRename("take_damage", "take_damage_renamed");

        result.Success.Should().BeTrue();

        // connection_test.tscn has [connection ... method="take_damage"]
        var allEdits = result.StrictEdits.Concat(result.PotentialEdits).ToList();
        var tscnEdits = allEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith(".tscn"))
            .ToList();

        tscnEdits.Should().NotBeEmpty(
            ".tscn files with [connection method=\"take_damage\"] should be found by rename");
    }

    #endregion

    #region Reflection-Style String Literal Tests

    [TestMethod]
    public void PlanRename_EmitSignal_StringLiteralInPotentialEdits()
    {
        var result = PlanRename("game_started", "reflection_test.gd");

        result.Success.Should().BeTrue();

        // emit_signal("game_started") should produce a Potential edit
        result.PotentialEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("reflection_test.gd") &&
            e.ConfidenceReason != null && e.ConfidenceReason.Contains("emit_signal"),
            "emit_signal(\"game_started\") should be a potential edit");

        // has_signal("game_started") should produce a Potential edit
        result.PotentialEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("reflection_test.gd") &&
            e.ConfidenceReason != null && e.ConfidenceReason.Contains("has_signal"),
            "has_signal(\"game_started\") should be a potential edit");

        // emit_signal(SIGNAL_NAME) where const SIGNAL_NAME = "game_started" should also produce a Potential edit
        // The edit should point to the const initializer string literal
        var constResolvedEdits = result.PotentialEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("reflection_test.gd") &&
                        e.ConfidenceReason != null && e.ConfidenceReason.Contains("emit_signal"))
            .ToList();
        constResolvedEdits.Count.Should().BeGreaterThanOrEqualTo(2,
            "both emit_signal(\"game_started\") and emit_signal(SIGNAL_NAME) should be found");
    }

    [TestMethod]
    public void PlanRename_Call_StringLiteralInPotentialEdits()
    {
        var result = PlanRename("start", "reflection_test.gd");

        result.Success.Should().BeTrue();

        var potentialInReflectionTest = result.PotentialEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("reflection_test.gd"))
            .ToList();

        // call("start"), call_deferred("start"), has_method("start"), call(METHOD_NAME),
        // Callable(self, "start"), Callable(self, METHOD_NAME)
        // That's 6 potential edits from reflection_test.gd
        potentialInReflectionTest.Count.Should().BeGreaterThanOrEqualTo(4,
            "call/call_deferred/has_method/Callable with string literal 'start' should be found");

        // Specifically: has_method("start")
        potentialInReflectionTest.Should().Contain(e =>
            e.ConfidenceReason != null && e.ConfidenceReason.Contains("has_method"),
            "has_method(\"start\") should be found");

        // Specifically: call("start")
        potentialInReflectionTest.Should().Contain(e =>
            e.ConfidenceReason != null && e.ConfidenceReason.Contains("call("),
            "call(\"start\") should be found");
    }

    [TestMethod]
    public void PlanRename_GetSet_StringLiteralInPotentialEdits()
    {
        var result = PlanRename("player_speed", "reflection_test.gd");

        result.Success.Should().BeTrue();

        // get("player_speed") and set("player_speed", ...) in reflection_test.gd
        var potentialInReflectionTest = result.PotentialEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("reflection_test.gd"))
            .ToList();

        potentialInReflectionTest.Should().Contain(e =>
            e.ConfidenceReason != null && e.ConfidenceReason.Contains("get("),
            "get(\"player_speed\") should produce a potential edit");

        potentialInReflectionTest.Should().Contain(e =>
            e.ConfidenceReason != null && e.ConfidenceReason.Contains("set("),
            "set(\"player_speed\", ...) should produce a potential edit");
    }

    [TestMethod]
    public void PlanRename_ConstResolvedToOriginalLiteral_PositionCorrect()
    {
        var result = PlanRename("game_started", "reflection_test.gd");

        result.Success.Should().BeTrue();

        // The const SIGNAL_NAME = "game_started" is on line 7
        // emit_signal(SIGNAL_NAME) should resolve to the const and the edit should
        // point to "game_started" inside the const definition
        var constLiteralEdit = result.PotentialEdits
            .FirstOrDefault(e => e.FilePath != null && e.FilePath.EndsWith("reflection_test.gd") &&
                                  e.Line == 7);
        constLiteralEdit.Should().NotBeNull(
            "emit_signal(SIGNAL_NAME) should produce edit pointing to const SIGNAL_NAME = \"game_started\" on line 7");
    }

    [TestMethod]
    public void PlanRename_StringConcatenation_ProducesWarningNotEdit()
    {
        var result = PlanRename("game_started", "reflection_test.gd");

        result.Success.Should().BeTrue();

        // emit_signal("game" + "_started") should NOT produce an edit
        // (concatenation cannot be auto-edited)
        // But it SHOULD produce a warning
        result.Warnings.Should().NotBeEmpty(
            "concatenated string matching symbol name should produce a warning");

        result.Warnings.Should().Contain(w =>
            w.Message.Contains("concatenated") || w.Message.Contains("manual"),
            "warning should mention concatenated/manual update");
    }

    #endregion

    #region Path Normalization & File Filter Tests

    [TestMethod]
    public void PlanRenameWithFile_BackslashPath_MatchesForwardSlashFullPath()
    {
        // Bug: GDScriptReference.NormalizePath converts \ to /,
        // but PlanRename used Path.GetFullPath (backslashes on Windows) for comparison.
        // Backslash is a path separator only on Windows; on Linux/macOS it's a valid filename char.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Inconclusive("Backslash path test is Windows-only");

        var script = TestProjectFixture.GetScript("unrelated_class.gd");
        script.Should().NotBeNull();

        // Simulate Windows-style backslash path
        var backslashPath = script!.FullPath!.Replace('/', '\\');

        var result = Service.PlanRename("take_damage", "take_damage_fixed", backslashPath);

        result.Success.Should().BeTrue();

        // Should find the symbol in unrelated_class.gd (NOT in BaseEntity hierarchy)
        var files = GetFiles(result);
        files.Should().Contain("unrelated_class.gd",
            "with --file pointing to unrelated_class.gd, the symbol should be found there");
    }

    [TestMethod]
    public void PlanRenameWithFile_SameNameDifferentClass_FindsTargetSymbol()
    {
        // When --file points to unrelated_class.gd, the symbol in that file
        // should be resolved and edits should include unrelated_class.gd
        var script = TestProjectFixture.GetScript("unrelated_class.gd");
        script.Should().NotBeNull();

        var result = Service.PlanRename("take_damage", "take_damage_fixed", script!.FullPath);

        result.Success.Should().BeTrue();

        // The target file must be in the edits (proves --file matched correctly)
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("unrelated_class.gd"),
            "declaration should be found in unrelated_class.gd when --file points to it");

        // Verify the symbol was found by checking we have the declaration edit
        var unrelatedEdits = result.StrictEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("unrelated_class.gd"))
            .ToList();
        unrelatedEdits.Should().HaveCountGreaterThanOrEqualTo(2,
            "unrelated_class.gd should have at least declaration + usage of take_damage");
    }

    #endregion

    #region Type-Aware Filtering Tests

    [TestMethod]
    public void PlanRenameSymbol_UnrelatedClass_StrictEditsExcludeEntityHierarchy()
    {
        // When renaming take_damage from unrelated_class.gd (UnrelatedClass),
        // strict edits must NOT contain BaseEntity hierarchy files.
        var script = TestProjectFixture.GetScript("unrelated_class.gd");
        Assert.IsNotNull(script);

        var symbol = TestProjectFixture.ProjectModel
            .GetSemanticModel(script!)?.FindSymbol("take_damage");
        Assert.IsNotNull(symbol, "take_damage should be defined in unrelated_class.gd");

        var result = Service.PlanRename(symbol!, "take_damage_renamed");
        result.Success.Should().BeTrue();

        // Strict edits should contain unrelated_class.gd
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("unrelated_class.gd"),
            "unrelated_class.gd defines take_damage and must be in strict edits");

        // Strict edits should NOT contain BaseEntity hierarchy
        result.StrictEdits.Should().NotContain(e =>
            e.FilePath != null && e.FilePath.EndsWith("base_entity.gd"),
            "base_entity.gd is in a separate hierarchy, must not be in strict edits");
        result.StrictEdits.Should().NotContain(e =>
            e.FilePath != null && e.FilePath.EndsWith("player_entity.gd"),
            "player_entity.gd extends BaseEntity, must not be in strict edits");
        result.StrictEdits.Should().NotContain(e =>
            e.FilePath != null && e.FilePath.EndsWith("enemy_entity.gd"),
            "enemy_entity.gd extends BaseEntity, must not be in strict edits");
    }

    [TestMethod]
    public void PlanRenameSymbol_BaseEntity_StrictEditsIncludeInheritanceHierarchy()
    {
        // When renaming take_damage from base_entity.gd (BaseEntity),
        // strict edits should contain player_entity.gd and enemy_entity.gd (overrides),
        // but NOT unrelated_class.gd (separate hierarchy).
        var result = PlanRename("take_damage", "base_entity.gd");

        result.Success.Should().BeTrue();

        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("player_entity.gd"),
            "player_entity.gd overrides take_damage in BaseEntity hierarchy");
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("enemy_entity.gd"),
            "enemy_entity.gd overrides take_damage in BaseEntity hierarchy");
        result.StrictEdits.Should().NotContain(e =>
            e.FilePath != null && e.FilePath.EndsWith("unrelated_class.gd"),
            "unrelated_class.gd is a separate hierarchy and must not be in strict edits");
    }

    [TestMethod]
    public void PlanRenameSymbol_UnrelatedClass_StrictEditsExcludeIncompatibleMemberAccess()
    {
        // When renaming UnrelatedClass.take_damage, typed member access like
        // target.take_damage() where target: BaseEntity should NOT appear in strict edits.
        var script = TestProjectFixture.GetScript("unrelated_class.gd");
        Assert.IsNotNull(script);

        var symbol = TestProjectFixture.ProjectModel
            .GetSemanticModel(script!)?.FindSymbol("take_damage");
        Assert.IsNotNull(symbol);

        var result = Service.PlanRename(symbol!, "take_damage_renamed");
        result.Success.Should().BeTrue();

        // No strict edits should point to files in BaseEntity hierarchy
        // (typed accesses with CallerType=BaseEntity/PlayerEntity/EnemyEntity are incompatible)
        var hierarchyStrictEdits = result.StrictEdits
            .Where(e => e.FilePath != null && (
                e.FilePath.EndsWith("player_entity.gd") ||
                e.FilePath.EndsWith("enemy_entity.gd") ||
                e.FilePath.EndsWith("base_entity.gd")))
            .ToList();

        hierarchyStrictEdits.Should().BeEmpty(
            "typed member access with incompatible CallerType should be excluded from strict edits");
    }

    #endregion

    #region Duck-Type Enrichment Tests

    [TestMethod]
    public void PlanRenameSymbol_DuckTypedReferences_RemainAsPotential()
    {
        // Duck-typed references (CallerType=Variant) should appear in potential edits, not strict.
        var result = PlanRename("take_damage", "base_entity.gd");

        result.Success.Should().BeTrue();

        // duck_caller.gd calls entity.take_damage(5) on Variant-typed array elements
        var duckCallerPotential = result.PotentialEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("duck_caller.gd"))
            .ToList();

        duckCallerPotential.Should().NotBeEmpty(
            "duck-typed entity.take_damage(5) call should be in potential edits");

        // They should NOT be in strict edits
        var duckCallerStrict = result.StrictEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("duck_caller.gd"))
            .ToList();

        duckCallerStrict.Should().BeEmpty(
            "duck-typed references should not be in strict edits");
    }

    [TestMethod]
    public void PlanRenameSymbol_DuckTypedReason_ContainsPossibleTypes()
    {
        // Duck-typed references should have clean confidence reasons.
        // Evidence-based type information goes into DetailedProvenance, not ConfidenceReason.
        var result = PlanRename("take_damage", "base_entity.gd");

        result.Success.Should().BeTrue();

        var duckCallerPotential = result.PotentialEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("duck_caller.gd"))
            .ToList();

        duckCallerPotential.Should().NotBeEmpty();

        // Confidence reason should be clean (no "could be" noise)
        foreach (var edit in duckCallerPotential)
        {
            if (edit.ConfidenceReason != null)
            {
                edit.ConfidenceReason.Should().NotContain("could be",
                    "type evidence goes in DetailedProvenance, not ConfidenceReason");
            }
        }
    }

    [TestMethod]
    public void PlanRenameSymbol_HasMethodStringLiteral_EnrichedWithPossibleTypes()
    {
        // has_method("take_damage") in duck_caller.gd should be in potential edits
        // with clean reason (evidence goes in DetailedProvenance, not ConfidenceReason).
        var result = PlanRename("take_damage", "base_entity.gd");

        result.Success.Should().BeTrue();

        var hasMethodEdits = result.PotentialEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("duck_caller.gd") &&
                        e.ConfidenceReason != null && e.ConfidenceReason.Contains("has_method"))
            .ToList();

        hasMethodEdits.Should().NotBeEmpty(
            "has_method(\"take_damage\") should produce a potential edit in duck_caller.gd");

        // ConfidenceReason should be clean — no "could be" noise
        foreach (var edit in hasMethodEdits)
        {
            edit.ConfidenceReason.Should().NotContain("could be",
                "type evidence goes in DetailedProvenance, not ConfidenceReason");
        }
    }

    [TestMethod]
    public void PlanRenameSymbol_DuckTypedReason_GroupsByInheritanceHierarchy()
    {
        // Evidence-based provenance: type grouping now goes into DetailedProvenance
        // rather than ConfidenceReason. ConfidenceReason stays clean.
        var result = PlanRename("take_damage", "base_entity.gd");

        result.Success.Should().BeTrue();

        var duckCallerPotential = result.PotentialEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("duck_caller.gd"))
            .ToList();

        duckCallerPotential.Should().NotBeEmpty();

        // ConfidenceReason should be clean — no hierarchy grouping noise
        foreach (var edit in duckCallerPotential)
        {
            if (edit.ConfidenceReason != null)
            {
                edit.ConfidenceReason.Should().NotContain("could be",
                    "type evidence goes in DetailedProvenance, not ConfidenceReason");
            }
        }
    }

    #endregion

    #region .tscn Type Filtering Tests

    [TestMethod]
    public void PlanRenameSymbol_TscnConnection_FilteredByTargetNodeType()
    {
        // connection_test.tscn has [connection method="take_damage"] where target node
        // has base_entity.gd script (BaseEntity type).
        // When renaming BaseEntity.take_damage → should be in strict edits.
        var result = PlanRename("take_damage", "base_entity.gd");

        result.Success.Should().BeTrue();

        var tscnEdits = result.StrictEdits.Concat(result.PotentialEdits)
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("connection_test.tscn"))
            .ToList();

        tscnEdits.Should().NotBeEmpty(
            "connection_test.tscn with method=\"take_damage\" connecting to BaseEntity node should be found");

        // When renaming UnrelatedClass.take_damage → should NOT include this .tscn
        // (target node has BaseEntity script, not UnrelatedClass)
        var unrelatedScript = TestProjectFixture.GetScript("unrelated_class.gd");
        Assert.IsNotNull(unrelatedScript);

        var unrelatedSymbol = TestProjectFixture.ProjectModel
            .GetSemanticModel(unrelatedScript!)?.FindSymbol("take_damage");
        Assert.IsNotNull(unrelatedSymbol);

        var unrelatedResult = Service.PlanRename(unrelatedSymbol!, "take_damage_renamed");
        unrelatedResult.Success.Should().BeTrue();

        var unrelatedTscnEdits = unrelatedResult.StrictEdits
            .Where(e => e.FilePath != null && e.FilePath.EndsWith("connection_test.tscn"))
            .ToList();

        unrelatedTscnEdits.Should().BeEmpty(
            "connection_test.tscn target node is BaseEntity, not UnrelatedClass — should be excluded");
    }

    #endregion

    #region Backward Compatibility Tests

    [TestMethod]
    public void PlanRenameByName_WithoutFile_BehaviorUnchanged()
    {
        // PlanRename("take_damage", "new_name") WITHOUT filterFilePath
        // should behave as before: include edits from all hierarchies (via hierarchy grouping).
        var result = Service.PlanRename("take_damage", "take_damage_renamed");

        result.Success.Should().BeTrue();

        // Should still include edits from BaseEntity hierarchy
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("base_entity.gd"),
            "base_entity.gd should still be in strict edits without --file");
        result.StrictEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("enemy_entity.gd"),
            "enemy_entity.gd should still be in strict edits without --file");

        // Duck-typed references should still appear
        var allEdits = result.StrictEdits.Concat(result.PotentialEdits).ToList();
        allEdits.Should().Contain(e =>
            e.FilePath != null && e.FilePath.EndsWith("duck_caller.gd"),
            "duck_caller.gd duck-typed references should still appear without --file");
    }

    #endregion

    #region Self Type Resolution Tests

    [TestMethod]
    public void SelfTypeResolution_InfersSelfAsConcreteType()
    {
        // Bug: InferIdentifierTypeNode("self") returned literal "self" instead of the class type.
        // This caused cross-file type compatibility checks to fail when callerType == "self".
        var script = TestProjectFixture.GetScript("base_entity.gd");
        script.Should().NotBeNull();

        var model = TestProjectFixture.ProjectModel.GetSemanticModel(script!);
        model.Should().NotBeNull();

        // Find a "self" identifier expression in the AST
        GDShrapt.Reader.GDIdentifierExpression? selfExpr = null;

        foreach (var token in script!.Class!.AllTokens)
        {
            if (token is GDShrapt.Reader.GDIdentifier id && id.Sequence == "self"
                && id.Parent is GDShrapt.Reader.GDIdentifierExpression identExpr)
            {
                selfExpr = identExpr;
                break;
            }
        }

        if (selfExpr != null)
        {
            var selfType = model!.TypeSystem.GetType(selfExpr);
            selfType.DisplayName.Should().NotBe("self",
                "self should resolve to the script's class type, not literal 'self'");
        }

        // Also verify via TypeName that the script has class_name
        script.TypeName.Should().Be("BaseEntity");
    }

    [TestMethod]
    public void SelfTypeResolution_CrossFileReferenceDoesNotRejectSelfCaller()
    {
        // Bug: When callerType = "self" and declaringType = "SomeClass",
        // IsTypeCompatible("self", "SomeClass") returned false, skipping valid references.
        // After fix, "self" in type engine resolves to actual class name,
        // and GDCrossFileReferenceFinder allows "self" as fallback.
        var script = TestProjectFixture.GetScript("base_entity.gd");
        script.Should().NotBeNull();

        var result = PlanRename("take_damage", "base_entity.gd");
        result.Success.Should().BeTrue();

        // All same-hierarchy references should be Strict, not Name-match
        result.StrictEdits.Should().NotBeEmpty();

        // No strict edit should have "self" as caller type in its reason
        result.StrictEdits.Should().NotContain(e =>
            e.ConfidenceReason != null && e.ConfidenceReason.Contains("type 'self'"),
            "no edit should reference literal 'self' as caller type");
    }

    #endregion
}
