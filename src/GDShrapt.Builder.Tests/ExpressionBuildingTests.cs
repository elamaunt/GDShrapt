using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Tests for building expressions programmatically.
    /// </summary>
    [TestClass]
    public class ExpressionBuildingTests
    {
        [TestMethod]
        public void BuildExpression_WithGetUniqueNode()
        {
            var expr = GD.Expression.GetUniqueNode("Player");
            var code = expr.ToString();
            Assert.AreEqual("%Player", code);
        }

        [TestMethod]
        public void BuildExpression_WithLambda()
        {
            var lambda = GD.Expression.Lambda(GD.Expression.Call(GD.Expression.Identifier("print"), GD.Expression.String("hello")));
            var code = lambda.ToString();
            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("print"));
        }
    }
}
