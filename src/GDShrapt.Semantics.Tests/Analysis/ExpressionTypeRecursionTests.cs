using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for recursion safety in GetExpressionType and related methods.
/// These tests verify that deeply nested or circular type queries don't cause
/// StackOverflowException.
/// </summary>
[TestClass]
public class ExpressionTypeRecursionTests
{
    /// <summary>
    /// Tests that deeply chained method calls don't cause stack overflow.
    /// This simulates: a.b().c().d().e().f().g().h().i().j().k()
    /// </summary>
    [TestMethod]
    public void GetExpressionType_DeepCallChain_DoesNotStackOverflow()
    {
        // Arrange - create a deeply nested call chain
        var code = @"
extends Node

func test():
    var result = create_a().get_b().get_c().get_d().get_e().get_f().get_g().get_h().get_i().get_j()
    return result

func create_a():
    return self

func get_b():
    return self

func get_c():
    return self

func get_d():
    return self

func get_e():
    return self

func get_f():
    return self

func get_g():
    return self

func get_h():
    return self

func get_i():
    return self

func get_j():
    return self
";
        var script = ParseAndAnalyze(code);
        var model = script.SemanticModel;
        Assert.IsNotNull(model);

        // Find the chained call expression
        var testMethod = script.Class?.Methods?.FirstOrDefault(m => m.Identifier?.Sequence == "test");
        Assert.IsNotNull(testMethod, "test method not found");

        var varDecl = testMethod.AllNodes.OfType<GDVariableDeclarationStatement>().FirstOrDefault();
        Assert.IsNotNull(varDecl, "Variable declaration not found");
        Assert.IsNotNull(varDecl.Initializer, "Variable initializer not found");

        // Act - should not throw StackOverflowException
        string? resultType = null;
        var exception = Record.Exception(() =>
        {
            resultType = model.GetExpressionType(varDecl.Initializer);
        });

        // Assert
        Assert.IsNull(exception, $"GetExpressionType threw exception: {exception?.Message}");
        // Type may be null or "Node" or something else - we just care it didn't overflow
    }

    /// <summary>
    /// Tests that recursive type resolution with mutual references doesn't cause stack overflow.
    /// This creates a scenario where inferring type A requires inferring type B,
    /// which may require inferring type A again.
    /// </summary>
    [TestMethod]
    public void GetExpressionType_MutuallyRecursiveReferences_DoesNotStackOverflow()
    {
        // Arrange - create mutually recursive variable usage
        var code = @"
extends Node

var a
var b

func test():
    a = get_a_from_b()
    b = get_b_from_a()
    var result = a
    return result

func get_a_from_b():
    return b

func get_b_from_a():
    return a
";
        var script = ParseAndAnalyze(code);
        var model = script.SemanticModel;
        Assert.IsNotNull(model);

        // Find the 'result' variable
        var testMethod = script.Class?.Methods?.FirstOrDefault(m => m.Identifier?.Sequence == "test");
        Assert.IsNotNull(testMethod, "test method not found");

        var varDecl = testMethod.AllNodes.OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "result");
        Assert.IsNotNull(varDecl, "result variable not found");
        Assert.IsNotNull(varDecl.Initializer, "Variable initializer not found");

        // Act - should not throw StackOverflowException
        string? resultType = null;
        var exception = Record.Exception(() =>
        {
            resultType = model.GetExpressionType(varDecl.Initializer);
        });

        // Assert
        Assert.IsNull(exception, $"GetExpressionType threw exception: {exception?.Message}");
    }

    /// <summary>
    /// Tests that deeply nested ternary expressions don't cause stack overflow.
    /// </summary>
    [TestMethod]
    public void GetExpressionType_DeeplyNestedTernary_DoesNotStackOverflow()
    {
        // Arrange - create nested ternary expressions
        var code = @"
extends Node

func test(a: bool, b: bool, c: bool, d: bool, e: bool):
    var result = 1 if a else (2 if b else (3 if c else (4 if d else (5 if e else 6))))
    return result
";
        var script = ParseAndAnalyze(code);
        var model = script.SemanticModel;
        Assert.IsNotNull(model);

        var testMethod = script.Class?.Methods?.FirstOrDefault(m => m.Identifier?.Sequence == "test");
        Assert.IsNotNull(testMethod, "test method not found");

        var varDecl = testMethod.AllNodes.OfType<GDVariableDeclarationStatement>().FirstOrDefault();
        Assert.IsNotNull(varDecl, "Variable declaration not found");
        Assert.IsNotNull(varDecl.Initializer, "Variable initializer not found");

        // Act - should not throw StackOverflowException
        string? resultType = null;
        var exception = Record.Exception(() =>
        {
            resultType = model.GetExpressionType(varDecl.Initializer);
        });

        // Assert
        Assert.IsNull(exception, $"GetExpressionType threw exception: {exception?.Message}");
        // Note: Ternary type inference returns null in some cases due to complex nesting
        // The main goal of this test is to ensure no StackOverflowException occurs
        // Type correctness is covered by other tests
    }

    /// <summary>
    /// Tests that FlowAnalyzer callback doesn't cause infinite recursion with SemanticModel.
    /// This is the specific scenario described in the audit: GetExpressionType ->
    /// GetOrCreateFlowAnalyzer -> callback -> GetExpressionTypeWithoutFlow -> GetExpressionType
    /// </summary>
    [TestMethod]
    public void GetExpressionType_FlowAnalyzerCallback_DoesNotCauseInfiniteRecursion()
    {
        // Arrange - create a scenario that would trigger flow analysis
        var code = @"
extends Node

func test():
    var x = 10
    x = ""hello""
    var y = x  # Flow analyzer needed to determine x is String here
    return y
";
        var script = ParseAndAnalyze(code);
        var model = script.SemanticModel;
        Assert.IsNotNull(model);

        var testMethod = script.Class?.Methods?.FirstOrDefault(m => m.Identifier?.Sequence == "test");
        Assert.IsNotNull(testMethod, "test method not found");

        // Find 'y' variable and its initializer
        var yDecl = testMethod.AllNodes.OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "y");
        Assert.IsNotNull(yDecl, "y variable not found");
        Assert.IsNotNull(yDecl.Initializer, "y initializer not found");

        // Act - should not throw StackOverflowException or get stuck in infinite recursion
        string? resultType = null;
        var exception = Record.Exception(() =>
        {
            resultType = model.GetExpressionType(yDecl.Initializer);
        });

        // Assert
        Assert.IsNull(exception, $"GetExpressionType threw exception: {exception?.Message}");
        // The main goal of this test is to ensure no infinite recursion between
        // GetExpressionType -> FlowAnalyzer -> callback -> GetExpressionTypeWithoutFlow
        // The actual type result ("String" or "Variant") depends on flow analysis implementation
        // which is tested elsewhere in FlowSensitiveTypeTests
    }

    private static GDScriptFile ParseAndAnalyze(string code)
    {
        var reference = new GDScriptReference("test://virtual/recursion_test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        script.Analyze();
        return script;
    }

    /// <summary>
    /// Helper to record exceptions without crashing the test on StackOverflowException.
    /// Note: StackOverflowException cannot actually be caught, so this test will crash
    /// if there's a real stack overflow. The test passing means no overflow occurred.
    /// </summary>
    private static class Record
    {
        public static Exception? Exception(Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
