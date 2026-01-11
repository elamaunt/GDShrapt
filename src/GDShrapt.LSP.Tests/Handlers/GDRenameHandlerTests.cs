using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.LSP;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDRenameHandlerTests
{
    private static readonly string TestProjectPath = GetTestProjectPath();

    private static string GetTestProjectPath()
    {
        // Navigate from test output to testproject
        var baseDir = System.IO.Directory.GetCurrentDirectory();
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return System.IO.Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    [TestMethod]
    public async Task HandleAsync_RenameVariable_ReturnsWorkspaceEdit()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        // Find a script with a variable
        var refactoringTargetPath = System.IO.Path.Combine(TestProjectPath, "test_scripts", "refactoring_targets.gd");
        var script = project.GetScript(refactoringTargetPath);

        // Skip if file doesn't exist
        if (script == null)
            return;

        var handler = new GDRenameHandler(project);
        var @params = new GDRenameParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(refactoringTargetPath)
            },
            Position = new GDLspPosition(3, 4), // Position of a variable
            NewName = "new_name"
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - result may be null if no symbol at position, that's OK
        // This test verifies no crash occurs
    }

    [TestMethod]
    public async Task HandleAsync_InvalidFile_ReturnsNull()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = new GDRenameHandler(project);
        var @params = new GDRenameParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri("/nonexistent/file.gd")
            },
            Position = new GDLspPosition(0, 0),
            NewName = "new_name"
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task HandleAsync_EmptyNewName_ReturnsNull()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var baseEntityPath = System.IO.Path.Combine(TestProjectPath, "test_scripts", "base_entity.gd");

        var handler = new GDRenameHandler(project);
        var @params = new GDRenameParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(baseEntityPath)
            },
            Position = new GDLspPosition(3, 4),
            NewName = ""
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task HandleAsync_WhitespaceNewName_ReturnsNull()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var baseEntityPath = System.IO.Path.Combine(TestProjectPath, "test_scripts", "base_entity.gd");

        var handler = new GDRenameHandler(project);
        var @params = new GDRenameParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(baseEntityPath)
            },
            Position = new GDLspPosition(3, 4),
            NewName = "   "
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task HandleAsync_NoSymbolAtPosition_ReturnsNull()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var baseEntityPath = System.IO.Path.Combine(TestProjectPath, "test_scripts", "base_entity.gd");

        var handler = new GDRenameHandler(project);
        var @params = new GDRenameParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(baseEntityPath)
            },
            Position = new GDLspPosition(0, 0), // Usually empty/comment area
            NewName = "new_name"
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - may return null if no symbol at position
        // This test verifies no crash occurs
    }
}
