using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.TypeInference;

/// <summary>
/// Tests for the unified GetTypeDiffForNode API.
/// </summary>
[TestClass]
public class TypeDiffTests
{
    private GDScriptReader _reader = null!;

    [TestInitialize]
    public void Setup()
    {
        _reader = new GDScriptReader();
    }

    #region Parameter Tests

    [TestMethod]
    public void GetTypeDiffForNode_Parameter_WithTypeAnnotation()
    {
        var code = @"
func process(data: Dictionary) -> void:
    print(data)
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var method = scriptFile.Class!.AllNodes.OfType<GDMethodDeclaration>().First();
        var param = method.Parameters!.First();

        var diff = model.GetTypeDiffForNode(param);

        Assert.IsNotNull(diff);
        Assert.AreEqual("data", diff.SymbolName);
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("Dictionary"));
        Assert.IsTrue(diff.HasExpectedTypes);
    }

    [TestMethod]
    public void GetTypeDiffForNode_Parameter_WithTypeGuard()
    {
        var code = @"
func process(value):
    if value is Player:
        print(value.name)
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var method = scriptFile.Class!.AllNodes.OfType<GDMethodDeclaration>().First();
        var param = method.Parameters!.First();

        var diff = model.GetTypeDiffForNode(param);

        Assert.IsNotNull(diff);
        Assert.AreEqual("value", diff.SymbolName);
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("Player"));
    }

    [TestMethod]
    public void GetTypeDiffForNode_Parameter_WithNullCheck()
    {
        var code = @"
func process(item):
    if item == null:
        return
    print(item)
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var method = scriptFile.Class!.AllNodes.OfType<GDMethodDeclaration>().First();
        var param = method.Parameters!.First();

        var diff = model.GetTypeDiffForNode(param);

        Assert.IsNotNull(diff);
        Assert.AreEqual("item", diff.SymbolName);
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("null"));
    }

    [TestMethod]
    public void GetTypeDiffForNode_Parameter_WithMatchPattern()
    {
        var code = @"
func process(value):
    match value:
        1, 2, 3:
            print(""number"")
        ""hello"":
            print(""string"")
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var method = scriptFile.Class!.AllNodes.OfType<GDMethodDeclaration>().First();
        var param = method.Parameters!.First();

        var diff = model.GetTypeDiffForNode(param);

        Assert.IsNotNull(diff);
        Assert.AreEqual("value", diff.SymbolName);
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("int"));
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("String"));
    }

    [TestMethod]
    public void GetTypeDiffForNode_Parameter_WithTypeofCheck()
    {
        var code = @"
func process(data):
    if typeof(data) == TYPE_INT:
        print(data * 2)
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var method = scriptFile.Class!.AllNodes.OfType<GDMethodDeclaration>().First();
        var param = method.Parameters!.First();

        var diff = model.GetTypeDiffForNode(param);

        Assert.IsNotNull(diff);
        Assert.AreEqual("data", diff.SymbolName);
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("int"));
    }

    [TestMethod]
    public void GetTypeDiffForNode_Parameter_WithAssertIs()
    {
        var code = @"
func process(entity):
    assert(entity is Node2D)
    entity.position = Vector2.ZERO
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var method = scriptFile.Class!.AllNodes.OfType<GDMethodDeclaration>().First();
        var param = method.Parameters!.First();

        var diff = model.GetTypeDiffForNode(param);

        Assert.IsNotNull(diff);
        Assert.AreEqual("entity", diff.SymbolName);
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("Node2D"));
    }

    #endregion

    #region Variable Tests

    [TestMethod]
    public void GetTypeDiffForNode_Variable_WithTypeAnnotation()
    {
        var code = @"
var score: int = 0
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var variable = scriptFile.Class!.AllNodes.OfType<GDVariableDeclaration>().First();

        var diff = model.GetTypeDiffForNode(variable);

        Assert.IsNotNull(diff);
        Assert.AreEqual("score", diff.SymbolName);
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("int"));
        Assert.IsTrue(diff.ActualTypes.Types.Contains("int"));
    }

    [TestMethod]
    public void GetTypeDiffForNode_Variable_WithInitializer()
    {
        var code = @"
var name = ""Player""
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var variable = scriptFile.Class!.AllNodes.OfType<GDVariableDeclaration>().First();

        var diff = model.GetTypeDiffForNode(variable);

        Assert.IsNotNull(diff);
        Assert.AreEqual("name", diff.SymbolName);
        Assert.IsTrue(diff.ActualTypes.Types.Contains("String"));
    }

    [TestMethod]
    public void GetTypeDiffForNode_LocalVariable_WithAssignment()
    {
        var code = @"
func test():
    var value = 10
    value = 20
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var method = scriptFile.Class!.AllNodes.OfType<GDMethodDeclaration>().First();
        var localVar = method.AllNodes.OfType<GDVariableDeclarationStatement>().First();

        var diff = model.GetTypeDiffForNode(localVar);

        Assert.IsNotNull(diff);
        Assert.AreEqual("value", diff.SymbolName);
        Assert.IsTrue(diff.ActualTypes.Types.Contains("int"));
    }

    #endregion

    #region Expression Tests

    [TestMethod]
    public void GetTypeDiffForNode_Identifier_ResolvesToDeclaration()
    {
        var code = @"
func test():
    var count: int = 0
    print(count)
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        // Find the identifier expression for 'count' in the print call
        var identExpr = scriptFile.Class!.AllNodes
            .OfType<GDIdentifierExpression>()
            .FirstOrDefault(x => x.Identifier?.Sequence == "count");

        Assert.IsNotNull(identExpr);

        var diff = model.GetTypeDiffForNode(identExpr);

        Assert.IsNotNull(diff);
        Assert.AreEqual("count", diff.SymbolName);
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("int"));
    }

    [TestMethod]
    public void GetTypeDiffForNode_MemberAccess_ResolvesType()
    {
        var code = @"
func test():
    var node: Node2D = Node2D.new()
    var pos = node.position
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var memberAccess = scriptFile.Class!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .FirstOrDefault(x => x.Identifier?.Sequence == "position");

        Assert.IsNotNull(memberAccess);

        var diff = model.GetTypeDiffForNode(memberAccess);

        Assert.IsNotNull(diff);
        Assert.AreEqual("position", diff.SymbolName);
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("Vector2"));
    }

    [TestMethod]
    public void GetTypeDiffForNode_MethodCall_ResolvesReturnType()
    {
        var code = @"
func test():
    var node = Node2D.new()
    var children = node.get_children()
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var callExpr = scriptFile.Class!.AllNodes
            .OfType<GDCallExpression>()
            .FirstOrDefault(c =>
                c.CallerExpression is GDMemberOperatorExpression m &&
                m.Identifier?.Sequence == "get_children");

        Assert.IsNotNull(callExpr);

        var diff = model.GetTypeDiffForNode(callExpr);

        Assert.IsNotNull(diff);
    }

    #endregion

    #region Method Tests

    [TestMethod]
    public void GetTypeDiffForNode_Method_WithReturnTypeAnnotation()
    {
        var code = @"
func calculate() -> int:
    return 42
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var method = scriptFile.Class!.AllNodes.OfType<GDMethodDeclaration>().First();

        var diff = model.GetTypeDiffForNode(method);

        Assert.IsNotNull(diff);
        Assert.AreEqual("calculate", diff.SymbolName);
        Assert.IsTrue(diff.ExpectedTypes.Types.Contains("int"));
        Assert.IsTrue(diff.ActualTypes.Types.Contains("int"));
        Assert.IsFalse(diff.HasMismatch);
    }

    [TestMethod]
    public void GetTypeDiffForNode_Method_InfersReturnType()
    {
        var code = @"
func get_name():
    return ""Player""
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var method = scriptFile.Class!.AllNodes.OfType<GDMethodDeclaration>().First();

        var diff = model.GetTypeDiffForNode(method);

        Assert.IsNotNull(diff);
        Assert.AreEqual("get_name", diff.SymbolName);
        Assert.IsTrue(diff.ActualTypes.Types.Contains("String"));
    }

    [TestMethod]
    public void GetTypeDiffForNode_Method_MultipleReturnTypes()
    {
        var code = @"
func get_value(flag: bool):
    if flag:
        return 42
    return ""none""
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var method = scriptFile.Class!.AllNodes.OfType<GDMethodDeclaration>().First();

        var diff = model.GetTypeDiffForNode(method);

        Assert.IsNotNull(diff);
        Assert.AreEqual("get_value", diff.SymbolName);
        Assert.IsTrue(diff.ActualTypes.Types.Contains("int"));
        Assert.IsTrue(diff.ActualTypes.Types.Contains("String"));
    }

    #endregion

    #region Duck Typing Tests

    [TestMethod]
    public void GetTypeDiffForNode_DuckTyping_MethodUsage()
    {
        var code = @"
func process(obj):
    obj.start()
    obj.update()
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var method = scriptFile.Class!.AllNodes.OfType<GDMethodDeclaration>().First();
        var param = method.Parameters!.First();

        var diff = model.GetTypeDiffForNode(param);

        Assert.IsNotNull(diff);
        Assert.AreEqual("obj", diff.SymbolName);
        // Duck constraints should include methods 'start' and 'update'
        // Note: This depends on the duck type collector being run
    }

    #endregion

    #region Confidence Tests

    [TestMethod]
    public void GetTypeDiffForNode_HighConfidence_WithTypeAnnotation()
    {
        var code = @"
func process(data: int) -> void:
    pass
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var method = scriptFile.Class!.AllNodes.OfType<GDMethodDeclaration>().First();
        var param = method.Parameters!.First();

        var diff = model.GetTypeDiffForNode(param);

        Assert.IsNotNull(diff);
        Assert.AreEqual(GDTypeConfidence.High, diff.Confidence);
    }

    [TestMethod]
    public void GetTypeDiffForNode_UnknownConfidence_NoTypeInfo()
    {
        var code = @"
func process(data):
    pass
";
        var scriptFile = CreateScriptFile(code);
        var model = GDSemanticModel.Create(scriptFile, new GDGodotTypesProvider());

        var method = scriptFile.Class!.AllNodes.OfType<GDMethodDeclaration>().First();
        var param = method.Parameters!.First();

        var diff = model.GetTypeDiffForNode(param);

        Assert.IsNotNull(diff);
        // Without any type info, should be Unknown
        Assert.AreEqual(GDTypeConfidence.Unknown, diff.Confidence);
        Assert.AreEqual("Variant", diff.TypeName);
    }

    #endregion

    #region Helper Methods

    private GDScriptFile CreateScriptFile(string code)
    {
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);
        return scriptFile;
    }

    #endregion
}
