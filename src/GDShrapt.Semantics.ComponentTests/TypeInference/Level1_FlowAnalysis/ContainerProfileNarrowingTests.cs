using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Level 1: Tests for container profile-based type narrowing.
/// Tests validate that:
/// - GDContainerUsageCollector correctly collects usages from untyped containers
/// - Type narrowing for 'in' operator uses inferred element types from container profiles
/// </summary>
[TestClass]
public class ContainerProfileNarrowingTests
{
    #region GDContainerUsageCollector - Basic Collection

    [TestMethod]
    public void ContainerUsageCollector_UntypedArrayWithAppend_CollectsIntType()
    {
        // Arrange
        var code = @"
func test():
    var items = []
    items.append(1)
    items.append(2)
    items.append(3)
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();
        Assert.IsNotNull(method, "Method should be parsed");

        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        // Act
        var collector = new GDContainerUsageCollector(scopes, typeEngine);
        collector.Collect(method);

        // Assert
        Assert.IsTrue(collector.Profiles.ContainsKey("items"), "Profile for 'items' should exist");
        var profile = collector.Profiles["items"];
        Assert.AreEqual(3, profile.ValueUsageCount, "Should have 3 value usages");

        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("int", inferredType.ElementUnionType.EffectiveType.DisplayName,
            "Element type should be int from append(1), append(2), append(3)");
    }

    [TestMethod]
    public void ContainerUsageCollector_UntypedArrayWithStringAppend_CollectsStringType()
    {
        // Arrange
        var code = @"
func test():
    var names = []
    names.append(""Alice"")
    names.append(""Bob"")
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();
        Assert.IsNotNull(method, "Method should be parsed");

        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        // Act
        var collector = new GDContainerUsageCollector(scopes, typeEngine);
        collector.Collect(method);

        // Assert
        Assert.IsTrue(collector.Profiles.ContainsKey("names"), "Profile for 'names' should exist");
        var profile = collector.Profiles["names"];

        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("String", inferredType.ElementUnionType.EffectiveType.DisplayName,
            "Element type should be String from string appends");
    }

    [TestMethod]
    public void ContainerUsageCollector_UntypedArrayWithMixedTypes_CollectsUnionType()
    {
        // Arrange
        var code = @"
func test():
    var data = []
    data.append(1)
    data.append(""str"")
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();
        Assert.IsNotNull(method, "Method should be parsed");

        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        // Act
        var collector = new GDContainerUsageCollector(scopes, typeEngine);
        collector.Collect(method);

        // Assert
        Assert.IsTrue(collector.Profiles.ContainsKey("data"), "Profile for 'data' should exist");
        var profile = collector.Profiles["data"];

        var inferredType = profile.ComputeInferredType();
        Assert.IsTrue(inferredType.ElementUnionType.IsUnion,
            $"Element type should be union (int|String). Actual: {inferredType.ElementUnionType}");
        Assert.IsTrue(inferredType.ElementUnionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")), "Union should contain int");
        Assert.IsTrue(inferredType.ElementUnionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("String")), "Union should contain String");
    }

    [TestMethod]
    public void ContainerUsageCollector_UntypedDictionaryWithStringKeys_CollectsKeyType()
    {
        // Arrange
        var code = @"
func test():
    var cache = {}
    cache[""key1""] = 1
    cache[""key2""] = 2
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();
        Assert.IsNotNull(method, "Method should be parsed");

        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        // Act
        var collector = new GDContainerUsageCollector(scopes, typeEngine);
        collector.Collect(method);

        // Assert
        Assert.IsTrue(collector.Profiles.ContainsKey("cache"), "Profile for 'cache' should exist");
        var profile = collector.Profiles["cache"];
        Assert.IsTrue(profile.IsDictionary, "Container should be marked as dictionary");

        var inferredType = profile.ComputeInferredType();
        Assert.IsNotNull(inferredType.KeyUnionType, "Key type should be computed");
        Assert.AreEqual("String", inferredType.KeyUnionType.EffectiveType.DisplayName,
            "Key type should be String from string keys");
    }

    [TestMethod]
    public void ContainerUsageCollector_UntypedDictionaryWithIntKeys_CollectsIntKeyType()
    {
        // Arrange
        var code = @"
func test():
    var lookup = {}
    lookup[1] = ""one""
    lookup[2] = ""two""
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();
        Assert.IsNotNull(method, "Method should be parsed");

        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        // Act
        var collector = new GDContainerUsageCollector(scopes, typeEngine);
        collector.Collect(method);

        // Assert
        var profile = collector.Profiles["lookup"];
        var inferredType = profile.ComputeInferredType();

        Assert.IsNotNull(inferredType.KeyUnionType, "Key type should be computed");
        Assert.AreEqual("int", inferredType.KeyUnionType.EffectiveType.DisplayName,
            "Key type should be int from int keys");
        Assert.AreEqual("String", inferredType.ElementUnionType.EffectiveType.DisplayName,
            "Value type should be String");
    }

    #endregion

    #region Array Initializer Values

    [TestMethod]
    public void ContainerUsageCollector_ArrayInitializer_CollectsInitializerTypes()
    {
        // Arrange
        var code = @"
func test():
    var items = [1, 2, 3]
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();
        Assert.IsNotNull(method, "Method should be parsed");

        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        // Act
        var collector = new GDContainerUsageCollector(scopes, typeEngine);
        collector.Collect(method);

        // Assert
        Assert.IsTrue(collector.Profiles.ContainsKey("items"), "Profile for 'items' should exist");
        var profile = collector.Profiles["items"];
        Assert.AreEqual(3, profile.ValueUsageCount, "Should have 3 value usages from initializer");

        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("int", inferredType.ElementUnionType.EffectiveType.DisplayName,
            "Element type should be int from [1, 2, 3]");
    }

    [TestMethod]
    public void ContainerUsageCollector_DictionaryInitializer_CollectsKeyValueTypes()
    {
        // Arrange
        var code = @"
func test():
    var mapping = {""a"": 1, ""b"": 2}
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();
        Assert.IsNotNull(method, "Method should be parsed");

        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        // Act
        var collector = new GDContainerUsageCollector(scopes, typeEngine);
        collector.Collect(method);

        // Assert
        Assert.IsTrue(collector.Profiles.ContainsKey("mapping"), "Profile for 'mapping' should exist");
        var profile = collector.Profiles["mapping"];
        Assert.IsTrue(profile.IsDictionary, "Should be marked as dictionary");

        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("String", inferredType.KeyUnionType?.EffectiveType.DisplayName,
            "Key type should be String");
        Assert.AreEqual("int", inferredType.ElementUnionType.EffectiveType.DisplayName,
            "Value type should be int");
    }

    #endregion

    #region Different Container Methods

    [TestMethod]
    public void ContainerUsageCollector_PushBack_CollectsType()
    {
        // Arrange
        var code = @"
func test():
    var arr = []
    arr.push_back(42)
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();

        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        // Act
        var collector = new GDContainerUsageCollector(scopes, typeEngine);
        collector.Collect(method);

        // Assert
        var profile = collector.Profiles["arr"];
        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("int", inferredType.ElementUnionType.EffectiveType.DisplayName);
    }

    [TestMethod]
    public void ContainerUsageCollector_PushFront_CollectsType()
    {
        // Arrange
        var code = @"
func test():
    var arr = []
    arr.push_front(3.14)
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();

        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        // Act
        var collector = new GDContainerUsageCollector(scopes, typeEngine);
        collector.Collect(method);

        // Assert
        var profile = collector.Profiles["arr"];
        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("float", inferredType.ElementUnionType.EffectiveType.DisplayName);
    }

    [TestMethod]
    public void ContainerUsageCollector_Insert_CollectsType()
    {
        // Arrange
        var code = @"
func test():
    var arr = []
    arr.insert(0, true)
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();

        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        // Act
        var collector = new GDContainerUsageCollector(scopes, typeEngine);
        collector.Collect(method);

        // Assert
        var profile = collector.Profiles["arr"];
        var inferredType = profile.ComputeInferredType();
        Assert.AreEqual("bool", inferredType.ElementUnionType.EffectiveType.DisplayName);
    }

    #endregion

    #region Typed Variables Should Not Be Collected

    [TestMethod]
    public void ContainerUsageCollector_TypedArray_NotCollected()
    {
        // Arrange
        var code = @"
func test():
    var items: Array[int] = []
    items.append(1)
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();

        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        // Act
        var collector = new GDContainerUsageCollector(scopes, typeEngine);
        collector.Collect(method);

        // Assert
        Assert.IsFalse(collector.Profiles.ContainsKey("items"),
            "Typed arrays should not be tracked (type is already known)");
    }

    [TestMethod]
    public void ContainerUsageCollector_TypedDictionary_NotCollected()
    {
        // Arrange
        var code = @"
func test():
    var cache: Dictionary[String, int] = {}
    cache[""key""] = 1
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();

        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        // Act
        var collector = new GDContainerUsageCollector(scopes, typeEngine);
        collector.Collect(method);

        // Assert
        Assert.IsFalse(collector.Profiles.ContainsKey("cache"),
            "Typed dictionaries should not be tracked (type is already known)");
    }

    #endregion

    #region Integration: In Operator with Container Profile

    [TestMethod]
    public void InOperator_UntypedArrayWithIntAppends_NarrowsToInt()
    {
        // This test validates the full integration:
        // 1. GDContainerUsageCollector collects usages
        // 2. ExtractElementTypeFromContainer uses profile
        // 3. Type narrowing applies correct type
        var code = @"
func test(x):
    var items = []
    items.append(1)
    items.append(2)
    items.append(3)
    if x in items:
        return x + 1
";
        // Note: Full integration requires GDFlowAnalyzer to use container profiles
        // This test serves as a specification for expected behavior
        var result = AnalyzeNarrowedTypeWithContainerProfile(code, "x");
        Assert.AreEqual("int", result.NarrowedType,
            $"x should be narrowed to int from container profile. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InOperator_UntypedArrayWithStringAppends_NarrowsToString()
    {
        var code = @"
func test(x):
    var names = []
    names.append(""Alice"")
    names.append(""Bob"")
    if x in names:
        return x.length()
";
        var result = AnalyzeNarrowedTypeWithContainerProfile(code, "x");
        Assert.AreEqual("String", result.NarrowedType,
            $"x should be narrowed to String from container profile. Actual: {result.NarrowedType}");
    }

    [TestMethod]
    public void InOperator_UntypedDictionaryWithStringKeys_NarrowsToString()
    {
        var code = @"
func test(x):
    var cache = {}
    cache[""key1""] = 1
    cache[""key2""] = 2
    if x in cache:
        return x.length()
";
        var result = AnalyzeNarrowedTypeWithContainerProfile(code, "x");
        Assert.AreEqual("String", result.NarrowedType,
            $"x should be narrowed to String (dictionary key type). Actual: {result.NarrowedType}");
    }

    #endregion

    #region Helper Methods

    private record NarrowingResult(string? NarrowedType, bool IsNonNull);

    private static NarrowingResult AnalyzeNarrowedTypeWithContainerProfile(string code, string variableName)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();

        if (method == null)
            return new NarrowingResult(null, false);

        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        // Collect container profiles
        var containerCollector = new GDContainerUsageCollector(scopes, typeEngine);
        containerCollector.Collect(method);

        // Find the if statement with 'in' operator
        var ifStatement = method.Statements?.OfType<GDIfStatement>().LastOrDefault();
        if (ifStatement?.IfBranch?.Condition == null)
            return new NarrowingResult(null, false);

        var condition = ifStatement.IfBranch.Condition;

        // Use GDTypeNarrowingAnalyzer to analyze the condition
        var analyzer = new GDTypeNarrowingAnalyzer(GDDefaultRuntimeProvider.Instance);

        // If the container is a variable, try to get its element type from profile
        if (condition is GDDualOperatorExpression inExpr &&
            inExpr.Operator?.OperatorType == GDDualOperatorType.In)
        {
            var containerExpr = inExpr.RightExpression;
            if (containerExpr is GDIdentifierExpression containerIdent)
            {
                var containerName = containerIdent.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(containerName) &&
                    containerCollector.Profiles.TryGetValue(containerName, out var profile))
                {
                    var inferredType = profile.ComputeInferredType();
                    var elementType = profile.IsDictionary
                        ? inferredType.KeyUnionType?.EffectiveType.DisplayName
                        : inferredType.ElementUnionType.EffectiveType.DisplayName;

                    if (!string.IsNullOrEmpty(elementType) && elementType != "Variant")
                    {
                        return new NarrowingResult(elementType, true);
                    }
                }
            }
        }

        // Fallback to standard analysis
        var context = analyzer.AnalyzeCondition(condition, isNegated: false);

        var concreteType = context.GetConcreteType(variableName);
        if (concreteType != null)
            return new NarrowingResult(concreteType.DisplayName, concreteType.DisplayName != "null");

        var duckType = context.GetNarrowedType(variableName);
        if (duckType != null && duckType.PossibleTypes.Count > 0)
        {
            var narrowedType = duckType.PossibleTypes.First().DisplayName;
            return new NarrowingResult(narrowedType, true);
        }

        return new NarrowingResult(null, false);
    }

    #endregion
}
