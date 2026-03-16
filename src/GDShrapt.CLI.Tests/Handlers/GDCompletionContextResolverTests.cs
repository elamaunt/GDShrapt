using System.IO;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;
using GDProjectLoader = GDShrapt.Semantics.GDProjectLoader;

namespace GDShrapt.CLI.Tests.Handlers;

[TestClass]
public class GDCompletionContextResolverTests
{
    private string? _tempProjectPath;
    private GDScriptProject? _project;

    [TestCleanup]
    public void Cleanup()
    {
        _project?.Dispose();
        if (_tempProjectPath != null)
            TestProjectHelper.DeleteTempProject(_tempProjectPath);
    }

    private GDSemanticModel? SetupAndGetModel(string code)
    {
        _tempProjectPath = TestProjectHelper.CreateTempProject(("test.gd", code));
        _project = GDProjectLoader.LoadProject(_tempProjectPath);
        var filePath = Path.Combine(_tempProjectPath, "test.gd");
        return _project.GetScript(filePath)?.SemanticModel;
    }

    #region Text-based fallback tests (no AST needed)

    [TestMethod]
    public void Resolve_ExtendsKeyword_ReturnsExtendsClause()
    {
        var result = GDCompletionContextResolver.Resolve(null, 0, 0, "extends ");
        result.Should().Be(GDCursorContext.ExtendsClause);
    }

    [TestMethod]
    public void Resolve_ExtendsWithPartialType_ReturnsExtendsClause()
    {
        var result = GDCompletionContextResolver.Resolve(null, 0, 0, "extends Nod");
        result.Should().Be(GDCursorContext.ExtendsClause);
    }

    [TestMethod]
    public void Resolve_VarTypeAnnotation_ReturnsTypeAnnotation()
    {
        var result = GDCompletionContextResolver.Resolve(null, 0, 0, "var x: ");
        result.Should().Be(GDCursorContext.TypeAnnotation);
    }

    [TestMethod]
    public void Resolve_ConstTypeAnnotation_ReturnsTypeAnnotation()
    {
        var result = GDCompletionContextResolver.Resolve(null, 0, 0, "const MY_CONST: ");
        result.Should().Be(GDCursorContext.TypeAnnotation);
    }

    [TestMethod]
    public void Resolve_ReturnTypeAnnotation_ReturnsTypeAnnotation()
    {
        var result = GDCompletionContextResolver.Resolve(null, 0, 0, "func test() -> ");
        result.Should().Be(GDCursorContext.TypeAnnotation);
    }

    [TestMethod]
    public void Resolve_AnnotationAt_ReturnsAnnotation()
    {
        var result = GDCompletionContextResolver.Resolve(null, 0, 0, "@export");
        result.Should().Be(GDCursorContext.Annotation);
    }

    [TestMethod]
    public void Resolve_AnnotationAtPartial_ReturnsAnnotation()
    {
        var result = GDCompletionContextResolver.Resolve(null, 0, 0, "@on");
        result.Should().Be(GDCursorContext.Annotation);
    }

    [TestMethod]
    public void Resolve_EmptyText_ReturnsUnknown()
    {
        var result = GDCompletionContextResolver.Resolve(null, 0, 0, "");
        result.Should().Be(GDCursorContext.Unknown);
    }

    [TestMethod]
    public void Resolve_NullText_ReturnsUnknown()
    {
        var result = GDCompletionContextResolver.Resolve(null, 0, 0, null);
        result.Should().Be(GDCursorContext.Unknown);
    }

    [TestMethod]
    public void Resolve_FuncParamTypeAnnotation_ReturnsTypeAnnotation()
    {
        var result = GDCompletionContextResolver.Resolve(null, 0, 0, "func test(param: ");
        result.Should().Be(GDCursorContext.TypeAnnotation);
    }

    #endregion

    #region AST-based tests

    [TestMethod]
    public void Resolve_CursorAtTopLevel_ReturnsClassLevel()
    {
        var model = SetupAndGetModel(@"extends Node

var x: int = 5

");
        model.Should().NotBeNull();

        // Line 3 (0-based), column 0 — after the var declaration, empty line at class level
        var result = GDCompletionContextResolver.Resolve(model, 3, 0, "");
        result.Should().Be(GDCursorContext.ClassLevel);
    }

    [TestMethod]
    public void Resolve_CursorInsideMethodBody_ReturnsMethodBody()
    {
        var model = SetupAndGetModel(@"extends Node

func _ready():
    pass
");
        model.Should().NotBeNull();

        // Line 3 (0-based), column 4 — inside _ready body at "pass"
        var result = GDCompletionContextResolver.Resolve(model, 3, 4, "    pass");
        result.Should().Be(GDCursorContext.MethodBody);
    }

    [TestMethod]
    public void Resolve_CursorInExtendsClause_ReturnsExtendsClause()
    {
        var model = SetupAndGetModel(@"extends Node
");
        model.Should().NotBeNull();

        // Line 0 (0-based), column 10 — inside "Node" in "extends Node"
        var result = GDCompletionContextResolver.Resolve(model, 0, 10, "extends No");
        result.Should().Be(GDCursorContext.ExtendsClause);
    }

    [TestMethod]
    public void Resolve_CursorInEnumBody_ReturnsEnumBody()
    {
        var model = SetupAndGetModel(@"extends Node

enum Direction {
    UP,
    DOWN,
}
");
        model.Should().NotBeNull();

        // Line 3 (0-based), column 4 — inside enum body at "UP,"
        var result = GDCompletionContextResolver.Resolve(model, 3, 4, "    UP,");
        result.Should().Be(GDCursorContext.EnumBody);
    }

    #endregion
}
