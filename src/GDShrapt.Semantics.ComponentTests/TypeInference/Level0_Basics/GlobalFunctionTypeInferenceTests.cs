using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.TypeInference;

/// <summary>
/// Tests for global function type inference through GDSemanticModel.
/// Verifies that return types of built-in functions (min, max, mini, maxi, etc.)
/// are correctly inferred.
/// </summary>
[TestClass]
public class GlobalFunctionTypeInferenceTests
{
    private static GDSemanticModel CreateSemanticModel(string code)
    {
        var reference = new GDScriptReference("test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDGodotTypesProvider();
        return GDSemanticModel.Create(scriptFile, runtimeProvider);
    }

    private static GDCallExpression FindCallExpression(GDSemanticModel model, string functionName)
    {
        return model.ScriptFile.Class!.AllNodes
            .OfType<GDCallExpression>()
            .First(c => c.CallerExpression is GDIdentifierExpression id &&
                       id.Identifier?.Sequence == functionName);
    }

    #region min/max Smart Type Inference (returns common type of args)

    [TestMethod]
    public void Min_WithIntArgs_ReturnsInt()
    {
        var code = @"
func test():
    var result = min(1, 2)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "min");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "min(int, int) should return int");
    }

    [TestMethod]
    public void Min_WithFloatArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = min(1.5, 2.5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "min");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "min(float, float) should return float");
    }

    [TestMethod]
    public void Min_WithMixedArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = min(1, 2.5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "min");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "min(int, float) should return float (numeric promotion)");
    }

    [TestMethod]
    public void Min_WithThreeIntArgs_ReturnsInt()
    {
        var code = @"
func test():
    var result = min(1, 2, 3)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "min");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "min(int, int, int) with 3 args should return int");
    }

    [TestMethod]
    public void Min_WithFiveIntArgs_ReturnsInt()
    {
        var code = @"
func test():
    var result = min(1, 2, 3, 4, 5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "min");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "min() with 5 int args (variadic) should return int");
    }

    [TestMethod]
    public void Max_WithIntArgs_ReturnsInt()
    {
        var code = @"
func test():
    var result = max(10, 20)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "max");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "max(int, int) should return int");
    }

    [TestMethod]
    public void Max_WithFloatArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = max(10.5, 20.5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "max");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "max(float, float) should return float");
    }

    [TestMethod]
    public void Max_WithMixedArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = max(10, 20.5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "max");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "max(int, float) should return float (numeric promotion)");
    }

    [TestMethod]
    public void Max_WithFourIntArgs_ReturnsInt()
    {
        var code = @"
func test():
    var result = max(1, 2, 3, 4)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "max");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "max(int, int, int, int) with 4 args (variadic) should return int");
    }

    [TestMethod]
    public void Max_WithThreeIntArgs_ReturnsInt()
    {
        var code = @"
func test():
    var result = max(1, 2, 3)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "max");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "max(int, int, int) with 3 args should return int");
    }

    [TestMethod]
    public void Min_WithMixedArgsVariadic_ReturnsFloat()
    {
        var code = @"
func test():
    var result = min(1, 2.0, 3)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "min");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "min(int, float, int) should return float (numeric promotion)");
    }

    [TestMethod]
    public void Max_WithMixedArgsVariadic_ReturnsFloat()
    {
        var code = @"
func test():
    var result = max(1.0, 2, 3.0, 4)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "max");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "max(float, int, float, int) should return float");
    }

    [TestMethod]
    public void Min_WithVector2Args_ReturnsVector2()
    {
        var code = @"
func test():
    var v1 := Vector2(1, 2)
    var v2 := Vector2(3, 4)
    var result = min(v1, v2)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "min");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("Vector2", "min(Vector2, Vector2) should return Vector2");
    }

    [TestMethod]
    public void Max_WithVector3Args_ReturnsVector3()
    {
        var code = @"
func test():
    var v1 := Vector3(1, 2, 3)
    var v2 := Vector3(4, 5, 6)
    var result = max(v1, v2)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "max");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("Vector3", "max(Vector3, Vector3) should return Vector3");
    }

    [TestMethod]
    public void Min_WithIncompatibleTypes_ReturnsVariant()
    {
        var code = @"
func test():
    var v := Vector2(1, 2)
    var result = min(v, 5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "min");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("Variant", "min(Vector2, int) has incompatible types → Variant");
    }

    [TestMethod]
    public void Min_WithDifferentVectorTypes_ReturnsVariant()
    {
        var code = @"
func test():
    var v2 := Vector2(1, 2)
    var v3 := Vector3(1, 2, 3)
    var result = min(v2, v3)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "min");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("Variant", "min(Vector2, Vector3) has different vector dims → Variant");
    }

    #endregion

    #region mini/maxi Variadic Functions (return int)

    [TestMethod]
    public void Mini_WithTwoArgs_ReturnsInt()
    {
        var code = @"
func test():
    var result = mini(1, 2)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "mini");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "mini() should return int");
    }

    [TestMethod]
    public void Mini_WithThreeArgs_ReturnsInt()
    {
        var code = @"
func test():
    var result = mini(10, 20, 5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "mini");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "mini(a, b, c) with 3 args (variadic) should return int");
    }

    [TestMethod]
    public void Maxi_WithTwoArgs_ReturnsInt()
    {
        var code = @"
func test():
    var result = maxi(1, 2)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "maxi");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "maxi() should return int");
    }

    [TestMethod]
    public void Maxi_WithFourArgs_ReturnsInt()
    {
        var code = @"
func test():
    var result = maxi(1, 2, 3, 4)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "maxi");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "maxi(a, b, c, d) with 4 args (variadic) should return int");
    }

    #endregion

    #region minf/maxf Exact 2-Arg Functions (return float)

    [TestMethod]
    public void Minf_WithTwoArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = minf(1.5, 2.5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "minf");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "minf() should return float");
    }

    [TestMethod]
    public void Maxf_WithTwoArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = maxf(1.5, 2.5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "maxf");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "maxf() should return float");
    }

    #endregion

    #region clampi/clampf Exact 3-Arg Functions

    [TestMethod]
    public void Clampi_WithThreeArgs_ReturnsInt()
    {
        var code = @"
func test():
    var result = clampi(5, 0, 10)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "clampi");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "clampi() should return int");
    }

    [TestMethod]
    public void Clampf_WithThreeArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = clampf(5.5, 0.0, 10.0)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "clampf");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "clampf() should return float");
    }

    #endregion

    #region abs/absi/absf Smart Type Inference

    [TestMethod]
    public void Abs_WithIntArg_ReturnsInt()
    {
        var code = @"
func test():
    var result = abs(-5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "abs");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "abs(int) should return int");
    }

    [TestMethod]
    public void Abs_WithFloatArg_ReturnsFloat()
    {
        var code = @"
func test():
    var result = abs(-5.5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "abs");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "abs(float) should return float");
    }

    [TestMethod]
    public void Absi_WithOneArg_ReturnsInt()
    {
        var code = @"
func test():
    var result = absi(-5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "absi");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "absi() should return int");
    }

    [TestMethod]
    public void Absf_WithOneArg_ReturnsFloat()
    {
        var code = @"
func test():
    var result = absf(-5.5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "absf");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "absf() should return float");
    }

    #endregion

    #region Other Global Functions

    [TestMethod]
    public void Clamp_WithIntArgs_ReturnsInt()
    {
        var code = @"
func test():
    var result = clamp(5, 0, 10)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "clamp");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("int", "clamp(int, int, int) should return int (first arg type)");
    }

    [TestMethod]
    public void Clamp_WithFloatArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = clamp(5.5, 0.0, 10.0)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "clamp");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "clamp(float, float, float) should return float (first arg type)");
    }

    [TestMethod]
    public void Lerp_WithFloatArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = lerp(0.0, 10.0, 0.5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "lerp");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "lerp(float, float, float) should return float (common type of a, b)");
    }

    [TestMethod]
    public void Lerp_WithIntArgs_ReturnsInt()
    {
        var code = @"
func test():
    var result = lerp(0, 10, 0.5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "lerp");

        var typeName = model.GetExpressionType(callExpr);

        // lerp(int, int, float) - common type of first two args is int
        typeName.Should().Be("int", "lerp(int, int, float) should return int (common type of a, b)");
    }

    [TestMethod]
    public void Range_WithOneArg_ReturnsArray()
    {
        var code = @"
func test():
    var result = range(10)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "range");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("Array", "range() should return Array");
    }

    [TestMethod]
    public void Range_WithThreeArgs_ReturnsArray()
    {
        var code = @"
func test():
    var result = range(0, 10, 2)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "range");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("Array", "range(start, end, step) should return Array");
    }

    #endregion

    #region Typed Variable with Null Initializer Tests

    [TestMethod]
    public void TypedClassVariable_WithNullInitializer_ReturnsExplicitType()
    {
        // This tests the GD7002 false positive scenario via SemanticModel
        var code = @"
extends Node2D

var target_entity: Node2D = null

func test():
    if is_instance_valid(target_entity):
        var pos = target_entity.position
";
        var model = CreateSemanticModel(code);

        // Find the identifier expression 'target_entity' used inside the if block (for .position)
        var memberAccess = model.ScriptFile.Class!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .First(m => m.Identifier?.Sequence == "position");

        var callerExpr = memberAccess.CallerExpression;
        var typeName = model.GetExpressionType(callerExpr);

        typeName.Should().Be("Node2D", "typed variable with null initializer should return explicit type Node2D");
    }

    [TestMethod]
    public void TypedClassVariable_GetMemberAccessConfidence_ReturnsStrict()
    {
        // This tests that confidence is Strict for typed variables
        var code = @"
extends Node2D

var target_entity: Node2D = null

func test():
    if is_instance_valid(target_entity):
        var pos = target_entity.position
";
        var model = CreateSemanticModel(code);

        // Find the member access for .position
        var memberAccess = model.ScriptFile.Class!.AllNodes
            .OfType<GDMemberOperatorExpression>()
            .First(m => m.Identifier?.Sequence == "position");

        var confidence = model.GetMemberAccessConfidence(memberAccess);

        confidence.Should().Be(GDReferenceConfidence.Strict,
            "typed variable should have Strict confidence, not NameMatch which causes GD7002");
    }

    #endregion
}
