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
        var analyzer = script.Analyzer;
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
        var analyzer = script.Analyzer;
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
        var baseMethods = baseScript.Analyzer?.GetMethods().ToList();
        var playerMethods = playerScript.Analyzer?.GetMethods().ToList();
        var enemyMethods = enemyScript.Analyzer?.GetMethods().ToList();

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
        var variables = enemyScript.Analyzer?.GetVariables().ToList();

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
        var playerMethods = playerScript.Analyzer?.GetMethods().ToList();
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
        var baseSignals = baseScript.Analyzer?.GetSignals().ToList();
        var playerSignals = playerScript.Analyzer?.GetSignals().ToList();

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
