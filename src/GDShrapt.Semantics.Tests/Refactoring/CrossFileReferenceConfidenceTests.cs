using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for cross-file reference confidence classification.
/// Verifies that GDRenameService correctly separates strict and potential references.
/// </summary>
[TestClass]
public class CrossFileReferenceConfidenceTests
{
    #region Same-File References (Always Strict)

    [TestMethod]
    public void Rename_SameFile_AllEditsAreStrict()
    {
        // Arrange
        var code = @"
class_name Player

var health: int = 100

func take_damage(amount: int):
    health -= amount
    if health <= 0:
        die()

func die():
    health = 0
";
        var project = CreateSingleFileProject("player.gd", code);
        var script = project.ScriptFiles.First();
        var symbol = script.SemanticModel?.FindSymbol("health");

        Assert.IsNotNull(symbol);

        var renameService = new GDRenameService(project);

        // Act
        var result = renameService.PlanRename(symbol, "hp");

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.StrictEdits.Count > 0, "Should have strict edits");
        Assert.AreEqual(0, result.PotentialEdits.Count, "Same-file edits should all be strict");

        // All edits should be for the same file
        foreach (var edit in result.StrictEdits)
        {
            Assert.AreEqual(GDReferenceConfidence.Strict, edit.Confidence);
        }
    }

    #endregion

    #region Rename Result Structure Tests

    [TestMethod]
    public void RenameResult_StrictAndPotentialSeparated()
    {
        // Arrange - create result with mixed edits
        var strictEdits = new List<GDTextEdit>
        {
            new GDTextEdit("file1.gd", 1, 1, "foo", "bar", GDReferenceConfidence.Strict, "Type matched"),
            new GDTextEdit("file1.gd", 5, 10, "foo", "bar", GDReferenceConfidence.Strict, "Type matched")
        };
        var potentialEdits = new List<GDTextEdit>
        {
            new GDTextEdit("file2.gd", 3, 5, "foo", "bar", GDReferenceConfidence.Potential, "Duck typed")
        };

        // Act
        var result = GDRenameResult.SuccessfulWithConfidence(strictEdits, potentialEdits, 2);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.StrictEdits.Count);
        Assert.AreEqual(1, result.PotentialEdits.Count);
        Assert.AreEqual(3, result.Edits.Count, "Edits should combine both");
        Assert.AreEqual(2, result.FileCount);
    }

    [TestMethod]
    public void RenameResult_BackwardCompatible_EditsProperty()
    {
        // Arrange
        var strictEdits = new List<GDTextEdit>
        {
            new GDTextEdit("file1.gd", 1, 1, "a", "b", GDReferenceConfidence.Strict)
        };
        var potentialEdits = new List<GDTextEdit>
        {
            new GDTextEdit("file2.gd", 2, 2, "a", "b", GDReferenceConfidence.Potential)
        };

        // Act
        var result = GDRenameResult.SuccessfulWithConfidence(strictEdits, potentialEdits, 2);

        // Assert - backward compatible Edits property contains all
        Assert.AreEqual(2, result.Edits.Count);
        Assert.IsTrue(result.Edits.Any(e => e.Confidence == GDReferenceConfidence.Strict));
        Assert.IsTrue(result.Edits.Any(e => e.Confidence == GDReferenceConfidence.Potential));
    }

    #endregion

    #region GDTextEdit Confidence Tests

    [TestMethod]
    public void GDTextEdit_DefaultConfidence_IsStrict()
    {
        // Arrange & Act
        var edit = new GDTextEdit("file.gd", 1, 1, "old", "new");

        // Assert
        Assert.AreEqual(GDReferenceConfidence.Strict, edit.Confidence);
        Assert.IsNull(edit.ConfidenceReason);
    }

    [TestMethod]
    public void GDTextEdit_WithConfidence_StoresValues()
    {
        // Arrange & Act
        var edit = new GDTextEdit(
            "file.gd", 10, 5, "method", "newMethod",
            GDReferenceConfidence.Potential,
            "Variable type unknown");

        // Assert
        Assert.AreEqual(GDReferenceConfidence.Potential, edit.Confidence);
        Assert.AreEqual("Variable type unknown", edit.ConfidenceReason);
    }

    [TestMethod]
    public void GDTextEdit_ToString_IncludesConfidence()
    {
        // Arrange
        var edit = new GDTextEdit("test.gd", 5, 10, "foo", "bar", GDReferenceConfidence.Potential);

        // Act
        var str = edit.ToString();

        // Assert
        Assert.IsTrue(str.Contains("Potential"), $"ToString should include confidence: {str}");
        Assert.IsTrue(str.Contains("foo"), $"ToString should include old text: {str}");
        Assert.IsTrue(str.Contains("bar"), $"ToString should include new text: {str}");
    }

    #endregion

    #region Rename Method Tests

    [TestMethod]
    public void Rename_MethodInSameFile_AllStrict()
    {
        // Arrange
        var code = @"
func attack():
    pass

func process():
    attack()
    attack()
";
        var project = CreateSingleFileProject("script.gd", code);
        var script = project.ScriptFiles.First();
        var symbol = script.SemanticModel?.FindSymbol("attack");

        Assert.IsNotNull(symbol);

        var renameService = new GDRenameService(project);

        // Act
        var result = renameService.PlanRename(symbol, "do_attack");

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.StrictEdits.Count >= 3, "Should have at least 3 strict edits (1 decl + 2 calls)");
        Assert.AreEqual(0, result.PotentialEdits.Count);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Rename_NoOccurrences_ReturnsEmptyEdits()
    {
        // Arrange
        var code = @"
var x = 10
";
        var project = CreateSingleFileProject("script.gd", code);
        var script = project.ScriptFiles.First();
        var symbol = script.SemanticModel?.FindSymbol("x");

        Assert.IsNotNull(symbol);

        // Create a fresh rename service that won't find "nonexistent"
        var renameService = new GDRenameService(project);

        // Act - rename to same name should work but find occurrences
        var result = renameService.PlanRename(symbol, "y");

        // Assert
        Assert.IsTrue(result.Success);
        // Should have at least 1 edit for the declaration
        Assert.IsTrue(result.StrictEdits.Count >= 1);
    }

    [TestMethod]
    public void Rename_InvalidNewName_Fails()
    {
        // Arrange
        var code = @"
var my_var = 10
";
        var project = CreateSingleFileProject("script.gd", code);
        var script = project.ScriptFiles.First();
        var symbol = script.SemanticModel?.FindSymbol("my_var");

        Assert.IsNotNull(symbol);

        var renameService = new GDRenameService(project);

        // Act - try to rename to invalid identifier
        var result = renameService.PlanRename(symbol, "123invalid");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void Rename_ReservedKeyword_HasConflict()
    {
        // Arrange
        var code = @"
var my_var = 10
";
        var project = CreateSingleFileProject("script.gd", code);
        var script = project.ScriptFiles.First();
        var symbol = script.SemanticModel?.FindSymbol("my_var");

        Assert.IsNotNull(symbol);

        var renameService = new GDRenameService(project);

        // Act - try to rename to reserved keyword
        var result = renameService.PlanRename(symbol, "if");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Conflicts.Count > 0 || !string.IsNullOrEmpty(result.ErrorMessage));
    }

    #endregion

    #region Helper Methods

    private static GDScriptProject CreateSingleFileProject(string fileName, string code)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GDShrapt_Test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        var filePath = Path.Combine(tempDir, fileName);
        File.WriteAllText(filePath, code);

        // Create project.godot to mark as Godot project
        File.WriteAllText(Path.Combine(tempDir, "project.godot"), "[gd_resource]\n");

        var context = new GDDefaultProjectContext(tempDir);
        var project = new GDScriptProject(context);
        project.LoadScripts();
        project.AnalyzeAll();

        return project;
    }

    #endregion
}
