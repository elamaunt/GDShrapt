using System.IO;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using GDProjectLoader = GDShrapt.Semantics.GDProjectLoader;

namespace GDShrapt.CLI.Tests.Handlers;

[TestClass]
public class GDCompletionHandlerContextTests
{
    private string? _tempProjectPath;
    private GDScriptProject? _project;
    private GDCompletionHandler? _handler;

    [TestCleanup]
    public void Cleanup()
    {
        _project?.Dispose();
        if (_tempProjectPath != null)
            TestProjectHelper.DeleteTempProject(_tempProjectPath);
    }

    private GDProjectSemanticModel? _projectModel;

    private string SetupProject(params (string name, string content)[] scripts)
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(scripts);
        _project = GDProjectLoader.LoadProject(_tempProjectPath);
        _projectModel = new GDProjectSemanticModel(_project);
        _handler = new GDCompletionHandler(_project, _projectModel.RuntimeProvider, _projectModel, _project.SceneTypesProvider);
        return _tempProjectPath;
    }

    #region Context-aware filtering

    [TestMethod]
    public void ClassLevel_ContainsKeywords_NoLocalVars()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

var health: int = 100

func _ready():
    var local_var = 1
    pass

"));

        var filePath = Path.Combine(projectPath, "test.gd");

        // Line 8 (1-based), empty line at class level after method
        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 8,
            Column = 1,
            CompletionType = GDCompletionType.Symbol,
            CursorContext = GDCursorContext.ClassLevel
        });

        items.Should().NotBeEmpty();

        // Should contain class-level keywords
        items.Should().Contain(i => i.Label == "func");
        items.Should().Contain(i => i.Label == "var");
        items.Should().Contain(i => i.Label == "signal");

        // Should contain class member
        items.Should().Contain(i => i.Label == "health");

        // Should NOT contain local variables from method body
        items.Should().NotContain(i => i.Label == "local_var");
    }

    [TestMethod]
    public void MethodBody_ContainsLocalVarsAndKeywords()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

var health: int = 100

func _ready():
    var local_var = 1

"));

        var filePath = Path.Combine(projectPath, "test.gd");

        // Line 7 (1-based), inside method body — empty line after local_var
        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 7,
            Column = 5,
            CompletionType = GDCompletionType.Symbol,
            CursorContext = GDCursorContext.MethodBody
        });

        items.Should().NotBeEmpty();

        // Should contain method body keywords
        items.Should().Contain(i => i.Label == "if");
        items.Should().Contain(i => i.Label == "for");
        items.Should().Contain(i => i.Label == "return");

        // Should contain local variable
        items.Should().Contain(i => i.Label == "local_var");

        // Should contain class member
        items.Should().Contain(i => i.Label == "health");

        // Should contain built-in functions
        items.Should().Contain(i => i.Label == "print");
    }

    [TestMethod]
    public void StringLiteral_ReturnsEmpty()
    {
        var items = _handler?.GetCompletions(new GDCompletionRequest
        {
            FilePath = "test.gd",
            Line = 1,
            Column = 1,
            CompletionType = GDCompletionType.Symbol,
            CursorContext = GDCursorContext.StringLiteral
        });

        // Need handler to exist for this test
        if (_handler == null)
        {
            SetupProject(("test.gd", "extends Node\n"));
            items = _handler!.GetCompletions(new GDCompletionRequest
            {
                FilePath = Path.Combine(_tempProjectPath!, "test.gd"),
                Line = 1,
                Column = 1,
                CompletionType = GDCompletionType.Symbol,
                CursorContext = GDCursorContext.StringLiteral
            });
        }

        items.Should().BeEmpty("completions should be suppressed in string literals");
    }

    [TestMethod]
    public void Comment_ReturnsEmpty()
    {
        var projectPath = SetupProject(("test.gd", "extends Node\n"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 1,
            Column = 1,
            CompletionType = GDCompletionType.Symbol,
            CursorContext = GDCursorContext.Comment
        });

        items.Should().BeEmpty("completions should be suppressed in comments");
    }

    [TestMethod]
    public void ExtendsClause_ReturnsOnlyClassTypes()
    {
        var projectPath = SetupProject(("test.gd", "extends \n"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 1,
            Column = 9,
            CompletionType = GDCompletionType.Symbol,
            CursorContext = GDCursorContext.ExtendsClause
        });

        items.Should().NotBeEmpty();

        // All items should be class types
        items.Should().OnlyContain(i => i.Kind == GDCompletionItemKind.Class);

        // Should not contain primitives like void, int, float
        items.Should().NotContain(i => i.Label == "void");
        items.Should().NotContain(i => i.Label == "int");
        items.Should().NotContain(i => i.Label == "float");
    }

    [TestMethod]
    public void TypeAnnotation_ContainsAllTypes()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

var x:
"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 3,
            Column = 7,
            CompletionType = GDCompletionType.TypeAnnotation
        });

        items.Should().NotBeEmpty();

        // Should contain common types
        items.Should().Contain(i => i.Label == "int");
        items.Should().Contain(i => i.Label == "float");
        items.Should().Contain(i => i.Label == "String");
        items.Should().Contain(i => i.Label == "Vector2");
        items.Should().Contain(i => i.Label == "Node");
        items.Should().Contain(i => i.Label == "Array");
    }

    [TestMethod]
    public void Annotation_ReturnsAnnotationItems()
    {
        var projectPath = SetupProject(("test.gd", "extends Node\n"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 1,
            Column = 1,
            CompletionType = GDCompletionType.Symbol,
            CursorContext = GDCursorContext.Annotation
        });

        items.Should().NotBeEmpty();
        items.Should().Contain(i => i.Label == "@export");
        items.Should().Contain(i => i.Label == "@onready");
        items.Should().Contain(i => i.Label == "@tool");
    }

    [TestMethod]
    public void MatchPattern_ContainsTypesAndConstants()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

const MY_CONST = 5

func test(value):
    match value:

"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 7,
            Column = 9,
            CompletionType = GDCompletionType.Symbol,
            CursorContext = GDCursorContext.MatchPattern
        });

        items.Should().NotBeEmpty();

        // Should contain type names for pattern matching
        items.Should().Contain(i => i.Label == "int");
        items.Should().Contain(i => i.Label == "String");

        // Should contain constants
        items.Should().Contain(i => i.Label == "MY_CONST");

        // Should contain match keywords
        items.Should().Contain(i => i.Label == "var");
        items.Should().Contain(i => i.Label == "null");
        items.Should().Contain(i => i.Label == "true");
        items.Should().Contain(i => i.Label == "false");
    }

    [TestMethod]
    public void FuncCallArgs_ContainsVariablesAndFunctions()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

var health: int = 100

func _ready():
    var local = 5
    print()
"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 7,
            Column = 10,
            CompletionType = GDCompletionType.Symbol,
            CursorContext = GDCursorContext.FuncCallArgs
        });

        items.Should().NotBeEmpty();

        // Should contain variables that can be passed as arguments
        items.Should().Contain(i => i.Label == "health");
        items.Should().Contain(i => i.Label == "local");

        // Should contain constants
        items.Should().Contain(i => i.Label == "true");
        items.Should().Contain(i => i.Label == "false");
        items.Should().Contain(i => i.Label == "null");
    }

    #endregion

    #region Prefix filtering

    [TestMethod]
    public void WordPrefix_FiltersResults()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

var health: int = 100
var height: float = 1.8
var width: float = 0.5

func _ready():
    pass
"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 8,
            Column = 5,
            CompletionType = GDCompletionType.Symbol,
            CursorContext = GDCursorContext.MethodBody,
            WordPrefix = "he"
        });

        items.Should().NotBeEmpty();

        // Should contain items matching "he" prefix
        items.Should().Contain(i => i.Label == "health");
        items.Should().Contain(i => i.Label == "height");

        // Should not contain unrelated items
        items.Should().NotContain(i => i.Label == "width");
    }

    #endregion

    #region Sorting

    [TestMethod]
    public void MethodBody_LocalVarsHaveHigherPriority()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

var class_var: int = 1

func test():
    var local_var: int = 2

"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 7,
            Column = 5,
            CompletionType = GDCompletionType.Symbol,
            CursorContext = GDCursorContext.MethodBody
        });

        var localVar = items.FirstOrDefault(i => i.Label == "local_var");
        var classVar = items.FirstOrDefault(i => i.Label == "class_var");

        localVar.Should().NotBeNull();
        classVar.Should().NotBeNull();

        // Local vars should have higher priority (lower number) than class vars
        localVar!.SortPriority.Should().BeLessThan(classVar!.SortPriority);
    }

    #endregion

    #region Override methods

    [TestMethod]
    public void ClassLevel_ContainsOverrideMethods()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 3,
            Column = 1,
            CompletionType = GDCompletionType.Symbol,
            CursorContext = GDCursorContext.ClassLevel
        });

        items.Should().NotBeEmpty();

        // Should contain common virtual methods from Node
        items.Should().Contain(i => i.Label == "_ready" && i.IsSnippet);
        items.Should().Contain(i => i.Label == "_process" && i.IsSnippet);
    }

    #endregion

    #region Snippets

    [TestMethod]
    public void ClassLevel_ContainsSnippets()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 3,
            Column = 1,
            CompletionType = GDCompletionType.Symbol,
            CursorContext = GDCursorContext.ClassLevel
        });

        // Class-level snippets
        items.Should().Contain(i => i.Label == "func" && i.Kind == GDCompletionItemKind.Snippet);
        items.Should().Contain(i => i.Label == "ready" && i.Kind == GDCompletionItemKind.Snippet);
    }

    [TestMethod]
    public void MethodBody_ContainsSnippets()
    {
        var projectPath = SetupProject(("test.gd", @"extends Node

func _ready():

"));
        var filePath = Path.Combine(projectPath, "test.gd");

        var items = _handler!.GetCompletions(new GDCompletionRequest
        {
            FilePath = filePath,
            Line = 4,
            Column = 5,
            CompletionType = GDCompletionType.Symbol,
            CursorContext = GDCursorContext.MethodBody
        });

        // Method body snippets
        items.Should().Contain(i => i.Label == "for" && i.Kind == GDCompletionItemKind.Snippet);
        items.Should().Contain(i => i.Label == "if" && i.Kind == GDCompletionItemKind.Snippet);
        items.Should().Contain(i => i.Label == "while" && i.Kind == GDCompletionItemKind.Snippet);
    }

    #endregion
}
