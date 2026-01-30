using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests.TypeInference;

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

    #region min/max Variadic Functions (return float)

    [TestMethod]
    public void Min_WithTwoArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = min(1, 2)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "min");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "min() should return float");
    }

    [TestMethod]
    public void Min_WithThreeArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = min(1, 2, 3)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "min");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "min(a, b, c) with 3 args should return float");
    }

    [TestMethod]
    public void Min_WithFiveArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = min(1, 2, 3, 4, 5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "min");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "min() with 5 args (variadic) should return float");
    }

    [TestMethod]
    public void Max_WithTwoArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = max(10, 20)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "max");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "max() should return float");
    }

    [TestMethod]
    public void Max_WithFourArgs_ReturnsFloat()
    {
        var code = @"
func test():
    var result = max(1, 2, 3, 4)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "max");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "max(a, b, c, d) with 4 args (variadic) should return float");
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

    #region abs/absi/absf Exact 1-Arg Functions

    [TestMethod]
    public void Abs_WithOneArg_ReturnsFloat()
    {
        var code = @"
func test():
    var result = abs(-5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "abs");

        var typeName = model.GetExpressionType(callExpr);

        typeName.Should().Be("float", "abs() should return float");
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
    public void Clamp_WithThreeArgs_ReturnsVariant()
    {
        var code = @"
func test():
    var result = clamp(5, 0, 10)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "clamp");

        var typeName = model.GetExpressionType(callExpr);

        // clamp() returns Variant because it can return any type depending on input
        (typeName == "Variant" || typeName == "float" || typeName == "int").Should().BeTrue(
            $"clamp() should return Variant or numeric type, got: {typeName}");
    }

    [TestMethod]
    public void Lerp_WithThreeArgs_ReturnsVariant()
    {
        var code = @"
func test():
    var result = lerp(0.0, 10.0, 0.5)
";
        var model = CreateSemanticModel(code);
        var callExpr = FindCallExpression(model, "lerp");

        var typeName = model.GetExpressionType(callExpr);

        // lerp() returns Variant because it can interpolate various types
        (typeName == "Variant" || typeName == "float").Should().BeTrue(
            $"lerp() should return Variant or float, got: {typeName}");
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
}
