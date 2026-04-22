using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests that operator precedence matches the official GDScript specification.
    /// Reference: https://docs.godotengine.org/en/stable/tutorials/scripting/gdscript/gdscript_basics.html#operators
    ///
    /// GDScript precedence (highest to lowest):
    ///  1. () grouping
    ///  2. x[i] subscription
    ///  3. x.attr attribute access
    ///  4. foo() calls
    ///  5. await
    ///  6. is (type check)
    ///  7. ** (power)
    ///  8. ~x (bitwise negate), -x (unary negate)
    ///  9. *, /, %
    /// 10. +, -
    /// 11. &lt;&lt;, &gt;&gt;
    /// 12. &amp; (bitwise AND)
    /// 13. ^ (bitwise XOR)
    /// 14. | (bitwise OR)
    /// 15. ==, !=, &lt;, &gt;, &lt;=, &gt;=
    /// 16. in, not in
    /// 17. not, ! (boolean NOT)
    /// 18. and, &amp;&amp;
    /// 19. or, ||
    /// 20. ternary if/else
    /// 21. as (type cast)
    /// 22. = assignment (lowest)
    /// </summary>
    [TestClass]
    public class OperatorPrecedenceTests
    {
        private GDScriptReader _reader;

        [TestInitialize]
        public void Setup()
        {
            _reader = new GDScriptReader();
        }

        #region Power vs Multiply/Divide (** > *, /, %)

        [TestMethod]
        public void Precedence_Power_HigherThan_Multiply()
        {
            // a ** 2 * b → (a ** 2) * b
            var expr = _reader.ParseExpression("a ** 2 * b");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.Multiply, dual.OperatorType);
            Assert.IsInstanceOfType(dual.LeftExpression, typeof(GDDualOperatorExpression));
            var left = (GDDualOperatorExpression)dual.LeftExpression;
            Assert.AreEqual(GDDualOperatorType.Power, left.OperatorType);
        }

        #endregion

        #region Unary negate vs Power (-x ** 2)

        [TestMethod]
        public void Precedence_Power_HigherThan_UnaryNegate()
        {
            // -a ** 2 → -(a ** 2)
            var expr = _reader.ParseExpression("-a ** 2");
            Assert.IsInstanceOfType(expr, typeof(GDSingleOperatorExpression));
            var unary = (GDSingleOperatorExpression)expr;
            Assert.AreEqual(GDSingleOperatorType.Negate, unary.OperatorType);
            Assert.IsInstanceOfType(unary.TargetExpression, typeof(GDDualOperatorExpression));
            var inner = (GDDualOperatorExpression)unary.TargetExpression;
            Assert.AreEqual(GDDualOperatorType.Power, inner.OperatorType);
        }

        #endregion

        #region Unary negate vs Multiply (~, - > *, /, %)

        [TestMethod]
        public void Precedence_UnaryNegate_HigherThan_Multiply()
        {
            // -a * b → (-a) * b
            var expr = _reader.ParseExpression("-a * b");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.Multiply, dual.OperatorType);
            Assert.IsInstanceOfType(dual.LeftExpression, typeof(GDSingleOperatorExpression));
        }

        #endregion

        #region Multiply vs Addition (*, /, % > +, -)

        [TestMethod]
        public void Precedence_Multiply_HigherThan_Addition()
        {
            // a + b * c → a + (b * c)
            var expr = _reader.ParseExpression("a + b * c");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.Addition, dual.OperatorType);
            Assert.IsInstanceOfType(dual.RightExpression, typeof(GDDualOperatorExpression));
            var right = (GDDualOperatorExpression)dual.RightExpression;
            Assert.AreEqual(GDDualOperatorType.Multiply, right.OperatorType);
        }

        #endregion

        #region Addition vs BitShift (+, - > <<, >>)

        [TestMethod]
        public void Precedence_Addition_HigherThan_BitShift()
        {
            // a + b << c → (a + b) << c
            var expr = _reader.ParseExpression("a + b << c");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.BitShiftLeft, dual.OperatorType);
            Assert.IsInstanceOfType(dual.LeftExpression, typeof(GDDualOperatorExpression));
            var left = (GDDualOperatorExpression)dual.LeftExpression;
            Assert.AreEqual(GDDualOperatorType.Addition, left.OperatorType);
        }

        #endregion

        #region BitShift vs BitwiseAnd (<<, >> > &)

        [TestMethod]
        public void Precedence_BitShift_HigherThan_BitwiseAnd()
        {
            // a << b & c → (a << b) & c
            var expr = _reader.ParseExpression("a << b & c");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.BitwiseAnd, dual.OperatorType);
            Assert.IsInstanceOfType(dual.LeftExpression, typeof(GDDualOperatorExpression));
            var left = (GDDualOperatorExpression)dual.LeftExpression;
            Assert.AreEqual(GDDualOperatorType.BitShiftLeft, left.OperatorType);
        }

        #endregion

        #region BitwiseAnd vs BitwiseXor (& > ^)

        [TestMethod]
        public void Precedence_BitwiseAnd_HigherThan_Xor()
        {
            // a & b ^ c → (a & b) ^ c
            var expr = _reader.ParseExpression("a & b ^ c");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.Xor, dual.OperatorType);
            Assert.IsInstanceOfType(dual.LeftExpression, typeof(GDDualOperatorExpression));
            var left = (GDDualOperatorExpression)dual.LeftExpression;
            Assert.AreEqual(GDDualOperatorType.BitwiseAnd, left.OperatorType);
        }

        #endregion

        #region BitwiseXor vs BitwiseOr (^ > |)

        [TestMethod]
        public void Precedence_BitwiseXor_HigherThan_BitwiseOr()
        {
            // a ^ b | c → (a ^ b) | c
            var expr = _reader.ParseExpression("a ^ b | c");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.BitwiseOr, dual.OperatorType);
            Assert.IsInstanceOfType(dual.LeftExpression, typeof(GDDualOperatorExpression));
            var left = (GDDualOperatorExpression)dual.LeftExpression;
            Assert.AreEqual(GDDualOperatorType.Xor, left.OperatorType);
        }

        #endregion

        #region BitwiseOr vs Comparison (| > ==, !=, <, >, <=, >=) — Issue #16 Bug 2

        [TestMethod]
        public void Precedence_BitwiseAnd_HigherThan_Equal()
        {
            // a & b == 0 → (a & b) == 0
            var expr = _reader.ParseExpression("a & b == 0");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.Equal, dual.OperatorType);
            Assert.IsInstanceOfType(dual.LeftExpression, typeof(GDDualOperatorExpression));
            var left = (GDDualOperatorExpression)dual.LeftExpression;
            Assert.AreEqual(GDDualOperatorType.BitwiseAnd, left.OperatorType);
        }

        [TestMethod]
        public void Precedence_BitwiseOr_HigherThan_NotEqual()
        {
            // a | b != 0 → (a | b) != 0
            var expr = _reader.ParseExpression("a | b != 0");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.NotEqual, dual.OperatorType);
            Assert.IsInstanceOfType(dual.LeftExpression, typeof(GDDualOperatorExpression));
            var left = (GDDualOperatorExpression)dual.LeftExpression;
            Assert.AreEqual(GDDualOperatorType.BitwiseOr, left.OperatorType);
        }

        [TestMethod]
        public void Precedence_BitwiseXor_HigherThan_MoreThan()
        {
            // a ^ b > c → (a ^ b) > c
            var expr = _reader.ParseExpression("a ^ b > c");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.MoreThan, dual.OperatorType);
            Assert.IsInstanceOfType(dual.LeftExpression, typeof(GDDualOperatorExpression));
            var left = (GDDualOperatorExpression)dual.LeftExpression;
            Assert.AreEqual(GDDualOperatorType.Xor, left.OperatorType);
        }

        #endregion

        #region Comparison vs In (==, != same level as in)

        [TestMethod]
        public void Precedence_Comparison_HigherThan_Not_Boolean()
        {
            // not a > 0 → not (a > 0)  — Issue #16 Bug 1
            var expr = _reader.ParseExpression("not a > 0");
            Assert.IsInstanceOfType(expr, typeof(GDSingleOperatorExpression));
            var unary = (GDSingleOperatorExpression)expr;
            Assert.AreEqual(GDSingleOperatorType.Not2, unary.OperatorType);
            Assert.IsInstanceOfType(unary.TargetExpression, typeof(GDDualOperatorExpression));
            var inner = (GDDualOperatorExpression)unary.TargetExpression;
            Assert.AreEqual(GDDualOperatorType.MoreThan, inner.OperatorType);
        }

        [TestMethod]
        public void Precedence_Equal_HigherThan_Not_Boolean()
        {
            // not a == b → not (a == b)
            var expr = _reader.ParseExpression("not a == b");
            Assert.IsInstanceOfType(expr, typeof(GDSingleOperatorExpression));
            var unary = (GDSingleOperatorExpression)expr;
            Assert.AreEqual(GDSingleOperatorType.Not2, unary.OperatorType);
            Assert.IsInstanceOfType(unary.TargetExpression, typeof(GDDualOperatorExpression));
            var inner = (GDDualOperatorExpression)unary.TargetExpression;
            Assert.AreEqual(GDDualOperatorType.Equal, inner.OperatorType);
        }

        #endregion

        #region Not vs And (not > and)

        [TestMethod]
        public void Precedence_Not_HigherThan_And()
        {
            // not a and b → (not a) and b
            var expr = _reader.ParseExpression("not a and b");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.And2, dual.OperatorType);
            Assert.IsInstanceOfType(dual.LeftExpression, typeof(GDSingleOperatorExpression));
            var left = (GDSingleOperatorExpression)dual.LeftExpression;
            Assert.AreEqual(GDSingleOperatorType.Not2, left.OperatorType);
        }

        [TestMethod]
        public void Precedence_Not_HigherThan_Or()
        {
            // not a or b → (not a) or b
            var expr = _reader.ParseExpression("not a or b");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.Or2, dual.OperatorType);
            Assert.IsInstanceOfType(dual.LeftExpression, typeof(GDSingleOperatorExpression));
            var left = (GDSingleOperatorExpression)dual.LeftExpression;
            Assert.AreEqual(GDSingleOperatorType.Not2, left.OperatorType);
        }

        #endregion

        #region And vs Or (and > or)

        [TestMethod]
        public void Precedence_And_HigherThan_Or()
        {
            // a or b and c → a or (b and c)
            var expr = _reader.ParseExpression("a or b and c");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.Or2, dual.OperatorType);
            Assert.IsInstanceOfType(dual.RightExpression, typeof(GDDualOperatorExpression));
            var right = (GDDualOperatorExpression)dual.RightExpression;
            Assert.AreEqual(GDDualOperatorType.And2, right.OperatorType);
        }

        #endregion

        #region Is (type check) — higher than all arithmetic

        [TestMethod]
        public void Precedence_Is_HigherThan_Comparison()
        {
            // a is Node == true → (a is Node) == true
            var expr = _reader.ParseExpression("a is Node == true");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.Equal, dual.OperatorType);
            Assert.IsInstanceOfType(dual.LeftExpression, typeof(GDDualOperatorExpression));
            var left = (GDDualOperatorExpression)dual.LeftExpression;
            Assert.AreEqual(GDDualOperatorType.Is, left.OperatorType);
        }

        #endregion

        #region Complex mixed expressions

        [TestMethod]
        public void Precedence_Complex_BitwiseAndComparison()
        {
            // a & 0xFF == b & 0xFF → (a & 0xFF) == (b & 0xFF)
            var expr = _reader.ParseExpression("a & 0xFF == b & 0xFF");
            Assert.IsInstanceOfType(expr, typeof(GDDualOperatorExpression));
            var dual = (GDDualOperatorExpression)expr;
            Assert.AreEqual(GDDualOperatorType.Equal, dual.OperatorType);
            Assert.IsInstanceOfType(dual.LeftExpression, typeof(GDDualOperatorExpression));
            Assert.IsInstanceOfType(dual.RightExpression, typeof(GDDualOperatorExpression));
            var left = (GDDualOperatorExpression)dual.LeftExpression;
            var right = (GDDualOperatorExpression)dual.RightExpression;
            Assert.AreEqual(GDDualOperatorType.BitwiseAnd, left.OperatorType);
            Assert.AreEqual(GDDualOperatorType.BitwiseAnd, right.OperatorType);
        }

        [TestMethod]
        public void Precedence_Complex_NotWithBitwiseAndComparison()
        {
            // not a & b == 0 → not ((a & b) == 0)
            var expr = _reader.ParseExpression("not a & b == 0");
            Assert.IsInstanceOfType(expr, typeof(GDSingleOperatorExpression));
            var unary = (GDSingleOperatorExpression)expr;
            Assert.AreEqual(GDSingleOperatorType.Not2, unary.OperatorType);
            Assert.IsInstanceOfType(unary.TargetExpression, typeof(GDDualOperatorExpression));
            var comparison = (GDDualOperatorExpression)unary.TargetExpression;
            Assert.AreEqual(GDDualOperatorType.Equal, comparison.OperatorType);
            Assert.IsInstanceOfType(comparison.LeftExpression, typeof(GDDualOperatorExpression));
            var bitwise = (GDDualOperatorExpression)comparison.LeftExpression;
            Assert.AreEqual(GDDualOperatorType.BitwiseAnd, bitwise.OperatorType);
        }

        #endregion
    }
}
