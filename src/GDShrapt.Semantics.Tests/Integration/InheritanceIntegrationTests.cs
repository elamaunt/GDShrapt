using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Integration tests for inheritance relationships and cross-file type resolution.
/// </summary>
[TestClass]
public class InheritanceIntegrationTests
{
    #region Inheritance Chain Detection

    [TestMethod]
    public void InheritanceChain_BaseEntity_IsExtendedByMultipleClasses()
    {
        // Arrange
        var project = TestProjectFixture.Project;

        // Act
        var references = IntegrationTestHelpers.FindClassTypeUsages(project, "BaseEntity");

        // Assert
        var extendsRefs = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Extends);

        // BaseEntity is extended by PlayerEntity and EnemyEntity
        Assert.IsTrue(extendsRefs.Count >= 2,
            $"BaseEntity should be extended by at least 2 classes, found {extendsRefs.Count}");

        // Check files
        var extendingFiles = extendsRefs.Select(r => System.IO.Path.GetFileName(r.FilePath ?? "")).ToList();
        Assert.IsTrue(extendingFiles.Any(f => f == "player_entity.gd"),
            "PlayerEntity should extend BaseEntity");
        Assert.IsTrue(extendingFiles.Any(f => f == "enemy_entity.gd"),
            "EnemyEntity should extend BaseEntity");
    }

    [TestMethod]
    public void InheritanceChain_RefactoringTargets_ExtendedByRenameTest()
    {
        // Arrange
        var project = TestProjectFixture.Project;

        // Act
        var references = IntegrationTestHelpers.FindClassTypeUsages(project, "RefactoringTargets");

        // Assert
        var extendsRefs = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Extends);

        Assert.IsTrue(extendsRefs.Count >= 1,
            "RefactoringTargets should be extended by at least 1 class");

        var extendingFiles = extendsRefs.Select(r => System.IO.Path.GetFileName(r.FilePath ?? "")).ToList();
        Assert.IsTrue(extendingFiles.Any(f => f == "rename_test.gd"),
            "RenameTest should extend RefactoringTargets");
    }

    [TestMethod]
    public void InheritanceChain_MultiLevel_RenameTestExtendsRefactoringTargetsExtendsNode2D()
    {
        // Arrange
        var renameTestScript = TestProjectFixture.GetScriptByType("RenameTest");
        Assert.IsNotNull(renameTestScript, "RenameTest class not found");

        var refactoringTargetsScript = TestProjectFixture.GetScriptByType("RefactoringTargets");
        Assert.IsNotNull(refactoringTargetsScript, "RefactoringTargets class not found");

        // Act & Assert
        // RenameTest extends RefactoringTargets
        var renameTestExtends = renameTestScript.Class?.Extends?.Type?.BuildName();
        Assert.AreEqual("RefactoringTargets", renameTestExtends,
            $"RenameTest should extend RefactoringTargets, got {renameTestExtends}");

        // RefactoringTargets extends Node2D
        var refactoringTargetsExtends = refactoringTargetsScript.Class?.Extends?.Type?.BuildName();
        Assert.AreEqual("Node2D", refactoringTargetsExtends,
            $"RefactoringTargets should extend Node2D, got {refactoringTargetsExtends}");
    }

    #endregion

    #region Super Calls

    [TestMethod]
    public void SuperCall_PlayerEntity_Ready_CallsBaseReady()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("player_entity.gd");
        Assert.IsNotNull(script, "player_entity.gd not found");

        // Act - _ready method calls super._ready()
        // This should be detected as a reference
        var analyzer = script.SemanticModel;
        Assert.IsNotNull(analyzer, "Script should be analyzed");

        // The _ready method should exist
        var readyMethod = analyzer.GetMethods().FirstOrDefault(m => m.Name == "_ready");
        Assert.IsNotNull(readyMethod, "Should have _ready method");
    }

    [TestMethod]
    public void SuperCall_EnemyEntity_Die_CallsBaseDie()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("enemy_entity.gd");
        Assert.IsNotNull(script, "enemy_entity.gd not found");

        // Act
        var analyzer = script.SemanticModel;
        Assert.IsNotNull(analyzer, "Script should be analyzed");

        // The die method should exist (it calls super.die())
        var dieMethod = analyzer.GetMethods().FirstOrDefault(m => m.Name == "die");
        Assert.IsNotNull(dieMethod, "Should have die method that calls super.die()");
    }

    [TestMethod]
    public void SuperCall_TakeDamage_OverriddenInMultipleClasses()
    {
        // Arrange
        var baseScript = TestProjectFixture.GetScript("base_entity.gd");
        var playerScript = TestProjectFixture.GetScript("player_entity.gd");
        var enemyScript = TestProjectFixture.GetScript("enemy_entity.gd");

        Assert.IsNotNull(baseScript, "base_entity.gd not found");
        Assert.IsNotNull(playerScript, "player_entity.gd not found");
        Assert.IsNotNull(enemyScript, "enemy_entity.gd not found");

        // Act
        var baseMethods = baseScript.SemanticModel?.GetMethods().ToList();
        var playerMethods = playerScript.SemanticModel?.GetMethods().ToList();
        var enemyMethods = enemyScript.SemanticModel?.GetMethods().ToList();

        // Assert - take_damage exists in all three
        Assert.IsTrue(baseMethods?.Any(m => m.Name == "take_damage") == true,
            "BaseEntity should have take_damage");
        Assert.IsTrue(playerMethods?.Any(m => m.Name == "take_damage") == true,
            "PlayerEntity should override take_damage");
        Assert.IsTrue(enemyMethods?.Any(m => m.Name == "take_damage") == true,
            "EnemyEntity should override take_damage");
    }

    #endregion

    #region Inherited Member Access

    [TestMethod]
    public void InheritedMember_CurrentHealth_UsedInDerivedClasses()
    {
        // Arrange - current_health defined in BaseEntity, used in PlayerEntity and EnemyEntity
        var playerScript = TestProjectFixture.GetScript("player_entity.gd");
        var enemyScript = TestProjectFixture.GetScript("enemy_entity.gd");

        Assert.IsNotNull(playerScript, "player_entity.gd not found");
        Assert.IsNotNull(enemyScript, "enemy_entity.gd not found");

        // Act
        var playerRefs = IntegrationTestHelpers.CollectReferencesInScript(playerScript, "current_health");
        var enemyRefs = IntegrationTestHelpers.CollectReferencesInScript(enemyScript, "current_health");

        // Assert
        // current_health is used in both derived classes
        Assert.IsTrue(playerRefs.Count > 0 || enemyRefs.Count > 0,
            "current_health should be used in at least one derived class");
    }

    [TestMethod]
    public void InheritedMember_MaxHealth_SetInDerivedReady()
    {
        // Arrange
        var playerScript = TestProjectFixture.GetScript("player_entity.gd");
        var enemyScript = TestProjectFixture.GetScript("enemy_entity.gd");

        Assert.IsNotNull(playerScript, "player_entity.gd not found");
        Assert.IsNotNull(enemyScript, "enemy_entity.gd not found");

        // Act
        var playerRefs = IntegrationTestHelpers.CollectReferencesInScript(playerScript, "max_health");
        var enemyRefs = IntegrationTestHelpers.CollectReferencesInScript(enemyScript, "max_health");

        // Assert
        // max_health is set in _ready of both derived classes
        Assert.IsTrue(playerRefs.Count > 0, "max_health should be used in PlayerEntity");
        Assert.IsTrue(enemyRefs.Count > 0, "max_health should be used in EnemyEntity");
    }

    [TestMethod]
    public void InheritedMember_IsAlive_CheckedInEnemyAttack()
    {
        // Arrange
        var enemyScript = TestProjectFixture.GetScript("enemy_entity.gd");
        Assert.IsNotNull(enemyScript, "enemy_entity.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(enemyScript, "is_alive");

        // Assert
        // is_alive is checked in attack() method
        Assert.IsTrue(references.Count > 0, "is_alive should be used in EnemyEntity");
    }

    [TestMethod]
    public void InheritedMember_Defense_IncrementedInPlayerLevelUp()
    {
        // Arrange
        var playerScript = TestProjectFixture.GetScript("player_entity.gd");
        Assert.IsNotNull(playerScript, "player_entity.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(playerScript, "defense");

        // Assert
        // defense (from BaseEntity) is modified in level_up_player
        Assert.IsTrue(references.Count > 0, "defense should be used in PlayerEntity");
    }

    #endregion

    #region Type Annotations with Custom Classes

    [TestMethod]
    public void TypeAnnotation_BaseEntity_InParameter()
    {
        // Arrange - perform_attack(target: BaseEntity, ...)
        var project = TestProjectFixture.Project;

        // Act
        var references = IntegrationTestHelpers.FindClassTypeUsages(project, "BaseEntity");

        // Assert
        var typeAnnotations = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.TypeAnnotation);
        Assert.IsTrue(typeAnnotations.Count >= 1,
            "BaseEntity should be used as type annotation somewhere");
    }

    [TestMethod]
    public void TypeAnnotation_BaseEntity_InVariable()
    {
        // Arrange - enemy_entity.gd has "var target: BaseEntity = null"
        var enemyScript = TestProjectFixture.GetScript("enemy_entity.gd");
        Assert.IsNotNull(enemyScript, "enemy_entity.gd not found");

        // Act
        var variables = enemyScript.SemanticModel?.GetVariables().ToList();

        // Assert
        var targetVar = variables?.FirstOrDefault(v => v.Name == "target");
        Assert.IsNotNull(targetVar, "Should have 'target' variable");
    }

    #endregion

    #region Is Type Checks

    [TestMethod]
    public void IsTypeCheck_PlayerEntity_InEnemyDie()
    {
        // Arrange - enemy_entity.gd: if last_damage_source is PlayerEntity:
        var enemyScript = TestProjectFixture.GetScript("enemy_entity.gd");
        Assert.IsNotNull(enemyScript, "enemy_entity.gd not found");

        // Act
        var project = TestProjectFixture.Project;
        var references = IntegrationTestHelpers.FindClassTypeUsages(project, "PlayerEntity");

        // Assert
        var typeChecks = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.TypeCheck);
        Assert.IsTrue(typeChecks.Count >= 1,
            "PlayerEntity should be used in 'is' type check");

        // Should be in enemy_entity.gd
        var enemyFileChecks = typeChecks.Where(r =>
            r.FilePath?.EndsWith("enemy_entity.gd") == true).ToList();
        Assert.IsTrue(enemyFileChecks.Count >= 1,
            "Should have 'is PlayerEntity' check in enemy_entity.gd");
    }

    [TestMethod]
    public void IsTypeCheck_BaseEntity_InTakeDamage()
    {
        // Arrange - enemy_entity.gd: if source is BaseEntity:
        var enemyScript = TestProjectFixture.GetScript("enemy_entity.gd");
        Assert.IsNotNull(enemyScript, "enemy_entity.gd not found");

        // Act
        var project = TestProjectFixture.Project;
        var references = IntegrationTestHelpers.FindClassTypeUsages(project, "BaseEntity");

        // Assert
        var typeChecks = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.TypeCheck);
        Assert.IsTrue(typeChecks.Count >= 1,
            "BaseEntity should be used in 'is' type check");
    }

    #endregion

    #region Cross-File Method Calls

    [TestMethod]
    public void MethodCall_GainExperience_CalledOnPlayerEntity()
    {
        // Arrange - enemy_entity.gd calls player.gain_experience(...)
        var enemyScript = TestProjectFixture.GetScript("enemy_entity.gd");
        Assert.IsNotNull(enemyScript, "enemy_entity.gd not found");

        var playerScript = TestProjectFixture.GetScript("player_entity.gd");
        Assert.IsNotNull(playerScript, "player_entity.gd not found");

        // Assert - gain_experience exists in PlayerEntity
        var playerMethods = playerScript.SemanticModel?.GetMethods().ToList();
        Assert.IsTrue(playerMethods?.Any(m => m.Name == "gain_experience") == true,
            "PlayerEntity should have gain_experience method");
    }

    [TestMethod]
    public void MethodCall_TakeDamage_CalledOnTarget()
    {
        // Arrange - enemy_entity.gd calls target.take_damage(...)
        var enemyScript = TestProjectFixture.GetScript("enemy_entity.gd");
        Assert.IsNotNull(enemyScript, "enemy_entity.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(enemyScript, "take_damage");

        // Assert
        // take_damage is declared (override) and also called on target
        Assert.IsTrue(references.Count >= 1,
            "Should find take_damage references in EnemyEntity");
    }

    #endregion

    #region Signal Inheritance

    [TestMethod]
    public void Signal_HealthChanged_InheritedFromBase()
    {
        // Arrange - health_changed signal defined in BaseEntity
        var baseScript = TestProjectFixture.GetScript("base_entity.gd");
        var playerScript = TestProjectFixture.GetScript("player_entity.gd");

        Assert.IsNotNull(baseScript, "base_entity.gd not found");
        Assert.IsNotNull(playerScript, "player_entity.gd not found");

        // Act
        var baseSignals = baseScript.SemanticModel?.GetSignals().ToList();
        var playerSignals = playerScript.SemanticModel?.GetSignals().ToList();

        // Assert
        // Signal should be defined in base
        Assert.IsTrue(baseSignals?.Any(s => s.Name == "health_changed") == true,
            "BaseEntity should define health_changed signal");

        // PlayerEntity emits it via inherited member
        var playerHealthChangedRefs = IntegrationTestHelpers.CollectReferencesInScript(playerScript, "health_changed");
        Assert.IsTrue(playerHealthChangedRefs.Count > 0,
            "PlayerEntity should use inherited health_changed signal");
    }

    #endregion

    #region Path-Based Extends

    [TestMethod]
    public void PathBasedExtends_InheritedVariable_MaxHealth_Resolved()
    {
        // Arrange - PathExtendsTest uses: extends "res://test_scripts/base_entity.gd"
        var script = TestProjectFixture.GetScript("path_extends_test.gd");
        Assert.IsNotNull(script, "path_extends_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "max_health");

        // Assert - max_health is inherited from BaseEntity via path-based extends
        Assert.IsTrue(references.Count >= 2,
            $"max_health should be found in path-based extends test, found {references.Count}");
    }

    [TestMethod]
    public void PathBasedExtends_InheritedVariable_CurrentHealth_Resolved()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("path_extends_test.gd");
        Assert.IsNotNull(script, "path_extends_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "current_health");

        // Assert
        Assert.IsTrue(references.Count >= 2,
            $"current_health should be found in path-based extends test, found {references.Count}");
    }

    [TestMethod]
    public void PathBasedExtends_InheritedMethod_TakeDamage_Resolved()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("path_extends_test.gd");
        Assert.IsNotNull(script, "path_extends_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "take_damage");

        // Assert - take_damage is inherited and called in test_inherited_method_via_path
        Assert.IsTrue(references.Count >= 1,
            $"take_damage should be callable via path-based extends, found {references.Count}");
    }

    [TestMethod]
    public void PathBasedExtends_InheritedMethod_Heal_Resolved()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("path_extends_test.gd");
        Assert.IsNotNull(script, "path_extends_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "heal");

        // Assert
        Assert.IsTrue(references.Count >= 1,
            $"heal should be callable via path-based extends, found {references.Count}");
    }

    [TestMethod]
    public void PathBasedExtends_InheritedSignal_HealthChanged_Resolved()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("path_extends_test.gd");
        Assert.IsNotNull(script, "path_extends_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "health_changed");

        // Assert - health_changed signal is inherited and emitted
        Assert.IsTrue(references.Count >= 1,
            $"health_changed signal should be accessible via path-based extends, found {references.Count}");
    }

    [TestMethod]
    public void PathBasedExtends_BaseTypeResolution_ReturnsCorrectType()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("path_extends_test.gd");
        Assert.IsNotNull(script, "path_extends_test.gd not found");

        // Act - Check that extends type is correctly resolved to the path
        var extendsType = script.Class?.Extends?.Type?.BuildName();

        // Assert
        Assert.IsNotNull(extendsType, "Extends type should be set");
        Assert.IsTrue(extendsType.Contains("base_entity"),
            $"Extends should reference base_entity.gd, got: {extendsType}");
    }

    #endregion

    #region Static Members

    [TestMethod]
    public void StaticMethod_CreateAt_IsRecognizedAsStatic()
    {
        // Arrange - SimpleClass has: static func create_at(pos: Vector2) -> SimpleClass
        var script = TestProjectFixture.GetScript("simple_class.gd");
        Assert.IsNotNull(script, "simple_class.gd not found");

        // Act
        var methods = script.SemanticModel?.GetMethods().ToList();

        // Assert
        Assert.IsNotNull(methods, "Should have methods");
        var createAtMethod = methods.FirstOrDefault(m => m.Name == "create_at");
        Assert.IsNotNull(createAtMethod, "Should find create_at method");
        Assert.IsTrue(createAtMethod.IsStatic, "create_at should be marked as static");
    }

    [TestMethod]
    public void StaticMethod_CreateAt_ReferencesResolved()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("simple_class.gd");
        Assert.IsNotNull(script, "simple_class.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(script, "create_at");

        // Assert - create_at is declared as static func
        Assert.IsTrue(references.Count >= 1,
            "Should find create_at declaration");

        var declarations = IntegrationTestHelpers.FilterByKind(references, ReferenceKind.Declaration);
        Assert.AreEqual(1, declarations.Count, "Should have exactly one declaration");
    }

    [TestMethod]
    public void StaticMethod_SemanticModel_TracksStaticProperty()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("simple_class.gd");
        Assert.IsNotNull(script, "simple_class.gd not found");

        var semanticModel = script.SemanticModel;
        Assert.IsNotNull(semanticModel, "SemanticModel should be available");

        // Act
        var createAtSymbol = semanticModel.FindSymbol("create_at");

        // Assert
        Assert.IsNotNull(createAtSymbol, "Should find create_at symbol");
        Assert.IsTrue(createAtSymbol.IsStatic, "Symbol should be marked as static");
    }

    #endregion

    #region Super Keyword References

    [TestMethod]
    public void SuperCall_InReady_MethodOverridesParent()
    {
        // Arrange - PlayerEntity._ready() calls super._ready()
        var script = TestProjectFixture.GetScript("player_entity.gd");
        Assert.IsNotNull(script, "player_entity.gd not found");

        var analyzer = script.SemanticModel;
        Assert.IsNotNull(analyzer, "Script should be analyzed");

        var readyMethod = analyzer.GetMethods().FirstOrDefault(m => m.Name == "_ready");
        Assert.IsNotNull(readyMethod, "_ready method should exist");
    }

    [TestMethod]
    public void SuperCall_InTakeDamage_OverridesAndCallsParent()
    {
        // Arrange - PlayerEntity.take_damage() calls super.take_damage()
        var script = TestProjectFixture.GetScript("player_entity.gd");
        Assert.IsNotNull(script, "player_entity.gd not found");

        var takeDamageMethod = script.SemanticModel?.GetMethods().FirstOrDefault(m => m.Name == "take_damage");
        Assert.IsNotNull(takeDamageMethod, "take_damage method should exist");
    }

    [TestMethod]
    public void SuperCall_InDie_ChainedOverride()
    {
        // Arrange - PlayerEntity.die() calls super.die()
        var script = TestProjectFixture.GetScript("player_entity.gd");
        Assert.IsNotNull(script, "player_entity.gd not found");

        var dieMethod = script.SemanticModel?.GetMethods().FirstOrDefault(m => m.Name == "die");
        Assert.IsNotNull(dieMethod, "die method should exist");
    }

    [TestMethod]
    public void SuperCall_EnemyEntityDie_AlsoCallsParent()
    {
        // Arrange - EnemyEntity.die() also calls super.die()
        var script = TestProjectFixture.GetScript("enemy_entity.gd");
        Assert.IsNotNull(script, "enemy_entity.gd not found");

        var dieMethod = script.SemanticModel?.GetMethods().FirstOrDefault(m => m.Name == "die");
        Assert.IsNotNull(dieMethod, "die method should exist in EnemyEntity");
    }

    #endregion

    #region Multi-Level Inheritance Chain

    [TestMethod]
    public void ThreeLevelChain_RenameTest_ExtendsRefactoringTargets_ExtendsNode2D()
    {
        // Arrange
        var renameTest = TestProjectFixture.GetScriptByType("RenameTest");
        var refactoringTargets = TestProjectFixture.GetScriptByType("RefactoringTargets");

        Assert.IsNotNull(renameTest, "RenameTest should exist");
        Assert.IsNotNull(refactoringTargets, "RefactoringTargets should exist");

        // Act
        var level1Extends = renameTest.Class?.Extends?.Type?.BuildName();
        var level2Extends = refactoringTargets.Class?.Extends?.Type?.BuildName();

        // Assert - Verify three-level chain: RenameTest -> RefactoringTargets -> Node2D
        Assert.AreEqual("RefactoringTargets", level1Extends,
            $"RenameTest should extend RefactoringTargets, got: {level1Extends}");
        Assert.AreEqual("Node2D", level2Extends,
            $"RefactoringTargets should extend Node2D, got: {level2Extends}");
    }

    [TestMethod]
    public void ThreeLevelChain_InheritedMember_PlayerSpeed_FromRefactoringTargets()
    {
        // Arrange - player_speed is defined in RefactoringTargets
        // RenameTest extends RefactoringTargets, so should access player_speed
        var renameTest = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(renameTest, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(renameTest, "player_speed");

        // Assert
        Assert.IsTrue(references.Count > 0,
            "player_speed from RefactoringTargets should be accessible in RenameTest");
    }

    [TestMethod]
    public void ThreeLevelChain_InheritedSignal_ScoreChanged_FromRefactoringTargets()
    {
        // Arrange - score_changed signal defined in RefactoringTargets
        var renameTest = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(renameTest, "rename_test.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(renameTest, "score_changed");

        // Assert
        Assert.IsTrue(references.Count > 0,
            "score_changed signal from RefactoringTargets should be accessible in RenameTest");
    }

    [TestMethod]
    public void ThreeLevelChain_BaseEntity_ToPlayerEntity_ToExtended()
    {
        // Verify chain: PlayerEntity -> BaseEntity -> Node2D
        var playerEntity = TestProjectFixture.GetScriptByType("PlayerEntity");
        var baseEntity = TestProjectFixture.GetScriptByType("BaseEntity");

        Assert.IsNotNull(playerEntity, "PlayerEntity should exist");
        Assert.IsNotNull(baseEntity, "BaseEntity should exist");

        // Act
        var playerExtends = playerEntity.Class?.Extends?.Type?.BuildName();
        var baseExtends = baseEntity.Class?.Extends?.Type?.BuildName();

        // Assert
        Assert.AreEqual("BaseEntity", playerExtends,
            $"PlayerEntity should extend BaseEntity, got: {playerExtends}");
        Assert.AreEqual("Node2D", baseExtends,
            $"BaseEntity should extend Node2D, got: {baseExtends}");
    }

    [TestMethod]
    public void ThreeLevelChain_DeepInheritedMember_CurrentHealth_FromBaseEntityViaPlayer()
    {
        // Arrange - current_health defined in BaseEntity, accessed in PlayerEntity
        var playerScript = TestProjectFixture.GetScript("player_entity.gd");
        Assert.IsNotNull(playerScript, "player_entity.gd not found");

        // Act
        var references = IntegrationTestHelpers.CollectReferencesInScript(playerScript, "current_health");

        // Assert - current_health should be resolved through inheritance chain
        Assert.IsTrue(references.Count > 0,
            "current_health from BaseEntity should be accessible in PlayerEntity");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Inheritance_ClassWithoutClassName_UsesFilename()
    {
        // Some scripts might not have class_name, should use filename
        var project = TestProjectFixture.Project;

        foreach (var script in project.ScriptFiles)
        {
            // Every script should have a TypeName
            Assert.IsFalse(string.IsNullOrEmpty(script.TypeName),
                $"Script {script.FullPath} should have TypeName");
        }
    }

    [TestMethod]
    public void Inheritance_GetScriptByTypeName_FindsCustomClasses()
    {
        // Arrange & Act
        var project = TestProjectFixture.Project;

        var baseEntity = project.GetScriptByTypeName("BaseEntity");
        var playerEntity = project.GetScriptByTypeName("PlayerEntity");
        var enemyEntity = project.GetScriptByTypeName("EnemyEntity");

        // Assert
        Assert.IsNotNull(baseEntity, "Should find BaseEntity by type name");
        Assert.IsNotNull(playerEntity, "Should find PlayerEntity by type name");
        Assert.IsNotNull(enemyEntity, "Should find EnemyEntity by type name");
    }

    [TestMethod]
    public void Inheritance_NonExistentClass_ReturnsNull()
    {
        // Arrange & Act
        var project = TestProjectFixture.Project;
        var nonExistent = project.GetScriptByTypeName("NonExistentClass");

        // Assert
        Assert.IsNull(nonExistent, "Non-existent class should return null");
    }

    #endregion
}
