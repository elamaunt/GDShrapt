using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Tests that duck-type inference correctly filters false positive types
/// based on per-member confidence scoring (argument mismatch, singleton filter, container affinity).
/// </summary>
[TestClass]
[TestCategory("ManualVerification")]
public class DuckTypeInferenceFPTests
{
    /// <summary>
    /// Rule 1: .has("string_literal") should exclude types whose .has() parameter
    /// doesn't accept String (e.g., TextServer.has(Rid)).
    /// </summary>
    [TestMethod]
    public void HasWithStringArg_ShouldNotIncludeTextServer()
    {
        var code = @"
func process(data):
    if data.has(""key""):
        return data.get(""key"")
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var param = GetParameter(classDecl, "process", "data");
        var inferred = model.InferParameterType(param);

        Assert.IsTrue(inferred.IsDuckTyped, "Should be duck-typed");

        var types = (inferred.UnionTypes ?? new[] { inferred.TypeName }).Select(t => t.DisplayName).ToList();
        CollectionAssert.DoesNotContain(types, "TextServer",
            $"TextServer should be excluded (has(Rid) doesn't accept String). Got: {inferred.TypeName.DisplayName}");
    }

    /// <summary>
    /// Rule 1: .has("string_literal") should exclude PackedArrays whose .has()
    /// parameter doesn't accept String (e.g., PackedColorArray.has(Color)).
    /// </summary>
    [TestMethod]
    public void HasWithStringArg_ShouldNotIncludeIncompatiblePackedArrays()
    {
        var code = @"
func process(data):
    if data.has(""key""):
        return data.get(""key"")
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var param = GetParameter(classDecl, "process", "data");
        var inferred = model.InferParameterType(param);

        var typeList = (inferred.UnionTypes ?? new[] { inferred.TypeName }).Select(t => t.DisplayName).ToList();

        // PackedColorArray.has(Color), PackedFloat32Array.has(float), etc. don't accept String
        CollectionAssert.DoesNotContain(typeList, "PackedColorArray",
            $"PackedColorArray should be excluded (has(Color) doesn't accept String). Got: {inferred.TypeName.DisplayName}");
        CollectionAssert.DoesNotContain(typeList, "PackedFloat32Array",
            $"PackedFloat32Array should be excluded (has(float) doesn't accept String). Got: {inferred.TypeName.DisplayName}");
        CollectionAssert.DoesNotContain(typeList, "PackedVector2Array",
            $"PackedVector2Array should be excluded (has(Vector2) doesn't accept String). Got: {inferred.TypeName.DisplayName}");

        // PackedStringArray.has(String) IS valid — should still be present
        Assert.IsTrue(
            typeList.Any(t => t.Contains("PackedStringArray") || t.Contains("Dictionary") || t.Contains("Array")),
            $"At least one valid container type should remain. Got: {inferred.TypeName.DisplayName}");
    }

    /// <summary>
    /// Rule 2: Godot singletons (TextServer, RenderingServer, etc.) should never
    /// appear in duck-typed unions because they are never passed as parameters.
    /// </summary>
    [TestMethod]
    public void DuckType_ShouldExcludeGodotSingletons()
    {
        var code = @"
func process(data):
    data.has(""key"")
    data.get(""key"")
    data.keys()
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var param = GetParameter(classDecl, "process", "data");
        var inferred = model.InferParameterType(param);

        var typeList = (inferred.UnionTypes ?? new[] { inferred.TypeName }).Select(t => t.DisplayName).ToList();

        CollectionAssert.DoesNotContain(typeList, "TextServer",
            $"TextServer (singleton) should be excluded. Got: {inferred.TypeName.DisplayName}");
        CollectionAssert.DoesNotContain(typeList, "RenderingServer",
            $"RenderingServer (singleton) should be excluded. Got: {inferred.TypeName.DisplayName}");
    }

    /// <summary>
    /// Rule 2: Internal types (PackedDataContainer, ScriptBacktrace) should never
    /// appear in duck-typed unions.
    /// </summary>
    [TestMethod]
    public void DuckType_ShouldExcludeInternalTypes()
    {
        var code = @"
func process(data):
    var s = data.size()
    for item in data:
        pass
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var param = GetParameter(classDecl, "process", "data");
        var inferred = model.InferParameterType(param);

        var typeList = (inferred.UnionTypes ?? new[] { inferred.TypeName }).Select(t => t.DisplayName).ToList();

        CollectionAssert.DoesNotContain(typeList, "PackedDataContainer",
            $"PackedDataContainer (internal) should be excluded. Got: {inferred.TypeName.DisplayName}");
        CollectionAssert.DoesNotContain(typeList, "PackedDataContainerRef",
            $"PackedDataContainerRef (internal) should be excluded. Got: {inferred.TypeName.DisplayName}");
        CollectionAssert.DoesNotContain(typeList, "ScriptBacktrace",
            $"ScriptBacktrace (internal) should be excluded. Got: {inferred.TypeName.DisplayName}");
    }

    /// <summary>
    /// Rule 1+3: .set("property", value) with String first arg should exclude
    /// PackedArrays whose .set() takes (int, T) not (String, Variant).
    /// Note: This test documents the FP — PackedArrays may still appear when
    /// argument types can't be inferred (parameter arguments default to Variant).
    /// </summary>
    [TestMethod]
    public void SetWithStringLiteral_ShouldNotIncludePackedArrays()
    {
        var code = @"
func process(obj):
    obj.set(""name"", ""value"")
    obj.get(""name"")
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var param = GetParameter(classDecl, "process", "obj");
        var inferred = model.InferParameterType(param);

        var typeList = (inferred.UnionTypes ?? new[] { inferred.TypeName }).Select(t => t.DisplayName).ToList();

        // PackedArrays .set(int, T) doesn't accept String as first arg
        CollectionAssert.DoesNotContain(typeList, "PackedColorArray",
            $"PackedColorArray should be excluded (.set(int, Color) doesn't accept String). Got: {inferred.TypeName.DisplayName}");
        CollectionAssert.DoesNotContain(typeList, "PackedByteArray",
            $"PackedByteArray should be excluded (.set(int, int) doesn't accept String). Got: {inferred.TypeName.DisplayName}");
    }

    /// <summary>
    /// Rule 3: Non-container types like Image, XMLParser, TileMapPattern
    /// should not appear in unions when all constraints are container methods.
    /// </summary>
    [TestMethod]
    public void ContainerMethods_ShouldExcludeNonContainers()
    {
        var code = @"
func process(data):
    if not data.is_empty():
        var chunk = data.slice(0, 10)
        return chunk.size()
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var param = GetParameter(classDecl, "process", "data");
        var inferred = model.InferParameterType(param);

        var typeList = (inferred.UnionTypes ?? new[] { inferred.TypeName }).Select(t => t.DisplayName).ToList();

        CollectionAssert.DoesNotContain(typeList, "Image",
            $"Image (non-container) should be excluded from container-method union. Got: {inferred.TypeName.DisplayName}");
        CollectionAssert.DoesNotContain(typeList, "XMLParser",
            $"XMLParser (non-container) should be excluded from container-method union. Got: {inferred.TypeName.DisplayName}");
        CollectionAssert.DoesNotContain(typeList, "TileMapPattern",
            $"TileMapPattern (non-container) should be excluded from container-method union. Got: {inferred.TypeName.DisplayName}");

        // Array should still be present
        Assert.IsTrue(typeList.Any(t => t.Contains("Array")),
            $"At least one Array type should remain. Got: {inferred.TypeName.DisplayName}");
    }

    /// <summary>
    /// Rule 4: .set(param_ref, value) where param_ref is another method parameter
    /// should exclude PackedArrays (whose .set() takes int, not StringName).
    /// Cross-parameter inference via GDCallArgInfo.
    /// </summary>
    [TestMethod]
    public void SetWithParamArg_ShouldNotIncludePackedArrays()
    {
        var code = @"
func process(obj, prop_name, value):
    obj.set(prop_name, value)
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var param = GetParameter(classDecl, "process", "obj");
        var inferred = model.InferParameterType(param);

        var typeList = (inferred.UnionTypes ?? new[] { inferred.TypeName }).Select(t => t.DisplayName).ToList();

        // PackedArrays .set(int, T) — ParameterRef as first arg + int expected → incompatible
        CollectionAssert.DoesNotContain(typeList, "PackedByteArray",
            $"PackedByteArray should be excluded (.set(int, int) incompatible with param ref). Got: {inferred.TypeName.DisplayName}");
        CollectionAssert.DoesNotContain(typeList, "PackedColorArray",
            $"PackedColorArray should be excluded (.set(int, Color) incompatible with param ref). Got: {inferred.TypeName.DisplayName}");

        // Object.set(StringName, Variant) should remain
        Assert.IsTrue(typeList.Contains("Object"),
            $"Object should remain (set(StringName, Variant) is compatible). Got: {inferred.TypeName.DisplayName}");
    }

    /// <summary>
    /// Rule 5: When Array + multiple PackedArrays are in a union and all required methods
    /// are common across them, PackedArrays should be deduplicated (collapsed to Array).
    /// </summary>
    [TestMethod]
    public void CommonArrayMethods_ShouldDeduplicatePackedArrays()
    {
        var code = @"
func process(data):
    if not data.is_empty():
        var first = data[0]
        return data.size()
";
        var (classDecl, model) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(model);

        var param = GetParameter(classDecl, "process", "data");
        var inferred = model.InferParameterType(param);

        var typeList = (inferred.UnionTypes ?? new[] { inferred.TypeName }).Select(t => t.DisplayName).ToList();

        // Array should be present
        Assert.IsTrue(typeList.Any(t => t.Contains("Array") && !t.Contains("Packed")),
            $"Array should be present. Got: {inferred.TypeName.DisplayName}");

        // Individual PackedArrays should be removed (deduplicated)
        var packedArrayCount = typeList.Count(t => t.Contains("Packed") && t.Contains("Array"));
        Assert.IsTrue(packedArrayCount < 2,
            $"PackedArrays should be deduplicated when using common methods. Found {packedArrayCount} PackedArray types. Got: {inferred.TypeName.DisplayName}");
    }

    #region Helpers

    private static GDParameterDeclaration GetParameter(
        GDClassDeclaration classDecl, string methodName, string paramName)
    {
        var method = classDecl.Methods
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);
        Assert.IsNotNull(method, $"Method '{methodName}' not found");

        var param = method.Parameters?
            .FirstOrDefault(p => p.Identifier?.Sequence == paramName);
        Assert.IsNotNull(param, $"Parameter '{paramName}' not found in method '{methodName}'");

        return param;
    }

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
            null, null, null);

        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        return (classDecl, semanticModel);
    }

    #endregion
}
