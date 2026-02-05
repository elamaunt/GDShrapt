using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Tests for cross-method type flow analysis.
/// Tests that parameter types can be inferred from call sites across files.
/// </summary>
[TestClass]
public class CrossMethodFlowTests
{
    #region GDCallSiteTypeAnalyzer Unit Tests

    [TestMethod]
    public void CallSiteAnalyzer_NoCallSites_ReturnsEmptyResult()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();
        var analyzer = new GDCallSiteTypeAnalyzer(
            registry,
            _ => null);

        var paramNames = new List<string> { "x", "y" };

        // Act
        var result = analyzer.AnalyzeCallSites(
            "TestClass",
            "testMethod",
            paramNames,
            _ => null);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(0, result["x"].CallSiteCount);
        Assert.AreEqual(0, result["y"].CallSiteCount);
        Assert.AreEqual("Variant", result["x"].EffectiveType);
        Assert.AreEqual("Variant", result["y"].EffectiveType);
    }

    [TestMethod]
    public void CallSiteAnalyzer_SingleCallSite_IntArgument_InfersIntType()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Parse: testMethod(10, "hello")
        var callExpr = ParseCallExpression("testMethod(10, \"hello\")");
        Assert.IsNotNull(callExpr);

        var callSite = new GDCallSiteEntry(
            sourceFilePath: "caller.gd",
            sourceMethodName: "caller_method",
            line: 10,
            column: 5,
            targetClassName: "TestClass",
            targetMethodName: "testMethod",
            callExpression: callExpr);

        registry.Register(callSite);

        var analyzer = new GDCallSiteTypeAnalyzer(
            registry,
            _ => null);

        var paramNames = new List<string> { "x", "y" };

        // Act
        var result = analyzer.AnalyzeCallSites(
            "TestClass",
            "testMethod",
            paramNames,
            _ => null);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(1, result["x"].CallSiteCount);
        Assert.AreEqual(1, result["y"].CallSiteCount);
        Assert.AreEqual("int", result["x"].EffectiveType);
        Assert.AreEqual("String", result["y"].EffectiveType);
    }

    [TestMethod]
    public void CallSiteAnalyzer_MultipleCallSites_DifferentTypes_CreatesUnion()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Call site 1: testMethod(10)
        var callExpr1 = ParseCallExpression("testMethod(10)");
        registry.Register(new GDCallSiteEntry(
            "caller1.gd", "method1", 10, 5, "TestClass", "testMethod", callExpr1));

        // Call site 2: testMethod("hello")
        var callExpr2 = ParseCallExpression("testMethod(\"hello\")");
        registry.Register(new GDCallSiteEntry(
            "caller2.gd", "method2", 20, 5, "TestClass", "testMethod", callExpr2));

        var analyzer = new GDCallSiteTypeAnalyzer(
            registry,
            _ => null);

        var paramNames = new List<string> { "x" };

        // Act
        var result = analyzer.AnalyzeCallSites(
            "TestClass",
            "testMethod",
            paramNames,
            _ => null);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(2, result["x"].CallSiteCount);
        Assert.IsTrue(result["x"].ArgumentTypes.Types.Contains("int"));
        Assert.IsTrue(result["x"].ArgumentTypes.Types.Contains("String"));
    }

    [TestMethod]
    public void CallSiteAnalyzer_MultipleCallSites_SameType_SingleType()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Call site 1: testMethod(10)
        var callExpr1 = ParseCallExpression("testMethod(10)");
        registry.Register(new GDCallSiteEntry(
            "caller1.gd", "method1", 10, 5, "TestClass", "testMethod", callExpr1));

        // Call site 2: testMethod(42)
        var callExpr2 = ParseCallExpression("testMethod(42)");
        registry.Register(new GDCallSiteEntry(
            "caller2.gd", "method2", 20, 5, "TestClass", "testMethod", callExpr2));

        var analyzer = new GDCallSiteTypeAnalyzer(
            registry,
            _ => null);

        var paramNames = new List<string> { "x" };

        // Act
        var result = analyzer.AnalyzeCallSites(
            "TestClass",
            "testMethod",
            paramNames,
            _ => null);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(2, result["x"].CallSiteCount);
        Assert.AreEqual("int", result["x"].EffectiveType);
        Assert.AreEqual(0, result["x"].UnknownTypeCount);
    }

    [TestMethod]
    public void CallSiteAnalyzer_BooleanLiteral_InfersBoolType()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Call: testMethod(true)
        var callExpr = ParseCallExpression("testMethod(true)");
        registry.Register(new GDCallSiteEntry(
            "caller.gd", "method", 10, 5, "TestClass", "testMethod", callExpr));

        var analyzer = new GDCallSiteTypeAnalyzer(
            registry,
            _ => null);

        var paramNames = new List<string> { "x" };

        // Act
        var result = analyzer.AnalyzeCallSites(
            "TestClass",
            "testMethod",
            paramNames,
            _ => null);

        // Assert
        Assert.AreEqual("bool", result["x"].EffectiveType);
    }

    [TestMethod]
    public void CallSiteAnalyzer_ArrayInitializer_InfersArrayType()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Call: testMethod([1, 2, 3])
        var callExpr = ParseCallExpression("testMethod([1, 2, 3])");
        registry.Register(new GDCallSiteEntry(
            "caller.gd", "method", 10, 5, "TestClass", "testMethod", callExpr));

        var analyzer = new GDCallSiteTypeAnalyzer(
            registry,
            _ => null);

        var paramNames = new List<string> { "arr" };

        // Act
        var result = analyzer.AnalyzeCallSites(
            "TestClass",
            "testMethod",
            paramNames,
            _ => null);

        // Assert
        Assert.AreEqual("Array", result["arr"].EffectiveType);
    }

    [TestMethod]
    public void CallSiteAnalyzer_DictionaryInitializer_InfersDictionaryType()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Call: testMethod({"key": "value"})
        var callExpr = ParseCallExpression("testMethod({\"key\": \"value\"})");
        registry.Register(new GDCallSiteEntry(
            "caller.gd", "method", 10, 5, "TestClass", "testMethod", callExpr));

        var analyzer = new GDCallSiteTypeAnalyzer(
            registry,
            _ => null);

        var paramNames = new List<string> { "dict" };

        // Act
        var result = analyzer.AnalyzeCallSites(
            "TestClass",
            "testMethod",
            paramNames,
            _ => null);

        // Assert
        Assert.AreEqual("Dictionary", result["dict"].EffectiveType);
    }

    [TestMethod]
    public void CallSiteAnalyzer_NullLiteral_InfersNullType()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Call: testMethod(null)
        var callExpr = ParseCallExpression("testMethod(null)");
        registry.Register(new GDCallSiteEntry(
            "caller.gd", "method", 10, 5, "TestClass", "testMethod", callExpr));

        var analyzer = new GDCallSiteTypeAnalyzer(
            registry,
            _ => null);

        var paramNames = new List<string> { "x" };

        // Act
        var result = analyzer.AnalyzeCallSites(
            "TestClass",
            "testMethod",
            paramNames,
            _ => null);

        // Assert
        Assert.AreEqual("null", result["x"].EffectiveType);
    }

    [TestMethod]
    public void CallSiteAnalyzer_FloatLiteral_InfersFloatType()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Call: testMethod(3.14)
        var callExpr = ParseCallExpression("testMethod(3.14)");
        registry.Register(new GDCallSiteEntry(
            "caller.gd", "method", 10, 5, "TestClass", "testMethod", callExpr));

        var analyzer = new GDCallSiteTypeAnalyzer(
            registry,
            _ => null);

        var paramNames = new List<string> { "x" };

        // Act
        var result = analyzer.AnalyzeCallSites(
            "TestClass",
            "testMethod",
            paramNames,
            _ => null);

        // Assert
        Assert.AreEqual("float", result["x"].EffectiveType);
    }

    [TestMethod]
    public void CallSiteAnalyzer_Confidence_AllKnownTypes_HighConfidence()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Two calls with known types
        var callExpr1 = ParseCallExpression("testMethod(10)");
        registry.Register(new GDCallSiteEntry(
            "caller1.gd", "method", 10, 5, "TestClass", "testMethod", callExpr1));

        var callExpr2 = ParseCallExpression("testMethod(20)");
        registry.Register(new GDCallSiteEntry(
            "caller2.gd", "method", 20, 5, "TestClass", "testMethod", callExpr2));

        var analyzer = new GDCallSiteTypeAnalyzer(
            registry,
            _ => null);

        var paramNames = new List<string> { "x" };

        // Act
        var result = analyzer.AnalyzeCallSites(
            "TestClass",
            "testMethod",
            paramNames,
            _ => null);

        // Assert
        Assert.AreEqual(GDTypeConfidence.High, result["x"].GetConfidence());
    }

    [TestMethod]
    public void CallSiteAnalyzer_UnknownArgumentType_CountsAsUnknown()
    {
        // Arrange
        var registry = new GDCallSiteRegistry();

        // Call with an unknown variable: testMethod(someVar)
        var callExpr = ParseCallExpression("testMethod(someVar)");
        registry.Register(new GDCallSiteEntry(
            "caller.gd", "method", 10, 5, "TestClass", "testMethod", callExpr));

        var analyzer = new GDCallSiteTypeAnalyzer(
            registry,
            _ => null);

        var paramNames = new List<string> { "x" };

        // Act
        var result = analyzer.AnalyzeCallSites(
            "TestClass",
            "testMethod",
            paramNames,
            _ => null);

        // Assert
        Assert.AreEqual(1, result["x"].CallSiteCount);
        Assert.AreEqual(1, result["x"].UnknownTypeCount, "Unknown variable should count as unknown type");
        Assert.AreEqual("Variant", result["x"].EffectiveType);
    }

    #endregion

    #region ToInferredParameterType Tests

    [TestMethod]
    public void ToInferredParameterType_SingleType_CreatesCorrectType()
    {
        // Arrange
        var callSiteResult = new GDCallSiteTypeAnalyzer.ParameterTypeFromCallSites("param", 0);
        callSiteResult.ArgumentTypes.AddType("int");
        callSiteResult.CallSiteCount = 3;
        callSiteResult.UnknownTypeCount = 0;

        // Act
        var inferredType = GDCallSiteTypeAnalyzer.ToInferredParameterType(callSiteResult);

        // Assert
        Assert.AreEqual("param", inferredType.ParameterName);
        Assert.AreEqual("int", inferredType.TypeName);
        Assert.AreEqual(GDTypeConfidence.High, inferredType.Confidence);
        Assert.IsFalse(inferredType.IsUnion);
    }

    [TestMethod]
    public void ToInferredParameterType_UnionType_CreatesUnion()
    {
        // Arrange
        var callSiteResult = new GDCallSiteTypeAnalyzer.ParameterTypeFromCallSites("param", 0);
        callSiteResult.ArgumentTypes.AddType("int");
        callSiteResult.ArgumentTypes.AddType("String");
        callSiteResult.CallSiteCount = 2;
        callSiteResult.UnknownTypeCount = 0;

        // Act
        var inferredType = GDCallSiteTypeAnalyzer.ToInferredParameterType(callSiteResult);

        // Assert
        Assert.AreEqual("param", inferredType.ParameterName);
        Assert.IsTrue(inferredType.IsUnion);
        Assert.IsNotNull(inferredType.UnionTypes);
        Assert.IsTrue(inferredType.UnionTypes.Contains("int"));
        Assert.IsTrue(inferredType.UnionTypes.Contains("String"));
    }

    [TestMethod]
    public void ToInferredParameterType_NoCallSites_ReturnsUnknown()
    {
        // Arrange
        var callSiteResult = new GDCallSiteTypeAnalyzer.ParameterTypeFromCallSites("param", 0);

        // Act
        var inferredType = GDCallSiteTypeAnalyzer.ToInferredParameterType(callSiteResult);

        // Assert
        Assert.IsTrue(inferredType.IsUnknown);
        Assert.AreEqual("Variant", inferredType.TypeName);
    }

    [TestMethod]
    public void ToInferredParameterType_SomeUnknown_MediumConfidence()
    {
        // Arrange
        var callSiteResult = new GDCallSiteTypeAnalyzer.ParameterTypeFromCallSites("param", 0);
        callSiteResult.ArgumentTypes.AddType("int");
        callSiteResult.CallSiteCount = 10;
        callSiteResult.UnknownTypeCount = 2; // 80% known - should be medium

        // Act
        var inferredType = GDCallSiteTypeAnalyzer.ToInferredParameterType(callSiteResult);

        // Assert
        Assert.AreEqual(GDTypeConfidence.Medium, inferredType.Confidence);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Parses a call expression from GDScript code.
    /// </summary>
    private static GDCallExpression? ParseCallExpression(string callCode)
    {
        var code = $"func test():\n    {callCode}";
        var parser = new GDScriptReader();
        var parsed = parser.ParseFileContent(code);

        var method = parsed.Members?.OfType<GDMethodDeclaration>().FirstOrDefault();
        var stmt = method?.Statements?.FirstOrDefault() as GDExpressionStatement;
        return stmt?.Expression as GDCallExpression;
    }

    #endregion
}
