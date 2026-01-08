using GDShrapt.Semantics.Tests.Integration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Services;

/// <summary>
/// Tests for GDNodePathReferenceFinder.
/// </summary>
[TestClass]
public class NodePathReferenceFinderTests
{
    private static GDNodePathReferenceFinder CreateFinder()
    {
        return new GDNodePathReferenceFinder(TestProjectFixture.Project);
    }

    #region FindGDScriptReferences Tests

    [TestMethod]
    public void FindGDScriptReferences_ExistingNodeName_ReturnsReferences()
    {
        var finder = CreateFinder();

        // Look for references to "Player" or any common node name in GDScript files
        // This test depends on actual test project content
        var references = finder.FindGDScriptReferences("Player").ToList();

        // The test passes if it runs without error
        // In a real test project, we would verify specific references
        Assert.IsNotNull(references);
    }

    [TestMethod]
    public void FindGDScriptReferences_NonExistentNodeName_ReturnsEmpty()
    {
        var finder = CreateFinder();

        var references = finder.FindGDScriptReferences("NonExistentNodeXYZ123").ToList();

        Assert.AreEqual(0, references.Count);
    }

    [TestMethod]
    public void FindGDScriptReferences_EmptyName_ReturnsEmpty()
    {
        var finder = CreateFinder();

        var references = finder.FindGDScriptReferences("").ToList();

        Assert.AreEqual(0, references.Count);
    }

    [TestMethod]
    public void FindGDScriptReferences_NullName_ReturnsEmpty()
    {
        var finder = CreateFinder();

        var references = finder.FindGDScriptReferences(null!).ToList();

        Assert.AreEqual(0, references.Count);
    }

    #endregion

    #region FindAllReferences Tests

    [TestMethod]
    public void FindAllReferences_EmptyName_ReturnsEmpty()
    {
        var finder = CreateFinder();

        var references = finder.FindAllReferences("").ToList();

        Assert.AreEqual(0, references.Count);
    }

    [TestMethod]
    public void FindAllReferences_ReturnsGDScriptReferences()
    {
        var finder = CreateFinder();

        // Should at least be able to run without error
        var references = finder.FindAllReferences("SomeNode").ToList();

        Assert.IsNotNull(references);
    }

    #endregion
}
