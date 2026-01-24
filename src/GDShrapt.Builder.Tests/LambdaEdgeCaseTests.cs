using System.Linq;
using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Edge case tests for lambda expressions.
    /// Tests multi-statement lambdas, typed parameters, nested lambdas, and closures.
    /// </summary>
    [TestClass]
    public class LambdaEdgeCaseTests
    {
        private readonly GDScriptReader _reader = new GDScriptReader();

        #region Multi-Statement Lambda Tests

        [TestMethod]
        public void Lambda_WithMultipleStatements()
        {
            // Test: var f = func():
            //     var x = 1
            //     return x + 1
            var lambda = GD.Expression.Lambda(setup => setup
                .AddFuncKeyword()
                .AddOpenBracket()
                .AddCloseBracket()
                .AddColon()
                .AddStatements(s => s
                    .AddNewLine()
                    .AddIntendation()
                    .Add<GDStatementsList, GDStatement>(GD.Statement.Variable("x", GD.Expression.Number(1)))
                    .AddNewLine()
                    .AddIntendation()
                    .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                        GD.Expression.Return(
                            GD.Expression.DualOperator(
                                GD.Expression.Identifier("x"),
                                GD.Syntax.DualOperator(GDDualOperatorType.Addition),
                                GD.Expression.Number(1)
                            )
                        )
                    ))
                )
            );

            var code = lambda.ToString();

            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("var x"));
            Assert.IsTrue(code.Contains("return"));
            AssertHelper.NoInvalidTokens(lambda);
        }

        [TestMethod]
        public void Lambda_WithReturnStatement()
        {
            // Test: func(): return 42
            var lambda = GD.Expression.Lambda(
                GD.Expression.Return(GD.Expression.Number(42))
            );

            var code = lambda.ToString();

            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("return"));
            Assert.IsTrue(code.Contains("42"));
            AssertHelper.NoInvalidTokens(lambda);
        }

        #endregion

        #region Typed Parameter Tests

        [TestMethod]
        public void Lambda_WithTypedParameters()
        {
            // Test: func(x: int, y: int): x + y
            var lambda = GD.Expression.Lambda(
                GD.List.Parameters(
                    GD.Declaration.Parameter("x", GD.Type.Single("int")),
                    GD.Declaration.Parameter("y", GD.Type.Single("int"))
                ),
                GD.Expression.DualOperator(
                    GD.Expression.Identifier("x"),
                    GD.Syntax.DualOperator(GDDualOperatorType.Addition),
                    GD.Expression.Identifier("y")
                )
            );

            var code = lambda.ToString();

            Assert.IsTrue(code.Contains("x: int"));
            Assert.IsTrue(code.Contains("y: int"));
            Assert.IsTrue(code.Contains("x + y"));
            AssertHelper.NoInvalidTokens(lambda);
        }

        [TestMethod]
        public void Lambda_WithDefaultParameters()
        {
            // Test: func(x: int = 10): x * 2
            var lambda = GD.Expression.Lambda(
                GD.List.Parameters(
                    GD.Declaration.Parameter("x", GD.Type.Single("int"), GD.Expression.Number(10))
                ),
                GD.Expression.DualOperator(
                    GD.Expression.Identifier("x"),
                    GD.Syntax.DualOperator(GDDualOperatorType.Multiply),
                    GD.Expression.Number(2)
                )
            );

            var code = lambda.ToString();

            Assert.IsTrue(code.Contains("x: int = 10"));
            Assert.IsTrue(code.Contains("x * 2"));
            AssertHelper.NoInvalidTokens(lambda);
        }

        #endregion

        #region Nested Lambda Tests

        [TestMethod]
        public void Lambda_ReturningLambda()
        {
            // Test: func(): func(): 42
            var innerLambda = GD.Expression.Lambda(GD.Expression.Number(42));
            var outerLambda = GD.Expression.Lambda(innerLambda);

            var code = outerLambda.ToString();

            // Should contain two "func" occurrences
            var funcCount = code.Split(new[] { "func" }, System.StringSplitOptions.None).Length - 1;
            Assert.AreEqual(2, funcCount, $"Expected 2 'func' keywords, got {funcCount}. Code: {code}");
            Assert.IsTrue(code.Contains("42"));
            AssertHelper.NoInvalidTokens(outerLambda);
        }

        [TestMethod]
        public void Lambda_WithCapturedVariables()
        {
            // Test: conceptually tests closure pattern
            // var multiplier = 5
            // var f = func(x): x * multiplier
            // The lambda references 'multiplier' from outer scope
            var lambda = GD.Expression.Lambda(
                GD.List.Parameters(GD.Declaration.Parameter("x")),
                GD.Expression.DualOperator(
                    GD.Expression.Identifier("x"),
                    GD.Syntax.DualOperator(GDDualOperatorType.Multiply),
                    GD.Expression.Identifier("multiplier")  // Captured variable
                )
            );

            var code = lambda.ToString();

            Assert.IsTrue(code.Contains("x"));
            Assert.IsTrue(code.Contains("multiplier"));
            Assert.IsTrue(code.Contains("*"));
            AssertHelper.NoInvalidTokens(lambda);
        }

        #endregion

        #region Callable Type Tests

        [TestMethod]
        public void Lambda_AssignedToCallableType()
        {
            // Test: var f: Callable = func(): pass
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("f", "Callable", GD.Expression.Lambda(GD.Expression.Pass()))
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            Assert.IsTrue(code.Contains("var f: Callable"));
            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("pass"));
            AssertHelper.NoInvalidTokens(classDecl);
        }

        #endregion

        #region Round-Trip Tests

        [TestMethod]
        public void RoundTrip_Lambda_SingleExpression()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("f", GD.Expression.Lambda(
                    GD.List.Parameters(GD.Declaration.Parameter("x")),
                    GD.Expression.DualOperator(
                        GD.Expression.Identifier("x"),
                        GD.Syntax.DualOperator(GDDualOperatorType.Multiply),
                        GD.Expression.Number(2)
                    )
                ))
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Lambda_Typed()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("adder", "Callable", GD.Expression.Lambda(
                    GD.List.Parameters(
                        GD.Declaration.Parameter("a", GD.Type.Single("int")),
                        GD.Declaration.Parameter("b", GD.Type.Single("int"))
                    ),
                    GD.Expression.DualOperator(
                        GD.Expression.Identifier("a"),
                        GD.Syntax.DualOperator(GDDualOperatorType.Addition),
                        GD.Expression.Identifier("b")
                    )
                ))
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        #endregion

        #region Triple Nested Lambda Tests

        [TestMethod]
        public void Lambda_TripleNested()
        {
            // Test: func(): func(): func(): 42
            var innermost = GD.Expression.Lambda(GD.Expression.Number(42));
            var middle = GD.Expression.Lambda(innermost);
            var outer = GD.Expression.Lambda(middle);

            var code = outer.ToString();

            // Should contain three "func" occurrences
            var funcCount = code.Split(new[] { "func" }, System.StringSplitOptions.None).Length - 1;
            Assert.AreEqual(3, funcCount, $"Expected 3 'func' keywords, got {funcCount}. Code: {code}");
            Assert.IsTrue(code.Contains("42"));
            AssertHelper.NoInvalidTokens(outer);
        }

        #endregion

        #region Lambda in Collections Tests

        [TestMethod]
        public void Lambda_InsideArrayInitializer()
        {
            // Test: [func(): 1, func(): 2, func(): 3]
            var expr = GD.Expression.Array(
                GD.Expression.Lambda(GD.Expression.Number(1)),
                GD.Expression.Lambda(GD.Expression.Number(2)),
                GD.Expression.Lambda(GD.Expression.Number(3))
            );

            var code = expr.ToString();

            var funcCount = code.Split(new[] { "func" }, System.StringSplitOptions.None).Length - 1;
            Assert.AreEqual(3, funcCount, $"Expected 3 'func' keywords in array");
            Assert.IsTrue(code.Contains("1"));
            Assert.IsTrue(code.Contains("2"));
            Assert.IsTrue(code.Contains("3"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void Lambda_InsideDictionaryInitializer()
        {
            // Test: {"add": func(a, b): a + b, "sub": func(a, b): a - b}
            var expr = GD.Expression.Dictionary(
                GD.Expression.KeyValue(
                    GD.Expression.String("add"),
                    GD.Expression.Lambda(
                        GD.List.Parameters(
                            GD.Declaration.Parameter("a"),
                            GD.Declaration.Parameter("b")
                        ),
                        GD.Expression.DualOperator(
                            GD.Expression.Identifier("a"),
                            GD.Syntax.DualOperator(GDDualOperatorType.Addition),
                            GD.Expression.Identifier("b")
                        )
                    )
                ),
                GD.Expression.KeyValue(
                    GD.Expression.String("sub"),
                    GD.Expression.Lambda(
                        GD.List.Parameters(
                            GD.Declaration.Parameter("a"),
                            GD.Declaration.Parameter("b")
                        ),
                        GD.Expression.DualOperator(
                            GD.Expression.Identifier("a"),
                            GD.Syntax.DualOperator(GDDualOperatorType.Subtraction),
                            GD.Expression.Identifier("b")
                        )
                    )
                )
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("\"add\""));
            Assert.IsTrue(code.Contains("\"sub\""));
            Assert.IsTrue(code.Contains("a + b"));
            Assert.IsTrue(code.Contains("a - b"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void Lambda_AsDictionaryValue()
        {
            // Test: var handlers = {"click": func(): handle_click()}
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("handlers", GD.Expression.Dictionary(
                    GD.Expression.KeyValue(
                        GD.Expression.String("click"),
                        GD.Expression.Lambda(
                            GD.Expression.Call(GD.Expression.Identifier("handle_click"))
                        )
                    )
                ))
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            Assert.IsTrue(code.Contains("var handlers"));
            Assert.IsTrue(code.Contains("\"click\""));
            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("handle_click()"));
            AssertHelper.NoInvalidTokens(classDecl);
        }

        #endregion

        #region Lambda with Control Flow Body Tests

        [TestMethod]
        public void Lambda_WithIfStatementInBody()
        {
            // Test: func(x):
            //     if x > 0:
            //         return x
            //     else:
            //         return -x
            var lambda = GD.Expression.Lambda(setup => setup
                .AddFuncKeyword()
                .AddOpenBracket()
                .AddParameters(GD.List.Parameters(GD.Declaration.Parameter("x")))
                .AddCloseBracket()
                .AddColon()
                .AddStatements(s => s
                    .AddNewLine()
                    .AddIntendation()
                    .Add<GDStatementsList, GDStatement>(GD.Statement.If(
                        GD.Branch.If(
                            GD.Expression.DualOperator(
                                GD.Expression.Identifier("x"),
                                GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                                GD.Expression.Number(0)
                            ),
                            GD.List.Statements(GD.Statement.Expression(
                                GD.Expression.Return(GD.Expression.Identifier("x"))
                            ))
                        ),
                        null,
                        GD.Branch.Else(GD.List.Statements(GD.Statement.Expression(
                            GD.Expression.Return(
                                GD.Expression.SingleOperator(
                                    GD.Syntax.SingleOperator(GDSingleOperatorType.Negate),
                                    GD.Expression.Identifier("x")
                                )
                            )
                        )))
                    ))
                )
            );

            var code = lambda.ToString();

            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("if"));
            Assert.IsTrue(code.Contains("x > 0"));
            Assert.IsTrue(code.Contains("return"));
            Assert.IsTrue(code.Contains("else"));
            AssertHelper.NoInvalidTokens(lambda);
        }

        [TestMethod]
        public void Lambda_WithForLoopInBody()
        {
            // Test: func(items):
            //     for item in items:
            //         process(item)
            var lambda = GD.Expression.Lambda(setup => setup
                .AddFuncKeyword()
                .AddOpenBracket()
                .AddParameters(GD.List.Parameters(GD.Declaration.Parameter("items")))
                .AddCloseBracket()
                .AddColon()
                .AddStatements(s => s
                    .AddNewLine()
                    .AddIntendation()
                    .Add<GDStatementsList, GDStatement>(GD.Statement.For(
                        GD.Syntax.Identifier("item"),
                        GD.Expression.Identifier("items"),
                        GD.Statement.Expression(
                            GD.Expression.Call(GD.Expression.Identifier("process"), GD.Expression.Identifier("item"))
                        )
                    ))
                )
            );

            var code = lambda.ToString();

            Assert.IsTrue(code.Contains("func(items)"));
            Assert.IsTrue(code.Contains("for"));
            Assert.IsTrue(code.Contains("item in items"));
            Assert.IsTrue(code.Contains("process(item)"));
            AssertHelper.NoInvalidTokens(lambda);
        }

        #endregion

        #region Lambda with Return Type Annotation Tests

        [TestMethod]
        public void Lambda_WithTypedParametersAndMultiplication()
        {
            // Test: func(x: int): x * 2
            var lambda = GD.Expression.Lambda(
                GD.List.Parameters(
                    GD.Declaration.Parameter("x", GD.Type.Single("int"))
                ),
                GD.Expression.DualOperator(
                    GD.Expression.Identifier("x"),
                    GD.Syntax.DualOperator(GDDualOperatorType.Multiply),
                    GD.Expression.Number(2)
                )
            );

            var code = lambda.ToString();

            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("x: int"));
            Assert.IsTrue(code.Contains("x * 2"));
            AssertHelper.NoInvalidTokens(lambda);
        }

        #endregion

        #region Lambda Higher-Order Function Tests

        [TestMethod]
        public void Lambda_PassedToHigherOrderFunction()
        {
            // Test: items.filter(func(x): x > 0)
            var expr = GD.Expression.Call(
                GD.Expression.Member(
                    GD.Expression.Identifier("items"),
                    GD.Syntax.Identifier("filter")
                ),
                GD.Expression.Lambda(
                    GD.List.Parameters(GD.Declaration.Parameter("x")),
                    GD.Expression.DualOperator(
                        GD.Expression.Identifier("x"),
                        GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                        GD.Expression.Number(0)
                    )
                )
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("items.filter"));
            Assert.IsTrue(code.Contains("func"));
            Assert.IsTrue(code.Contains("x"));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void Lambda_ChainedHigherOrderFunctions()
        {
            // Test: items.filter(func(x): x > 0).map(func(x): x * 2)
            var filterCall = GD.Expression.Call(
                GD.Expression.Member(
                    GD.Expression.Identifier("items"),
                    GD.Syntax.Identifier("filter")
                ),
                GD.Expression.Lambda(
                    GD.List.Parameters(GD.Declaration.Parameter("x")),
                    GD.Expression.DualOperator(
                        GD.Expression.Identifier("x"),
                        GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                        GD.Expression.Number(0)
                    )
                )
            );

            var expr = GD.Expression.Call(
                GD.Expression.Member(filterCall, GD.Syntax.Identifier("map")),
                GD.Expression.Lambda(
                    GD.List.Parameters(GD.Declaration.Parameter("x")),
                    GD.Expression.DualOperator(
                        GD.Expression.Identifier("x"),
                        GD.Syntax.DualOperator(GDDualOperatorType.Multiply),
                        GD.Expression.Number(2)
                    )
                )
            );

            var code = expr.ToString();

            Assert.IsTrue(code.Contains("filter"));
            Assert.IsTrue(code.Contains("map"));
            Assert.IsTrue(code.Contains("x > 0"));
            Assert.IsTrue(code.Contains("x * 2"));
            AssertHelper.NoInvalidTokens(expr);
        }

        #endregion

        #region Additional Round-Trip Tests

        [TestMethod]
        public void RoundTrip_Lambda_InArray()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("funcs", GD.Expression.Array(
                    GD.Expression.Lambda(GD.Expression.Number(1)),
                    GD.Expression.Lambda(GD.Expression.Number(2))
                ))
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Lambda_WithControlFlow()
        {
            // Lambda with ternary if expression
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("abs_func", GD.Expression.Lambda(
                    GD.List.Parameters(GD.Declaration.Parameter("x")),
                    GD.Expression.If(
                        GD.Expression.DualOperator(
                            GD.Expression.Identifier("x"),
                            GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                            GD.Expression.Number(0)
                        ),
                        GD.Expression.Identifier("x"),
                        GD.Expression.SingleOperator(
                            GD.Syntax.SingleOperator(GDSingleOperatorType.Negate),
                            GD.Expression.Identifier("x")
                        )
                    )
                ))
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        #endregion
    }
}

