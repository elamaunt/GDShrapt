using System.Linq;
using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Tests for advanced match pattern building.
    /// Covers binding patterns, rest patterns, default patterns, and complex match scenarios.
    /// </summary>
    [TestClass]
    public class MatchPatternTests
    {
        private readonly GDScriptReader _reader = new GDScriptReader();

        #region Binding Pattern Tests

        [TestMethod]
        public void Match_WithBindingPattern_VarFirst()
        {
            // Test: match x:
            //     var captured:
            //         pass
            var matchCase = GD.Declaration.MatchCase(
                GD.Expression.MatchCaseVariable("captured"),
                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
            );

            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("x"),
                GD.List.MatchCases(matchCase)
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("match"));
            Assert.IsTrue(code.Contains("var captured"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void Match_WithBindingPattern_InArray()
        {
            // Test: match arr:
            //     [var first, var second]:
            //         pass
            var arrayPattern = GD.Expression.Array(
                GD.Expression.MatchCaseVariable("first"),
                GD.Expression.MatchCaseVariable("second")
            );

            var matchCase = GD.Declaration.MatchCase(
                arrayPattern,
                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
            );

            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("arr"),
                GD.List.MatchCases(matchCase)
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("var first"));
            Assert.IsTrue(code.Contains("var second"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region Rest Pattern Tests

        [TestMethod]
        public void Match_WithRestPattern_DoubleDot()
        {
            // Test: match arr:
            //     [1, ..]:
            //         pass
            var arrayPattern = GD.Expression.Array(
                GD.Expression.Number(1),
                GD.Expression.Rest()
            );

            var matchCase = GD.Declaration.MatchCase(
                arrayPattern,
                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
            );

            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("arr"),
                GD.List.MatchCases(matchCase)
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains(".."));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void Match_WithBindingAndRest_Combined()
        {
            // Test: match arr:
            //     [var first, ..]:
            //         pass
            var arrayPattern = GD.Expression.Array(
                GD.Expression.MatchCaseVariable("first"),
                GD.Expression.Rest()
            );

            var matchCase = GD.Declaration.MatchCase(
                arrayPattern,
                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
            );

            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("arr"),
                GD.List.MatchCases(matchCase)
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("var first"));
            Assert.IsTrue(code.Contains(".."));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region Default Pattern Tests

        [TestMethod]
        public void Match_WithDefaultPattern_Underscore()
        {
            // Test: match x:
            //     _:
            //         pass
            var matchCase = GD.Declaration.MatchCase(
                GD.Expression.MatchDefaultOperator(),
                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
            );

            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("x"),
                GD.List.MatchCases(matchCase)
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("_"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void Match_DefaultCatchAll()
        {
            // Test: match value:
            //     1: pass
            //     2: pass
            //     _: pass  # catch-all
            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("value"),
                GD.List.MatchCases(
                    GD.Declaration.MatchCase(GD.Expression.Number(1), GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))),
                    GD.Declaration.MatchCase(GD.Expression.Number(2), GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))),
                    GD.Declaration.MatchCase(GD.Expression.MatchDefaultOperator(), GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass())))
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("1"));
            Assert.IsTrue(code.Contains("2"));
            Assert.IsTrue(code.Contains("_"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region Dictionary Pattern Tests

        [TestMethod]
        public void Match_WithDictionaryPattern_LiteralValue()
        {
            // Test: match dict:
            //     {"key": 42}:
            //         pass
            // Note: GDScript dictionary patterns match against literal values, not var bindings
            var dictPattern = GD.Expression.Dictionary(
                GD.Expression.KeyValue(
                    GD.Expression.String("key"),
                    GD.Expression.Number(42)
                )
            );

            var matchCase = GD.Declaration.MatchCase(
                dictPattern,
                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
            );

            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("dict"),
                GD.List.MatchCases(matchCase)
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("\"key\""));
            Assert.IsTrue(code.Contains("42"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region Complex Pattern Tests

        [TestMethod]
        public void Match_WithNestedArrayPattern()
        {
            // Test: match nested:
            //     [[var a, var b], var c]:
            //         pass
            var innerArray = GD.Expression.Array(
                GD.Expression.MatchCaseVariable("a"),
                GD.Expression.MatchCaseVariable("b")
            );

            var outerArray = GD.Expression.Array(
                innerArray,
                GD.Expression.MatchCaseVariable("c")
            );

            var matchCase = GD.Declaration.MatchCase(
                outerArray,
                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
            );

            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("nested"),
                GD.List.MatchCases(matchCase)
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("var a"));
            Assert.IsTrue(code.Contains("var b"));
            Assert.IsTrue(code.Contains("var c"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void Match_WithEnumMemberPattern()
        {
            // Test: match state:
            //     State.IDLE:
            //         pass
            //     State.RUNNING:
            //         pass
            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("state"),
                GD.List.MatchCases(
                    GD.Declaration.MatchCase(
                        GD.Expression.Member(GD.Expression.Identifier("State"), GD.Syntax.Identifier("IDLE")),
                        GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                    ),
                    GD.Declaration.MatchCase(
                        GD.Expression.Member(GD.Expression.Identifier("State"), GD.Syntax.Identifier("RUNNING")),
                        GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                    )
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("State.IDLE"));
            Assert.IsTrue(code.Contains("State.RUNNING"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region Round-Trip Tests

        [TestMethod]
        public void RoundTrip_Match_BindingPattern()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.Match(
                        GD.Expression.Identifier("x"),
                        GD.List.MatchCases(
                            GD.Declaration.MatchCase(
                                GD.Expression.MatchCaseVariable("captured"),
                                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                            )
                        )
                    )
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Match_RestPattern()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.Match(
                        GD.Expression.Identifier("arr"),
                        GD.List.MatchCases(
                            GD.Declaration.MatchCase(
                                GD.Expression.Array(GD.Expression.Number(1), GD.Expression.Rest()),
                                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                            )
                        )
                    )
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Match_EmptyDictionaryPattern()
        {
            // Empty dictionary pattern
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.Match(
                        GD.Expression.Identifier("dict"),
                        GD.List.MatchCases(
                            GD.Declaration.MatchCase(
                                GD.Expression.Dictionary(),
                                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                            )
                        )
                    )
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Match_DefaultPattern()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.Match(
                        GD.Expression.Identifier("x"),
                        GD.List.MatchCases(
                            GD.Declaration.MatchCase(GD.Expression.Number(1), GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))),
                            GD.Declaration.MatchCase(GD.Expression.MatchDefaultOperator(), GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass())))
                        )
                    )
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Match_NestedPattern()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.Match(
                        GD.Expression.Identifier("data"),
                        GD.List.MatchCases(
                            GD.Declaration.MatchCase(
                                GD.Expression.Array(
                                    GD.Expression.Array(GD.Expression.MatchCaseVariable("a")),
                                    GD.Expression.MatchCaseVariable("b")
                                ),
                                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                            )
                        )
                    )
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        #endregion

        #region Multiple Patterns (OR)

        [TestMethod]
        public void Match_WithMultipleCases()
        {
            // Test: match value with multiple separate cases
            //     1: pass
            //     2: pass
            //     3: pass
            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("value"),
                GD.List.MatchCases(
                    GD.Declaration.MatchCase(GD.Expression.Number(1), GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))),
                    GD.Declaration.MatchCase(GD.Expression.Number(2), GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))),
                    GD.Declaration.MatchCase(GD.Expression.Number(3), GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass())))
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("1"));
            Assert.IsTrue(code.Contains("2"));
            Assert.IsTrue(code.Contains("3"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region Numeric Pattern Tests

        [TestMethod]
        public void Match_WithNumericPatterns()
        {
            // Test: match score:
            //     0: print("zero")
            //     50: print("half")
            //     100: print("full")
            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("score"),
                GD.List.MatchCases(
                    GD.Declaration.MatchCase(
                        GD.Expression.Number(0),
                        GD.List.Statements(GD.Statement.Expression(
                            GD.Expression.Call(GD.Expression.Identifier("print"), GD.Expression.String("zero"))
                        ))
                    ),
                    GD.Declaration.MatchCase(
                        GD.Expression.Number(50),
                        GD.List.Statements(GD.Statement.Expression(
                            GD.Expression.Call(GD.Expression.Identifier("print"), GD.Expression.String("half"))
                        ))
                    ),
                    GD.Declaration.MatchCase(
                        GD.Expression.Number(100),
                        GD.List.Statements(GD.Statement.Expression(
                            GD.Expression.Call(GD.Expression.Identifier("print"), GD.Expression.String("full"))
                        ))
                    )
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("0"));
            Assert.IsTrue(code.Contains("50"));
            Assert.IsTrue(code.Contains("100"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region String Pattern Tests

        [TestMethod]
        public void Match_WithStringPatterns()
        {
            // Test: match command:
            //     "start": handle_start()
            //     "stop": handle_stop()
            //     "pause": handle_pause()
            //     _: pass
            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("command"),
                GD.List.MatchCases(
                    GD.Declaration.MatchCase(
                        GD.Expression.String("start"),
                        GD.List.Statements(GD.Statement.Expression(
                            GD.Expression.Call(GD.Expression.Identifier("handle_start"))
                        ))
                    ),
                    GD.Declaration.MatchCase(
                        GD.Expression.String("stop"),
                        GD.List.Statements(GD.Statement.Expression(
                            GD.Expression.Call(GD.Expression.Identifier("handle_stop"))
                        ))
                    ),
                    GD.Declaration.MatchCase(
                        GD.Expression.String("pause"),
                        GD.List.Statements(GD.Statement.Expression(
                            GD.Expression.Call(GD.Expression.Identifier("handle_pause"))
                        ))
                    ),
                    GD.Declaration.MatchCase(
                        GD.Expression.MatchDefaultOperator(),
                        GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                    )
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("\"start\""));
            Assert.IsTrue(code.Contains("\"stop\""));
            Assert.IsTrue(code.Contains("\"pause\""));
            Assert.IsTrue(code.Contains("handle_start()"));
            Assert.IsTrue(code.Contains("handle_stop()"));
            Assert.IsTrue(code.Contains("handle_pause()"));
            Assert.IsTrue(code.Contains("_"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region Complex Real-World Patterns

        [TestMethod]
        public void Match_ComplexStateMachine()
        {
            // Test: match [state, event]:
            //     [State.IDLE, Event.START]:
            //         transition_to(State.RUNNING)
            //     [State.RUNNING, Event.STOP]:
            //         transition_to(State.IDLE)
            //     _:
            //         pass
            var stmt = GD.Statement.Match(
                GD.Expression.Array(
                    GD.Expression.Identifier("state"),
                    GD.Expression.Identifier("event")
                ),
                GD.List.MatchCases(
                    GD.Declaration.MatchCase(
                        GD.Expression.Array(
                            GD.Expression.Member(GD.Expression.Identifier("State"), GD.Syntax.Identifier("IDLE")),
                            GD.Expression.Member(GD.Expression.Identifier("Event"), GD.Syntax.Identifier("START"))
                        ),
                        GD.List.Statements(GD.Statement.Expression(
                            GD.Expression.Call(
                                GD.Expression.Identifier("transition_to"),
                                GD.Expression.Member(GD.Expression.Identifier("State"), GD.Syntax.Identifier("RUNNING"))
                            )
                        ))
                    ),
                    GD.Declaration.MatchCase(
                        GD.Expression.Array(
                            GD.Expression.Member(GD.Expression.Identifier("State"), GD.Syntax.Identifier("RUNNING")),
                            GD.Expression.Member(GD.Expression.Identifier("Event"), GD.Syntax.Identifier("STOP"))
                        ),
                        GD.List.Statements(GD.Statement.Expression(
                            GD.Expression.Call(
                                GD.Expression.Identifier("transition_to"),
                                GD.Expression.Member(GD.Expression.Identifier("State"), GD.Syntax.Identifier("IDLE"))
                            )
                        ))
                    ),
                    GD.Declaration.MatchCase(
                        GD.Expression.MatchDefaultOperator(),
                        GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                    )
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("State.IDLE"));
            Assert.IsTrue(code.Contains("Event.START"));
            Assert.IsTrue(code.Contains("State.RUNNING"));
            Assert.IsTrue(code.Contains("Event.STOP"));
            Assert.IsTrue(code.Contains("transition_to"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void Match_WithMultipleCasesAndMultipleStatements()
        {
            // Test: match action:
            //     "attack":
            //         play_animation("attack")
            //         deal_damage()
            //         cooldown()
            //     "defend":
            //         play_animation("defend")
            //         reduce_damage()
            //     _:
            //         pass
            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("action"),
                GD.List.MatchCases(
                    GD.Declaration.MatchCase(
                        GD.Expression.String("attack"),
                        GD.List.Statements(
                            GD.Statement.Expression(GD.Expression.Call(
                                GD.Expression.Identifier("play_animation"),
                                GD.Expression.String("attack")
                            )),
                            GD.Statement.Expression(GD.Expression.Call(GD.Expression.Identifier("deal_damage"))),
                            GD.Statement.Expression(GD.Expression.Call(GD.Expression.Identifier("cooldown")))
                        )
                    ),
                    GD.Declaration.MatchCase(
                        GD.Expression.String("defend"),
                        GD.List.Statements(
                            GD.Statement.Expression(GD.Expression.Call(
                                GD.Expression.Identifier("play_animation"),
                                GD.Expression.String("defend")
                            )),
                            GD.Statement.Expression(GD.Expression.Call(GD.Expression.Identifier("reduce_damage")))
                        )
                    ),
                    GD.Declaration.MatchCase(
                        GD.Expression.MatchDefaultOperator(),
                        GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                    )
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("play_animation(\"attack\")"));
            Assert.IsTrue(code.Contains("deal_damage()"));
            Assert.IsTrue(code.Contains("cooldown()"));
            Assert.IsTrue(code.Contains("play_animation(\"defend\")"));
            Assert.IsTrue(code.Contains("reduce_damage()"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region Additional Round-Trip Tests

        [TestMethod]
        public void RoundTrip_Match_MultipleStatements()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.Match(
                        GD.Expression.Identifier("x"),
                        GD.List.MatchCases(
                            GD.Declaration.MatchCase(
                                GD.Expression.Number(1),
                                GD.List.Statements(
                                    GD.Statement.Expression(GD.Expression.Call(GD.Expression.Identifier("a"))),
                                    GD.Statement.Expression(GD.Expression.Call(GD.Expression.Identifier("b")))
                                )
                            ),
                            GD.Declaration.MatchCase(
                                GD.Expression.MatchDefaultOperator(),
                                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                            )
                        )
                    )
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Match_ArrayOfTuple()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.Match(
                        GD.Expression.Array(
                            GD.Expression.Identifier("a"),
                            GD.Expression.Identifier("b")
                        ),
                        GD.List.MatchCases(
                            GD.Declaration.MatchCase(
                                GD.Expression.Array(GD.Expression.Number(1), GD.Expression.Number(2)),
                                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                            ),
                            GD.Declaration.MatchCase(
                                GD.Expression.MatchDefaultOperator(),
                                GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                            )
                        )
                    )
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        #endregion
    }
}
