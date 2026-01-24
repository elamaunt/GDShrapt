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
        #region Literal Expressions

        [TestMethod]
        public void BuildExpression_String()
        {
            var expr = GD.Expression.String("Hello World");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("\"Hello World\""));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_StringName()
        {
            var expr = GD.Expression.StringName("Player");
            var code = expr.ToString();
            Assert.AreEqual("&\"Player\"", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_StringName_WithSingleQuote()
        {
            var expr = GD.Expression.StringName(new GDSingleQuotasStringNode()
            {
                OpeningBounder = new GDSingleQuotas(),
                Parts = new GDStringPartsList() { new GDStringPart() { Sequence = "Player" } },
                ClosingBounder = new GDSingleQuotas()
            });
            var code = expr.ToString();
            Assert.AreEqual("&'Player'", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Number_Integer()
        {
            var expr = GD.Expression.Number(42);
            var code = expr.ToString();
            Assert.AreEqual("42", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Number_Float()
        {
            var expr = GD.Expression.Number(3.14);
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("3"));
            Assert.IsTrue(code.Contains("14"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Bool_True()
        {
            var expr = GD.Expression.Bool(true);
            var code = expr.ToString();
            Assert.AreEqual("true", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Bool_False()
        {
            var expr = GD.Expression.Bool(false);
            var code = expr.ToString();
            Assert.AreEqual("false", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_True()
        {
            var expr = GD.Expression.True();
            var code = expr.ToString();
            Assert.AreEqual("true", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_False()
        {
            var expr = GD.Expression.False();
            var code = expr.ToString();
            Assert.AreEqual("false", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Identifier Expressions

        [TestMethod]
        public void BuildExpression_Identifier()
        {
            var expr = GD.Expression.Identifier("player");
            var code = expr.ToString();
            Assert.AreEqual("player", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Collection Expressions

        [TestMethod]
        public void BuildExpression_Array_Empty()
        {
            var expr = GD.Expression.Array();
            var code = expr.ToString();
            Assert.IsNotNull(code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Array_WithElements()
        {
            var expr = GD.Expression.Array(
                GD.Expression.Number(1),
                GD.Expression.Number(2),
                GD.Expression.Number(3)
            );
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("1"));
            Assert.IsTrue(code.Contains("2"));
            Assert.IsTrue(code.Contains("3"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Array_Typed()
        {
            var expr = GD.Expression.Array(
                GD.Type.Array("int"),
                GD.Expression.Number(10),
                GD.Expression.Number(20)
            );
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("Array"));
            Assert.IsTrue(code.Contains("int"));
            Assert.IsTrue(code.Contains("10"));
            Assert.IsTrue(code.Contains("20"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Dictionary_Empty()
        {
            var expr = GD.Expression.Dictionary();
            var code = expr.ToString();
            Assert.IsNotNull(code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Dictionary_WithKeyValues()
        {
            var expr = GD.Expression.Dictionary(
                GD.Expression.KeyValue(GD.Expression.String("name"), GD.Expression.String("Player")),
                GD.Expression.KeyValue(GD.Expression.String("health"), GD.Expression.Number(100))
            );
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("name"));
            Assert.IsTrue(code.Contains("Player"));
            Assert.IsTrue(code.Contains("health"));
            Assert.IsTrue(code.Contains("100"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Member Access and Indexer

        [TestMethod]
        public void BuildExpression_Member()
        {
            var expr = GD.Expression.Member(
                GD.Expression.Identifier("player"),
                GD.Syntax.Identifier("health")
            );
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("player"));
            Assert.IsTrue(code.Contains("."));
            Assert.IsTrue(code.Contains("health"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Member_Chained()
        {
            var expr = GD.Expression.Member(
                GD.Expression.Member(
                    GD.Expression.Identifier("game"),
                    GD.Syntax.Identifier("player")
                ),
                GD.Syntax.Identifier("position")
            );
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("game"));
            Assert.IsTrue(code.Contains("player"));
            Assert.IsTrue(code.Contains("position"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Indexer()
        {
            var expr = GD.Expression.Indexer(
                GD.Expression.Identifier("items"),
                GD.Expression.Number(0)
            );
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("items"));
            Assert.IsTrue(code.Contains("["));
            Assert.IsTrue(code.Contains("0"));
            Assert.IsTrue(code.Contains("]"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Indexer_String()
        {
            var expr = GD.Expression.Indexer(
                GD.Expression.Identifier("dictionary"),
                GD.Expression.String("key")
            );
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("dictionary"));
            Assert.IsTrue(code.Contains("["));
            Assert.IsTrue(code.Contains("\"key\""));
            Assert.IsTrue(code.Contains("]"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Call Expressions

        [TestMethod]
        public void BuildExpression_Call_NoArguments()
        {
            var expr = GD.Expression.Call(GD.Expression.Identifier("get_position"));
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("get_position"));
            Assert.IsTrue(code.Contains("("));
            Assert.IsTrue(code.Contains(")"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Call_SingleArgument()
        {
            var expr = GD.Expression.Call(
                GD.Expression.Identifier("print"),
                GD.Expression.String("Hello")
            );
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("print"));
            Assert.IsTrue(code.Contains("("));
            Assert.IsTrue(code.Contains("Hello"));
            Assert.IsTrue(code.Contains(")"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Call_MultipleArguments()
        {
            var expr = GD.Expression.Call(
                GD.Expression.Identifier("move_to"),
                GD.Expression.Number(100),
                GD.Expression.Number(200)
            );
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("move_to"));
            Assert.IsTrue(code.Contains("100"));
            Assert.IsTrue(code.Contains("200"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Call_OnMember()
        {
            var expr = GD.Expression.Call(
                GD.Expression.Member(
                    GD.Expression.Identifier("player"),
                    GD.Syntax.Identifier("move")
                ),
                GD.Expression.Identifier("delta")
            );
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("player"));
            Assert.IsTrue(code.Contains("move"));
            Assert.IsTrue(code.Contains("delta"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Control Flow Expressions

        [TestMethod]
        public void BuildExpression_Return()
        {
            var expr = GD.Expression.Return(GD.Expression.Number(42));
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("return"));
            Assert.IsTrue(code.Contains("42"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Return_NoValue()
        {
            var expr = GD.Expression.Return();
            var code = expr.ToString();
            Assert.AreEqual("return", code.Trim());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Break()
        {
            var expr = GD.Expression.Break();
            var code = expr.ToString();
            Assert.AreEqual("break", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Continue()
        {
            var expr = GD.Expression.Continue();
            var code = expr.ToString();
            Assert.AreEqual("continue", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Pass()
        {
            var expr = GD.Expression.Pass();
            var code = expr.ToString();
            Assert.AreEqual("pass", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Yield()
        {
            var expr = GD.Expression.Yield();
            var code = expr.ToString();
            Assert.IsNotNull(code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_Yield_WithExpression()
        {
            var expr = GD.Expression.Yield(GD.Expression.Identifier("signal_name"));
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("yield"));
            Assert.IsTrue(code.Contains("signal_name"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Await Expression

        [TestMethod]
        public void BuildExpression_Await()
        {
            var expr = GD.Expression.Await(
                GD.Expression.Call(
                    GD.Expression.Identifier("async_function")
                )
            );
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("await"));
            Assert.IsTrue(code.Contains("async_function"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Node Path Expressions

        [TestMethod]
        public void BuildExpression_WithGetUniqueNode()
        {
            var expr = GD.Expression.GetUniqueNode("Player");
            var code = expr.ToString();
            Assert.AreEqual("%Player", code);
        }

        [TestMethod]
        public void BuildExpression_GetNode_Simple()
        {
            var expr = GD.Expression.GetNode("Player");
            var code = expr.ToString();
            Assert.AreEqual("$Player", code);
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_GetNode_Path()
        {
            var expr = GD.Expression.GetNode("LevelPlayer");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("$"));
            Assert.IsTrue(code.Contains("LevelPlayer"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void BuildExpression_NodePath()
        {
            var expr = GD.Expression.NodePath();
            var code = expr.ToString();
            Assert.IsNotNull(code);
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Bracket Expressions

        [TestMethod]
        public void BuildExpression_Bracket()
        {
            var expr = GD.Expression.Bracket(
                GD.Expression.DualOperator(
                    GD.Expression.Identifier("a"),
                    GD.Syntax.DualOperator(GDDualOperatorType.Addition),
                    GD.Expression.Identifier("b")
                )
            );
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("("));
            Assert.IsTrue(code.Contains("a"));
            Assert.IsTrue(code.Contains("+"));
            Assert.IsTrue(code.Contains("b"));
            Assert.IsTrue(code.Contains(")"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Lambda Expression

        [TestMethod]
        public void BuildExpression_WithLambda()
        {
            var lambda = GD.Expression.Lambda(GD.Expression.Call(GD.Expression.Identifier("print"), GD.Expression.String("hello")));
            var code = lambda.ToString();
            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("print"));
        }

        [TestMethod]
        public void BuildExpression_Lambda_WithParameter()
        {
            var lambda = GD.Expression.Lambda(
                GD.List.Parameters(
                    GD.Declaration.Parameter(p => p
                        .Add(GD.Syntax.Identifier("x"))
                    )
                ),
                GD.Expression.DualOperator(
                    GD.Expression.Identifier("x"),
                    GD.Syntax.DualOperator(GDDualOperatorType.Multiply),
                    GD.Expression.Number(2)
                )
            );
            var code = lambda.ToString();
            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("x"));
            Assert.IsTrue(code.Contains("*"));
            Assert.IsTrue(code.Contains("2"));
            AssertHelper.NoInvalidTokens(lambda);
        }

        #endregion

        #region If Expression

        [TestMethod]
        public void BuildExpression_If_Ternary()
        {
            var expr = GD.Expression.If(
                GD.Expression.Identifier("condition"),
                GD.Expression.String("yes"),
                GD.Expression.String("no")
            );
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("condition"));
            Assert.IsTrue(code.Contains("yes"));
            Assert.IsTrue(code.Contains("no"));
            Assert.IsTrue(code.Contains("if"));
            Assert.IsTrue(code.Contains("else"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Pattern Matching

        [TestMethod]
        public void BuildExpression_MatchCaseVariable()
        {
            var expr = GD.Expression.MatchCaseVariable("x");
            var code = expr.ToString();
            Assert.IsTrue(code.Contains("var"));
            Assert.IsTrue(code.Contains("x"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Type Cast Expressions

        [TestMethod]
        public void BuildExpression_As()
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
        public void BuildExpression_Is()
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

        #endregion

        #region Assert Call Tests

        [TestMethod]
        public void BuildExpression_Assert_SingleArgument()
        {
            // Test: assert(condition)
            var assertCall = GD.Expression.Call(
                GD.Expression.Identifier("assert"),
                GD.Expression.Identifier("condition")
            );
            var code = assertCall.ToString();

            Assert.IsTrue(code.Contains("assert"));
            Assert.IsTrue(code.Contains("condition"));
            AssertHelper.NoInvalidTokens(assertCall);
        }

        [TestMethod]
        public void BuildExpression_Assert_WithMessage()
        {
            // Test: assert(x > 0, "x must be positive")
            var assertCall = GD.Expression.Call(
                GD.Expression.Identifier("assert"),
                GD.Expression.DualOperator(
                    GD.Expression.Identifier("x"),
                    GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                    GD.Expression.Number(0)
                ),
                GD.Expression.String("x must be positive")
            );
            var code = assertCall.ToString();

            Assert.IsTrue(code.Contains("assert"));
            Assert.IsTrue(code.Contains("x >"));
            Assert.IsTrue(code.Contains("\"x must be positive\""));
            AssertHelper.NoInvalidTokens(assertCall);
        }

        #endregion

        #region Advanced Await Tests

        [TestMethod]
        public void BuildExpression_Await_Signal()
        {
            // Test: await timer.timeout
            var awaitExpr = GD.Expression.Await(
                GD.Expression.Member(
                    GD.Expression.Identifier("timer"),
                    GD.Syntax.Identifier("timeout")
                )
            );
            var code = awaitExpr.ToString();

            Assert.IsTrue(code.Contains("await"));
            Assert.IsTrue(code.Contains("timer.timeout"));
            AssertHelper.NoInvalidTokens(awaitExpr);
        }

        [TestMethod]
        public void BuildExpression_Await_Chained()
        {
            // Test: await get_tree().create_timer(1.0).timeout
            var awaitExpr = GD.Expression.Await(
                GD.Expression.Member(
                    GD.Expression.Call(
                        GD.Expression.Member(
                            GD.Expression.Call(GD.Expression.Identifier("get_tree")),
                            GD.Syntax.Identifier("create_timer")
                        ),
                        GD.Expression.Number(1.0)
                    ),
                    GD.Syntax.Identifier("timeout")
                )
            );
            var code = awaitExpr.ToString();

            Assert.IsTrue(code.Contains("await"));
            Assert.IsTrue(code.Contains("get_tree()"));
            Assert.IsTrue(code.Contains("create_timer"));
            Assert.IsTrue(code.Contains("timeout"));
            AssertHelper.NoInvalidTokens(awaitExpr);
        }

        [TestMethod]
        public void BuildExpression_Await_OnMemberCall()
        {
            // Test: await $Timer.timeout
            var awaitExpr = GD.Expression.Await(
                GD.Expression.Member(
                    GD.Expression.GetNode(GD.Syntax.Identifier("Timer")),
                    GD.Syntax.Identifier("timeout")
                )
            );
            var code = awaitExpr.ToString();

            Assert.IsTrue(code.Contains("await"));
            Assert.IsTrue(code.Contains("$Timer"));
            Assert.IsTrue(code.Contains("timeout"));
            AssertHelper.NoInvalidTokens(awaitExpr);
        }

        #endregion
    }
}
