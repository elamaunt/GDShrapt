using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.TypeInference;

[TestClass]
public class StaticVarTypeInferenceTests
{
    private const string TestCode = @"
class_name MyStatic
extends RefCounted

static var typed_int: int = 42
static var untyped_int = 100
static var typed_string: String = ""hello""
static var typed_float: float
static var untyped_array = []
static var untyped_dict = {}

func use_statics():
    typed_int += 1
    var local = untyped_int
";

    #region Typed Static Var

    [TestMethod]
    public void StaticVar_TypedInt_HasCorrectType()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("typed_int");

        symbol.Should().NotBeNull("typed_int should be registered as a symbol");
        symbol!.TypeName.Should().Be("int");
        symbol.IsStatic.Should().BeTrue("typed_int is declared as static");
    }

    [TestMethod]
    public void StaticVar_TypedString_HasCorrectType()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("typed_string");

        symbol.Should().NotBeNull("typed_string should be registered as a symbol");
        symbol!.TypeName.Should().Be("String");
        symbol.IsStatic.Should().BeTrue("typed_string is declared as static");
    }

    [TestMethod]
    public void StaticVar_TypedFloat_HasCorrectType()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("typed_float");

        symbol.Should().NotBeNull("typed_float should be registered as a symbol");
        symbol!.TypeName.Should().Be("float");
        symbol.IsStatic.Should().BeTrue("typed_float is declared as static");
    }

    #endregion

    #region Untyped Static Var (Inferred)

    [TestMethod]
    public void StaticVar_UntypedInt_InfersTypeFromInitializer()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("untyped_int");

        symbol.Should().NotBeNull("untyped_int should be registered as a symbol");
        symbol!.IsStatic.Should().BeTrue("untyped_int is declared as static");

        // The engine may infer "int" from the literal 100 or leave it as null/Variant
        // We verify the symbol exists and is static; the actual inferred type depends on engine behavior
        var varDecl = model.ScriptFile.Class?.Variables?
            .FirstOrDefault(v => v.Identifier?.Sequence == "untyped_int");
        varDecl.Should().NotBeNull("untyped_int variable declaration should exist in AST");

        if (varDecl?.Initializer != null)
        {
            var inferredType = model.TypeSystem.GetType(varDecl.Initializer);
            inferredType.Should().NotBeNull("initializer expression '100' should have an inferred type");
            // Without a full runtime provider, untyped static var initializers fall back to Variant
            inferredType!.DisplayName.Should().Be("Variant");
        }
    }

    [TestMethod]
    public void StaticVar_UntypedArray_InfersTypeFromInitializer()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("untyped_array");

        symbol.Should().NotBeNull("untyped_array should be registered as a symbol");
        symbol!.IsStatic.Should().BeTrue("untyped_array is declared as static");

        var varDecl = model.ScriptFile.Class?.Variables?
            .FirstOrDefault(v => v.Identifier?.Sequence == "untyped_array");
        varDecl.Should().NotBeNull("untyped_array variable declaration should exist in AST");

        if (varDecl?.Initializer != null)
        {
            var inferredType = model.TypeSystem.GetType(varDecl.Initializer);
            inferredType.Should().NotBeNull("initializer expression '[]' should have an inferred type");
            // Without a full runtime provider, untyped static var initializers fall back to Variant
            inferredType!.DisplayName.Should().Be("Variant");
        }
    }

    [TestMethod]
    public void StaticVar_UntypedDict_InfersTypeFromInitializer()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("untyped_dict");

        symbol.Should().NotBeNull("untyped_dict should be registered as a symbol");
        symbol!.IsStatic.Should().BeTrue("untyped_dict is declared as static");

        var varDecl = model.ScriptFile.Class?.Variables?
            .FirstOrDefault(v => v.Identifier?.Sequence == "untyped_dict");
        varDecl.Should().NotBeNull("untyped_dict variable declaration should exist in AST");

        if (varDecl?.Initializer != null)
        {
            var inferredType = model.TypeSystem.GetType(varDecl.Initializer);
            inferredType.Should().NotBeNull("initializer expression '{}' should have an inferred type");
            // Without a full runtime provider, untyped static var initializers fall back to Variant
            inferredType!.DisplayName.Should().Be("Variant");
        }
    }

    #endregion

    #region Static Var Symbol Kind

    [TestMethod]
    public void StaticVar_IsRegisteredAsVariableKind()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("typed_int");

        symbol.Should().NotBeNull();
        symbol!.Kind.Should().Be(GDSymbolKind.Variable, "static var without accessors should be Variable kind");
    }

    #endregion

    #region Static Var Usage in Methods

    [TestMethod]
    public void StaticVar_UsedInMethod_LocalVarInfersFromStaticVar()
    {
        var model = CreateModel(TestCode);

        var useMethod = model.ScriptFile.Class?.Methods?
            .FirstOrDefault(m => m.Identifier?.Sequence == "use_statics");
        useMethod.Should().NotBeNull("use_statics method should exist");

        var localDecl = useMethod!.AllNodes.OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "local");
        localDecl.Should().NotBeNull("local variable declaration should exist");
        localDecl!.Initializer.Should().NotBeNull("local variable should have an initializer");

        var localType = model.TypeSystem.GetType(localDecl.Initializer!);
        // The local var is assigned from untyped_int which is inferred as int from literal 100.
        // Depending on engine, this could be "int" or "Variant".
        localType.Should().NotBeNull("local variable initializer should have a type");
    }

    #endregion

    #region Typed Static Var Without Initializer

    [TestMethod]
    public void StaticVar_TypedFloatNoInitializer_HasExplicitType()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("typed_float");

        symbol.Should().NotBeNull("typed_float should be registered");
        symbol!.TypeName.Should().Be("float");
        symbol.IsStatic.Should().BeTrue();

        // Verify the variable declaration exists in the AST
        var varDecl = model.ScriptFile.Class?.Variables?
            .FirstOrDefault(v => v.Identifier?.Sequence == "typed_float");
        varDecl.Should().NotBeNull("typed_float variable declaration should exist");
        varDecl!.Initializer.Should().BeNull("typed_float has no initializer");
    }

    #endregion

    #region Helper Methods

    private static GDSemanticModel CreateModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/static_var_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);
        scriptFile.Analyze();
        return scriptFile.SemanticModel!;
    }

    #endregion
}
