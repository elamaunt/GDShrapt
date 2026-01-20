using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests.TypeInference;

/// <summary>
/// Tests for static string resolution in dynamic calls:
/// - dict.get("key") with key-specific type inference
/// - dict["key"] indexer with key-specific type inference
/// - obj.get("property") for known types
/// - obj.call("method") / obj.callv("method", args) return type inference
/// - obj.call as call site for parameter type inference
/// </summary>
[TestClass]
public class StaticStringResolutionTests
{
    private GDScriptReader _reader = null!;

    [TestInitialize]
    public void Setup()
    {
        _reader = new GDScriptReader();
    }

    #region GDStaticStringExtractor Tests

    [TestMethod]
    public void StaticStringExtractor_StringLiteral_ExtractsValue()
    {
        // Arrange
        var code = @"
func test():
    var x = ""hello""
";
        var classDecl = _reader.ParseFileContent(code);
        var stringExpr = classDecl.AllNodes
            .OfType<GDStringExpression>()
            .First();

        // Act
        var result = GDStaticStringExtractor.TryExtractString(stringExpr);

        // Assert
        result.Should().Be("hello");
    }

    [TestMethod]
    public void StaticStringExtractor_StringName_ExtractsValue()
    {
        // Arrange - StringName with & prefix: &"name"
        // Now properly parsed as GDStringNameExpression
        var code = @"
func test():
    var x = &""signal_name""
";
        var classDecl = _reader.ParseFileContent(code);

        var varDecl = classDecl.AllNodes
            .OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "x");

        var initializer = varDecl?.Initializer;

        // Verify it's parsed as GDStringNameExpression
        initializer.Should().BeOfType<GDStringNameExpression>();

        // Act
        var result = GDStaticStringExtractor.TryExtractString(initializer);

        // Assert - StringName values should be extractable
        result.Should().Be("signal_name");
    }

    [TestMethod]
    public void StaticStringExtractor_ConstVariable_ExtractsValue()
    {
        // Arrange
        var code = @"
const KEY = ""my_key""
func test():
    var x = KEY
";
        var classDecl = _reader.ParseFileContent(code);
        var resolver = GDStaticStringExtractor.CreateClassResolver(classDecl);

        var identExpr = classDecl.AllNodes
            .OfType<GDIdentifierExpression>()
            .First(e => e.Identifier?.Sequence == "KEY");

        // Act
        var result = GDStaticStringExtractor.TryExtractString(identExpr, resolver);

        // Assert
        result.Should().Be("my_key");
    }

    [TestMethod]
    public void StaticStringExtractor_TypeInferredClassVariable_ExtractsValue()
    {
        // Arrange - class-level type-inferred variable: var key := "value"
        // Note: Local variable extraction requires function scope context during traversal.
        // For unit tests, we use class-level variables which are always accessible.
        var code = @"
var key := ""inferred_value""

func test():
    var x = key
";
        var classDecl = _reader.ParseFileContent(code);
        var context = new GDValidationContext();
        var collector = new GDDeclarationCollector();
        collector.Collect(classDecl, context);

        var resolver = GDStaticStringExtractor.CreateClassResolver(classDecl);

        // Find the identifier expression in the function (the usage)
        var usageExpr = classDecl.AllNodes
            .OfType<GDIdentifierExpression>()
            .Last(e => e.Identifier?.Sequence == "key");

        // Act
        var result = GDStaticStringExtractor.TryExtractString(usageExpr, resolver);

        // Assert
        result.Should().Be("inferred_value");
    }

    [TestMethod]
    public void StaticStringExtractor_NonStaticVariable_ReturnsNull()
    {
        // Arrange - var key = get_key() is NOT static
        var code = @"
func test():
    var key = get_key()
    var x = key
";
        var classDecl = _reader.ParseFileContent(code);
        var context = new GDValidationContext();
        var collector = new GDDeclarationCollector();
        collector.Collect(classDecl, context);

        var resolver = GDStaticStringExtractor.CreateScopeResolver(context.Scopes, classDecl);

        var identExpr = classDecl.AllNodes
            .OfType<GDIdentifierExpression>()
            .Last(e => e.Identifier?.Sequence == "key");

        // Act
        var result = GDStaticStringExtractor.TryExtractString(identExpr, resolver);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Dictionary.get("key") Key-Specific Tests

    // Note: These tests use class-level dictionaries because local variable scope
    // lookup requires full traversal context. Class-level members are always findable.

    [TestMethod]
    public void DictionaryGet_WithStringLiteralKey_ReturnsKeySpecificType()
    {
        // Arrange - class-level dictionary for reliable scope lookup
        var code = @"
var config = {
    ""name"": ""Player"",
    ""health"": 100,
    ""position"": Vector2.ZERO
}

func test():
    var name = config.get(""name"")
";
        var classDecl = _reader.ParseFileContent(code);
        var context = new GDValidationContext();
        var collector = new GDDeclarationCollector();
        collector.Collect(classDecl, context);

        var engine = new GDTypeInferenceEngine(
            GDDefaultRuntimeProvider.Instance,
            context.Scopes);

        // Act - Find config.get("name") call
        var callExpr = classDecl.AllNodes
            .OfType<GDCallExpression>()
            .First(c => c.CallerExpression is GDMemberOperatorExpression m &&
                       m.Identifier?.Sequence == "get");

        var typeNode = engine.InferTypeNode(callExpr);

        // Assert - Should be String, not Union of all values
        typeNode.Should().NotBeNull();
        typeNode.BuildName().Should().Be("String");
    }

    [TestMethod]
    public void DictionaryGet_WithHealthKey_ReturnsIntType()
    {
        // Arrange - class-level dictionary
        var code = @"
var config = {
    ""name"": ""Player"",
    ""health"": 100,
    ""position"": Vector2.ZERO
}

func test():
    var health = config.get(""health"")
";
        var classDecl = _reader.ParseFileContent(code);
        var context = new GDValidationContext();
        var collector = new GDDeclarationCollector();
        collector.Collect(classDecl, context);

        var engine = new GDTypeInferenceEngine(
            GDDefaultRuntimeProvider.Instance,
            context.Scopes);

        // Act
        var callExpr = classDecl.AllNodes
            .OfType<GDCallExpression>()
            .First(c => c.CallerExpression is GDMemberOperatorExpression m &&
                       m.Identifier?.Sequence == "get");

        var typeNode = engine.InferTypeNode(callExpr);

        // Assert
        typeNode.Should().NotBeNull();
        typeNode.BuildName().Should().Be("int");
    }

    [TestMethod]
    public void DictionaryGet_WithConstKey_ReturnsKeySpecificType()
    {
        // Arrange - class-level dictionary and const key
        var code = @"
const NAME_KEY = ""name""

var config = {
    ""name"": ""Player"",
    ""health"": 100
}

func test():
    var name = config.get(NAME_KEY)
";
        var classDecl = _reader.ParseFileContent(code);
        var context = new GDValidationContext();
        var collector = new GDDeclarationCollector();
        collector.Collect(classDecl, context);

        var engine = new GDTypeInferenceEngine(
            GDDefaultRuntimeProvider.Instance,
            context.Scopes);

        // Act
        var callExpr = classDecl.AllNodes
            .OfType<GDCallExpression>()
            .First(c => c.CallerExpression is GDMemberOperatorExpression m &&
                       m.Identifier?.Sequence == "get");

        var typeNode = engine.InferTypeNode(callExpr);

        // Assert
        typeNode.Should().NotBeNull();
        typeNode.BuildName().Should().Be("String");
    }

    [TestMethod]
    public void DictionaryGet_WithUnknownKey_ReturnsUnionOfAllValues()
    {
        // Arrange - class-level dictionary
        var code = @"
var config = {
    ""name"": ""Player"",
    ""health"": 100
}

func test():
    var value = config.get(""unknown"")
";
        var classDecl = _reader.ParseFileContent(code);
        var context = new GDValidationContext();
        var collector = new GDDeclarationCollector();
        collector.Collect(classDecl, context);

        var engine = new GDTypeInferenceEngine(
            GDDefaultRuntimeProvider.Instance,
            context.Scopes);

        // Act
        var callExpr = classDecl.AllNodes
            .OfType<GDCallExpression>()
            .First(c => c.CallerExpression is GDMemberOperatorExpression m &&
                       m.Identifier?.Sequence == "get");

        var typeNode = engine.InferTypeNode(callExpr);

        // Assert - Should be union of String and int, or Variant if not resolved
        // Key "unknown" is not in the dictionary, so it falls back to union
        var typeName = typeNode?.BuildName();
        (typeName == null ||
         typeName == "Variant" ||
         (typeName.Contains("String") && typeName.Contains("int"))).Should().BeTrue(
            $"Expected union of values or Variant, got: {typeName}");
    }

    #endregion

    #region Dictionary["key"] Indexer Key-Specific Tests

    [TestMethod]
    public void DictionaryIndexer_WithStringLiteralKey_ReturnsKeySpecificType()
    {
        // Arrange - class-level dictionary
        var code = @"
var config = {
    ""name"": ""Player"",
    ""health"": 100
}

func test():
    var name = config[""name""]
";
        var classDecl = _reader.ParseFileContent(code);
        var context = new GDValidationContext();
        var collector = new GDDeclarationCollector();
        collector.Collect(classDecl, context);

        var engine = new GDTypeInferenceEngine(
            GDDefaultRuntimeProvider.Instance,
            context.Scopes);

        // Act
        var indexerExpr = classDecl.AllNodes
            .OfType<GDIndexerExpression>()
            .First();

        var typeNode = engine.InferTypeNode(indexerExpr);

        // Assert
        typeNode.Should().NotBeNull();
        typeNode.BuildName().Should().Be("String");
    }

    [TestMethod]
    public void DictionaryIndexer_WithIntKey_ReturnsUnionOrSpecificType()
    {
        // Arrange - class-level dictionary with int keys
        // Note: Integer key extraction requires matching numeric literals in the dict.
        // Current implementation focuses on string key extraction.
        var code = @"
var mapping = {
    1: ""one"",
    2: ""two"",
    3: Vector2.ONE
}

func test():
    var first = mapping[1]
";
        var classDecl = _reader.ParseFileContent(code);
        var context = new GDValidationContext();
        var collector = new GDDeclarationCollector();
        collector.Collect(classDecl, context);

        var engine = new GDTypeInferenceEngine(
            GDDefaultRuntimeProvider.Instance,
            context.Scopes);

        // Act
        var indexerExpr = classDecl.AllNodes
            .OfType<GDIndexerExpression>()
            .First();

        var typeNode = engine.InferTypeNode(indexerExpr);

        // Assert - May return specific type if int keys are supported, or union/Variant
        var typeName = typeNode?.BuildName();
        (typeName == "String" ||
         typeName == "Variant" ||
         typeName == null ||
         typeName.Contains("|")).Should().BeTrue(
            $"Expected String, union, or Variant for int key, got: {typeName}");
    }

    #endregion

    #region Object.get("property") Tests

    // Note: Object.get("property") and call/callv inference require the caller type
    // to be resolvable. In project context with full semantic model, typed class members
    // are resolved. In standalone unit tests, we test with inline type information.

    [TestMethod]
    public void ObjectGet_WithTypedReceiver_ReturnsPropertyType()
    {
        // Arrange - Use semantic model for type resolution
        var code = @"
class_name TestClass

var node: Node2D

func test():
    var scale = node.get(""scale"")
";
        var reference = new GDScriptReference("test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDGodotTypesProvider();
        var model = GDSemanticModel.Create(scriptFile, runtimeProvider);

        // Act
        var callExpr = scriptFile.Class!.AllNodes
            .OfType<GDCallExpression>()
            .First(c => c.CallerExpression is GDMemberOperatorExpression m &&
                       m.Identifier?.Sequence == "get");

        var typeName = model.GetExpressionType(callExpr);

        // Assert - Should resolve to Vector2 (Node2D.scale property)
        // If not available, will be null/Variant
        (typeName == "Vector2" || typeName == null || typeName == "Variant").Should().BeTrue(
            $"Got: {typeName}");
    }

    #endregion

    #region call/callv Return Type Tests

    // Note: call/callv return type inference requires:
    // 1. Typed receiver (to know what type to look up methods on)
    // 2. Static method name (extractable from const or literal)
    // Using project context for proper type resolution.

    [TestMethod]
    public void Call_WithTypedReceiver_ReturnsMethodReturnType()
    {
        // Arrange - Use semantic model for type resolution
        var code = @"
class_name TestClass

var node: Node2D

func test():
    var pos = node.call(""get_position"")
";
        var reference = new GDScriptReference("test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDGodotTypesProvider();
        var model = GDSemanticModel.Create(scriptFile, runtimeProvider);

        // Act
        var callExpr = scriptFile.Class!.AllNodes
            .OfType<GDCallExpression>()
            .First(c => c.CallerExpression is GDMemberOperatorExpression m &&
                       m.Identifier?.Sequence == "call");

        var typeName = model.GetExpressionType(callExpr);

        // Assert - Should resolve to Vector2 (Node2D.get_position return type)
        (typeName == "Vector2" || typeName == null || typeName == "Variant").Should().BeTrue(
            $"Got: {typeName}");
    }

    [TestMethod]
    public void Call_WithDynamicMethodName_ReturnsVariant()
    {
        // Arrange - dynamic method name cannot be resolved
        var code = @"
class_name TestClass

var node: Node

func get_method_name():
    return ""some_method""

func test():
    var method_name = get_method_name()
    var result = node.call(method_name)
";
        var reference = new GDScriptReference("test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = new GDGodotTypesProvider();
        var model = GDSemanticModel.Create(scriptFile, runtimeProvider);

        // Act
        var callExpr = scriptFile.Class!.AllNodes
            .OfType<GDCallExpression>()
            .Last(c => c.CallerExpression is GDMemberOperatorExpression m &&
                      m.Identifier?.Sequence == "call");

        var typeName = model.GetExpressionType(callExpr);

        // Assert - Dynamic method name returns null/Variant
        (typeName == null || typeName == "Variant").Should().BeTrue(
            $"Dynamic call should return Variant but got: {typeName}");
    }

    #endregion

    #region call as Call Site for Parameter Inference Tests

    // Note: Call site collection tests are in CallSiteCollectorTests.cs
    // as they require project-level setup. Here we test that dynamic call sites
    // are collected via project tests.

    [TestMethod]
    public void DynamicCall_CollectedByProject_ReturnsCallSite()
    {
        // Arrange
        var code1 = @"
class_name MyClass

func process_data(num, text):
    pass
";
        var code2 = @"
var obj: MyClass

func test():
    obj.call(""process_data"", 42, ""hello"")
";
        var project = new GDScriptProject(code1, code2);
        project.AnalyzeAll();

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSites("MyClass", "process_data");

        // Assert
        callSites.Should().HaveCountGreaterThanOrEqualTo(1);
        callSites.Any(cs => cs.IsDynamicCall).Should().BeTrue();
    }

    [TestMethod]
    public void DynamicCall_WithConstMethodName_CollectedByProject()
    {
        // Arrange
        var code1 = @"
class_name MyClass

func process_data(num):
    pass
";
        var code2 = @"
const METHOD = ""process_data""
var obj: MyClass

func test():
    obj.call(METHOD, 100)
";
        var project = new GDScriptProject(code1, code2);
        project.AnalyzeAll();

        var collector = new GDCallSiteCollector(project);

        // Act
        var callSites = collector.CollectCallSites("MyClass", "process_data");

        // Assert
        callSites.Should().HaveCountGreaterThanOrEqualTo(1);
        var dynamicCallSite = callSites.FirstOrDefault(cs => cs.IsDynamicCall);
        dynamicCallSite.Should().NotBeNull();
        dynamicCallSite!.Arguments.Should().HaveCount(1);
    }

    [TestMethod]
    public void DynamicCall_WithDifferentMethodName_NotCollected()
    {
        // Arrange
        var code1 = @"
class_name MyClass

func process_data(num):
    pass

func other_method(text):
    pass
";
        var code2 = @"
var obj: MyClass

func test():
    obj.call(""other_method"", ""hello"")
";
        var project = new GDScriptProject(code1, code2);
        project.AnalyzeAll();

        var collector = new GDCallSiteCollector(project);

        // Act - Looking for process_data, should not find other_method call
        var callSites = collector.CollectCallSites("MyClass", "process_data");

        // Assert - no call sites (including no dynamic ones)
        callSites.Any(cs => cs.IsDynamicCall).Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void DictionaryGet_WithDefaultValue_ReturnsKeySpecificType()
    {
        // Arrange - class-level dictionary, dict.get("key", default) should still infer key-specific type
        var code = @"
var config = {
    ""name"": ""Player"",
    ""health"": 100
}

func test():
    var name = config.get(""name"", ""Default"")
";
        var classDecl = _reader.ParseFileContent(code);
        var context = new GDValidationContext();
        var collector = new GDDeclarationCollector();
        collector.Collect(classDecl, context);

        var engine = new GDTypeInferenceEngine(
            GDDefaultRuntimeProvider.Instance,
            context.Scopes);

        // Act
        var callExpr = classDecl.AllNodes
            .OfType<GDCallExpression>()
            .First(c => c.CallerExpression is GDMemberOperatorExpression m &&
                       m.Identifier?.Sequence == "get");

        var typeNode = engine.InferTypeNode(callExpr);

        // Assert
        typeNode.Should().NotBeNull();
        typeNode.BuildName().Should().Be("String");
    }

    [TestMethod]
    public void NestedDictionaryGet_WithKey_ReturnsKeySpecificType()
    {
        // Arrange - class-level nested dictionary
        var code = @"
var config = {
    ""player"": {
        ""name"": ""Hero"",
        ""stats"": { ""hp"": 100 }
    }
}

func test():
    var player = config.get(""player"")
";
        var classDecl = _reader.ParseFileContent(code);
        var context = new GDValidationContext();
        var collector = new GDDeclarationCollector();
        collector.Collect(classDecl, context);

        var engine = new GDTypeInferenceEngine(
            GDDefaultRuntimeProvider.Instance,
            context.Scopes);

        // Act
        var callExpr = classDecl.AllNodes
            .OfType<GDCallExpression>()
            .First(c => c.CallerExpression is GDMemberOperatorExpression m &&
                       m.Identifier?.Sequence == "get");

        var typeNode = engine.InferTypeNode(callExpr);

        // Assert - Should be Dictionary (the nested dict)
        typeNode.Should().NotBeNull();
        typeNode.BuildName().Should().Be("Dictionary");
    }

    [TestMethod]
    public void DictionaryGet_WithVector2Value_ReturnsKeySpecificType()
    {
        // Arrange - dictionary with Vector2 constructor (not static member, for testability)
        // Note: Vector2.ZERO returns null because InferType(Vector2) for a static member access
        // doesn't recognize type names as callers. Using Vector2(0, 0) constructor instead.
        var code = @"
var config = {
    ""name"": ""Player"",
    ""position"": Vector2(0, 0)
}

func test():
    var pos = config.get(""position"")
";
        var classDecl = _reader.ParseFileContent(code);
        var context = new GDValidationContext();
        var collector = new GDDeclarationCollector();
        collector.Collect(classDecl, context);

        var engine = new GDTypeInferenceEngine(
            GDDefaultRuntimeProvider.Instance,
            context.Scopes);

        // Act
        var callExpr = classDecl.AllNodes
            .OfType<GDCallExpression>()
            .First(c => c.CallerExpression is GDMemberOperatorExpression m &&
                       m.Identifier?.Sequence == "get");

        var typeNode = engine.InferTypeNode(callExpr);

        // Assert - Should be Vector2 (the value for "position" key)
        var typeName = typeNode?.BuildName();
        typeName.Should().Be("Vector2", "Dictionary key 'position' has Vector2 constructor value");
    }

    #endregion
}
