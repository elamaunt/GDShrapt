using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.TypeInference;

[TestClass]
public class ConstTypeInferenceTests
{
    private const string TestCode = @"extends Node

const MAX_HP = 100
const TYPED_MAX: int = 200
const PI_VAL = 3.14159
const GREETING = ""hello""
const IS_DEBUG = true
const EMPTY_ARRAY = []
const EMPTY_DICT = {}
const NEGATIVE = -1

func use_const():
    var hp = MAX_HP
    var typed_hp = TYPED_MAX
";

    private static GDSemanticModel CreateModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/const_type_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);
        scriptFile.Analyze();
        return scriptFile.SemanticModel!;
    }

    #region Symbol-Level Type (explicit annotation only)

    [TestMethod]
    public void Const_ExplicitIntType_SymbolHasIntType()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("TYPED_MAX");

        symbol.Should().NotBeNull();
        symbol!.Kind.Should().Be(GDSymbolKind.Constant);
        symbol.TypeName.Should().Be("int");
    }

    [TestMethod]
    public void Const_UntypedIntLiteral_SymbolTypeNameIsNull()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("MAX_HP");

        symbol.Should().NotBeNull();
        symbol!.Kind.Should().Be(GDSymbolKind.Constant);
        symbol.TypeName.Should().BeNull("untyped const declarations do not store inferred type on the symbol");
    }

    [TestMethod]
    public void Const_UntypedFloatLiteral_SymbolTypeNameIsNull()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("PI_VAL");

        symbol.Should().NotBeNull();
        symbol!.Kind.Should().Be(GDSymbolKind.Constant);
        symbol.TypeName.Should().BeNull("untyped const declarations do not store inferred type on the symbol");
    }

    [TestMethod]
    public void Const_UntypedStringLiteral_SymbolTypeNameIsNull()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("GREETING");

        symbol.Should().NotBeNull();
        symbol!.Kind.Should().Be(GDSymbolKind.Constant);
        symbol.TypeName.Should().BeNull("untyped const declarations do not store inferred type on the symbol");
    }

    [TestMethod]
    public void Const_UntypedBoolLiteral_SymbolTypeNameIsNull()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("IS_DEBUG");

        symbol.Should().NotBeNull();
        symbol!.Kind.Should().Be(GDSymbolKind.Constant);
        symbol.TypeName.Should().BeNull("untyped const declarations do not store inferred type on the symbol");
    }

    [TestMethod]
    public void Const_UntypedArrayLiteral_SymbolTypeNameIsNull()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("EMPTY_ARRAY");

        symbol.Should().NotBeNull();
        symbol!.Kind.Should().Be(GDSymbolKind.Constant);
        symbol.TypeName.Should().BeNull("untyped const declarations do not store inferred type on the symbol");
    }

    [TestMethod]
    public void Const_UntypedDictLiteral_SymbolTypeNameIsNull()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("EMPTY_DICT");

        symbol.Should().NotBeNull();
        symbol!.Kind.Should().Be(GDSymbolKind.Constant);
        symbol.TypeName.Should().BeNull("untyped const declarations do not store inferred type on the symbol");
    }

    [TestMethod]
    public void Const_UntypedNegativeInt_SymbolTypeNameIsNull()
    {
        var model = CreateModel(TestCode);

        var symbol = model.FindSymbol("NEGATIVE");

        symbol.Should().NotBeNull();
        symbol!.Kind.Should().Be(GDSymbolKind.Constant);
        symbol.TypeName.Should().BeNull("untyped const declarations do not store inferred type on the symbol");
    }

    #endregion

    #region Standalone Expression Type Inference

    [TestMethod]
    public void StandaloneExpression_IntLiteral_ResolvesToVariant()
    {
        var model = CreateModel(TestCode);
        var reader = new GDScriptReader();
        var expression = reader.ParseExpression("100");

        var result = model.ResolveStandaloneExpression(expression);

        result.Should().NotBeNull();
        result.TypeName.Should().NotBeNull();
        result.TypeName.DisplayName.Should().Be("Variant",
            "standalone literal expressions without file context resolve to Variant");
    }

    [TestMethod]
    public void StandaloneExpression_FloatLiteral_ResolvesToVariant()
    {
        var model = CreateModel(TestCode);
        var reader = new GDScriptReader();
        var expression = reader.ParseExpression("3.14159");

        var result = model.ResolveStandaloneExpression(expression);

        result.Should().NotBeNull();
        result.TypeName.Should().NotBeNull();
        result.TypeName.DisplayName.Should().Be("Variant",
            "standalone literal expressions without file context resolve to Variant");
    }

    [TestMethod]
    public void StandaloneExpression_StringLiteral_ResolvesToVariant()
    {
        var model = CreateModel(TestCode);
        var reader = new GDScriptReader();
        var expression = reader.ParseExpression("\"hello\"");

        var result = model.ResolveStandaloneExpression(expression);

        result.Should().NotBeNull();
        result.TypeName.Should().NotBeNull();
        result.TypeName.DisplayName.Should().Be("Variant",
            "standalone literal expressions without file context resolve to Variant");
    }

    [TestMethod]
    public void StandaloneExpression_BoolLiteral_ResolvesToVariant()
    {
        var model = CreateModel(TestCode);
        var reader = new GDScriptReader();
        var expression = reader.ParseExpression("true");

        var result = model.ResolveStandaloneExpression(expression);

        result.Should().NotBeNull();
        result.TypeName.Should().NotBeNull();
        result.TypeName.DisplayName.Should().Be("Variant",
            "standalone literal expressions without file context resolve to Variant");
    }

    [TestMethod]
    public void StandaloneExpression_TypedConstIdentifier_ResolvesToInt()
    {
        var model = CreateModel(TestCode);
        var reader = new GDScriptReader();
        var expression = reader.ParseExpression("TYPED_MAX");

        var result = model.ResolveStandaloneExpression(expression);

        result.Should().NotBeNull();
        result.IsResolved.Should().BeTrue();
        result.TypeName.DisplayName.Should().Be("int");
    }

    [TestMethod]
    public void StandaloneExpression_UntypedConstIdentifier_ResolvesToVariant()
    {
        var model = CreateModel(TestCode);
        var reader = new GDScriptReader();
        var expression = reader.ParseExpression("MAX_HP");

        var result = model.ResolveStandaloneExpression(expression);

        result.Should().NotBeNull();
        result.TypeName.DisplayName.Should().Be("Variant",
            "untyped constants without explicit annotation resolve to Variant");
    }

    #endregion

    #region Constant Initializer Node Access

    [TestMethod]
    public void Const_IntLiteral_HasInitializerNode()
    {
        var model = CreateModel(TestCode);

        var initializer = GetConstInitializer(model, "MAX_HP");

        initializer.Should().NotBeNull();
        initializer.Should().BeOfType<GDNumberExpression>();
    }

    [TestMethod]
    public void Const_FloatLiteral_HasInitializerNode()
    {
        var model = CreateModel(TestCode);

        var initializer = GetConstInitializer(model, "PI_VAL");

        initializer.Should().NotBeNull();
        initializer.Should().BeOfType<GDNumberExpression>();
    }

    [TestMethod]
    public void Const_StringLiteral_HasInitializerNode()
    {
        var model = CreateModel(TestCode);

        var initializer = GetConstInitializer(model, "GREETING");

        initializer.Should().NotBeNull();
        initializer.Should().BeOfType<GDStringExpression>();
    }

    [TestMethod]
    public void Const_BoolLiteral_HasInitializerNode()
    {
        var model = CreateModel(TestCode);

        var initializer = GetConstInitializer(model, "IS_DEBUG");

        initializer.Should().NotBeNull();
        initializer.Should().BeOfType<GDBoolExpression>();
    }

    [TestMethod]
    public void Const_EmptyArray_HasInitializerNode()
    {
        var model = CreateModel(TestCode);

        var initializer = GetConstInitializer(model, "EMPTY_ARRAY");

        initializer.Should().NotBeNull();
        initializer.Should().BeOfType<GDArrayInitializerExpression>();
    }

    [TestMethod]
    public void Const_EmptyDictionary_HasInitializerNode()
    {
        var model = CreateModel(TestCode);

        var initializer = GetConstInitializer(model, "EMPTY_DICT");

        initializer.Should().NotBeNull();
        initializer.Should().BeOfType<GDDictionaryInitializerExpression>();
    }

    [TestMethod]
    public void Const_NegativeInt_HasInitializerNode()
    {
        var model = CreateModel(TestCode);

        var initializer = GetConstInitializer(model, "NEGATIVE");

        initializer.Should().NotBeNull();
        initializer.Should().BeOfType<GDNumberExpression>(
            "the parser folds the unary minus into the number literal for const initializers");
    }

    #endregion

    #region Constants Used in Expressions

    [TestMethod]
    public void Variable_InitializedFromTypedConst_InfersIntType()
    {
        var model = CreateModel(TestCode);

        var method = model.ScriptFile.Class?.Methods?
            .FirstOrDefault(m => m.Identifier?.Sequence == "use_const");
        method.Should().NotBeNull();

        var typedHpDecl = method!.AllNodes.OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "typed_hp");
        typedHpDecl.Should().NotBeNull();
        typedHpDecl!.Initializer.Should().NotBeNull();

        var type = model.TypeSystem.GetType(typedHpDecl.Initializer!);

        type.Should().NotBeNull();
        type.DisplayName.Should().Be("int");
    }

    [TestMethod]
    public void Variable_InitializedFromUntypedConst_ResolvesToVariant()
    {
        var model = CreateModel(TestCode);

        var method = model.ScriptFile.Class?.Methods?
            .FirstOrDefault(m => m.Identifier?.Sequence == "use_const");
        method.Should().NotBeNull();

        var hpDecl = method!.AllNodes.OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "hp");
        hpDecl.Should().NotBeNull();
        hpDecl!.Initializer.Should().NotBeNull();

        var type = model.TypeSystem.GetType(hpDecl.Initializer!);

        type.Should().NotBeNull();
        type.DisplayName.Should().Be("Variant",
            "untyped constants do not propagate inferred type to consuming expressions");
    }

    #endregion

    #region GetConstants Enumeration

    [TestMethod]
    public void GetConstants_ReturnsAllConstants()
    {
        var model = CreateModel(TestCode);

        var constants = model.GetConstants().ToList();

        constants.Should().HaveCountGreaterThanOrEqualTo(8);
    }

    [TestMethod]
    public void GetConstants_AllHaveConstantKind()
    {
        var model = CreateModel(TestCode);

        var constants = model.GetConstants().ToList();

        constants.Should().OnlyContain(c => c.Kind == GDSymbolKind.Constant);
    }

    [TestMethod]
    public void GetConstants_ContainsExpectedNames()
    {
        var model = CreateModel(TestCode);

        var constantNames = model.GetConstants().Select(c => c.Name).ToList();

        constantNames.Should().Contain("MAX_HP");
        constantNames.Should().Contain("TYPED_MAX");
        constantNames.Should().Contain("PI_VAL");
        constantNames.Should().Contain("GREETING");
        constantNames.Should().Contain("IS_DEBUG");
        constantNames.Should().Contain("EMPTY_ARRAY");
        constantNames.Should().Contain("EMPTY_DICT");
        constantNames.Should().Contain("NEGATIVE");
    }

    #endregion

    #region Helpers

    private static GDExpression? GetConstInitializer(GDSemanticModel model, string constName)
    {
        var symbol = model.FindSymbol(constName);
        if (symbol?.DeclarationNode is GDVariableDeclaration varDecl)
            return varDecl.Initializer;
        return null;
    }

    #endregion
}
