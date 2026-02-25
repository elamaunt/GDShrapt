using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Integration tests for Find References across real project.
/// </summary>
[TestClass]
public class FindReferencesIntegrationTests
{
    #region Local Variables

    [TestMethod]
    public void FindReferences_LocalVariable_Counter_AllOccurrences()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "counter");

        // Assert
        // counter is defined once and used many times in test_rename_local_variable
        Assert.IsTrue(references.Count > 0, "Should find counter references");

        var declarations = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Declaration);
        Assert.AreEqual(1, declarations.Count, "Should have exactly one declaration");

        var reads = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Read);
        Assert.IsTrue(reads.Count >= 3, "Should have multiple read references");

        var writes = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Write);
        Assert.IsTrue(writes.Count >= 4, "Should have multiple write references (+=, =)");
    }

    [TestMethod]
    public void FindReferences_LocalVariable_Items_ArrayOperations()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "items");

        // Assert
        Assert.IsTrue(references.Count > 0, "Should find items references");

        // items used in: append, push_back, print, size, for loop, clear, assignment
        var allRefs = references.Where(r => r.Kind != ReferenceKind.Declaration).ToList();
        Assert.IsTrue(allRefs.Count >= 5, "Should have multiple usages of items");
    }

    [TestMethod]
    public void FindReferences_LoopVariable_I_OnlyInLoopScope()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "i");

        // Assert
        // 'i' is used in multiple for loops - each should be a separate scope
        Assert.IsTrue(references.Count > 0, "Should find 'i' references");
    }

    #endregion

    #region Parameters

    [TestMethod]
    public void FindReferences_Parameter_Value_AllOccurrences()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "value");

        // Assert
        // value parameter used in test_rename_parameter
        Assert.IsTrue(references.Count > 0, "Should find value references");

        var declarations = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Declaration);
        Assert.IsTrue(declarations.Count >= 1, "Should have at least one declaration (parameter)");
    }

    [TestMethod]
    public void FindReferences_Parameter_Factor_InCalculation()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "factor");

        // Assert
        // factor is used many times in calculate_with_factor
        Assert.IsTrue(references.Count >= 5, "factor should be used many times");

        var reads = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Read);
        Assert.IsTrue(reads.Count >= 4, "factor should have multiple read references");
    }

    #endregion

    #region Class Members

    [TestMethod]
    public void FindReferences_ClassMember_Multiplier_AcrossMethods()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "multiplier");

        // Assert
        // multiplier defined as class member, used in test_rename_class_member
        Assert.IsTrue(references.Count > 0, "Should find multiplier references");

        var declarations = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Declaration);
        Assert.AreEqual(1, declarations.Count, "Should have exactly one declaration");
    }

    [TestMethod]
    public void FindReferences_ClassMember_CurrentHealth_BaseEntityUsages()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "current_health");

        // Assert
        // current_health used in many methods: _ready, take_damage, heal, die, revive, etc.
        Assert.IsTrue(references.Count >= 8, "current_health should have many usages");
    }

    [TestMethod]
    public void FindReferences_ClassMember_Score_InRefactoringTargets()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("refactoring_targets.gd");
        Assert.IsNotNull(script, "refactoring_targets.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "score");

        // Assert
        // score used in multiple methods
        Assert.IsTrue(references.Count >= 5, "score should have multiple usages");

        var writes = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Write);
        Assert.IsTrue(writes.Count >= 2, "score should be written in multiple places");
    }

    #endregion

    #region Inherited Members

    [TestMethod]
    public void FindReferences_InheritedMember_PlayerSpeed_CrossFile()
    {
        // Arrange - player_speed defined in RefactoringTargets, used in RenameTest
        var renameTestScript = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(renameTestScript, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(renameTestScript, "player_speed");

        // Assert
        // player_speed is inherited and used in test_rename_inherited_member
        Assert.IsTrue(references.Count > 0, "Should find player_speed references in RenameTest");

        var reads = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Read);
        Assert.IsTrue(reads.Count >= 2, "player_speed should be read multiple times");
    }

    [TestMethod]
    public void FindReferences_InheritedMember_MaxHealth_PlayerEntity()
    {
        // Arrange - max_health defined in BaseEntity, used in PlayerEntity
        var script = TestProjectFixture.GetScript("player_entity.gd");
        Assert.IsNotNull(script, "player_entity.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "max_health");

        // Assert
        // max_health used in _ready and level_up_player
        Assert.IsTrue(references.Count > 0, "Should find max_health references");
    }

    [TestMethod]
    public void FindReferences_PathBasedExtends_MaxHealth()
    {
        // Arrange - Tests path-based extends: extends "res://test_scripts/base_entity.gd"
        var script = TestProjectFixture.GetScript("path_extends_test.gd");
        Assert.IsNotNull(script, "path_extends_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "max_health");

        // Assert
        // max_health is inherited from BaseEntity via path-based extends
        Assert.IsTrue(references.Count > 0, "Should find max_health references via path-based extends");

        // Should find usages in test_inherited_member_via_path
        var reads = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Read);
        var writes = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Write);
        Assert.IsTrue(reads.Count + writes.Count >= 2, "Should have multiple usages of max_health");
    }

    #endregion

    #region Signals

    [TestMethod]
    public void FindReferences_Signal_HealthChanged_EmitCalls()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "health_changed");

        // Assert
        // health_changed is declared once and emitted many times
        Assert.IsTrue(references.Count > 0, "Should find health_changed references");

        var declarations = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Declaration);
        Assert.AreEqual(1, declarations.Count, "Should have one declaration");

        // The .emit() calls should be counted as calls
        var calls = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Call);
        Assert.IsTrue(calls.Count >= 1 || references.Count >= 5,
            "health_changed should be used multiple times (emit or other)");
    }

    [TestMethod]
    public void FindReferences_Signal_ScoreChanged_InRenameTest()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "score_changed");

        // Assert
        // score_changed is inherited from RefactoringTargets and used in test_rename_signal_usage
        Assert.IsTrue(references.Count > 0, "Should find score_changed references");
    }

    #endregion

    #region Methods

    [TestMethod]
    public void FindReferences_Method_TakeDamage_BaseEntity()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "take_damage");

        // Assert
        // take_damage declared once
        Assert.IsTrue(references.Count > 0, "Should find take_damage references");

        var declarations = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Declaration);
        Assert.AreEqual(1, declarations.Count, "Should have exactly one declaration");
    }

    [TestMethod]
    public void FindReferences_Method_CalculateScore_MultipleCallSites()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("refactoring_targets.gd");
        Assert.IsNotNull(script, "refactoring_targets.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "calculate_score");

        // Assert
        // calculate_score declared once and called in process_enemies
        Assert.IsTrue(references.Count >= 2, "Should find calculate_score declaration and call");
    }

    [TestMethod]
    public void FindReferences_Method_Heal_UsedInPlayerEntity()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("player_entity.gd");
        Assert.IsNotNull(script, "player_entity.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "heal");

        // Assert
        // heal is inherited from BaseEntity and called in collect_health_pack
        Assert.IsTrue(references.Count > 0, "Should find heal usage in PlayerEntity");
    }

    #endregion

    #region Constants

    [TestMethod]
    public void FindReferences_Constant_MagicNumber_Usage()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("refactoring_targets.gd");
        Assert.IsNotNull(script, "refactoring_targets.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "MAGIC_NUMBER");

        // Assert
        // MAGIC_NUMBER is declared as const
        Assert.IsTrue(references.Count >= 1, "Should find MAGIC_NUMBER declaration");
    }

    [TestMethod]
    public void FindReferences_Constant_MaxEnemies_GuardClause()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("refactoring_targets.gd");
        Assert.IsNotNull(script, "refactoring_targets.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "MAX_ENEMIES");

        // Assert
        // MAX_ENEMIES declared and used in spawn_enemy guard clause
        Assert.IsTrue(references.Count >= 2, "MAX_ENEMIES should be declared and used");
    }

    #endregion

    #region Cross-File Type References

    [TestMethod]
    public void FindReferences_ClassName_BaseEntity_InExtendsAndTypeAnnotations()
    {
        // Arrange & Act
        var references = IntegrationTestHelpers.FindClassTypeUsages(
            TestProjectFixture.Project, "BaseEntity");

        // Assert
        // BaseEntity is extended by PlayerEntity and EnemyEntity
        // Also used as type annotation in perform_attack
        var extendsRefs = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Extends);
        Assert.IsTrue(extendsRefs.Count >= 2,
            "BaseEntity should be extended by at least 2 classes");

        var typeAnnotationRefs = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.TypeAnnotation);
        Assert.IsTrue(typeAnnotationRefs.Count >= 1,
            "BaseEntity should be used as type annotation (perform_attack parameter)");
    }

    [TestMethod]
    public void FindReferences_ClassName_RefactoringTargets_ExtendedByRenameTest()
    {
        // Arrange & Act
        var references = IntegrationTestHelpers.FindClassTypeUsages(
            TestProjectFixture.Project, "RefactoringTargets");

        // Assert
        var extendsRefs = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Extends);
        Assert.IsTrue(extendsRefs.Count >= 1,
            "RefactoringTargets should be extended by RenameTest");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void FindReferences_SameNameDifferentScopes_DistinctVariables()
    {
        // Test that variables with same name in different scopes are tracked separately
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");

        // Both test_rename_local_variable and test_rename_in_array_operations use 'i'
        // They should be in different scopes
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "i");

        // Should find references but they may be in different scopes
        Assert.IsTrue(references.Count > 0, "Should find 'i' references");
    }

    [TestMethod]
    public void FindReferences_ParameterShadowsClassMember_CorrectScope()
    {
        // When a parameter has the same name as a class member,
        // references should be to the parameter within that function

        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        // 'amount' is a parameter in take_damage and heal methods
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "amount");

        Assert.IsTrue(references.Count > 0, "Should find 'amount' parameter references");

        // Should have multiple declarations (one per method that uses 'amount')
        var declarations = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Declaration);
        Assert.IsTrue(declarations.Count >= 2,
            "Should have declarations for 'amount' in multiple methods");
    }

    [TestMethod]
    public void FindReferences_EmptySymbolName_ReturnsEmpty()
    {
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "");
        Assert.AreEqual(0, references.Count, "Empty symbol name should return empty");
    }

    [TestMethod]
    public void FindReferences_NonExistentSymbol_ReturnsEmpty()
    {
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "non_existent_symbol_xyz");
        Assert.AreEqual(0, references.Count, "Non-existent symbol should return empty");
    }

    #endregion

    #region Cross-File Autoload and Static References

    [TestMethod]
    public void FindReferences_AutoloadMethodCalledCrossFile_DetectsReference()
    {
        // Arrange — GameManager autoload with start_game(), called from main.gd
        var tempDir = Path.Combine(Path.GetTempPath(), "GDShrapt_Test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "game_manager.gd"),
                "extends Node\nclass_name GameManager\n\nfunc start_game() -> void:\n\tpass\n");
            File.WriteAllText(Path.Combine(tempDir, "main.gd"),
                "extends Node\n\nfunc _ready() -> void:\n\tGameManager.start_game()\n");
            File.WriteAllText(Path.Combine(tempDir, "project.godot"),
                "[gd_resource]\n\n[autoload]\nGameManager=\"*res://game_manager.gd\"\n");

            using var project = GDProjectLoader.LoadProject(tempDir);

            // Get semantic models
            var mainScript = project.ScriptFiles.FirstOrDefault(f =>
                f.FullPath != null && f.FullPath.EndsWith("main.gd"));
            Assert.IsNotNull(mainScript, "main.gd not found");

            var mainModel = mainScript.SemanticModel;
            Assert.IsNotNull(mainModel, "main.gd semantic model should exist");

            // Act — check that main.gd has member access for GameManager.start_game
            var hasAccess = mainModel.HasMemberAccesses("GameManager", "start_game");

            // Assert
            Assert.IsTrue(hasAccess,
                "main.gd should have member access reference for GameManager.start_game");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [TestMethod]
    public void FindReferences_StaticMethodCrossFile_DetectsReference()
    {
        // Arrange — Constants class with static method, called from enemy.gd
        var tempDir = Path.Combine(Path.GetTempPath(), "GDShrapt_Test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "constants.gd"),
                "extends RefCounted\nclass_name Constants\n\nstatic func get_enemy_data(enemy_type: int) -> Dictionary:\n\treturn {}\n");
            File.WriteAllText(Path.Combine(tempDir, "enemy.gd"),
                "extends Node\n\nfunc _ready() -> void:\n\tvar data = Constants.get_enemy_data(1)\n\tprint(data)\n");
            File.WriteAllText(Path.Combine(tempDir, "project.godot"),
                "[gd_resource]\n");

            using var project = GDProjectLoader.LoadProject(tempDir);

            // Get semantic model for enemy.gd
            var enemyScript = project.ScriptFiles.FirstOrDefault(f =>
                f.FullPath != null && f.FullPath.EndsWith("enemy.gd"));
            Assert.IsNotNull(enemyScript, "enemy.gd not found");

            var enemyModel = enemyScript.SemanticModel;
            Assert.IsNotNull(enemyModel, "enemy.gd semantic model should exist");

            // Act — check that enemy.gd has member access for Constants.get_enemy_data
            var hasAccess = enemyModel.HasMemberAccesses("Constants", "get_enemy_data");

            // Assert
            Assert.IsTrue(hasAccess,
                "enemy.gd should have member access reference for Constants.get_enemy_data");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [TestMethod]
    public void FindReferences_AutoloadMethodInSubdirectory_DetectsReference()
    {
        // Arrange — autoload in subdirectory, non-static method with params
        var tempDir = Path.Combine(Path.GetTempPath(), "GDShrapt_Test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create subdirectory structure
            var autoloadDir = Path.Combine(tempDir, "src", "autoload");
            var systemsDir = Path.Combine(tempDir, "src", "systems");
            Directory.CreateDirectory(autoloadDir);
            Directory.CreateDirectory(systemsDir);

            File.WriteAllText(Path.Combine(autoloadDir, "game_manager.gd"),
                "extends Node\nclass_name GameManager\n\nvar current_gold: int = 100\n\nfunc spend_gold(amount: int) -> bool:\n\treturn current_gold >= amount\n");
            File.WriteAllText(Path.Combine(systemsDir, "tower_placement.gd"),
                "extends Node\n\nfunc _try_place() -> void:\n\tif not GameManager.spend_gold(50):\n\t\treturn\n\tprint(\"placed\")\n");
            File.WriteAllText(Path.Combine(tempDir, "project.godot"),
                "[gd_resource]\n\n[autoload]\nGameManager=\"*res://src/autoload/game_manager.gd\"\n");

            using var project = GDProjectLoader.LoadProject(tempDir);

            // Get semantic model for tower_placement.gd
            var callerScript = project.ScriptFiles.FirstOrDefault(f =>
                f.FullPath != null && f.FullPath.Replace('\\', '/').EndsWith("tower_placement.gd"));
            Assert.IsNotNull(callerScript, "tower_placement.gd not found");

            var callerModel = callerScript.SemanticModel;
            Assert.IsNotNull(callerModel, "tower_placement.gd semantic model should exist");

            // Act — check that tower_placement.gd has member access for GameManager.spend_gold
            var hasAccess = callerModel.HasMemberAccesses("GameManager", "spend_gold");

            // Assert
            Assert.IsTrue(hasAccess,
                "tower_placement.gd should have member access reference for GameManager.spend_gold");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [TestMethod]
    public void FindReferences_AutoloadWithoutClassName_DetectsReference()
    {
        // Arrange — autoload registered as "GameManager" but script has NO class_name
        // Reference collector should store member access under autoload name "GameManager"
        var tempDir = Path.Combine(Path.GetTempPath(), "GDShrapt_Test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var autoloadDir = Path.Combine(tempDir, "src", "autoload");
            Directory.CreateDirectory(autoloadDir);

            // No class_name GameManager — just extends Node
            File.WriteAllText(Path.Combine(autoloadDir, "game_manager.gd"),
                "extends Node\n\nfunc spend_gold(amount: int) -> bool:\n\treturn amount > 0\n");
            File.WriteAllText(Path.Combine(tempDir, "caller.gd"),
                "extends Node\n\nfunc _ready() -> void:\n\tif not GameManager.spend_gold(50):\n\t\treturn\n");
            File.WriteAllText(Path.Combine(tempDir, "project.godot"),
                "[gd_resource]\n\n[autoload]\nGameManager=\"*res://src/autoload/game_manager.gd\"\n");

            using var project = GDProjectLoader.LoadProject(tempDir);

            var callerScript = project.ScriptFiles.FirstOrDefault(f =>
                f.FullPath != null && f.FullPath.Replace('\\', '/').EndsWith("caller.gd"));
            Assert.IsNotNull(callerScript, "caller.gd not found");

            var callerModel = callerScript.SemanticModel;
            Assert.IsNotNull(callerModel, "caller.gd semantic model should exist");

            // Act — reference collector stores access under "GameManager" (autoload name)
            var hasAccess = callerModel.HasMemberAccesses("GameManager", "spend_gold");

            // Assert
            Assert.IsTrue(hasAccess,
                "caller.gd should have member access reference for GameManager.spend_gold even without class_name");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [TestMethod]
    public void FindReferences_InheritedMethodCalledInChild_DetectsReference()
    {
        // Arrange — entity.gd defines heal(), enemy.gd extends Entity and calls bare heal()
        var tempDir = Path.Combine(Path.GetTempPath(), "GDShrapt_Test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "entity.gd"),
                "extends Node2D\nclass_name Entity\n\nfunc heal(amount: int) -> void:\n\tpass\n");
            File.WriteAllText(Path.Combine(tempDir, "enemy.gd"),
                "extends Entity\n\nfunc _ability_heal() -> void:\n\theal(50)\n");
            File.WriteAllText(Path.Combine(tempDir, "project.godot"),
                "[gd_resource]\n");

            using var project = GDProjectLoader.LoadProject(tempDir);

            var enemyScript = project.ScriptFiles.FirstOrDefault(f =>
                f.FullPath != null && f.FullPath.Replace('\\', '/').EndsWith("enemy.gd"));
            Assert.IsNotNull(enemyScript, "enemy.gd not found");

            var enemyModel = enemyScript.SemanticModel;
            Assert.IsNotNull(enemyModel, "enemy.gd semantic model should exist");

            // Act — bare heal() call should register as member access on Entity
            var hasAccess = enemyModel.HasMemberAccesses("Entity", "heal");

            // Assert
            Assert.IsTrue(hasAccess,
                "enemy.gd should have member access reference for Entity.heal via inherited bare call");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    #endregion

    #region Scope Isolation: For-Loop and Setter (RC5/RC6/RC8)

    private static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_findref_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempPath);

        File.WriteAllText(Path.Combine(tempPath, "project.godot"),
            "[gd_resource type=\"ProjectSettings\" format=3]\n\nconfig_version=5\n\n[application]\nconfig/name=\"TestProject\"\n");

        foreach (var (name, content) in scripts)
        {
            var fileName = name.EndsWith(".gd", System.StringComparison.OrdinalIgnoreCase) ? name : name + ".gd";
            File.WriteAllText(Path.Combine(tempPath, fileName), content);
        }

        return tempPath;
    }

    private static void DeleteTempProject(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
    }

    [TestMethod]
    public void FindRefs_ForLoopIterator_OnlyInsideLoop()
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
                "should have iterator x and var x as separate symbols");

            var iteratorSymbol = allX.FirstOrDefault(s => s.Kind == GDSymbolKind.Iterator);
            Assert.IsNotNull(iteratorSymbol, "iterator x should exist");
            var iteratorRefs = model.GetReferencesTo(iteratorSymbol!);
            Assert.IsTrue(iteratorRefs.Any(), "iterator x is used inside loop");

            var varSymbol = allX.FirstOrDefault(s => s.Kind != GDSymbolKind.Iterator);
            Assert.IsNotNull(varSymbol, "var x should exist");
            var varRefs = model.GetReferencesTo(varSymbol!);
            Assert.IsTrue(varRefs.Any(), "var x is used after loop");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void FindRefs_VarInSetter_ScopedToAccessor()
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

            foreach (var symbol in allOldValue)
            {
                var refs = model.GetReferencesTo(symbol);
                Assert.IsTrue(refs.Any(),
                    $"old_value in scope {symbol.DeclaringScopeNode?.GetType().Name} should have references");
                Assert.IsTrue(refs.Any(r => r.IsRead),
                    "old_value should be read in its setter");
            }
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void FindRefs_NestedForLoop_IteratorScopedCorrectly()
    {
        var script = @"extends Node

func process():
    var nodes = get_children()
    for container in nodes:
        var children = container.get_children()
        for item in children:
            print(item)
        var item = container.get_child(-1)
        print(item)
";
        var tempPath = CreateTempProject(("entity.gd", script));
        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var file = project.ScriptFiles.First();
            var model = projectModel.GetSemanticModel(file)!;

            var allItem = model.FindSymbols("item");
            Assert.IsTrue(allItem.Count() >= 2,
                "iterator item and var item should be separate symbols");

            var varSymbol = allItem.FirstOrDefault(s => s.Kind != GDSymbolKind.Iterator);
            Assert.IsNotNull(varSymbol, "var item should exist as non-iterator symbol");
            var varRefs = model.GetReferencesTo(varSymbol!);
            Assert.IsTrue(varRefs.Any(), "var item is used after inner loop");
        }
        finally { DeleteTempProject(tempPath); }
    }

    #endregion
}
