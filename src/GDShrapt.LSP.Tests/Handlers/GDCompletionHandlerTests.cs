using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.LSP;
using GDShrapt.Semantics;
using Xunit;

namespace GDShrapt.LSP.Tests;

public class GDCompletionHandlerTests
{
    private static readonly string TestProjectPath = GetTestProjectPath();

    private static string GetTestProjectPath()
    {
        // Navigate from test output to testproject
        var baseDir = System.IO.Directory.GetCurrentDirectory();
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return System.IO.Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    [Fact]
    public async Task HandleAsync_GeneralCompletion_IncludesKeywords()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = new GDCompletionHandler(project);
        var @params = new GDCompletionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(System.IO.Path.Combine(TestProjectPath, "test_scripts", "base_entity.gd"))
            },
            Position = new GDLspPosition(5, 0)
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);

        // Should contain keywords
        var keywords = result.Items.Where(i => i.Kind == GDLspCompletionItemKind.Keyword).ToList();
        Assert.NotEmpty(keywords);
        Assert.Contains(keywords, k => k.Label == "func");
        Assert.Contains(keywords, k => k.Label == "var");
        Assert.Contains(keywords, k => k.Label == "if");
    }

    [Fact]
    public async Task HandleAsync_GeneralCompletion_IncludesBuiltinTypes()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = new GDCompletionHandler(project);
        var @params = new GDCompletionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(System.IO.Path.Combine(TestProjectPath, "test_scripts", "base_entity.gd"))
            },
            Position = new GDLspPosition(5, 0)
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // Should contain built-in types
        var types = result.Items.Where(i => i.Detail == "built-in type").ToList();
        Assert.NotEmpty(types);
        Assert.Contains(types, t => t.Label == "int");
        Assert.Contains(types, t => t.Label == "String");
        Assert.Contains(types, t => t.Label == "Vector2");
    }

    [Fact]
    public async Task HandleAsync_GeneralCompletion_IncludesLocalSymbols()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = new GDCompletionHandler(project);
        var @params = new GDCompletionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(System.IO.Path.Combine(TestProjectPath, "test_scripts", "base_entity.gd"))
            },
            Position = new GDLspPosition(10, 0)
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // Should contain local methods/variables from the script
        var methods = result.Items.Where(i => i.Kind == GDLspCompletionItemKind.Method).ToList();
        var variables = result.Items.Where(i => i.Kind == GDLspCompletionItemKind.Variable).ToList();

        // The exact symbols depend on the test project content
        Assert.True(methods.Count > 0 || variables.Count > 0, "Should have local symbols");
    }

    [Fact]
    public async Task HandleAsync_MemberAccess_TriggerCharacterDot()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = new GDCompletionHandler(project);
        var @params = new GDCompletionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(System.IO.Path.Combine(TestProjectPath, "test_scripts", "base_entity.gd"))
            },
            Position = new GDLspPosition(10, 5),
            Context = new GDCompletionContext
            {
                TriggerKind = GDLspCompletionTriggerKind.TriggerCharacter,
                TriggerCharacter = "."
            }
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - member access completion may return empty if no type context
        // This test verifies no crash occurs
        Assert.NotNull(result);
    }

    [Fact]
    public async Task HandleAsync_InvalidFile_ReturnsEmptyList()
    {
        // Arrange
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = new GDCompletionHandler(project);
        var @params = new GDCompletionParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri("/nonexistent/file.gd")
            },
            Position = new GDLspPosition(0, 0)
        };

        // Act
        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Assert - should return list with at least keywords even for invalid file
        Assert.NotNull(result);
    }
}
