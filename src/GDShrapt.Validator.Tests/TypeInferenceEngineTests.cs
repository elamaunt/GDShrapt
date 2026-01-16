using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for GDTypeInferenceEngine: type inference for expressions.
    /// Specifically tests handling of generic types like Array[T] and Dictionary[K,V].
    /// </summary>
    [TestClass]
    public class TypeInferenceEngineTests
    {
        private GDScriptReader _reader;

        [TestInitialize]
        public void Setup()
        {
            _reader = new GDScriptReader();
        }

        #region Array[T] Index Access Tests

        [TestMethod]
        public void InferType_TypedArrayIndexer_ReturnsElementType()
        {
            // Arrange
            var code = @"
var arr: Array[int] = [1, 2, 3]
func test():
    var x = arr[0]
";
            var classDecl = _reader.ParseFileContent(code);
            var context = new GDValidationContext();
            var collector = new GDDeclarationCollector();
            collector.Collect(classDecl, context);

            var engine = new GDTypeInferenceEngine(
                GDDefaultRuntimeProvider.Instance,
                context.Scopes);

            // Act: Find the indexer expression and infer its type
            var indexerExpr = classDecl.AllNodes
                .OfType<GDIndexerExpression>()
                .First();
            var typeNode = engine.InferTypeNode(indexerExpr);

            // Assert
            typeNode.Should().NotBeNull();
            typeNode.BuildName().Should().Be("int");
        }

        [TestMethod]
        public void InferType_TypedArrayStringIndexer_ReturnsElementType()
        {
            // Arrange
            var code = @"
var names: Array[String] = [""Alice"", ""Bob""]
func test():
    var name = names[0]
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
        public void InferType_NestedArrayIndexer_ReturnsInnerArrayType()
        {
            // Arrange
            var code = @"
var matrix: Array[Array[int]] = [[1, 2], [3, 4]]
func test():
    var row = matrix[0]
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
            typeNode.BuildName().Should().Be("Array[int]");
        }

        #endregion

        #region Dictionary[K,V] Index Access Tests

        [TestMethod]
        public void InferType_TypedDictionaryIndexer_ReturnsValueType()
        {
            // Arrange
            var code = @"
var dict: Dictionary[String, int] = {""a"": 1, ""b"": 2}
func test():
    var value = dict[""a""]
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
            typeNode.BuildName().Should().Be("int");
        }

        [TestMethod]
        public void InferType_TypedDictionaryWithArrayValue_ReturnsArrayType()
        {
            // Arrange
            var code = @"
var dict: Dictionary[String, Array[int]] = {""nums"": [1, 2, 3]}
func test():
    var arr = dict[""nums""]
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
            typeNode.BuildName().Should().Be("Array[int]");
        }

        #endregion

        #region Untyped Container Tests

        [TestMethod]
        public void InferType_UntypedArrayIndexer_ReturnsVariant()
        {
            // Arrange: Untyped array returns Variant
            var code = @"
var arr: Array = [1, ""two"", 3.0]
func test():
    var x = arr[0]
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

            // Assert: Untyped containers return Variant
            typeNode.Should().NotBeNull();
            typeNode!.BuildName().Should().Be("Variant");
        }

        [TestMethod]
        public void InferType_UntypedDictionaryIndexer_ReturnsVariant()
        {
            // Arrange: Untyped dictionary returns Variant
            var code = @"
var dict: Dictionary = {""a"": 1}
func test():
    var x = dict[""a""]
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

            // Assert: Untyped containers return Variant
            typeNode.Should().NotBeNull();
            typeNode!.BuildName().Should().Be("Variant");
        }

        #endregion

        #region CreateSimpleType Does Not Throw Tests

        [TestMethod]
        public void CreateSimpleType_GenericArrayType_DoesNotThrow()
        {
            // This test verifies that the fix for "Invalid identifier format" works.
            // Before the fix, this would throw ArgumentException.

            // Arrange
            var code = @"
var arr: Array[int] = [1, 2, 3]
func test():
    var x = arr[0]
";
            var classDecl = _reader.ParseFileContent(code);
            var context = new GDValidationContext();
            var collector = new GDDeclarationCollector();
            collector.Collect(classDecl, context);

            // Act & Assert: Declaration collection should not throw
            var act = () => collector.Collect(classDecl, context);
            act.Should().NotThrow<System.ArgumentException>();

            // Also verify type inference engine works
            var engine = new GDTypeInferenceEngine(
                GDDefaultRuntimeProvider.Instance,
                context.Scopes);
            var indexerExpr = classDecl.AllNodes
                .OfType<GDIndexerExpression>()
                .First();
            var inferAct = () => engine.InferTypeNode(indexerExpr);
            inferAct.Should().NotThrow<System.ArgumentException>();
        }

        [TestMethod]
        public void CreateSimpleType_GenericDictionaryType_DoesNotThrow()
        {
            // Arrange
            var code = @"
var dict: Dictionary[String, int] = {""a"": 1}
func test():
    var x = dict[""a""]
";
            var classDecl = _reader.ParseFileContent(code);
            var context = new GDValidationContext();
            var collector = new GDDeclarationCollector();

            // Act & Assert: Declaration collection should not throw
            var act = () => collector.Collect(classDecl, context);
            act.Should().NotThrow<System.ArgumentException>();

            // Also verify type inference engine works
            collector.Collect(classDecl, context);
            var engine = new GDTypeInferenceEngine(
                GDDefaultRuntimeProvider.Instance,
                context.Scopes);
            var indexerExpr = classDecl.AllNodes
                .OfType<GDIndexerExpression>()
                .First();
            var inferAct = () => engine.InferTypeNode(indexerExpr);
            inferAct.Should().NotThrow<System.ArgumentException>();
        }

        [TestMethod]
        public void CreateSimpleType_NestedGenericType_DoesNotThrow()
        {
            // Arrange
            var code = @"
var complex: Dictionary[String, Array[int]] = {""nums"": [1, 2]}
func test():
    var arr = complex[""nums""]
    var num = arr[0]
";
            var classDecl = _reader.ParseFileContent(code);
            var context = new GDValidationContext();
            var collector = new GDDeclarationCollector();

            // Act & Assert: Declaration collection should not throw
            var act = () => collector.Collect(classDecl, context);
            act.Should().NotThrow<System.ArgumentException>();

            // Also verify type inference engine works
            collector.Collect(classDecl, context);
            var engine = new GDTypeInferenceEngine(
                GDDefaultRuntimeProvider.Instance,
                context.Scopes);
            var indexerExpr = classDecl.AllNodes
                .OfType<GDIndexerExpression>()
                .First();
            var inferAct = () => engine.InferTypeNode(indexerExpr);
            inferAct.Should().NotThrow<System.ArgumentException>();
        }

        #endregion
    }
}
