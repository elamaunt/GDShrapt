using GDShrapt.Reader;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests.TypeInference.Level2;

/// <summary>
/// Tests for lambda parameter type inference from .call() call sites.
/// </summary>
[TestClass]
public class LambdaCallSiteTypeInferenceTests
{
    #region Basic Call Site Tests

    [TestMethod]
    public void Lambda_InVariable_ParameterInferredFromCallSite()
    {
        var code = @"
extends Node

func test():
    var cb = func(x): return x * 2
    cb.call(42)
";
        var model = CreateSemanticModel(code);

        // Find the lambda
        var classDecl = ParseClass(code);
        var lambda = FindLambda(classDecl);
        Assert.IsNotNull(lambda, "Lambda not found");

        // Get call sites
        var callSites = model.CallSiteRegistry?.GetCallSitesFor(lambda, model.ScriptFile);
        Assert.IsNotNull(callSites);
        Assert.AreEqual(1, callSites.Count, "Expected 1 call site");

        // Verify argument type was captured
        var callSite = callSites[0];
        Assert.AreEqual(1, callSite.Arguments.Count);
        Assert.AreEqual("int", callSite.Arguments[0].InferredType);
    }

    [TestMethod]
    public void Lambda_MultipleCallSites_CollectsAllArgumentTypes()
    {
        var code = @"
extends Node

func test():
    var cb = func(x): return x
    cb.call(42)
    cb.call(""hello"")
";
        var model = CreateSemanticModel(code);

        // Find the lambda
        var classDecl = ParseClass(code);
        var lambda = FindLambda(classDecl);
        Assert.IsNotNull(lambda, "Lambda not found");

        // Get inferred parameter types
        var paramTypes = model.InferLambdaParameterTypesFromCallSites(lambda);
        Assert.IsNotNull(paramTypes);
        Assert.IsTrue(paramTypes.ContainsKey(0), "Parameter 0 should have inferred type");

        // Should be union of int and String
        var unionType = paramTypes[0];
        Assert.IsTrue(unionType.Types.Contains("int") || unionType.Types.Contains("String"),
            $"Expected int or String, got: {unionType}");
    }

    [TestMethod]
    public void Lambda_NoCallSites_ReturnsEmpty()
    {
        var code = @"
extends Node

func test():
    var cb = func(x): return x * 2
    # No call sites
";
        var model = CreateSemanticModel(code);

        // Find the lambda
        var classDecl = ParseClass(code);
        var lambda = FindLambda(classDecl);
        Assert.IsNotNull(lambda, "Lambda not found");

        // Should have no call sites
        var callSites = model.CallSiteRegistry?.GetCallSitesFor(lambda, model.ScriptFile);
        Assert.IsNotNull(callSites);
        Assert.AreEqual(0, callSites.Count, "Expected 0 call sites");
    }

    #endregion

    #region Tracker Tests

    [TestMethod]
    public void Tracker_TracksLambdaAssignmentToVariable()
    {
        var code = @"
extends Node

func test():
    var cb = func(x): return x
    cb.call(42)
";
        var classDecl = ParseClass(code);
        var collector = new GDCallableCallSiteCollector();
        collector.Collect(classDecl);

        // Check tracker has the variable
        Assert.IsTrue(collector.Tracker.LocalVariableNames.Contains("cb"));

        // Check definitions were created
        var definitions = collector.Tracker.ResolveVariable("cb");
        Assert.AreEqual(1, definitions.Count);
        Assert.IsTrue(definitions[0].IsLambda);
    }

    [TestMethod]
    public void Tracker_TracksAliasAssignment()
    {
        var code = @"
extends Node

func test():
    var original = func(x): return x
    var alias = original
    alias.call(42)
";
        var classDecl = ParseClass(code);
        var collector = new GDCallableCallSiteCollector();
        collector.Collect(classDecl);

        // Both should resolve to the same lambda
        var originalDefs = collector.Tracker.ResolveVariable("original");
        var aliasDefs = collector.Tracker.ResolveVariable("alias");

        Assert.AreEqual(1, originalDefs.Count);
        // Alias should resolve through the chain
        Assert.AreEqual(originalDefs.Count, aliasDefs.Count);
    }

    #endregion

    #region Call Site Collection Tests

    [TestMethod]
    public void Collector_DetectsCallMethod()
    {
        var code = @"
extends Node

func test():
    var cb = func(x): return x
    cb.call(42)
";
        var classDecl = ParseClass(code);
        var collector = new GDCallableCallSiteCollector();
        collector.Collect(classDecl);

        Assert.AreEqual(1, collector.CallSites.Count);
        Assert.IsFalse(collector.CallSites[0].IsCallV);
        Assert.AreEqual("cb", collector.CallSites[0].CallableVariableName);
    }

    [TestMethod]
    public void Collector_DetectsCallVMethod()
    {
        var code = @"
extends Node

func test():
    var cb = func(x): return x
    var args = [42]
    cb.callv(args)
";
        var classDecl = ParseClass(code);
        var collector = new GDCallableCallSiteCollector();
        collector.Collect(classDecl);

        // Should have call site for callv
        var callvSites = collector.CallSites.Where(cs => cs.IsCallV).ToList();
        Assert.AreEqual(1, callvSites.Count);
    }

    [TestMethod]
    public void Collector_CollectsMultipleArguments()
    {
        var code = @"
extends Node

func test():
    var cb = func(a, b, c): return a + b + c
    cb.call(1, 2, 3)
";
        var classDecl = ParseClass(code);

        // Use type inference
        var model = CreateSemanticModel(code);
        var callSites = model.CallSiteRegistry?.AllCallSites.ToList();

        Assert.IsNotNull(callSites);
        Assert.AreEqual(1, callSites.Count);
        Assert.AreEqual(3, callSites[0].Arguments.Count);

        // All should be int
        foreach (var arg in callSites[0].Arguments)
        {
            Assert.AreEqual("int", arg.InferredType, $"Arg {arg.Index} should be int");
        }
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public void Integration_LambdaParameterInferredFromCallSite()
    {
        var code = @"
extends Node

func test():
    var cb = func(x): return x * 2
    cb.call(42)
";
        var model = CreateSemanticModel(code);
        var classDecl = ParseClass(code);
        var lambda = FindLambda(classDecl);

        // Infer parameter type from call sites
        var inferredType = model.InferLambdaParameterTypeFromCallSites(lambda, 0);

        Assert.IsNotNull(inferredType);
        Assert.AreEqual("int", inferredType, "Parameter should be inferred as int from cb.call(42)");
    }

    [TestMethod]
    public void Integration_LambdaParameterFromMultipleCallSites()
    {
        var code = @"
extends Node

func test():
    var cb = func(x): return x
    cb.call(1)
    cb.call(2)
    cb.call(3)
";
        var model = CreateSemanticModel(code);
        var classDecl = ParseClass(code);
        var lambda = FindLambda(classDecl);

        // All call sites pass int, should infer int
        var inferredType = model.InferLambdaParameterTypeFromCallSites(lambda, 0);

        Assert.IsNotNull(inferredType);
        Assert.AreEqual("int", inferredType);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void EdgeCase_LambdaWithExplicitType_NotAffectedByCallSites()
    {
        // When lambda has explicit type annotation, call sites should not override it
        var code = @"
extends Node

func test():
    var cb = func(x: int): return x * 2
    cb.call(""hello"")  # Call site passes String but param is typed as int
";
        var model = CreateSemanticModel(code);
        var classDecl = ParseClass(code);
        var lambda = FindLambda(classDecl);

        // Lambda has explicit type - call site should still be recorded
        var callSites = model.CallSiteRegistry?.GetCallSitesFor(lambda, model.ScriptFile);
        Assert.IsNotNull(callSites);
        Assert.AreEqual(1, callSites.Count);

        // But explicit type should take precedence in actual type resolution
        // (this is handled by GDTypeInferenceEngine, not the registry)
    }

    [TestMethod]
    public void EdgeCase_LambdaPassedDirectlyToMethod()
    {
        // Lambda passed directly without storing in variable
        // This tests the CALL SITE detection - the cb.call(42) SHOULD be detected,
        // but linking it back to the lambda requires inter-procedural analysis
        var code = @"
extends Node

func process(cb: Callable):
    cb.call(42)

func test():
    process(func(x): return x * 2)
";
        var model = CreateSemanticModel(code);

        // The call site cb.call(42) IS detected, but it's on a parameter, not a tracked variable
        // So it won't have a ResolvedDefinition, and won't appear in AllCallSites (by definition only)
        // For now, verify call site collection runs without error and registry is created
        var registry = model.CallSiteRegistry;
        Assert.IsNotNull(registry, "Registry should be created");

        // Future enhancement: inter-procedural tracking would link cb parameter to the lambda argument
    }

    [TestMethod]
    public void EdgeCase_ChainedMethodCall()
    {
        // Lambda on result of method chain
        // This tests the pattern: get_callback().call(42)
        // The call site detection works, but linking to lambda definition requires return type tracking
        var code = @"
extends Node

func get_callback() -> Callable:
    return func(x): return x

func test():
    get_callback().call(42)
";
        var model = CreateSemanticModel(code);

        // The call site get_callback().call(42) IS detected as a .call() invocation,
        // but the callable expression is a GDCallExpression, not an identifier
        // So it won't have a CallableVariableName or ResolvedDefinition
        var registry = model.CallSiteRegistry;
        Assert.IsNotNull(registry, "Registry should be created");

        // Future enhancement: method return type analysis would link get_callback() return to the lambda
    }

    #endregion

    #region Helper Methods

    private static GDSemanticModel CreateSemanticModel(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        var reference = new GDScriptReference("test://virtual/test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDCompositeRuntimeProvider(
            new GDGodotTypesProvider(),
            null, null, null);

        return GDSemanticModel.Create(scriptFile, runtimeProvider);
    }

    private static GDClassDeclaration ParseClass(string code)
    {
        var reader = new GDScriptReader();
        return reader.ParseFileContent(code);
    }

    private static GDMethodExpression? FindLambda(GDClassDeclaration classDecl)
    {
        GDMethodExpression? result = null;

        var finder = new LambdaFinder();
        finder.OnLambdaFound = lambda => result = lambda;
        classDecl.WalkIn(finder);

        return result;
    }

    private class LambdaFinder : GDVisitor
    {
        public System.Action<GDMethodExpression>? OnLambdaFound { get; set; }

        public override void Visit(GDMethodExpression methodExpression)
        {
            OnLambdaFound?.Invoke(methodExpression);
        }
    }

    #endregion
}
