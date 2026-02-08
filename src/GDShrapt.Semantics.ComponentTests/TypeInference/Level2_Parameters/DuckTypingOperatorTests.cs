using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.TypeInference.Level2;

/// <summary>
/// Tests for duck typing operator collection.
/// Verifies that operators used on untyped variables are recorded as requirements.
/// </summary>
[TestClass]
public class DuckTypingOperatorTests
{
    #region Addition Operator Collection

    [TestMethod]
    public void DuckType_Addition_CollectsRequiredOperator()
    {
        var code = @"
func process(a, b):
    var result = a + b
";
        var (_, semanticModel) = AnalyzeCode(code);

        var duckTypeA = semanticModel?.GetDuckType("a");
        var duckTypeB = semanticModel?.GetDuckType("b");

        Assert.IsNotNull(duckTypeA, "Variable 'a' should have duck type requirements");
        Assert.IsTrue(duckTypeA.RequiredOperators.ContainsKey(GDDualOperatorType.Addition),
            "Variable 'a' should require Addition operator");

        Assert.IsNotNull(duckTypeB, "Variable 'b' should have duck type requirements");
        Assert.IsTrue(duckTypeB.RequiredOperators.ContainsKey(GDDualOperatorType.Addition),
            "Variable 'b' should require Addition operator");
    }

    [TestMethod]
    public void DuckType_AdditionWithInt_CollectsOperandType()
    {
        var code = @"
func process(a):
    var result = a + 5
";
        var (_, semanticModel) = AnalyzeCode(code);

        var duckType = semanticModel?.GetDuckType("a");
        Assert.IsNotNull(duckType, "Variable 'a' should have duck type requirements");
        Assert.IsTrue(duckType.RequiredOperators.ContainsKey(GDDualOperatorType.Addition),
            "Variable 'a' should require Addition operator");

        var operandTypes = duckType.RequiredOperators[GDDualOperatorType.Addition];
        Assert.IsTrue(operandTypes.Any(t => t.DisplayName == "int"),
            "Addition operand type should include 'int'");
    }

    [TestMethod]
    public void DuckType_AdditionWithString_CollectsOperandType()
    {
        var code = @"
func process(a):
    var result = a + ""hello""
";
        var (_, semanticModel) = AnalyzeCode(code);

        var duckType = semanticModel?.GetDuckType("a");
        Assert.IsNotNull(duckType, "Variable 'a' should have duck type requirements");
        Assert.IsTrue(duckType.RequiredOperators.ContainsKey(GDDualOperatorType.Addition),
            "Variable 'a' should require Addition operator");

        var operandTypes = duckType.RequiredOperators[GDDualOperatorType.Addition];
        Assert.IsTrue(operandTypes.Any(t => t.DisplayName == "String"),
            "Addition operand type should include 'String'");
    }

    #endregion

    #region Multiple Operators

    [TestMethod]
    public void DuckType_MultipleOperators_CollectsAll()
    {
        var code = @"
func process(a):
    var x = a + 1
    var y = a - 1
    var z = a * 2
";
        var (_, semanticModel) = AnalyzeCode(code);

        var duckType = semanticModel?.GetDuckType("a");
        Assert.IsNotNull(duckType, "Variable 'a' should have duck type requirements");

        Assert.IsTrue(duckType.RequiredOperators.ContainsKey(GDDualOperatorType.Addition),
            "Variable 'a' should require Addition operator");
        Assert.IsTrue(duckType.RequiredOperators.ContainsKey(GDDualOperatorType.Subtraction),
            "Variable 'a' should require Subtraction operator");
        Assert.IsTrue(duckType.RequiredOperators.ContainsKey(GDDualOperatorType.Multiply),
            "Variable 'a' should require Multiply operator");
    }

    [TestMethod]
    public void DuckType_DivisionAndMod_CollectsAll()
    {
        var code = @"
func process(value):
    var div_result = value / 2
    var mod_result = value % 3
";
        var (_, semanticModel) = AnalyzeCode(code);

        var duckType = semanticModel?.GetDuckType("value");
        Assert.IsNotNull(duckType, "Variable 'value' should have duck type requirements");

        Assert.IsTrue(duckType.RequiredOperators.ContainsKey(GDDualOperatorType.Division),
            "Variable 'value' should require Division operator");
        Assert.IsTrue(duckType.RequiredOperators.ContainsKey(GDDualOperatorType.Mod),
            "Variable 'value' should require Mod operator");
    }

    #endregion

    #region Typed Variables

    [TestMethod]
    public void DuckType_TypedVariable_HasKnownType()
    {
        // For typed variables, the declared type takes precedence over duck type inference.
        // Duck types may still be collected but the semantic model knows the actual type.
        var code = @"
func process(a: int, b: int):
    var result = a + b
";
        var (_, semanticModel) = AnalyzeCode(code);

        // The semantic model should know the declared types
        var typeA = semanticModel?.GetEffectiveType("a");
        var typeB = semanticModel?.GetEffectiveType("b");

        Assert.AreEqual("int", typeA, "Variable 'a' should have declared type 'int'");
        Assert.AreEqual("int", typeB, "Variable 'b' should have declared type 'int'");
    }

    #endregion

    #region Comparison Operators - Not Collected

    [TestMethod]
    public void DuckType_ComparisonOperators_NotCollected()
    {
        var code = @"
func process(a, b):
    if a > b:
        return true
    return false
";
        var (_, semanticModel) = AnalyzeCode(code);

        var duckTypeA = semanticModel?.GetDuckType("a");

        // Comparison operators are not arithmetic, should not be collected
        if (duckTypeA != null)
        {
            Assert.IsFalse(duckTypeA.RequiredOperators.ContainsKey(GDDualOperatorType.MoreThan),
                "Comparison operators should not be collected");
        }
    }

    #endregion

    #region DuckType Resolution

    [TestMethod]
    public void DuckTypeResolver_AdditionWithInt_FindsNumericTypes()
    {
        var runtimeProvider = new GDGodotTypesProvider();
        var resolver = new GDDuckTypeResolver(runtimeProvider);

        var duckType = new GDDuckType();
        duckType.RequireOperator(GDDualOperatorType.Addition, GDSemanticType.FromRuntimeTypeName("int"));

        var compatibleTypes = resolver.FindCompatibleTypes(duckType).ToList();

        Assert.IsTrue(compatibleTypes.Contains("int"),
            "int should support Addition with int");
        Assert.IsTrue(compatibleTypes.Contains("float"),
            "float should support Addition with int");
        Assert.IsTrue(compatibleTypes.Contains("String"),
            "String should support Addition");
    }

    [TestMethod]
    public void DuckTypeResolver_SubtractionWithInt_FindsNumericTypes()
    {
        var runtimeProvider = new GDGodotTypesProvider();
        var resolver = new GDDuckTypeResolver(runtimeProvider);

        var duckType = new GDDuckType();
        duckType.RequireOperator(GDDualOperatorType.Subtraction, GDSemanticType.FromRuntimeTypeName("int"));

        var compatibleTypes = resolver.FindCompatibleTypes(duckType).ToList();

        Assert.IsTrue(compatibleTypes.Contains("int"),
            "int should support Subtraction");
        Assert.IsTrue(compatibleTypes.Contains("float"),
            "float should support Subtraction");
        Assert.IsTrue(compatibleTypes.Contains("Vector2"),
            "Vector2 should support Subtraction");
        Assert.IsFalse(compatibleTypes.Contains("String"),
            "String should NOT support Subtraction");
    }

    #endregion

    #region Helper Methods

    private static (GDClassDeclaration?, GDSemanticModel?) AnalyzeCode(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return (null, null);

        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null,
            null,
            null);

        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        return (classDecl, semanticModel);
    }

    #endregion
}
