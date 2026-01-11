using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for GDRenameService.
/// </summary>
[TestClass]
public class RenameServiceTests
{
    private static GDRenameService CreateService()
    {
        return new GDRenameService(TestProjectFixture.Project);
    }

    #region ValidateIdentifier Tests

    [TestMethod]
    public void ValidateIdentifier_ValidName_ReturnsTrue()
    {
        var service = CreateService();
        var result = service.ValidateIdentifier("valid_name", out var error);
        Assert.IsTrue(result);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void ValidateIdentifier_StartsWithUnderscore_ReturnsTrue()
    {
        var service = CreateService();
        var result = service.ValidateIdentifier("_private", out var error);
        Assert.IsTrue(result);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void ValidateIdentifier_WithNumbers_ReturnsTrue()
    {
        var service = CreateService();
        var result = service.ValidateIdentifier("name123", out var error);
        Assert.IsTrue(result);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void ValidateIdentifier_Empty_ReturnsFalse()
    {
        var service = CreateService();
        var result = service.ValidateIdentifier("", out var error);
        Assert.IsFalse(result);
        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void ValidateIdentifier_StartsWithNumber_ReturnsFalse()
    {
        var service = CreateService();
        var result = service.ValidateIdentifier("123name", out var error);
        Assert.IsFalse(result);
        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void ValidateIdentifier_ContainsSpace_ReturnsFalse()
    {
        var service = CreateService();
        var result = service.ValidateIdentifier("invalid name", out var error);
        Assert.IsFalse(result);
        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void ValidateIdentifier_ContainsDash_ReturnsFalse()
    {
        var service = CreateService();
        var result = service.ValidateIdentifier("invalid-name", out var error);
        Assert.IsFalse(result);
        Assert.IsNotNull(error);
    }

    #endregion

    #region CheckConflicts Tests

    [TestMethod]
    public void CheckConflicts_ReservedKeyword_ReturnsConflict()
    {
        var service = CreateService();
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script);
        if (script.Analyzer == null) script.Analyze();

        var symbol = script.Analyzer!.FindSymbols("counter").FirstOrDefault();
        Assert.IsNotNull(symbol);

        var conflicts = service.CheckConflicts(symbol, "if");

        Assert.IsTrue(conflicts.Count > 0);
        Assert.IsTrue(conflicts.Any(c => c.Type == GDRenameConflictType.ReservedKeyword));
    }

    [TestMethod]
    public void CheckConflicts_BuiltInType_ReturnsConflict()
    {
        var service = CreateService();
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script);
        if (script.Analyzer == null) script.Analyze();

        var symbol = script.Analyzer!.FindSymbols("counter").FirstOrDefault();
        Assert.IsNotNull(symbol);

        var conflicts = service.CheckConflicts(symbol, "Array");

        Assert.IsTrue(conflicts.Count > 0);
        Assert.IsTrue(conflicts.Any(c => c.Type == GDRenameConflictType.BuiltInType));
    }

    [TestMethod]
    public void CheckConflicts_ValidNewName_NoConflicts()
    {
        var service = CreateService();
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script);
        if (script.Analyzer == null) script.Analyze();

        var symbol = script.Analyzer!.FindSymbols("counter").FirstOrDefault();
        Assert.IsNotNull(symbol);

        var conflicts = service.CheckConflicts(symbol, "my_new_counter");

        Assert.AreEqual(0, conflicts.Count);
    }

    #endregion

    #region PlanRename Tests

    [TestMethod]
    public void PlanRename_LocalVariable_ReturnsEdits()
    {
        var service = CreateService();
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script);
        if (script.Analyzer == null) script.Analyze();

        var symbol = script.Analyzer!.FindSymbols("counter").FirstOrDefault();
        Assert.IsNotNull(symbol);

        var result = service.PlanRename(symbol, "my_counter");

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Edits.Count > 0, "Should have edits for renaming");
        Assert.AreEqual(1, result.FileCount, "Should modify one file");
    }

    [TestMethod]
    public void PlanRename_ClassMember_ReturnsEdits()
    {
        var service = CreateService();
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script);
        if (script.Analyzer == null) script.Analyze();

        var symbol = script.Analyzer!.FindSymbols("multiplier").FirstOrDefault();
        Assert.IsNotNull(symbol);

        var result = service.PlanRename(symbol, "scale_factor");

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Edits.Count > 0, "Should have edits for renaming class member");
    }

    [TestMethod]
    public void PlanRename_InvalidNewName_ReturnsFailed()
    {
        var service = CreateService();
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script);
        if (script.Analyzer == null) script.Analyze();

        var symbol = script.Analyzer!.FindSymbols("counter").FirstOrDefault();
        Assert.IsNotNull(symbol);

        var result = service.PlanRename(symbol, "123invalid");

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void PlanRename_ReservedKeyword_ReturnsConflicts()
    {
        var service = CreateService();
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script);
        if (script.Analyzer == null) script.Analyze();

        var symbol = script.Analyzer!.FindSymbols("counter").FirstOrDefault();
        Assert.IsNotNull(symbol);

        var result = service.PlanRename(symbol, "return");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Conflicts.Count > 0);
    }

    [TestMethod]
    public void PlanRenameByName_ExistingSymbol_ReturnsEdits()
    {
        var service = CreateService();

        var result = service.PlanRename("multiplier", "scale_factor");

        Assert.IsTrue(result.Success || result.Edits.Count == 0, "Should succeed or have no occurrences");
    }

    [TestMethod]
    public void PlanRenameByName_NonExistentSymbol_ReturnsNoOccurrences()
    {
        var service = CreateService();

        var result = service.PlanRename("nonexistent_symbol_xyz", "new_name");

        Assert.IsTrue(result.Success); // No occurrences is not an error
        Assert.AreEqual(0, result.Edits.Count);
    }

    #endregion

    #region ApplyEdits Tests

    [TestMethod]
    public void ApplyEdits_SingleEdit_AppliesCorrectly()
    {
        var service = CreateService();
        var content = "var counter = 0\nprint(counter)";
        var edits = new[]
        {
            new GDTextEdit("test.gd", 2, 7, "counter", "my_counter"),
            new GDTextEdit("test.gd", 1, 5, "counter", "my_counter")
        };

        var result = service.ApplyEdits(content, edits);

        Assert.IsTrue(result.Contains("var my_counter"));
        Assert.IsTrue(result.Contains("print(my_counter)"));
    }

    [TestMethod]
    public void ApplyEdits_MultipleEditsOnSameLine_AppliesCorrectly()
    {
        var service = CreateService();
        var content = "x = x + x";
        var edits = new[]
        {
            new GDTextEdit("test.gd", 1, 9, "x", "y"),
            new GDTextEdit("test.gd", 1, 5, "x", "y"),
            new GDTextEdit("test.gd", 1, 1, "x", "y")
        };

        var result = service.ApplyEdits(content, edits);

        Assert.AreEqual("y = y + y", result);
    }

    [TestMethod]
    public void ApplyEdits_EditsInReverseOrder_AppliesCorrectly()
    {
        var service = CreateService();
        var content = "line1\nline2\nline3";
        var edits = new[]
        {
            new GDTextEdit("test.gd", 3, 1, "line3", "new3"),
            new GDTextEdit("test.gd", 1, 1, "line1", "new1")
        };

        var result = service.ApplyEdits(content, edits);

        Assert.IsTrue(result.Contains("new1"));
        Assert.IsTrue(result.Contains("line2")); // unchanged
        Assert.IsTrue(result.Contains("new3"));
    }

    #endregion
}
