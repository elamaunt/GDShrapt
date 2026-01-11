using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for GDExpressionHelper.
    /// </summary>
    [TestClass]
    public class ExpressionHelperTests
    {
        [TestMethod]
        public void ExpressionHelper_IsPreload()
        {
            var reader = new GDScriptReader();
            var expression = reader.ParseExpression(@"preload(""res://player.gd"")") as GDCallExpression;

            expression.Should().NotBeNull();
            expression.IsPreload().Should().BeTrue();
            expression.GetSimpleCallName().Should().Be("preload");
            GDExpressionHelper.IsBuiltInFunction("preload").Should().BeTrue();
        }

        [TestMethod]
        public void ExpressionHelper_IsPrint()
        {
            var reader = new GDScriptReader();

            var printExpr = reader.ParseExpression(@"print(""hello"")") as GDCallExpression;
            var printsExpr = reader.ParseExpression(@"prints(""a"", ""b"")") as GDCallExpression;

            printExpr.IsPrint().Should().BeTrue();
            printsExpr.IsPrint().Should().BeTrue();
        }

        [TestMethod]
        public void ExpressionHelper_IsAssert()
        {
            var reader = new GDScriptReader();
            var expression = reader.ParseExpression(@"assert(x > 0, ""x must be positive"")") as GDCallExpression;

            expression.Should().NotBeNull();
            expression.IsAssert().Should().BeTrue();
        }

        [TestMethod]
        public void ExpressionHelper_IsRange()
        {
            var reader = new GDScriptReader();
            var expression = reader.ParseExpression(@"range(10)") as GDCallExpression;

            expression.Should().NotBeNull();
            expression.IsRange().Should().BeTrue();
        }

        [TestMethod]
        public void ExpressionHelper_IsMathFunction()
        {
            var reader = new GDScriptReader();

            var absExpr = reader.ParseExpression(@"abs(-5)") as GDCallExpression;
            var clampExpr = reader.ParseExpression(@"clamp(x, 0, 100)") as GDCallExpression;
            var lerpExpr = reader.ParseExpression(@"lerp(a, b, 0.5)") as GDCallExpression;

            absExpr.IsMathFunction().Should().BeTrue();
            clampExpr.IsMathFunction().Should().BeTrue();
            lerpExpr.IsMathFunction().Should().BeTrue();
        }

        [TestMethod]
        public void ExpressionHelper_IsGetUniqueNode()
        {
            var reader = new GDScriptReader();
            var expression = reader.ParseExpression(@"%Player");

            expression.IsGetUniqueNode().Should().BeTrue();
        }

        [TestMethod]
        public void ExpressionHelper_IsGetNode()
        {
            var reader = new GDScriptReader();
            var expression = reader.ParseExpression(@"$Sprite2D");

            expression.IsGetNode().Should().BeTrue();
        }

        [TestMethod]
        public void ExpressionHelper_IsLambda()
        {
            var reader = new GDScriptReader();
            var expression = reader.ParseExpression(@"func(): print(""hello"")");

            expression.IsLambda().Should().BeTrue();
        }

        [TestMethod]
        public void ExpressionHelper_IsAwait()
        {
            var reader = new GDScriptReader();
            var expression = reader.ParseExpression(@"await get_tree().create_timer(1.0).timeout");

            expression.IsAwait().Should().BeTrue();
        }

        [TestMethod]
        public void ExpressionHelper_IsMethodCall()
        {
            var reader = new GDScriptReader();
            var expression = reader.ParseExpression(@"node.get_child(0)") as GDCallExpression;

            expression.Should().NotBeNull();
            expression.IsMethodCall().Should().BeTrue();
            expression.GetMethodCallName().Should().Be("get_child");
        }

        [TestMethod]
        public void ExpressionHelper_IsArrayInitializer()
        {
            var reader = new GDScriptReader();
            var expression = reader.ParseExpression(@"[1, 2, 3]");

            expression.IsArrayInitializer().Should().BeTrue();
        }

        [TestMethod]
        public void ExpressionHelper_IsDictionaryInitializer()
        {
            var reader = new GDScriptReader();
            var expression = reader.ParseExpression(@"{""key"": ""value""}");

            expression.IsDictionaryInitializer().Should().BeTrue();
        }

        [TestMethod]
        public void ExpressionHelper_IsMemberAccess()
        {
            var reader = new GDScriptReader();
            var expression = reader.ParseExpression(@"node.position");

            expression.IsMemberAccess().Should().BeTrue();
        }

        [TestMethod]
        public void ExpressionHelper_IsIndexer()
        {
            var reader = new GDScriptReader();
            var expression = reader.ParseExpression(@"array[0]");

            expression.IsIndexer().Should().BeTrue();
        }

        [TestMethod]
        public void ExpressionHelper_IsBuiltInFunction()
        {
            GDExpressionHelper.IsBuiltInFunction("preload").Should().BeTrue();
            GDExpressionHelper.IsBuiltInFunction("load").Should().BeTrue();
            GDExpressionHelper.IsBuiltInFunction("assert").Should().BeTrue();
            GDExpressionHelper.IsBuiltInFunction("print").Should().BeTrue();
            GDExpressionHelper.IsBuiltInFunction("range").Should().BeTrue();
            GDExpressionHelper.IsBuiltInFunction("abs").Should().BeTrue();
            GDExpressionHelper.IsBuiltInFunction("clamp").Should().BeTrue();
            GDExpressionHelper.IsBuiltInFunction("my_function").Should().BeFalse();
        }
    }
}
