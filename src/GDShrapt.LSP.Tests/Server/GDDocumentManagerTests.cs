using System.IO;
using GDShrapt.Abstractions;
using GDShrapt.LSP.Server;
using GDShrapt.Semantics;
using Xunit;

namespace GDShrapt.LSP.Tests.Server;

public class GDDocumentManagerTests
{
    private static string GetTestProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var testProjectPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "..", "testproject", "GDShrapt.TestProject"));
        return testProjectPath;
    }

    [Fact]
    public void UriToPath_WindowsPath_ConvertsCorrectly()
    {
        // Arrange
        var uri = "file:///C:/Users/test/project/script.gd";

        // Act
        var path = GDDocumentManager.UriToPath(uri);

        // Assert
        Assert.Equal("C:\\Users\\test\\project\\script.gd", path);
    }

    [Fact]
    public void UriToPath_UnixPath_ConvertsCorrectly()
    {
        // Arrange
        var uri = "file:///home/user/project/script.gd";

        // Act
        var path = GDDocumentManager.UriToPath(uri);

        // Assert
        Assert.Equal("/home/user/project/script.gd", path);
    }

    [Fact]
    public void PathToUri_WindowsPath_ConvertsCorrectly()
    {
        // Arrange
        var path = "C:\\Users\\test\\project\\script.gd";

        // Act
        var uri = GDDocumentManager.PathToUri(path);

        // Assert
        Assert.Equal("file:///C:/Users/test/project/script.gd", uri);
    }

    [Fact]
    public void PathToUri_UnixPath_ConvertsCorrectly()
    {
        // Arrange
        var path = "/home/user/project/script.gd";

        // Act
        var uri = GDDocumentManager.PathToUri(path);

        // Assert
        Assert.Equal("file:///home/user/project/script.gd", uri);
    }

    [Fact]
    public void OpenDocument_ValidContent_StoresDocument()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            // Skip if test project doesn't exist
            return;
        }

        var context = new GDDefaultProjectContext(testProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        var manager = new GDDocumentManager(project);

        var uri = "file:///test/script.gd";
        var content = "var x = 1";
        var version = 1;

        // Act
        manager.OpenDocument(uri, content, version);

        // Assert
        var doc = manager.GetDocument(uri);
        Assert.NotNull(doc);
        Assert.Equal(uri, doc.Uri);
        Assert.Equal(content, doc.Content);
        Assert.Equal(version, doc.Version);
    }

    [Fact]
    public void UpdateDocument_ExistingDocument_UpdatesContent()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            return;
        }

        var context = new GDDefaultProjectContext(testProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        var manager = new GDDocumentManager(project);

        var uri = "file:///test/script.gd";
        manager.OpenDocument(uri, "var x = 1", 1);

        // Act
        manager.UpdateDocument(uri, "var x = 2", 2);

        // Assert
        var doc = manager.GetDocument(uri);
        Assert.NotNull(doc);
        Assert.Equal("var x = 2", doc.Content);
        Assert.Equal(2, doc.Version);
    }

    [Fact]
    public void CloseDocument_ExistingDocument_RemovesDocument()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            return;
        }

        var context = new GDDefaultProjectContext(testProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        var manager = new GDDocumentManager(project);

        var uri = "file:///test/script.gd";
        manager.OpenDocument(uri, "var x = 1", 1);

        // Act
        manager.CloseDocument(uri);

        // Assert
        var doc = manager.GetDocument(uri);
        Assert.Null(doc);
    }

    [Fact]
    public void GetAllDocuments_MultipleDocuments_ReturnsAll()
    {
        // Arrange
        var testProjectPath = GetTestProjectPath();
        if (!Directory.Exists(testProjectPath))
        {
            return;
        }

        var context = new GDDefaultProjectContext(testProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        var manager = new GDDocumentManager(project);

        manager.OpenDocument("file:///test/a.gd", "var a = 1", 1);
        manager.OpenDocument("file:///test/b.gd", "var b = 2", 1);
        manager.OpenDocument("file:///test/c.gd", "var c = 3", 1);

        // Act
        var docs = manager.GetAllDocuments();

        // Assert
        var list = new System.Collections.Generic.List<GDOpenDocument>(docs);
        Assert.Equal(3, list.Count);
    }
}
