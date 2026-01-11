using System.IO;
using GDShrapt.Abstractions;
using GDShrapt.LSP;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDDocumentManagerTests
{
    private static string GetTestProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var testProjectPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "..", "testproject", "GDShrapt.TestProject"));
        return testProjectPath;
    }

    [TestMethod]
    public void UriToPath_WindowsPath_ConvertsCorrectly()
    {
        // Arrange
        var uri = "file:///C:/Users/test/project/script.gd";

        // Act
        var path = GDDocumentManager.UriToPath(uri);

        // Assert
        path.Should().Be("C:\\Users\\test\\project\\script.gd");
    }

    [TestMethod]
    public void UriToPath_UnixPath_ConvertsCorrectly()
    {
        // Arrange
        var uri = "file:///home/user/project/script.gd";

        // Act
        var path = GDDocumentManager.UriToPath(uri);

        // Assert
        path.Should().Be("/home/user/project/script.gd");
    }

    [TestMethod]
    public void PathToUri_WindowsPath_ConvertsCorrectly()
    {
        // Arrange
        var path = "C:\\Users\\test\\project\\script.gd";

        // Act
        var uri = GDDocumentManager.PathToUri(path);

        // Assert
        uri.Should().Be("file:///C:/Users/test/project/script.gd");
    }

    [TestMethod]
    public void PathToUri_UnixPath_ConvertsCorrectly()
    {
        // Arrange
        var path = "/home/user/project/script.gd";

        // Act
        var uri = GDDocumentManager.PathToUri(path);

        // Assert
        uri.Should().Be("file:///home/user/project/script.gd");
    }

    [TestMethod]
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
        doc.Should().NotBeNull();
        doc.Uri.Should().Be(uri);
        doc.Content.Should().Be(content);
        doc.Version.Should().Be(version);
    }

    [TestMethod]
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
        doc.Should().NotBeNull();
        doc.Content.Should().Be("var x = 2");
        doc.Version.Should().Be(2);
    }

    [TestMethod]
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
        doc.Should().BeNull();
    }

    [TestMethod]
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
        list.Count.Should().Be(3);
    }
}
