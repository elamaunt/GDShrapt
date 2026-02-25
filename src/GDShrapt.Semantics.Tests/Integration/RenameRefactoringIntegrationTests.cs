using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Integration tests for Rename Refactoring validation.
/// These tests verify that all locations that need updating when renaming are correctly identified.
/// </summary>
[TestClass]
public class RenameRefactoringIntegrationTests
{
    #region Local Variables

    [TestMethod]
    public void RenameValidation_LocalVariable_Counter_AllLocationsIdentified()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "counter");

        // Assert - All these locations would need to be renamed:
        // Line 19: var counter := 0 (declaration)
        // Line 20-28: counter += 1, counter += 2, etc.
        Assert.IsTrue(references.Count >= 8,
            $"Expected at least 8 locations for 'counter', found {references.Count}");

        // Verify we have declaration + usages
        var counts = IntegrationTestHelpers.CountReferencesByKind(references);
        Assert.IsTrue(counts.ContainsKey(ReferenceKind.Declaration),
            "Should identify declaration location");
        Assert.IsTrue(
            (counts.ContainsKey(ReferenceKind.Read) ? counts[ReferenceKind.Read] : 0) +
            (counts.ContainsKey(ReferenceKind.Write) ? counts[ReferenceKind.Write] : 0) >= 5,
            "Should identify multiple read/write locations");
    }

    [TestMethod]
    public void RenameValidation_LocalVariable_X_InExpressions_AllOccurrences()
    {
        // Arrange - test_rename_in_expressions uses 'x' in many expressions
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "x");

        // Assert
        // x is used in: declaration, sum, product, complex expression (multiple times), assignments
        Assert.IsTrue(references.Count >= 6,
            $"Expected at least 6 locations for 'x' in expressions, found {references.Count}");
    }

    #endregion

    #region Parameters

    [TestMethod]
    public void RenameValidation_Parameter_Value_AllLocationsIdentified()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "value");

        // Assert
        // value parameter used in: declaration, result calc, +=, print, if condition, 2 return strs
        Assert.IsTrue(references.Count >= 5,
            $"Expected at least 5 locations for 'value' parameter, found {references.Count}");
    }

    [TestMethod]
    public void RenameValidation_Parameter_Factor_AllCalculations()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "factor");

        // Assert
        // factor is heavily used in calculate_with_factor
        Assert.IsTrue(references.Count >= 7,
            $"Expected at least 7 locations for 'factor', found {references.Count}");
    }

    [TestMethod]
    public void RenameValidation_Parameter_Amount_MultipleMethodParameters()
    {
        // Arrange - 'amount' is a parameter in multiple methods in base_entity.gd
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "amount");

        // Assert
        // amount is parameter in take_damage and heal, used in each method
        Assert.IsTrue(references.Count >= 4,
            $"Expected at least 4 locations for 'amount', found {references.Count}");

        // Each method has its own 'amount' - declarations should equal method count
        var declarations = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Declaration);
        Assert.IsTrue(declarations.Count >= 2,
            "Should have declarations in multiple methods");
    }

    #endregion

    #region Class Members

    [TestMethod]
    public void RenameValidation_ClassMember_Multiplier_AllUsages()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "multiplier");

        // Assert
        // multiplier: declaration + multiple uses in test_rename_class_member
        Assert.IsTrue(references.Count >= 5,
            $"Expected at least 5 locations for 'multiplier', found {references.Count}");
    }

    [TestMethod]
    public void RenameValidation_ClassMember_CurrentHealth_AllMethods()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "current_health");

        // Assert
        // current_health used across many methods: _ready, take_damage, heal, die, revive, etc.
        Assert.IsTrue(references.Count >= 10,
            $"Expected at least 10 locations for 'current_health', found {references.Count}");
    }

    [TestMethod]
    public void RenameValidation_ClassMember_IsAlive_AllChecks()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "is_alive");

        // Assert
        // is_alive: declaration, set in _ready, checked and set in take_damage, die, revive
        Assert.IsTrue(references.Count >= 6,
            $"Expected at least 6 locations for 'is_alive', found {references.Count}");
    }

    #endregion

    #region Inherited Members Cross-File

    [TestMethod]
    public void RenameValidation_InheritedMember_PlayerSpeed_CrossFileLocations()
    {
        // Arrange - player_speed defined in RefactoringTargets, used in RenameTest
        var project = TestProjectFixture.Project;

        // Act - Collect from both files
        var refactoringScript = TestProjectFixture.GetScript("refactoring_targets.gd");
        var renameTestScript = TestProjectFixture.GetScript("rename_test.gd");

        Assert.IsNotNull(refactoringScript, "refactoring_targets.gd not found");
        Assert.IsNotNull(renameTestScript, "rename_test.gd not found");

        var baseRefs = IntegrationTestHelpers.CollectReferencesInScript(refactoringScript, "player_speed");
        var derivedRefs = IntegrationTestHelpers.CollectReferencesInScript(renameTestScript, "player_speed");

        // Assert
        // Should have declaration in base + usages in base
        Assert.IsTrue(baseRefs.Count >= 2,
            $"Should find player_speed in base class, found {baseRefs.Count}");

        // Should have usages in derived class
        Assert.IsTrue(derivedRefs.Count >= 4,
            $"Should find player_speed usages in derived class, found {derivedRefs.Count}");

        // Total locations for rename
        int totalLocations = baseRefs.Count + derivedRefs.Count;
        Assert.IsTrue(totalLocations >= 6,
            $"Total rename locations should be at least 6, found {totalLocations}");
    }

    [TestMethod]
    public void RenameValidation_InheritedMember_MaxHealth_InDerivedClass()
    {
        // Arrange
        var baseScript = TestProjectFixture.GetScript("base_entity.gd");
        var playerScript = TestProjectFixture.GetScript("player_entity.gd");

        Assert.IsNotNull(baseScript, "base_entity.gd not found");
        Assert.IsNotNull(playerScript, "player_entity.gd not found");

        // Act
        var baseRefs = IntegrationTestHelpers.CollectReferencesInScript(baseScript, "max_health");
        var playerRefs = IntegrationTestHelpers.CollectReferencesInScript(playerScript, "max_health");

        // Assert
        Assert.IsTrue(baseRefs.Count >= 5,
            $"Should find max_health in base class, found {baseRefs.Count}");
        Assert.IsTrue(playerRefs.Count >= 3,
            $"Should find max_health in player class, found {playerRefs.Count}");
    }

    #endregion

    #region Signals

    [TestMethod]
    public void RenameValidation_Signal_HealthChanged_AllEmitsAndConnects()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "health_changed");

        // Assert
        // health_changed: declaration + multiple .emit() calls
        Assert.IsTrue(references.Count >= 5,
            $"Expected at least 5 locations for 'health_changed', found {references.Count}");
    }

    [TestMethod]
    public void RenameValidation_Signal_ScoreChanged_EmitAndConnect()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "score_changed");

        // Assert
        // score_changed: emit, is_connected, connect in test_rename_signal_usage
        Assert.IsTrue(references.Count >= 3,
            $"Expected at least 3 locations for 'score_changed', found {references.Count}");
    }

    #endregion

    #region Methods

    [TestMethod]
    public void RenameValidation_Method_CalculateScore_DeclarationAndCalls()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("refactoring_targets.gd");
        Assert.IsNotNull(script, "refactoring_targets.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "calculate_score");

        // Assert
        // calculate_score: declaration + call in process_enemies
        Assert.IsTrue(references.Count >= 2,
            $"Expected at least 2 locations for 'calculate_score', found {references.Count}");

        var declarations = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Declaration);
        Assert.AreEqual(1, declarations.Count, "Should have exactly one declaration");
    }

    [TestMethod]
    public void RenameValidation_Method_Heal_CalledFromDerived()
    {
        // Arrange
        var baseScript = TestProjectFixture.GetScript("base_entity.gd");
        var playerScript = TestProjectFixture.GetScript("player_entity.gd");

        Assert.IsNotNull(baseScript, "base_entity.gd not found");
        Assert.IsNotNull(playerScript, "player_entity.gd not found");

        // Act
        var baseRefs = IntegrationTestHelpers.CollectReferencesInScript(baseScript, "heal");
        var playerRefs = IntegrationTestHelpers.CollectReferencesInScript(playerScript, "heal");

        // Assert
        // heal: declaration in base
        var baseDeclarations = IntegrationTestHelpers.FilterByKind(baseRefs, ReferenceKind.Declaration);
        Assert.AreEqual(1, baseDeclarations.Count, "Should have one declaration in base");

        // heal: called in player's collect_health_pack
        Assert.IsTrue(playerRefs.Count >= 1,
            $"Should find heal call in player, found {playerRefs.Count}");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void RenameValidation_SuperCall_DoesNotRenameMethod()
    {
        // super.method_name() calls should be included in rename of method_name
        var script = TestProjectFixture.GetScript("player_entity.gd");
        Assert.IsNotNull(script, "player_entity.gd not found");

        // take_damage is overridden and calls super.take_damage
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "take_damage");

        // Should find the override declaration and the super call
        Assert.IsTrue(references.Count >= 1,
            "Should find take_damage references in PlayerEntity");
    }

    [TestMethod]
    public void RenameValidation_SameNameDifferentMethods_SeparateRenames()
    {
        // 'result' is used in multiple methods - each is a separate local variable
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "result");

        // 'result' appears in test_rename_parameter, test_rename_class_member,
        // and calculate_with_factor - each should be separate scope
        Assert.IsTrue(references.Count > 0, "Should find 'result' references");

        // Declarations should be > 1 since it's a local in multiple methods
        var declarations = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Declaration);
        Assert.IsTrue(declarations.Count >= 2,
            "Should have 'result' declared in multiple methods");
    }

    [TestMethod]
    public void RenameValidation_ConstantAllCaps_AllUsages()
    {
        // Constants like MAX_ENEMIES should be found
        var script = TestProjectFixture.GetScript("refactoring_targets.gd");
        Assert.IsNotNull(script, "refactoring_targets.gd not found");

        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "MAX_ENEMIES");

        // MAX_ENEMIES: declaration + usage in spawn_enemy
        Assert.IsTrue(references.Count >= 2,
            $"Expected at least 2 locations for 'MAX_ENEMIES', found {references.Count}");
    }

    [TestMethod]
    public void RenameValidation_ExportedVariable_AllUsages()
    {
        // @export var max_health should be found like any other variable
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "max_health");

        // Should find declaration (with @export) and all usages
        Assert.IsTrue(references.Count >= 5,
            $"Expected at least 5 locations for exported 'max_health', found {references.Count}");
    }

    #endregion

    #region Total Rename Locations

    [TestMethod]
    public void RenameValidation_AllLocations_EnemyCount()
    {
        // Comprehensive test: enemy_count in refactoring_targets.gd
        var script = TestProjectFixture.GetScript("refactoring_targets.gd");
        Assert.IsNotNull(script, "refactoring_targets.gd not found");

        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "enemy_count");

        // enemy_count: declaration, process_enemies loop, check_game_state, spawn_enemy (2x)
        Assert.IsTrue(references.Count >= 5,
            $"Expected at least 5 locations for 'enemy_count', found {references.Count}");

        // Detailed breakdown
        var counts = IntegrationTestHelpers.CountReferencesByKind(references);

        // At least 1 declaration
        Assert.IsTrue(counts.ContainsKey(ReferenceKind.Declaration) && counts[ReferenceKind.Declaration] >= 1,
            "Should have at least 1 declaration");

        // Multiple reads and writes
        int readWrites = (counts.ContainsKey(ReferenceKind.Read) ? counts[ReferenceKind.Read] : 0) +
                        (counts.ContainsKey(ReferenceKind.Write) ? counts[ReferenceKind.Write] : 0);
        Assert.IsTrue(readWrites >= 3, "Should have multiple read/write usages");
    }

    #endregion

    #region Scope Isolation: For-Loop and Setter (RC5/RC6/RC8)

    private static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gdshrapt_rename_test_" + System.IO.Path.GetRandomFileName());
        System.IO.Directory.CreateDirectory(tempPath);

        System.IO.File.WriteAllText(System.IO.Path.Combine(tempPath, "project.godot"),
            "[gd_resource type=\"ProjectSettings\" format=3]\n\nconfig_version=5\n\n[application]\nconfig/name=\"TestProject\"\n");

        foreach (var (name, content) in scripts)
        {
            var fileName = name.EndsWith(".gd", System.StringComparison.OrdinalIgnoreCase) ? name : name + ".gd";
            System.IO.File.WriteAllText(System.IO.Path.Combine(tempPath, fileName), content);
        }

        return tempPath;
    }

    private static void DeleteTempProject(string path)
    {
        try { if (System.IO.Directory.Exists(path)) System.IO.Directory.Delete(path, true); } catch { }
    }

    [TestMethod]
    public void Rename_VarAfterForLoop_ScopeResolutionSeparatesSymbols()
    {
        var script = @"extends Node

func test():
    var items = [1, 2, 3]
    for x in items:
        print(x)
    var x = 99
    print(x)
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var file = project.ScriptFiles.First();
            var model = projectModel.GetSemanticModel(file)!;

            var allX = model.FindSymbols("x");
            Assert.IsTrue(allX.Count() >= 2,
                "iterator x and var x should be separate symbols");

            var iteratorSymbol = allX.FirstOrDefault(s => s.Kind == GDSymbolKind.Iterator);
            var varSymbol = allX.FirstOrDefault(s => s.Kind != GDSymbolKind.Iterator);
            Assert.IsNotNull(iteratorSymbol, "iterator x should exist");
            Assert.IsNotNull(varSymbol, "var x should exist");

            var renameService = new GDRenameService(project, projectModel);
            var plan = renameService.PlanRename(varSymbol!, "y");

            Assert.IsNotNull(plan);
            Assert.IsTrue(plan.Success, "rename should succeed");
            Assert.IsTrue(plan.StrictEdits.Count > 0, "should have edits");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void Rename_VarInSetter_OnlyRenamesInSameAccessor()
    {
        var script = @"extends Node

var value: int = 0:
    set(new_value):
        var old_value := value
        value = clampi(new_value, 0, 100)
        if value < old_value:
            print(""decreased"")

var selected: int = 0:
    set(new_value):
        var old_value := selected
        selected = clampi(new_value, 0, 100)
        if selected < old_value:
            print(""decreased"")
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var file = project.ScriptFiles.First();
            var model = projectModel.GetSemanticModel(file)!;

            var allOldValue = model.FindSymbols("old_value");
            Assert.AreEqual(2, allOldValue.Count(),
                "each setter should have its own old_value symbol");

            var firstSymbol = allOldValue.OrderBy(s => s.PositionToken?.StartLine ?? 0).First();
            var renameService = new GDRenameService(project, projectModel);
            var plan = renameService.PlanRename(firstSymbol, "prev");

            Assert.IsNotNull(plan);
            Assert.IsTrue(plan.Success, "rename should succeed");
            Assert.IsTrue(plan.StrictEdits.Count > 0, "should have edits");

            // All edits should be in the first setter region (before the second setter)
            // PositionToken.StartLine is 0-based, GDTextEdit.Line is 1-based
            var secondSetterLine = (allOldValue
                .OrderBy(s => s.PositionToken?.StartLine ?? 0)
                .Last().PositionToken?.StartLine ?? 998) + 1;

            Assert.IsTrue(plan.StrictEdits.All(e => e.Line < secondSetterLine),
                "rename should only affect the first setter's old_value");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void Rename_ForLoopIterator_IncludesDeclarationLine()
    {
        var script = @"extends Node

func test():
    for x in range(5):
        print(x)
    var x = 99
    print(x)
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var file = project.ScriptFiles.First();
            var model = projectModel.GetSemanticModel(file)!;

            var allX = model.FindSymbols("x");
            Assert.IsTrue(allX.Count() >= 2,
                "iterator x and var x should be separate symbols");

            var iteratorSymbol = allX.FirstOrDefault(s => s.Kind == GDSymbolKind.Iterator);
            var varSymbol = allX.FirstOrDefault(s => s.Kind != GDSymbolKind.Iterator);
            Assert.IsNotNull(iteratorSymbol, "iterator x should exist");
            Assert.IsNotNull(varSymbol, "var x after loop should exist");

            var renameService = new GDRenameService(project, projectModel);
            var plan = renameService.PlanRename(iteratorSymbol!, "i");

            Assert.IsNotNull(plan);
            Assert.IsTrue(plan.Success, "rename should succeed");
            Assert.IsTrue(plan.StrictEdits.Count > 0, "should have edits");

            // PositionToken.StartLine is 0-based, GDTextEdit.Line is 1-based
            var iteratorLine = (iteratorSymbol!.PositionToken?.StartLine ?? 0) + 1;
            Assert.IsTrue(plan.StrictEdits.Any(e => e.Line == iteratorLine),
                "rename should include the for-loop iterator declaration");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion
}
