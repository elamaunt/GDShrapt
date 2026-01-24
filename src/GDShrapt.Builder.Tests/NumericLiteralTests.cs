using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Tests for building numeric literals in various formats.
    /// </summary>
    [TestClass]
    public class NumericLiteralTests
    {
        #region Decimal Literals

        [TestMethod]
        public void BuildNumber_Integer()
        {
            var expr = GD.Expression.Number(42);
            Assert.AreEqual("42", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildNumber_LargeInteger()
        {
            var expr = GD.Expression.Number(1000000);
            Assert.AreEqual("1000000", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildNumber_NegativeInteger()
        {
            var expr = GD.Expression.Number(-100);
            Assert.AreEqual("-100", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildNumber_Float()
        {
            var expr = GD.Expression.Number(3.14);
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("3.14") || code.Contains("3,14"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildNumber_SmallFloat()
        {
            var expr = GD.Expression.Number(0.001);
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("0.001") || code.Contains("0,001"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region String-based Number Literals

        [TestMethod]
        public void BuildNumber_Hexadecimal()
        {
            var expr = GD.Expression.Number("0xFF");
            Assert.AreEqual("0xFF", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildNumber_HexadecimalLowercase()
        {
            var expr = GD.Expression.Number("0xdeadbeef");
            Assert.AreEqual("0xdeadbeef", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildNumber_Binary()
        {
            var expr = GD.Expression.Number("0b1010");
            Assert.AreEqual("0b1010", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildNumber_BinaryLong()
        {
            var expr = GD.Expression.Number("0b11110000");
            Assert.AreEqual("0b11110000", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildNumber_WithUnderscores()
        {
            var expr = GD.Expression.Number("1_000_000");
            Assert.AreEqual("1_000_000", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildNumber_HexWithUnderscores()
        {
            var expr = GD.Expression.Number("0xFF_FF_FF");
            Assert.AreEqual("0xFF_FF_FF", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildNumber_BinaryWithUnderscores()
        {
            var expr = GD.Expression.Number("0b1111_0000");
            Assert.AreEqual("0b1111_0000", expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        // Note: Scientific notation (e.g., 1.5e-3) is parsed as identifier access by GDScript parser,
        // not as a number literal. Use GD.Expression.Number(double) with actual values instead.

        #endregion

        #region Number in Context

        [TestMethod]
        public void BuildVariable_WithHexValue()
        {
            var decl = GD.Declaration.Variable("color", GD.Expression.Number("0xFF0000"));
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var color"));
            Assert.IsTrue(code.Contains("0xFF0000"));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void BuildVariable_WithBinaryValue()
        {
            var decl = GD.Declaration.Variable("flags", GD.Expression.Number("0b1010"));
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var flags"));
            Assert.IsTrue(code.Contains("0b1010"));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void BuildConst_WithUnderscoreValue()
        {
            var decl = GD.Declaration.Const("MAX_VALUE", GD.Expression.Number("999_999_999"));
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("const MAX_VALUE"));
            Assert.IsTrue(code.Contains("999_999_999"));
            AssertHelper.NoInvalidTokens(decl);
        }

        #endregion
    }
}
