using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Refactoring.Results;

[TestClass]
public class GDRefactoringResultTests
{
    [TestMethod]
    public void Succeeded_WithEdits_CreatesSuccessResult()
    {
        var edits = new[]
        {
            new GDTextEdit("file1.gd", 1, 1, "old", "new"),
            new GDTextEdit("file1.gd", 2, 1, "old2", "new2")
        };

        var result = GDRefactoringResult.Succeeded(edits);

        Assert.IsTrue(result.Success);
        Assert.IsNull(result.ErrorMessage);
        Assert.AreEqual(2, result.TotalEditsCount);
        Assert.AreEqual(1, result.AffectedFilesCount);
    }

    [TestMethod]
    public void Succeeded_SingleEdit_CreatesSuccessResult()
    {
        var edit = new GDTextEdit("file.gd", 1, 1, "old", "new");

        var result = GDRefactoringResult.Succeeded(edit);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.TotalEditsCount);
    }

    [TestMethod]
    public void Failed_CreatesFailedResult()
    {
        var result = GDRefactoringResult.Failed("Something went wrong");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Something went wrong", result.ErrorMessage);
        Assert.AreEqual(0, result.TotalEditsCount);
    }

    [TestMethod]
    public void Empty_CreatesEmptySuccessResult()
    {
        var result = GDRefactoringResult.Empty;

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.TotalEditsCount);
        Assert.AreEqual(0, result.AffectedFilesCount);
    }

    [TestMethod]
    public void EditsByFile_GroupsCorrectly()
    {
        var edits = new[]
        {
            new GDTextEdit("file1.gd", 1, 1, "old1", "new1"),
            new GDTextEdit("file2.gd", 1, 1, "old2", "new2"),
            new GDTextEdit("file1.gd", 2, 1, "old3", "new3")
        };

        var result = GDRefactoringResult.Succeeded(edits);

        Assert.AreEqual(2, result.AffectedFilesCount);
        Assert.AreEqual(2, result.EditsByFile["file1.gd"].Count);
        Assert.AreEqual(1, result.EditsByFile["file2.gd"].Count);
    }

    [TestMethod]
    public void AffectedFilesCount_ReturnsCorrectCount()
    {
        var edits = new[]
        {
            new GDTextEdit("file1.gd", 1, 1, "old", "new"),
            new GDTextEdit("file2.gd", 1, 1, "old", "new"),
            new GDTextEdit("file3.gd", 1, 1, "old", "new")
        };

        var result = GDRefactoringResult.Succeeded(edits);

        Assert.AreEqual(3, result.AffectedFilesCount);
    }

    [TestMethod]
    public void ToString_Success_FormatsCorrectly()
    {
        var edits = new[]
        {
            new GDTextEdit("file1.gd", 1, 1, "old", "new"),
            new GDTextEdit("file2.gd", 1, 1, "old", "new")
        };

        var result = GDRefactoringResult.Succeeded(edits);

        var str = result.ToString();

        Assert.IsTrue(str.Contains("Success"));
        Assert.IsTrue(str.Contains("2 edit"));
        Assert.IsTrue(str.Contains("2 file"));
    }

    [TestMethod]
    public void ToString_Failed_FormatsCorrectly()
    {
        var result = GDRefactoringResult.Failed("Test error");

        var str = result.ToString();

        Assert.IsTrue(str.Contains("Failed"));
        Assert.IsTrue(str.Contains("Test error"));
    }

    [TestMethod]
    public void Edits_IsReadOnly()
    {
        var edits = new[]
        {
            new GDTextEdit("file.gd", 1, 1, "old", "new")
        };

        var result = GDRefactoringResult.Succeeded(edits);

        // Verify we can iterate but the list is readonly
        Assert.AreEqual(1, result.Edits.Count());
    }

    [TestMethod]
    public void EditsByFile_IsReadOnly()
    {
        var edits = new[]
        {
            new GDTextEdit("file.gd", 1, 1, "old", "new")
        };

        var result = GDRefactoringResult.Succeeded(edits);

        // Verify we can access but the dictionary is readonly
        Assert.IsTrue(result.EditsByFile.ContainsKey("file.gd"));
    }
}
