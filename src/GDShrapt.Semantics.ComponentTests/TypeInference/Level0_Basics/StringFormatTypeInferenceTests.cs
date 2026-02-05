using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.TypeInference.Level0;

/// <summary>
/// Tests for String format operator (%) type inference.
/// Verifies that "format" % anything = String (GDScript printf-style formatting).
/// </summary>
[TestClass]
public class StringFormatTypeInferenceTests
{
    #region String % Array

    [TestMethod]
    public void StringFormat_WithArray_ReturnsString()
    {
        var code = @"
extends Node
func test():
    var result = ""Hello %s, age %d"" % [""World"", 42]
";
        var model = CreateSemanticModel(code);
        var resultType = GetVariableType(model, "result");
        Assert.AreEqual("String", resultType,
            "String % Array should return String");
    }

    [TestMethod]
    public void StringFormat_WithTypedArray_ReturnsString()
    {
        var code = @"
extends Node
func test():
    var values: Array[String] = [""a"", ""b""]
    var result = ""Values: %s, %s"" % values
";
        var model = CreateSemanticModel(code);
        var resultType = GetVariableType(model, "result");
        Assert.AreEqual("String", resultType,
            "String % Array[String] should return String");
    }

    #endregion

    #region String % Primitive Types

    [TestMethod]
    public void StringFormat_WithInt_ReturnsString()
    {
        var code = @"
extends Node
func test():
    var x = 42
    var result = ""Value: %d"" % x
";
        var model = CreateSemanticModel(code);
        var resultType = GetVariableType(model, "result");
        Assert.AreEqual("String", resultType,
            "String % int should return String");
    }

    [TestMethod]
    public void StringFormat_WithIntLiteral_ReturnsString()
    {
        var code = @"
extends Node
func test():
    var result = ""negative: %d"" % -42
";
        var model = CreateSemanticModel(code);
        var resultType = GetVariableType(model, "result");
        Assert.AreEqual("String", resultType,
            "String % int literal should return String");
    }

    [TestMethod]
    public void StringFormat_WithFloat_ReturnsString()
    {
        var code = @"
extends Node
func test():
    var result = ""%.2f"" % 3.14
";
        var model = CreateSemanticModel(code);
        var resultType = GetVariableType(model, "result");
        Assert.AreEqual("String", resultType,
            "String % float should return String");
    }

    [TestMethod]
    public void StringFormat_WithString_ReturnsString()
    {
        var code = @"
extends Node
func test():
    var greeting = ""Hello, %s!"" % ""World""
";
        var model = CreateSemanticModel(code);
        var greetingType = GetVariableType(model, "greeting");
        Assert.AreEqual("String", greetingType,
            "String % String should return String");
    }

    [TestMethod]
    public void StringFormat_WithBool_ReturnsString()
    {
        var code = @"
extends Node
func test():
    var result = ""Enabled: %s"" % true
";
        var model = CreateSemanticModel(code);
        var resultType = GetVariableType(model, "result");
        Assert.AreEqual("String", resultType,
            "String % bool should return String");
    }

    #endregion

    #region String % Expression

    [TestMethod]
    public void StringFormat_WithExpression_ReturnsString()
    {
        var code = @"
extends Node
func test():
    var result = ""%3.0f"" % (100.0 / 3.0)
";
        var model = CreateSemanticModel(code);
        var resultType = GetVariableType(model, "result");
        Assert.AreEqual("String", resultType,
            "String % (float expression) should return String");
    }

    [TestMethod]
    public void StringFormat_WithMethodCall_ReturnsString()
    {
        var code = @"
extends Node
func test():
    var result = ""Length: %d"" % ""hello"".length()
";
        var model = CreateSemanticModel(code);
        var resultType = GetVariableType(model, "result");
        Assert.AreEqual("String", resultType,
            "String % method call should return String");
    }

    #endregion

    #region Chained Operations

    [TestMethod]
    public void StringFormat_ChainedWithString_ReturnsString()
    {
        var code = @"
extends Node
func test():
    var greeting = ""Hello, %s!"" % ""World""
    var result = greeting + "" Welcome!""
";
        var model = CreateSemanticModel(code);
        var greetingType = GetVariableType(model, "greeting");
        var resultType = GetVariableType(model, "result");
        Assert.AreEqual("String", greetingType,
            "String % String should return String");
        Assert.AreEqual("String", resultType,
            "String + String should return String");
    }

    [TestMethod]
    public void StringFormat_MultipleFormats_ReturnsString()
    {
        var code = @"
extends Node
func test():
    var a = ""First: %s"" % ""A""
    var b = ""Second: %d"" % 42
    var result = a + "" "" + b
";
        var model = CreateSemanticModel(code);
        var aType = GetVariableType(model, "a");
        var bType = GetVariableType(model, "b");
        var resultType = GetVariableType(model, "result");
        Assert.AreEqual("String", aType);
        Assert.AreEqual("String", bType);
        Assert.AreEqual("String", resultType);
    }

    #endregion

    #region StringName % anything

    [TestMethod]
    public void StringNameFormat_WithArray_ReturnsString()
    {
        var code = @"
extends Node
func test():
    var format: StringName = &""Value: %s""
    var result = format % [""test""]
";
        var model = CreateSemanticModel(code);
        var resultType = GetVariableType(model, "result");
        Assert.AreEqual("String", resultType,
            "StringName % Array should return String");
    }

    #endregion

    #region Helper Methods

    private static GDSemanticModel CreateSemanticModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null,
            null,
            null);
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        return collector.BuildSemanticModel();
    }

    private static string GetVariableType(GDSemanticModel model, string varName)
    {
        var varDecl = model.ScriptFile.Class?.AllNodes
            .OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == varName);

        if (varDecl?.Initializer != null)
        {
            return model.GetExpressionType(varDecl.Initializer);
        }

        return null;
    }

    #endregion
}
