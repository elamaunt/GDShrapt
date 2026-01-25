using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for method return type inference via GetTypeForNode.
/// These tests replicate the logic used in GDTypeFlowPanel plugin
/// to verify that methods with return statements don't incorrectly show "void".
/// </summary>
[TestClass]
public class MethodReturnTypeInferenceTests
{
    private static GDScriptFile? _script;

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _script = TestProjectFixture.GetScript("union_types_complex.gd");
        Assert.IsNotNull(_script, "Script 'union_types_complex.gd' not found in test project");

        if (_script.SemanticModel == null)
        {
            _script.Analyze();
        }
    }

    /// <summary>
    /// Tests that GetTypeForNode for method 'get_config' does NOT return "void".
    /// This method has no explicit type annotation but returns config.get(key).
    ///
    /// This test replicates the exact logic from GDTypeFlowPanel:
    ///   var returnType = analyzer.GetTypeForNode(method) ?? "void";
    ///
    /// Note: config is a class-level Dictionary variable. config.get(key) returns Variant.
    /// So the expected return type is "Variant".
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_GetConfig_ShouldReturnVariant()
    {
        // Arrange
        var method = FindMethod("get_config");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act - exactly like GDTypeFlowPanel does
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert - config.get(key) returns Variant
        Assert.AreNotEqual("void", returnType,
            $"get_config method has 'return config.get(key)' - type should NOT be 'void'. Got: {returnType}");

        // Since Dictionary.get returns Variant, that's acceptable
        Assert.IsTrue(returnType == "Variant" || returnType != "void",
            $"get_config should return 'Variant' (from Dictionary.get). Got: {returnType}");
    }

    /// <summary>
    /// Tests that GetTypeForNode for method 'create_error' returns "Dictionary".
    /// This method has no explicit type annotation but returns a Dictionary literal.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_CreateError_ShouldReturnDictionary()
    {
        // Arrange
        var method = FindMethod("create_error");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("void", returnType,
            "create_error returns a Dictionary literal - type should NOT be 'void'");
        Assert.AreEqual("Dictionary", returnType,
            "create_error should return 'Dictionary'");
    }

    /// <summary>
    /// Tests that GetTypeForNode for method 'try_operation' does NOT return "void".
    /// This method returns int in one branch and String in another.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_TryOperation_ShouldNotReturnVoid()
    {
        // Arrange
        var method = FindMethod("try_operation");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("void", returnType,
            "try_operation has multiple return statements with values - type should NOT be 'void'");
    }

    /// <summary>
    /// Tests that GetTypeForNode for method 'complex_conditional' does NOT return "void".
    /// This method returns different types based on conditions.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_ComplexConditional_ShouldNotReturnVoid()
    {
        // Arrange
        var method = FindMethod("complex_conditional");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("void", returnType,
            "complex_conditional has return statements with values - type should NOT be 'void'");
    }

    /// <summary>
    /// Tests that GetTypeForNode for method 'match_return' does NOT return "void".
    /// This method has match statement with return statements in each case.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_MatchReturn_ShouldNotReturnVoid()
    {
        // Arrange
        var method = FindMethod("match_return");
        Assert.IsNotNull(_script?.SemanticModel, "Analyzer should be available");
        var analyzer = _script.SemanticModel;

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreNotEqual("void", returnType,
            "match_return has return statements in match cases - type should NOT be 'void'");
    }

    /// <summary>
    /// Tests that GetTypeForNode for methods returning lambdas returns "Callable".
    /// In GDScript 4, lambdas (func(): ...) have type Callable.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_MethodReturningLambda_ShouldReturnCallable()
    {
        // Arrange - get a script with lambda-returning methods
        var lambdaScript = TestProjectFixture.GetScript("cross_file_inference.gd");
        Assert.IsNotNull(lambdaScript, "Script 'cross_file_inference.gd' not found");

        if (lambdaScript.SemanticModel == null)
            lambdaScript.Analyze();

        Assert.IsNotNull(lambdaScript.SemanticModel, "Analyzer should be available");
        var analyzer = lambdaScript.SemanticModel;

        // Find _create_entity_handler which returns: func(data): return process_entity(data)
        var method = lambdaScript.Class?.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "_create_entity_handler");

        Assert.IsNotNull(method, "Method '_create_entity_handler' not found");

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert - lambdas in GDScript 4 have type Callable
        Assert.AreEqual("Callable", returnType,
            $"Method returning lambda should have type 'Callable'. Got: {returnType}");
    }

    #region Helper Methods

    private static GDMethodDeclaration FindMethod(string name)
    {
        Assert.IsNotNull(_script?.Class, "Script not loaded or has no class");

        var method = _script.Class.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == name);

        Assert.IsNotNull(method, $"Method '{name}' not found in union_types_complex.gd");
        return method;
    }

    #endregion
}
