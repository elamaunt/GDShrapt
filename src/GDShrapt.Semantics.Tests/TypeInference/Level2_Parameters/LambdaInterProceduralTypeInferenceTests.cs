using GDShrapt.Reader;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests.TypeInference.Level2;

/// <summary>
/// Tests for inter-procedural lambda parameter type inference.
/// Phase 2: Lambda passed to method parameter → call sites on parameter → infer lambda param types.
/// </summary>
[TestClass]
public class LambdaInterProceduralTypeInferenceTests
{
    #region Method Profile Tests

    [TestMethod]
    public void MethodProfile_CollectsCallableParameter()
    {
        var code = @"
extends Node

func process(callback: Callable):
    callback.call(42)
";
        var classDecl = ParseClass(code);
        var collector = new GDCallableFlowCollector();
        collector.Collect(classDecl);

        Assert.AreEqual(1, collector.MethodProfiles.Count, "Should collect 1 method profile");

        var profile = collector.MethodProfiles[0];
        Assert.AreEqual("process", profile.MethodName);
        Assert.IsTrue(profile.IsCallableParameter("callback"));
        Assert.AreEqual(0, profile.GetParameterIndex("callback"));
    }

    [TestMethod]
    public void MethodProfile_CollectsCallSitesOnParameter()
    {
        var code = @"
extends Node

func process(callback: Callable):
    callback.call(42)
    callback.call(""hello"")
";
        var classDecl = ParseClass(code);
        var collector = new GDCallableFlowCollector();
        collector.Collect(classDecl);

        var profile = collector.MethodProfiles[0];
        var callSites = profile.GetCallSitesForParameter("callback");

        Assert.AreEqual(2, callSites.Count, "Should have 2 call sites");
    }

    [TestMethod]
    public void MethodProfile_MultipleCallableParameters()
    {
        var code = @"
extends Node

func dual_callback(on_success: Callable, on_error: Callable):
    on_success.call(100)
    on_error.call(""error message"")
";
        var classDecl = ParseClass(code);
        var collector = new GDCallableFlowCollector();
        collector.Collect(classDecl);

        var profile = collector.MethodProfiles[0];
        Assert.IsTrue(profile.IsCallableParameter("on_success"));
        Assert.IsTrue(profile.IsCallableParameter("on_error"));
        Assert.AreEqual(0, profile.GetParameterIndex("on_success"));
        Assert.AreEqual(1, profile.GetParameterIndex("on_error"));
    }

    #endregion

    #region Argument Binding Tests

    [TestMethod]
    public void ArgumentBinding_LambdaPassedToMethod()
    {
        var code = @"
extends Node

func process(callback: Callable):
    callback.call(42)

func test():
    process(func(x): return x * 2)
";
        var classDecl = ParseClass(code);
        var collector = new GDCallableFlowCollector();
        collector.Collect(classDecl);

        Assert.AreEqual(1, collector.ArgumentBindings.Count, "Should have 1 argument binding");

        var binding = collector.ArgumentBindings[0];
        Assert.IsNotNull(binding.LambdaDefinition);
        Assert.IsTrue(binding.TargetMethodKey.EndsWith(".process"));
        Assert.AreEqual(0, binding.TargetParameterIndex);
    }

    [TestMethod]
    public void ArgumentBinding_MultipleLambdasToSameMethod()
    {
        var code = @"
extends Node

func process(callback: Callable):
    callback.call(42)

func test():
    process(func(x): return x * 2)
    process(func(y): return y + 1)
";
        var classDecl = ParseClass(code);
        var collector = new GDCallableFlowCollector();
        collector.Collect(classDecl);

        Assert.AreEqual(2, collector.ArgumentBindings.Count, "Should have 2 argument bindings");
    }

    #endregion

    #region Registry Integration Tests

    [TestMethod]
    public void Registry_RegistersMethodProfileAndBindings()
    {
        var code = @"
extends Node

func process(callback: Callable):
    callback.call(42)

func test():
    process(func(x): return x * 2)
";
        var model = CreateSemanticModel(code);
        var registry = model.CallSiteRegistry;

        Assert.IsNotNull(registry);
        Assert.IsTrue(registry.AllMethodProfiles.Any(), "Should have method profiles");
        Assert.IsTrue(registry.AllArgumentBindings.Any(), "Should have argument bindings");
    }

    [TestMethod]
    public void Registry_GetCallSitesForMethodParameter()
    {
        var code = @"
extends Node

func process(callback: Callable):
    callback.call(42)
    callback.call(""hello"")

func test():
    process(func(x): return x)
";
        var model = CreateSemanticModel(code);
        var registry = model.CallSiteRegistry;

        // Get method profile
        var profile = registry.AllMethodProfiles.FirstOrDefault(p => p.MethodName == "process");
        Assert.IsNotNull(profile);

        // Get call sites via registry
        var callSites = registry.GetCallSitesForMethodParameter(profile.MethodKey, 0);
        Assert.AreEqual(2, callSites.Count);
    }

    #endregion

    #region Inter-Procedural Type Inference Tests

    [TestMethod]
    public void InterProcedural_LambdaParamInferredFromMethodCallSite()
    {
        var code = @"
extends Node

func process(callback: Callable):
    callback.call(42)

func test():
    process(func(x): return x * 2)
";
        var model = CreateSemanticModel(code);
        var classDecl = ParseClass(code);
        var lambda = FindLambda(classDecl);

        Assert.IsNotNull(lambda, "Lambda not found");

        // Use inter-procedural inference
        var inferredType = model.InferLambdaParameterTypeWithFlow(lambda, 0);

        Assert.IsNotNull(inferredType, "Should infer parameter type");
        Assert.AreEqual("int", inferredType, "Parameter x should be inferred as int from callback.call(42)");
    }

    [TestMethod]
    public void InterProcedural_MultipleCallSitesOnParameter()
    {
        var code = @"
extends Node

func process(callback: Callable):
    callback.call(1)
    callback.call(2)
    callback.call(3)

func test():
    process(func(x): return x * 2)
";
        var model = CreateSemanticModel(code);
        var classDecl = ParseClass(code);
        var lambda = FindLambda(classDecl);

        var inferredTypes = model.InferLambdaParameterTypesWithFlow(lambda);

        Assert.IsTrue(inferredTypes.ContainsKey(0), "Should have type for parameter 0");
        Assert.AreEqual("int", inferredTypes[0].EffectiveType);
    }

    [TestMethod]
    public void InterProcedural_MultipleParametersInferred()
    {
        var code = @"
extends Node

func process(callback: Callable):
    callback.call(42, ""hello"")

func test():
    process(func(x, y): return str(x) + y)
";
        var model = CreateSemanticModel(code);
        var classDecl = ParseClass(code);
        var lambda = FindLambda(classDecl);

        var inferredTypes = model.InferLambdaParameterTypesWithFlow(lambda);

        Assert.IsTrue(inferredTypes.ContainsKey(0), "Should have type for parameter 0");
        Assert.IsTrue(inferredTypes.ContainsKey(1), "Should have type for parameter 1");
        Assert.AreEqual("int", inferredTypes[0].EffectiveType, "First param should be int");
        Assert.AreEqual("String", inferredTypes[1].EffectiveType, "Second param should be String");
    }

    [TestMethod]
    public void InterProcedural_CombinesDirectAndInterProceduralCallSites()
    {
        var code = @"
extends Node

func process(callback: Callable):
    callback.call(42)

func test():
    var cb = func(x): return x * 2
    cb.call(100)      # Direct call site
    process(cb)       # Will also use callback.call(42) from process
";
        var model = CreateSemanticModel(code);
        var classDecl = ParseClass(code);
        var lambda = FindLambda(classDecl);

        var inferredType = model.InferLambdaParameterTypeWithFlow(lambda, 0);

        // Should get int from both direct (100) and inter-procedural (42) call sites
        Assert.IsNotNull(inferredType);
        Assert.AreEqual("int", inferredType);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void EdgeCase_NoCallSitesOnParameter()
    {
        var code = @"
extends Node

func process(callback: Callable):
    # Callback not called
    pass

func test():
    process(func(x): return x * 2)
";
        var model = CreateSemanticModel(code);
        var classDecl = ParseClass(code);
        var lambda = FindLambda(classDecl);

        var inferredType = model.InferLambdaParameterTypeWithFlow(lambda, 0);

        // No call sites, should return null (fallback to duck-typing or Variant)
        Assert.IsNull(inferredType, "No call sites means no inferred type from flow");
    }

    [TestMethod]
    public void EdgeCase_ParameterNotTypedAsCallable()
    {
        // When parameter is untyped, we can't reliably track it
        var code = @"
extends Node

func process(callback):
    callback.call(42)

func test():
    process(func(x): return x * 2)
";
        var model = CreateSemanticModel(code);

        // This is a known limitation - untyped parameters are not tracked
        // The flow collector only tracks explicitly typed Callable parameters
        var registry = model.CallSiteRegistry;
        var profiles = registry.AllMethodProfiles.ToList();

        // Should have no profiles because 'callback' is not typed as Callable
        Assert.AreEqual(0, profiles.Count, "Untyped parameters should not be tracked");
    }

    [TestMethod]
    public void EdgeCase_CallableV_ArrayArguments()
    {
        var code = @"
extends Node

func process(callback: Callable):
    var args = [42, ""hello""]
    callback.callv(args)

func test():
    process(func(x, y): return str(x) + y)
";
        var model = CreateSemanticModel(code);
        var registry = model.CallSiteRegistry;

        // callv is tracked but array argument types are harder to infer
        var profile = registry.AllMethodProfiles.FirstOrDefault(p => p.MethodName == "process");
        Assert.IsNotNull(profile);

        var callSites = profile.GetCallSitesForParameter("callback");
        Assert.AreEqual(1, callSites.Count);
        Assert.IsTrue(callSites[0].IsCallV, "Should detect callv");
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
