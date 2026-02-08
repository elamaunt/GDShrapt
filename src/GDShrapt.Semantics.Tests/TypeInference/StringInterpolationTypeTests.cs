using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.TypeInference;

/// <summary>
/// Tests that the GDScript string format operator (%) is correctly inferred as String.
/// In GDScript 2.0, string interpolation uses the % operator:
///   "text %s" % value
///   "text %s %d" % [val1, val2]
/// The type system should always resolve String % anything to String.
/// </summary>
[TestClass]
public class GDStringInterpolationTypeTests
{
    private const string TestScript = @"
extends Node

var count: int = 42
var name: String = ""world""

func get_value() -> int:
    return 10

func test_interpolation():
    var basic = ""hello %s"" % name
    var interp = ""count: %d"" % count
    var multi = ""name=%s, count=%d"" % [name, count]
    var with_call = ""result: %d"" % get_value()
    var empty_fmt = """" % 0
    var nested_str = ""outer %s"" % (""inner %s"" % name)
    var typed: String = ""typed %s"" % name
";

    [TestMethod]
    public void BasicStringFormat_ReturnsString()
    {
        var script = ParseAndAnalyze(TestScript);
        var model = script.SemanticModel;
        model.Should().NotBeNull();

        var initializer = FindVariableInitializer(script, "test_interpolation", "basic");
        initializer.Should().NotBeNull("variable 'basic' should have an initializer");

        var type = model!.TypeSystem.GetType(initializer!);
        type.DisplayName.Should().Be("String");
    }

    [TestMethod]
    public void StringFormatWithIntExpression_ReturnsString()
    {
        var script = ParseAndAnalyze(TestScript);
        var model = script.SemanticModel;
        model.Should().NotBeNull();

        var initializer = FindVariableInitializer(script, "test_interpolation", "interp");
        initializer.Should().NotBeNull("variable 'interp' should have an initializer");

        var type = model!.TypeSystem.GetType(initializer!);
        type.DisplayName.Should().Be("String");
    }

    [TestMethod]
    public void StringFormatWithMethodCall_ReturnsString()
    {
        var script = ParseAndAnalyze(TestScript);
        var model = script.SemanticModel;
        model.Should().NotBeNull();

        var initializer = FindVariableInitializer(script, "test_interpolation", "with_call");
        initializer.Should().NotBeNull("variable 'with_call' should have an initializer");

        var type = model!.TypeSystem.GetType(initializer!);
        type.DisplayName.Should().Be("String");
    }

    [TestMethod]
    public void MultipleFormatPlaceholders_ReturnsString()
    {
        var script = ParseAndAnalyze(TestScript);
        var model = script.SemanticModel;
        model.Should().NotBeNull();

        var initializer = FindVariableInitializer(script, "test_interpolation", "multi");
        initializer.Should().NotBeNull("variable 'multi' should have an initializer");

        var type = model!.TypeSystem.GetType(initializer!);
        type.DisplayName.Should().Be("String");
    }

    [TestMethod]
    public void EmptyStringFormat_ReturnsString()
    {
        var script = ParseAndAnalyze(TestScript);
        var model = script.SemanticModel;
        model.Should().NotBeNull();

        var initializer = FindVariableInitializer(script, "test_interpolation", "empty_fmt");
        initializer.Should().NotBeNull("variable 'empty_fmt' should have an initializer");

        var type = model!.TypeSystem.GetType(initializer!);
        type.DisplayName.Should().Be("String");
    }

    [TestMethod]
    public void NestedStringFormat_ReturnsString()
    {
        var script = ParseAndAnalyze(TestScript);
        var model = script.SemanticModel;
        model.Should().NotBeNull();

        var initializer = FindVariableInitializer(script, "test_interpolation", "nested_str");
        initializer.Should().NotBeNull("variable 'nested_str' should have an initializer");

        var type = model!.TypeSystem.GetType(initializer!);
        type.DisplayName.Should().Be("String");
    }

    [TestMethod]
    public void TypedVarWithStringFormat_ReturnsString()
    {
        var script = ParseAndAnalyze(TestScript);
        var model = script.SemanticModel;
        model.Should().NotBeNull();

        var initializer = FindVariableInitializer(script, "test_interpolation", "typed");
        initializer.Should().NotBeNull("variable 'typed' should have an initializer");

        var type = model!.TypeSystem.GetType(initializer!);
        type.DisplayName.Should().Be("String");
    }

    private static GDScriptFile ParseAndAnalyze(string code)
    {
        var reference = new GDScriptReference("test://virtual/string_interpolation_test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        script.Analyze(new GDGodotTypesProvider());
        return script;
    }

    private static GDExpression? FindVariableInitializer(GDScriptFile script, string methodName, string variableName)
    {
        var method = script.Class?.Methods?.FirstOrDefault(m => m.Identifier?.Sequence == methodName);
        if (method == null)
            return null;

        var varDecl = method.AllNodes
            .OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == variableName);

        return varDecl?.Initializer;
    }
}
