using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Systematic tests for building operator expressions (binary and unary) using GD.Expression.DualOperator and GD.Expression.SingleOperator.
    /// </summary>
    [TestClass]
    public class OperatorBuildingTests
    {
        #region Arithmetic Operators

        [TestMethod]
        public void BuildOperator_Addition()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Number(5),
                GD.Syntax.DualOperator(GDDualOperatorType.Addition),
                GD.Expression.Number(3)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("5"));
            Assert.IsTrue(code.Contains("+"));
            Assert.IsTrue(code.Contains("3"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_Subtraction()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Number(10),
                GD.Syntax.DualOperator(GDDualOperatorType.Subtraction),
                GD.Expression.Number(7)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("10"));
            Assert.IsTrue(code.Contains("-"));
            Assert.IsTrue(code.Contains("7"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_Multiplication()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Number(4),
                GD.Syntax.DualOperator(GDDualOperatorType.Multiply),
                GD.Expression.Number(2)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("4"));
            Assert.IsTrue(code.Contains("*"));
            Assert.IsTrue(code.Contains("2"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_Division()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Number(20),
                GD.Syntax.DualOperator(GDDualOperatorType.Division),
                GD.Expression.Number(5)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("20"));
            Assert.IsTrue(code.Contains("/"));
            Assert.IsTrue(code.Contains("5"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_Modulo()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Number(10),
                GD.Syntax.DualOperator(GDDualOperatorType.Mod),
                GD.Expression.Number(3)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("10"));
            Assert.IsTrue(code.Contains("%"));
            Assert.IsTrue(code.Contains("3"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Comparison Operators

        [TestMethod]
        public void BuildOperator_Equal()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("x"),
                GD.Syntax.DualOperator(GDDualOperatorType.Equal),
                GD.Expression.Number(5)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("x"));
            Assert.IsTrue(code.Contains("=="));
            Assert.IsTrue(code.Contains("5"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_NotEqual()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("y"),
                GD.Syntax.DualOperator(GDDualOperatorType.NotEqual),
                GD.Expression.Number(0)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("y"));
            Assert.IsTrue(code.Contains("!="));
            Assert.IsTrue(code.Contains("0"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_LessThan()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("score"),
                GD.Syntax.DualOperator(GDDualOperatorType.LessThan),
                GD.Expression.Number(100)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("score"));
            Assert.IsTrue(code.Contains("<"));
            Assert.IsTrue(code.Contains("100"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_MoreThan()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("health"),
                GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                GD.Expression.Number(0)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("health"));
            Assert.IsTrue(code.Contains(">"));
            Assert.IsTrue(code.Contains("0"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_LessThanOrEqual()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("count"),
                GD.Syntax.DualOperator(GDDualOperatorType.LessThanOrEqual),
                GD.Expression.Number(10)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("count"));
            Assert.IsTrue(code.Contains("<="));
            Assert.IsTrue(code.Contains("10"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_MoreThanOrEqual()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("level"),
                GD.Syntax.DualOperator(GDDualOperatorType.MoreThanOrEqual),
                GD.Expression.Number(5)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("level"));
            Assert.IsTrue(code.Contains(">="));
            Assert.IsTrue(code.Contains("5"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Logical Operators

        [TestMethod]
        public void BuildOperator_And()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("is_valid"),
                GD.Syntax.DualOperator(GDDualOperatorType.And2),
                GD.Expression.Identifier("is_active")
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("is_valid"));
            Assert.IsTrue(code.Contains("and"));
            Assert.IsTrue(code.Contains("is_active"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_Or()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("has_key"),
                GD.Syntax.DualOperator(GDDualOperatorType.Or),
                GD.Expression.Identifier("has_password")
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("has_key"));
            Assert.IsTrue(code.Contains("or"));
            Assert.IsTrue(code.Contains("has_password"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Bitwise Operators

        [TestMethod]
        public void BuildOperator_BitwiseAnd()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("flags"),
                GD.Syntax.DualOperator(GDDualOperatorType.BitwiseAnd),
                GD.Expression.Number(0xFF)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("flags"));
            Assert.IsTrue(code.Contains("&"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_BitwiseOr()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("mask"),
                GD.Syntax.DualOperator(GDDualOperatorType.BitwiseOr),
                GD.Expression.Number(0x01)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("mask"));
            Assert.IsTrue(code.Contains("|"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_BitwiseXor()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("value"),
                GD.Syntax.DualOperator(GDDualOperatorType.Xor),
                GD.Expression.Number(0x0F)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("value"));
            Assert.IsTrue(code.Contains("^"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_BitShiftLeft()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Number(1),
                GD.Syntax.DualOperator(GDDualOperatorType.BitShiftLeft),
                GD.Expression.Number(4)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("1"));
            Assert.IsTrue(code.Contains("<<"));
            Assert.IsTrue(code.Contains("4"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_BitShiftRight()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Number(16),
                GD.Syntax.DualOperator(GDDualOperatorType.BitShiftRight),
                GD.Expression.Number(2)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("16"));
            Assert.IsTrue(code.Contains(">>"));
            Assert.IsTrue(code.Contains("2"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Assignment Operators

        [TestMethod]
        public void BuildOperator_Assignment()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("x"),
                GD.Syntax.DualOperator(GDDualOperatorType.Assignment),
                GD.Expression.Number(5)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("x"));
            Assert.IsTrue(code.Contains("="));
            Assert.IsTrue(code.Contains("5"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_AddAndAssign()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("count"),
                GD.Syntax.DualOperator(GDDualOperatorType.AddAndAssign),
                GD.Expression.Number(1)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("count"));
            Assert.IsTrue(code.Contains("+="));
            Assert.IsTrue(code.Contains("1"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_SubtractAndAssign()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("health"),
                GD.Syntax.DualOperator(GDDualOperatorType.SubtractAndAssign),
                GD.Expression.Number(10)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("health"));
            Assert.IsTrue(code.Contains("-="));
            Assert.IsTrue(code.Contains("10"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_MultiplyAndAssign()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("damage"),
                GD.Syntax.DualOperator(GDDualOperatorType.MultiplyAndAssign),
                GD.Expression.Number(2)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("damage"));
            Assert.IsTrue(code.Contains("*="));
            Assert.IsTrue(code.Contains("2"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Unary Operators

        [TestMethod]
        public void BuildOperator_Negate()
        {
            var expr = GD.Expression.SingleOperator(
                GD.Syntax.SingleOperator(GDSingleOperatorType.Negate),
                GD.Expression.Number(5)
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("-"));
            Assert.IsTrue(code.Contains("5"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_Not()
        {
            var expr = GD.Expression.SingleOperator(
                GD.Syntax.SingleOperator(GDSingleOperatorType.Not2),
                GD.Expression.Identifier("is_valid")
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("not"));
            Assert.IsTrue(code.Contains("is_valid"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_BitwiseNegate()
        {
            var expr = GD.Expression.SingleOperator(
                GD.Syntax.SingleOperator(GDSingleOperatorType.BitwiseNegate),
                GD.Expression.Identifier("flags")
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("~"));
            Assert.IsTrue(code.Contains("flags"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Special Operators

        [TestMethod]
        public void BuildOperator_Is()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("obj"),
                GD.Syntax.DualOperator(GDDualOperatorType.Is),
                GD.Expression.Identifier("Node")
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("obj"));
            Assert.IsTrue(code.Contains("is"));
            Assert.IsTrue(code.Contains("Node"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_As()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("node"),
                GD.Syntax.DualOperator(GDDualOperatorType.As),
                GD.Expression.Identifier("Sprite2D")
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("node"));
            Assert.IsTrue(code.Contains("as"));
            Assert.IsTrue(code.Contains("Sprite2D"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_In()
        {
            var expr = GD.Expression.In(
                GD.Expression.Identifier("x"),
                GD.Expression.Identifier("arr")
            );

            var code = expr.ToString();

            Assert.AreEqual("x in arr", code);
            Assert.AreEqual(GDDualOperatorType.In, expr.OperatorType);
            Assert.IsNull(expr.NotKeyword);
            Assert.IsFalse(expr.IsNotIn);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_In_WithDualOperator()
        {
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("key"),
                GD.Syntax.DualOperator(GDDualOperatorType.In),
                GD.Expression.Identifier("dict")
            );

            var code = expr.ToString();

            Assert.AreEqual("key in dict", code);
            Assert.AreEqual(GDDualOperatorType.In, expr.OperatorType);
            Assert.IsNull(expr.NotKeyword);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_NotIn()
        {
            var expr = GD.Expression.NotIn(
                GD.Expression.Identifier("x"),
                GD.Expression.Identifier("arr")
            );

            var code = expr.ToString();

            Assert.AreEqual("x not in arr", code);
            Assert.AreEqual(GDDualOperatorType.In, expr.OperatorType);
            Assert.IsNotNull(expr.NotKeyword);
            Assert.IsTrue(expr.IsNotIn);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_NotIn_WithComplexExpressions()
        {
            var expr = GD.Expression.NotIn(
                GD.Expression.String("key"),
                GD.Expression.Call(
                    GD.Expression.Member(
                        GD.Expression.Identifier("obj"),
                        GD.Syntax.Identifier("keys")
                    )
                )
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("\"key\""));
            Assert.IsTrue(code.Contains("not in"));
            Assert.IsTrue(code.Contains("obj.keys()"));
            Assert.IsTrue(expr.IsNotIn);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_NotIn_RoundTrip()
        {
            var expr = GD.Expression.NotIn(
                GD.Expression.Identifier("item"),
                GD.Expression.Identifier("inventory")
            );

            var code = expr.ToString();
            var reader = new GDScriptReader();
            var parsed = reader.ParseExpression(code);

            Assert.IsInstanceOfType(parsed, typeof(GDDualOperatorExpression));
            var dualOp = (GDDualOperatorExpression)parsed;
            Assert.IsTrue(dualOp.IsNotIn);
            Assert.AreEqual(code, parsed.ToString());
        }

        #endregion

        #region Operator Precedence and Brackets

        [TestMethod]
        public void BuildOperator_Precedence_WithoutBrackets()
        {
            // a + b * c
            var expr = GD.Expression.DualOperator(
                GD.Expression.Identifier("a"),
                GD.Syntax.DualOperator(GDDualOperatorType.Addition),
                GD.Expression.DualOperator(
                    GD.Expression.Identifier("b"),
                    GD.Syntax.DualOperator(GDDualOperatorType.Multiply),
                    GD.Expression.Identifier("c")
                )
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("a"));
            Assert.IsTrue(code.Contains("+"));
            Assert.IsTrue(code.Contains("b"));
            Assert.IsTrue(code.Contains("*"));
            Assert.IsTrue(code.Contains("c"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildOperator_Precedence_WithBrackets()
        {
            // (a + b) * c
            var expr = GD.Expression.DualOperator(
                GD.Expression.Bracket(
                    GD.Expression.DualOperator(
                        GD.Expression.Identifier("a"),
                        GD.Syntax.DualOperator(GDDualOperatorType.Addition),
                        GD.Expression.Identifier("b")
                    )
                ),
                GD.Syntax.DualOperator(GDDualOperatorType.Multiply),
                GD.Expression.Identifier("c")
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("("));
            Assert.IsTrue(code.Contains("a"));
            Assert.IsTrue(code.Contains("+"));
            Assert.IsTrue(code.Contains("b"));
            Assert.IsTrue(code.Contains(")"));
            Assert.IsTrue(code.Contains("*"));
            Assert.IsTrue(code.Contains("c"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion
    }
}
