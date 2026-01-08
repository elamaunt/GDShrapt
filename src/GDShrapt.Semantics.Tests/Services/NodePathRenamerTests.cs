using GDShrapt.Semantics.Tests.Integration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Services;

/// <summary>
/// Tests for GDNodePathRenamer.
/// </summary>
[TestClass]
public class NodePathRenamerTests
{
    private static GDNodePathRenamer CreateRenamer()
    {
        return new GDNodePathRenamer(TestProjectFixture.Project);
    }

    #region RenameInSceneContent Tests

    [TestMethod]
    public void RenameInSceneContent_NodeName_Replaces()
    {
        var renamer = CreateRenamer();
        var content = @"[node name=""Player"" type=""CharacterBody2D""]";

        var result = renamer.RenameInSceneContent(content, "Player", "Hero");

        Assert.AreEqual(@"[node name=""Hero"" type=""CharacterBody2D""]", result);
    }

    [TestMethod]
    public void RenameInSceneContent_ParentPath_Replaces()
    {
        var renamer = CreateRenamer();
        var content = @"[node name=""Weapon"" type=""Node2D"" parent=""Player""]";

        var result = renamer.RenameInSceneContent(content, "Player", "Hero");

        Assert.AreEqual(@"[node name=""Weapon"" type=""Node2D"" parent=""Hero""]", result);
    }

    [TestMethod]
    public void RenameInSceneContent_ParentPathPrefix_Replaces()
    {
        var renamer = CreateRenamer();
        var content = @"[node name=""Sprite"" type=""Sprite2D"" parent=""Player/Weapon""]";

        var result = renamer.RenameInSceneContent(content, "Player", "Hero");

        Assert.AreEqual(@"[node name=""Sprite"" type=""Sprite2D"" parent=""Hero/Weapon""]", result);
    }

    [TestMethod]
    public void RenameInSceneContent_ParentPathSuffix_Replaces()
    {
        var renamer = CreateRenamer();
        var content = @"[node name=""Sprite"" type=""Sprite2D"" parent=""Root/Player""]";

        var result = renamer.RenameInSceneContent(content, "Player", "Hero");

        Assert.AreEqual(@"[node name=""Sprite"" type=""Sprite2D"" parent=""Root/Hero""]", result);
    }

    [TestMethod]
    public void RenameInSceneContent_ParentPathMiddle_Replaces()
    {
        var renamer = CreateRenamer();
        var content = @"[node name=""Sprite"" type=""Sprite2D"" parent=""Root/Player/Weapon""]";

        var result = renamer.RenameInSceneContent(content, "Player", "Hero");

        Assert.AreEqual(@"[node name=""Sprite"" type=""Sprite2D"" parent=""Root/Hero/Weapon""]", result);
    }

    [TestMethod]
    public void RenameInSceneContent_MultipleOccurrences_ReplacesAll()
    {
        var renamer = CreateRenamer();
        var content = @"[node name=""Player"" type=""CharacterBody2D""]
[node name=""Sprite"" type=""Sprite2D"" parent=""Player""]
[node name=""Collision"" type=""CollisionShape2D"" parent=""Player""]";

        var result = renamer.RenameInSceneContent(content, "Player", "Hero");

        Assert.IsTrue(result.Contains(@"[node name=""Hero"""));
        Assert.IsTrue(result.Contains(@"parent=""Hero"""));
        Assert.IsFalse(result.Contains(@"name=""Player"""));
        Assert.IsFalse(result.Contains(@"parent=""Player"""));
    }

    [TestMethod]
    public void RenameInSceneContent_NoMatch_ReturnsUnchanged()
    {
        var renamer = CreateRenamer();
        var content = @"[node name=""Enemy"" type=""CharacterBody2D""]";

        var result = renamer.RenameInSceneContent(content, "Player", "Hero");

        Assert.AreEqual(content, result);
    }

    [TestMethod]
    public void RenameInSceneContent_PartialMatch_DoesNotReplace()
    {
        var renamer = CreateRenamer();
        // Should not replace "PlayerController" when renaming "Player"
        var content = @"[node name=""PlayerController"" type=""Node""]";

        var result = renamer.RenameInSceneContent(content, "Player", "Hero");

        Assert.AreEqual(content, result);
    }

    #endregion

    #region ApplyRename Tests

    [TestMethod]
    public void ApplyRename_EmptyNewName_ReturnsFailed()
    {
        var renamer = CreateRenamer();

        var result = renamer.ApplyRename(
            System.Array.Empty<GDNodePathReference>(),
            "OldName",
            "");

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public void ApplyRename_SameName_ReturnsEmpty()
    {
        var renamer = CreateRenamer();

        var result = renamer.ApplyRename(
            System.Array.Empty<GDNodePathReference>(),
            "Player",
            "Player");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.EditCount);
    }

    [TestMethod]
    public void ApplyRename_EmptyReferences_ReturnsSuccess()
    {
        var renamer = CreateRenamer();

        var result = renamer.ApplyRename(
            System.Array.Empty<GDNodePathReference>(),
            "OldName",
            "NewName");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.EditCount);
    }

    #endregion
}
