using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

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
    /// Tests that method returning a lambda expression has type Callable with signature.
    /// The lambda's signature is inferred from its parameters and return type.
    /// </summary>
    [TestMethod]
    public void GetTypeForNode_MethodReturningLambda_ReturnsCallableWithSignature()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("cross_file_inference.gd");
        Assert.IsNotNull(script, "Script 'cross_file_inference.gd' not found");

        if (script.SemanticModel == null)
            script.Analyze();

        Assert.IsNotNull(script.SemanticModel, "Analyzer should be available");
        var analyzer = script.SemanticModel;

        var method = script.Class?.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "_create_entity_handler");

        Assert.IsNotNull(method, "Method '_create_entity_handler' not found");

        // Act
        var returnType = analyzer.GetTypeForNode(method) ?? "void";

        // Assert - method returns a lambda with semantic Callable type (includes signature)
        Assert.IsTrue(returnType.StartsWith("Callable"),
            $"Method returning lambda should have Callable type. Got: {returnType}");
    }

    #region Lambda Signature Inference Tests

    /// <summary>
    /// Tests that lambda with typed parameters has full signature inferred.
    /// Example: func(x: int, y: String) -> bool: return x > 0
    /// Expected type: Callable[[int, String], bool]
    /// </summary>
    [TestMethod]
    public void InferType_LambdaWithTypedParams_InfersSignature()
    {
        var code = @"
extends Node
var cb = func(x: int, y: String) -> bool:
    return x > 0
";
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "cb");

        var lambda = variable?.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Act - infer the full semantic type of the lambda expression
        var type = engine.InferType(lambda);

        // Assert - semantic type includes full signature
        Assert.IsNotNull(type, "Lambda type should be inferred");
        Assert.IsTrue(type.StartsWith("Callable"),
            $"Lambda should be inferred as Callable type. Got: {type}");
        Assert.IsTrue(type.Contains("int"), $"Lambda semantic type should include 'int' param. Got: {type}");
        Assert.IsTrue(type.Contains("String"), $"Lambda semantic type should include 'String' param. Got: {type}");
        Assert.IsTrue(type.Contains("bool"), $"Lambda semantic type should include 'bool' return. Got: {type}");
    }

    /// <summary>
    /// Tests that lambda without typed params returns generic Callable.
    /// Example: func(x, y): return x + y
    /// </summary>
    [TestMethod]
    public void InferType_LambdaNoParams_InfersCallable()
    {
        var code = @"
extends Node
var cb = func(): pass
";
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "cb");

        var lambda = variable?.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Act
        var type = engine.InferType(lambda);

        // Assert - no params, no return → simple Callable
        Assert.AreEqual("Callable", type,
            $"Lambda with no params and void return should be 'Callable'. Got: {type}");
    }

    /// <summary>
    /// Tests that lambda with untyped params still infers as Callable.
    /// Example: func(x, y): return x + y
    /// </summary>
    [TestMethod]
    public void InferType_LambdaUntypedParams_InfersCallable()
    {
        var code = @"
extends Node
var cb = func(x, y): return x + y
";
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "cb");

        var lambda = variable?.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Act
        var type = engine.InferType(lambda);

        // Assert - semantic type includes Variant for untyped params
        Assert.IsTrue(type.StartsWith("Callable"),
            $"Lambda with untyped params should be Callable. Got: {type}");
        Assert.IsTrue(type.Contains("Variant"),
            $"Lambda semantic type with untyped params should contain Variant. Got: {type}");
    }

    /// <summary>
    /// Tests that lambda assigned to typed variable inherits constraints.
    /// </summary>
    [TestMethod]
    public void InferType_LambdaWithTypedVariable_InfersFromVariable()
    {
        var code = @"
extends Node
var cb: Callable = func(x: int): return x * 2
";
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "cb");

        var lambda = variable?.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Act
        var type = engine.InferType(lambda);

        // Assert - should be Callable with signature
        Assert.IsTrue(type.StartsWith("Callable"),
            $"Lambda assigned to Callable variable should be Callable. Got: {type}");
    }

    /// <summary>
    /// Tests exact semantic signature format: Callable[[int, String], bool]
    /// </summary>
    [TestMethod]
    public void InferType_LambdaWithTypedParams_ExactSignatureFormat()
    {
        var code = @"
extends Node
var cb = func(x: int, y: String) -> bool:
    return x > 0
";
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "cb");

        var lambda = variable?.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Act - InferType returns semantic type with full signature
        var type = engine.InferType(lambda);

        // Assert exact format: Callable[[int, String], bool]
        Assert.AreEqual("Callable[[int, String], bool]", type,
            $"Lambda semantic type should be 'Callable[[int, String], bool]'. Got: {type}");
    }

    /// <summary>
    /// Tests that lambda with return type but no params has correct format.
    /// Example: func() -> int: return 42
    /// Expected: Callable[[], int]
    /// </summary>
    [TestMethod]
    public void InferType_LambdaNoParamsWithReturn_InfersSignature()
    {
        var code = @"
extends Node
var cb = func() -> int: return 42
";
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "cb");

        var lambda = variable?.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Act
        var type = engine.InferType(lambda);

        // Assert - no params but has return type: Callable[[], int]
        Assert.AreEqual("Callable[[], int]", type,
            $"Lambda with no params but return type should be 'Callable[[], int]'. Got: {type}");
    }

    /// <summary>
    /// Tests that lambda with single param has correct format.
    /// Example: func(x: int): return x * 2
    /// Expected: Callable[[int], int]
    /// </summary>
    [TestMethod]
    public void InferType_LambdaSingleParam_InfersSignature()
    {
        var code = @"
extends Node
var cb = func(x: int): return x * 2
";
        var classDecl = ParseClass(code);
        var engine = CreateTypeEngine();

        var variable = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "cb");

        var lambda = variable?.Initializer as GDMethodExpression;
        Assert.IsNotNull(lambda, "Initializer should be a lambda");

        // Act
        var type = engine.InferType(lambda);

        // Assert: Callable[[int], int]
        Assert.AreEqual("Callable[[int], int]", type,
            $"Lambda with int param returning int should be 'Callable[[int], int]'. Got: {type}");
    }

    /// <summary>
    /// Tests extracting return type from Callable signature.
    /// </summary>
    [TestMethod]
    public void ExtractCallableReturnType_WithSignature_ReturnsCorrectType()
    {
        Assert.AreEqual("bool", GDTypeInferenceEngine.ExtractCallableReturnType("Callable[[int, String], bool]"));
        Assert.AreEqual("int", GDTypeInferenceEngine.ExtractCallableReturnType("Callable[[String], int]"));
        Assert.AreEqual("void", GDTypeInferenceEngine.ExtractCallableReturnType("Callable[[], void]"));
        Assert.AreEqual("Array[int]", GDTypeInferenceEngine.ExtractCallableReturnType("Callable[[int], Array[int]]"));
        Assert.IsNull(GDTypeInferenceEngine.ExtractCallableReturnType("Callable"));
        Assert.IsNull(GDTypeInferenceEngine.ExtractCallableReturnType(null));
    }

    /// <summary>
    /// Tests extracting parameter types from Callable signature.
    /// </summary>
    [TestMethod]
    public void ExtractCallableParameterTypes_WithSignature_ReturnsCorrectTypes()
    {
        var types = GDTypeInferenceEngine.ExtractCallableParameterTypes("Callable[[int, String], bool]");
        Assert.AreEqual(2, types.Count);
        Assert.AreEqual("int", types[0]);
        Assert.AreEqual("String", types[1]);

        var singleParam = GDTypeInferenceEngine.ExtractCallableParameterTypes("Callable[[String], int]");
        Assert.AreEqual(1, singleParam.Count);
        Assert.AreEqual("String", singleParam[0]);

        var noParams = GDTypeInferenceEngine.ExtractCallableParameterTypes("Callable[[], void]");
        Assert.AreEqual(0, noParams.Count);

        var simple = GDTypeInferenceEngine.ExtractCallableParameterTypes("Callable");
        Assert.AreEqual(0, simple.Count);
    }

    /// <summary>
    /// Tests that Callable.call() returns the correct type from signature.
    /// Note: This requires full semantic analysis with scope tracking.
    /// Using direct type extraction test as the basic unit test.
    /// </summary>
    [TestMethod]
    public void InferType_CallableCall_ExtractsReturnType()
    {
        // Test the extraction logic directly
        // When cb has type Callable[[int], String], cb.call(42) should return String
        var callableType = "Callable[[int], String]";
        var returnType = GDTypeInferenceEngine.ExtractCallableReturnType(callableType);

        Assert.AreEqual("String", returnType,
            $"Callable[[int], String].call() should return String. Got: {returnType}");

        // Complex case with nested generic
        var complexType = "Callable[[Array[int], Dictionary], Array[String]]";
        var complexReturn = GDTypeInferenceEngine.ExtractCallableReturnType(complexType);

        Assert.AreEqual("Array[String]", complexReturn,
            $"Complex Callable should extract correct return type. Got: {complexReturn}");
    }

    #endregion

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
