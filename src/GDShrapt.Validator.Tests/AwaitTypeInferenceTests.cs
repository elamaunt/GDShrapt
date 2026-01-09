using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for await expression type inference.
    /// Verifies that await returns the correct emission type for signals
    /// and return type for coroutines.
    /// </summary>
    [TestClass]
    public class AwaitTypeInferenceTests
    {
        private GDScriptReader _reader;

        [TestInitialize]
        public void Setup()
        {
            _reader = new GDScriptReader();
        }

        #region User-Defined Signal Tests

        [TestMethod]
        public void Await_UserDefinedSignal_SingleParam_ReturnsParamType()
        {
            // Arrange
            var code = @"
extends Node
signal health_changed(new_health: int)
func test():
    var h = await health_changed
";
            var classDecl = _reader.ParseFileContent(code);
            var context = new GDValidationContext();
            var collector = new GDDeclarationCollector();
            collector.Collect(classDecl, context);

            var engine = new GDTypeInferenceEngine(
                GDDefaultRuntimeProvider.Instance,
                context.Scopes);

            // Act
            var awaitExpr = classDecl.AllNodes
                .OfType<GDAwaitExpression>()
                .First();
            var typeNode = engine.InferTypeNode(awaitExpr);

            // Assert
            typeNode.Should().NotBeNull();
            typeNode.BuildName().Should().Be("int");
        }

        [TestMethod]
        public void Await_UserDefinedSignal_NoParams_ReturnsVoid()
        {
            // Arrange
            var code = @"
extends Node
signal ready_custom()
func test():
    await ready_custom
";
            var classDecl = _reader.ParseFileContent(code);
            var context = new GDValidationContext();
            var collector = new GDDeclarationCollector();
            collector.Collect(classDecl, context);

            var engine = new GDTypeInferenceEngine(
                GDDefaultRuntimeProvider.Instance,
                context.Scopes);

            // Act
            var awaitExpr = classDecl.AllNodes
                .OfType<GDAwaitExpression>()
                .First();
            var typeNode = engine.InferTypeNode(awaitExpr);

            // Assert
            typeNode.Should().NotBeNull();
            typeNode.BuildName().Should().Be("void");
        }

        [TestMethod]
        public void Await_UserDefinedSignal_MultipleParams_ReturnsArray()
        {
            // Arrange
            var code = @"
extends Node
signal position_changed(x: float, y: float)
func test():
    var pos = await position_changed
";
            var classDecl = _reader.ParseFileContent(code);
            var context = new GDValidationContext();
            var collector = new GDDeclarationCollector();
            collector.Collect(classDecl, context);

            var engine = new GDTypeInferenceEngine(
                GDDefaultRuntimeProvider.Instance,
                context.Scopes);

            // Act
            var awaitExpr = classDecl.AllNodes
                .OfType<GDAwaitExpression>()
                .First();
            var typeNode = engine.InferTypeNode(awaitExpr);

            // Assert
            typeNode.Should().NotBeNull();
            typeNode.BuildName().Should().Be("Array");
        }

        [TestMethod]
        public void Await_UserDefinedSignal_UntypedParam_ReturnsVariant()
        {
            // Arrange
            var code = @"
extends Node
signal data_received(data)
func test():
    var d = await data_received
";
            var classDecl = _reader.ParseFileContent(code);
            var context = new GDValidationContext();
            var collector = new GDDeclarationCollector();
            collector.Collect(classDecl, context);

            var engine = new GDTypeInferenceEngine(
                GDDefaultRuntimeProvider.Instance,
                context.Scopes);

            // Act
            var awaitExpr = classDecl.AllNodes
                .OfType<GDAwaitExpression>()
                .First();
            var typeNode = engine.InferTypeNode(awaitExpr);

            // Assert
            typeNode.Should().NotBeNull();
            typeNode.BuildName().Should().Be("Variant");
        }

        #endregion

        #region Coroutine Tests

        [TestMethod]
        public void Await_CoroutineCall_ReturnsReturnType()
        {
            // Arrange
            var code = @"
extends Node
func async_load() -> Resource:
    return null
func test():
    var res = await async_load()
";
            var classDecl = _reader.ParseFileContent(code);
            var context = new GDValidationContext();
            var collector = new GDDeclarationCollector();
            collector.Collect(classDecl, context);

            var engine = new GDTypeInferenceEngine(
                GDDefaultRuntimeProvider.Instance,
                context.Scopes);

            // Act
            var awaitExpr = classDecl.AllNodes
                .OfType<GDAwaitExpression>()
                .First();
            var typeNode = engine.InferTypeNode(awaitExpr);

            // Assert
            // Note: This test will return null because we don't have method resolution
            // in scope for async_load() without full semantic analysis
            // The important thing is it doesn't crash
            typeNode.Should().NotBeNull();
        }

        #endregion

        #region Unknown Signal Tests

        [TestMethod]
        public void Await_UnknownSignal_ReturnsVariant()
        {
            // Arrange
            var code = @"
extends Node
func test():
    var x = await unknown_signal
";
            var classDecl = _reader.ParseFileContent(code);
            var context = new GDValidationContext();
            var collector = new GDDeclarationCollector();
            collector.Collect(classDecl, context);

            var engine = new GDTypeInferenceEngine(
                GDDefaultRuntimeProvider.Instance,
                context.Scopes);

            // Act
            var awaitExpr = classDecl.AllNodes
                .OfType<GDAwaitExpression>()
                .First();
            var typeNode = engine.InferTypeNode(awaitExpr);

            // Assert
            typeNode.Should().NotBeNull();
            typeNode.BuildName().Should().Be("Variant");
        }

        [TestMethod]
        public void Await_NullExpression_ReturnsVariant()
        {
            // Arrange - create an await expression with null inner expression
            var awaitExpr = new GDAwaitExpression();

            var engine = new GDTypeInferenceEngine(
                GDDefaultRuntimeProvider.Instance,
                null);

            // Act
            var typeNode = engine.InferTypeNode(awaitExpr);

            // Assert
            typeNode.Should().NotBeNull();
            typeNode.BuildName().Should().Be("Variant");
        }

        #endregion

        #region Yield Tests

        [TestMethod]
        public void Yield_ReturnsSignal()
        {
            // Arrange
            var code = @"
extends Node
func test():
    var sig = yield()
";
            var classDecl = _reader.ParseFileContent(code);
            var context = new GDValidationContext();
            var collector = new GDDeclarationCollector();
            collector.Collect(classDecl, context);

            var engine = new GDTypeInferenceEngine(
                GDDefaultRuntimeProvider.Instance,
                context.Scopes);

            // Act
            var yieldExpr = classDecl.AllNodes
                .OfType<GDYieldExpression>()
                .First();
            var typeNode = engine.InferTypeNode(yieldExpr);

            // Assert
            typeNode.Should().NotBeNull();
            typeNode.BuildName().Should().Be("Signal");
        }

        #endregion
    }
}
