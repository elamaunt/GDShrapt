using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for GDRenameService.PlanNodePathRename().
/// </summary>
[TestClass]
public class NodePathRenamerTests
{
    private static GDRenameService CreateRenameService()
    {
        return new GDRenameService(TestProjectFixture.Project);
    }

    #region PlanNodePathRename Validation Tests

    [TestMethod]
    public void PlanNodePathRename_EmptyNewName_ReturnsFailed()
    {
        var service = CreateRenameService();

        var result = service.PlanNodePathRename("OldName", "");

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void PlanNodePathRename_EmptyOldName_ReturnsFailed()
    {
        var service = CreateRenameService();

        var result = service.PlanNodePathRename("", "NewName");

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void PlanNodePathRename_SameName_ReturnsNoOccurrences()
    {
        var service = CreateRenameService();

        var result = service.PlanNodePathRename("Player", "Player");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.StrictEdits.Count);
    }

    #endregion
}
