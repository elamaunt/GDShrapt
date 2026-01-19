using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for lambda (GDMethodExpression) return type inference.
/// These tests verify that lambda expressions have their return types properly inferred
/// from their body, not just returning "Callable" for all lambdas.
/// </summary>
[TestClass]
public class LambdaTypeInferenceTests
{
    /// <summary>
    /// Tests that inline lambda returning a string literal has type String.
    /// Example: func(): return "hello"
    /// </summary>
    [TestMethod]
    public void InferType_InlineLambdaReturningString_ReturnsString()
    {
        // Arrange
        var code = @"
extends Node
var callback = func(): return ""hello""
";
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "callback");

        Assert.IsNotNull(variable?.Initializer, "Variable 'callback' should have initializer");
        var lambda = variable.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Act - use InferLambdaReturnType to get the return type of the lambda body
        var returnType = engine.InferLambdaReturnType(lambda);

        // Assert
        Assert.AreEqual("String", returnType,
            $"Lambda returning string literal should have type 'String'. Got: {returnType}");
    }

    /// <summary>
    /// Tests that inline lambda returning an integer literal has type int.
    /// Example: func(): return 42
    /// </summary>
    [TestMethod]
    public void InferType_InlineLambdaReturningInt_ReturnsInt()
    {
        // Arrange
        var code = @"
extends Node
var callback = func(): return 42
";
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "callback");

        var lambda = variable?.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Act - use InferLambdaReturnType to get the return type of the lambda body
        var returnType = engine.InferLambdaReturnType(lambda);

        // Assert
        Assert.AreEqual("int", returnType,
            $"Lambda returning int literal should have type 'int'. Got: {returnType}");
    }

    /// <summary>
    /// Tests that lambda with explicit return type annotation uses that type.
    /// Example: func() -> int: return 42
    /// </summary>
    [TestMethod]
    public void InferType_LambdaWithExplicitReturnType_ReturnsAnnotatedType()
    {
        // Arrange
        var code = @"
extends Node
var callback = func() -> int: return 42
";
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "callback");

        var lambda = variable?.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Act - use InferLambdaReturnType to get the return type of the lambda body
        var returnType = engine.InferLambdaReturnType(lambda);

        // Assert
        Assert.AreEqual("int", returnType,
            $"Lambda with explicit '-> int' should have type 'int'. Got: {returnType}");
    }

    /// <summary>
    /// Tests that lambda returning a Dictionary literal has type Dictionary.
    /// Example: func(): return {"key": "value"}
    /// </summary>
    [TestMethod]
    public void InferType_InlineLambdaReturningDictionary_ReturnsDictionary()
    {
        // Arrange
        var code = @"
extends Node
var callback = func(): return {""key"": ""value""}
";
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "callback");

        var lambda = variable?.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Act - use InferLambdaReturnType to get the return type of the lambda body
        var returnType = engine.InferLambdaReturnType(lambda);

        // Assert
        Assert.AreEqual("Dictionary", returnType,
            $"Lambda returning dictionary literal should have type 'Dictionary'. Got: {returnType}");
    }

    /// <summary>
    /// Tests that lambda returning an Array literal has type Array.
    /// Example: func(): return [1, 2, 3]
    /// </summary>
    [TestMethod]
    public void InferType_InlineLambdaReturningArray_ReturnsArray()
    {
        // Arrange
        var code = @"
extends Node
var callback = func(): return [1, 2, 3]
";
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "callback");

        var lambda = variable?.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Act - use InferLambdaReturnType to get the return type of the lambda body
        var returnType = engine.InferLambdaReturnType(lambda);

        // Assert - Array[int] is now correctly inferred with element type
        Assert.IsTrue(returnType?.StartsWith("Array") == true,
            $"Lambda returning array literal should have type starting with 'Array'. Got: {returnType}");
    }

    /// <summary>
    /// Tests that lambda without return statement has type void.
    /// Example: func(): print("hello")
    /// </summary>
    [TestMethod]
    public void InferType_LambdaWithoutReturn_ReturnsVoid()
    {
        // Arrange
        var code = @"
extends Node
var callback = func(): print(""hello"")
";
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "callback");

        var lambda = variable?.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Act - use InferLambdaReturnType to get the return type of the lambda body
        var returnType = engine.InferLambdaReturnType(lambda);

        // Assert - print() returns void, so the lambda should return void
        Assert.AreEqual("void", returnType,
            $"Lambda with print() call (no explicit return) should have type 'void'. Got: {returnType}");
    }

    /// <summary>
    /// Tests that method returning a lambda expression has type Callable (for the method itself).
    /// The lambda's internal return type can be inferred, but the method returns Callable.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_MethodReturningLambda_ReturnsCallable()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("cross_file_inference.gd");
        Assert.IsNotNull(script, "Script 'cross_file_inference.gd' not found");

        if (script.Analyzer == null)
            script.Analyze();

        Assert.IsNotNull(script.Analyzer, "Analyzer should be available");
        var analyzer = script.Analyzer;

        var method = script.Class?.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "_create_entity_handler");

        Assert.IsNotNull(method, "Method '_create_entity_handler' not found");

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert - method returns a lambda, which is Callable
        Assert.AreEqual("Callable", returnType,
            $"Method returning lambda should have type 'Callable'. Got: {returnType}");
    }

    #region Helper Methods

    private static GDClassDeclaration ParseClass(string code)
    {
        var parser = new GDScriptReader();
        var classDecl = parser.ParseFileContent(code);
        Assert.IsNotNull(classDecl, "Failed to parse class");
        return classDecl;
    }

    private static GDTypeInferenceEngine CreateTypeEngine()
    {
        return new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance);
    }

    #endregion
}
