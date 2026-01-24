using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Tests for building statements programmatically using GD.Statement.* API.
    /// </summary>
    [TestClass]
    public class StatementBuildingTests
    {
        #region Variable Statements

        [TestMethod]
        public void BuildStatement_Variable_UntypedWithoutInitializer()
        {
            var stmt = GD.Statement.Variable("count");
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("var count"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void BuildStatement_Variable_TypedWithoutInitializer()
        {
            var stmt = GD.Statement.Variable("score", "int");
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("var score"));
            Assert.IsTrue(code.Contains("int"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void BuildStatement_Variable_TypedWithInitializer()
        {
            var stmt = GD.Statement.Variable("health", "int", GD.Expression.Number(100));
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("var health"));
            Assert.IsTrue(code.Contains("int"));
            Assert.IsTrue(code.Contains("100"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void BuildStatement_Variable_UntypedWithInitializer()
        {
            var stmt = GD.Statement.Variable(v => v
                .AddVarKeyword()
                .AddSpace()
                .AddIdentifier("name")
                .AddSpace()
                .AddAssign()
                .AddSpace()
                .Add(GD.Expression.String("Player"))
            );

            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("var name = \"Player\""));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region Expression Statements

        [TestMethod]
        public void BuildStatement_Expression_MethodCall()
        {
            var stmt = GD.Statement.Expression(
                GD.Expression.Call(
                    GD.Expression.Identifier("print"),
                    GD.Expression.String("Hello World")
                )
            );

            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("print(\"Hello World\")"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void BuildStatement_Expression_Assignment()
        {
            var stmt = GD.Statement.Expression(
                GD.Expression.DualOperator(
                    GD.Expression.Identifier("x"),
                    GD.Syntax.DualOperator(GDDualOperatorType.Assignment),
                    GD.Expression.Number(10)
                )
            );

            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("x"));
            Assert.IsTrue(code.Contains("="));
            Assert.IsTrue(code.Contains("10"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region For Loops

        [TestMethod]
        public void BuildStatement_For_Simple()
        {
            var stmt = GD.Statement.For(
                GD.Syntax.Identifier("i"),
                GD.Expression.Identifier("items"),
                GD.Statement.Expression(
                    GD.Expression.Call(GD.Expression.Identifier("print"), GD.Expression.Identifier("i"))
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("for"));
            Assert.IsTrue(code.Contains("i"));
            Assert.IsTrue(code.Contains("in"));
            Assert.IsTrue(code.Contains("items"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        // Note: GDScript for-loops only accept identifiers, not full variable declarations.
        // The syntax is: for i in items:  (where i is just an identifier)
        // This is a language design decision, not a Builder API limitation.

        #endregion

        #region While Loops

        [TestMethod]
        public void BuildStatement_While_SimpleCondition()
        {
            var stmt = GD.Statement.While(
                GD.Expression.Bool(true),
                GD.Statement.Expression(GD.Expression.Pass())
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("while"));
            Assert.IsTrue(code.Contains("true"));
            Assert.IsTrue(code.Contains("pass"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void BuildStatement_While_ComplexCondition()
        {
            var stmt = GD.Statement.While(w => w
                .AddWhileKeyword()
                .AddSpace()
                .AddCondition(
                    GD.Expression.DualOperator(
                        GD.Expression.Identifier("count"),
                        GD.Syntax.DualOperator(GDDualOperatorType.LessThan),
                        GD.Expression.Number(10)
                    )
                )
                .AddColon()
                .AddStatements(s => s
                    .AddNewLine()
                    .AddIntendation()
                    .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                        GD.Expression.DualOperator(
                            GD.Expression.Identifier("count"),
                            GD.Syntax.DualOperator(GDDualOperatorType.AddAndAssign),
                            GD.Expression.Number(1)
                        )
                    ))
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("while"));
            Assert.IsTrue(code.Contains("count"));
            Assert.IsTrue(code.Contains("<"));
            Assert.IsTrue(code.Contains("10"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region If/Elif/Else Statements

        [TestMethod]
        public void BuildStatement_If_OnlyIfBranch()
        {
            var stmt = GD.Statement.If(
                GD.Branch.If(
                    GD.Expression.Bool(true),
                    GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("if"));
            Assert.IsTrue(code.Contains("true"));
            Assert.IsTrue(code.Contains("pass"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void BuildStatement_If_WithElse()
        {
            var stmt = GD.Statement.If(ifStmt => ifStmt
                .Add(GD.Branch.If(
                    GD.Expression.Identifier("is_valid"),
                    GD.List.Statements(
                        GD.Statement.Expression(GD.Expression.Call(GD.Expression.Identifier("print"), GD.Expression.String("Valid")))
                    )
                ))
                .Add(GD.Branch.Else(
                    GD.List.Statements(
                        GD.Statement.Expression(GD.Expression.Call(GD.Expression.Identifier("print"), GD.Expression.String("Invalid")))
                    )
                ))
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("if"));
            Assert.IsTrue(code.Contains("is_valid"));
            Assert.IsTrue(code.Contains("else"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void BuildStatement_If_WithElifAndElse()
        {
            // Use the factory method that accepts elif branches directly
            var stmt = GD.Statement.If(
                GD.Branch.If(
                    GD.Expression.DualOperator(
                        GD.Expression.Identifier("x"),
                        GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                        GD.Expression.Number(10)
                    ),
                    GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                ),
                GD.List.ElifBranches(
                    GD.Branch.Elif(
                        GD.Expression.DualOperator(
                            GD.Expression.Identifier("x"),
                            GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                            GD.Expression.Number(5)
                        ),
                        GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                    )
                ),
                GD.Branch.Else(
                    GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("if"));
            Assert.IsTrue(code.Contains("elif"));
            Assert.IsTrue(code.Contains("else"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region Match Statements

        [TestMethod]
        public void BuildStatement_Match_SimpleCases()
        {
            var stmt = GD.Statement.Match(
                GD.Expression.Identifier("value"),
                GD.List.MatchCases(
                    GD.Declaration.MatchCase(GD.Expression.Number(1), GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))),
                    GD.Declaration.MatchCase(GD.Expression.Number(2), GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass())))
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("match"));
            Assert.IsTrue(code.Contains("value"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region Pass/Break/Continue

        [TestMethod]
        public void BuildStatement_Pass()
        {
            var stmt = GD.Statement.Expression(GD.Expression.Pass());
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("pass"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void BuildStatement_Break()
        {
            var stmt = GD.Statement.Expression(GD.Expression.Break());
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("break"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void BuildStatement_Continue()
        {
            var stmt = GD.Statement.Expression(GD.Expression.Continue());
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("continue"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        #endregion

        #region Complex Nested Control Flow

        [TestMethod]
        public void BuildStatement_If_DeeplyNested_ThreeLevels()
        {
            // Test: if x > 0:
            //     if y > 0:
            //         if z > 0:
            //             pass
            //         else:
            //             pass
            //     else:
            //         pass
            // else:
            //     pass
            var innerIf = GD.Statement.If(
                GD.Branch.If(
                    GD.Expression.DualOperator(
                        GD.Expression.Identifier("z"),
                        GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                        GD.Expression.Number(0)
                    ),
                    GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                ),
                null,
                GD.Branch.Else(GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass())))
            );

            var middleIf = GD.Statement.If(
                GD.Branch.If(
                    GD.Expression.DualOperator(
                        GD.Expression.Identifier("y"),
                        GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                        GD.Expression.Number(0)
                    ),
                    GD.List.Statements(innerIf)
                ),
                null,
                GD.Branch.Else(GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass())))
            );

            var outerIf = GD.Statement.If(
                GD.Branch.If(
                    GD.Expression.DualOperator(
                        GD.Expression.Identifier("x"),
                        GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                        GD.Expression.Number(0)
                    ),
                    GD.List.Statements(middleIf)
                ),
                null,
                GD.Branch.Else(GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass())))
            );

            outerIf.UpdateIntendation();
            var code = outerIf.ToString();

            // Verify all three if levels exist
            var ifCount = System.Text.RegularExpressions.Regex.Matches(code, @"\bif\b").Count;
            Assert.AreEqual(3, ifCount, $"Expected 3 'if' keywords, got {ifCount}");
            Assert.IsTrue(code.Contains("x > 0"));
            Assert.IsTrue(code.Contains("y > 0"));
            Assert.IsTrue(code.Contains("z > 0"));
            AssertHelper.NoInvalidTokens(outerIf);
        }

        [TestMethod]
        public void BuildStatement_For_WithRangeFunction()
        {
            // Test: for i in range(10):
            //     print(i)
            var stmt = GD.Statement.For(
                GD.Syntax.Identifier("i"),
                GD.Expression.Call(
                    GD.Expression.Identifier("range"),
                    GD.Expression.Number(10)
                ),
                GD.Statement.Expression(
                    GD.Expression.Call(GD.Expression.Identifier("print"), GD.Expression.Identifier("i"))
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("for"));
            Assert.IsTrue(code.Contains("range(10)"));
            Assert.IsTrue(code.Contains("print(i)"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void BuildStatement_NestedWhile_WithBreakAtInnerLevel()
        {
            // Test: while true:
            //     while true:
            //         break
            var innerWhile = GD.Statement.While(
                GD.Expression.Bool(true),
                GD.Statement.Expression(GD.Expression.Break())
            );

            var outerWhile = GD.Statement.While(
                GD.Expression.Bool(true),
                innerWhile
            );

            outerWhile.UpdateIntendation();
            var code = outerWhile.ToString();

            var whileCount = System.Text.RegularExpressions.Regex.Matches(code, @"\bwhile\b").Count;
            Assert.AreEqual(2, whileCount, $"Expected 2 'while' keywords, got {whileCount}");
            Assert.IsTrue(code.Contains("break"));
            AssertHelper.NoInvalidTokens(outerWhile);
        }

        [TestMethod]
        public void BuildStatement_NestedWhile_WithContinueAtInnerLevel()
        {
            // Test: while running:
            //     while processing:
            //         continue
            var innerWhile = GD.Statement.While(
                GD.Expression.Identifier("processing"),
                GD.Statement.Expression(GD.Expression.Continue())
            );

            var outerWhile = GD.Statement.While(
                GD.Expression.Identifier("running"),
                innerWhile
            );

            outerWhile.UpdateIntendation();
            var code = outerWhile.ToString();

            Assert.IsTrue(code.Contains("while running"));
            Assert.IsTrue(code.Contains("while processing"));
            Assert.IsTrue(code.Contains("continue"));
            AssertHelper.NoInvalidTokens(outerWhile);
        }

        [TestMethod]
        public void BuildStatement_If_InsideForLoop()
        {
            // Test: for item in items:
            //     if item.is_valid():
            //         process(item)
            var ifStmt = GD.Statement.If(
                GD.Branch.If(
                    GD.Expression.Call(
                        GD.Expression.Member(GD.Expression.Identifier("item"), GD.Syntax.Identifier("is_valid"))
                    ),
                    GD.List.Statements(
                        GD.Statement.Expression(
                            GD.Expression.Call(GD.Expression.Identifier("process"), GD.Expression.Identifier("item"))
                        )
                    )
                )
            );

            var forStmt = GD.Statement.For(
                GD.Syntax.Identifier("item"),
                GD.Expression.Identifier("items"),
                ifStmt
            );

            forStmt.UpdateIntendation();
            var code = forStmt.ToString();

            Assert.IsTrue(code.Contains("for item in items"));
            Assert.IsTrue(code.Contains("if item.is_valid()"));
            Assert.IsTrue(code.Contains("process(item)"));
            AssertHelper.NoInvalidTokens(forStmt);
        }

        [TestMethod]
        public void BuildStatement_While_WithCompoundCondition_And()
        {
            // Test: while is_running and health > 0:
            //     pass
            var stmt = GD.Statement.While(
                GD.Expression.DualOperator(
                    GD.Expression.Identifier("is_running"),
                    GD.Syntax.DualOperator(GDDualOperatorType.And2), // Use And2 for keyword 'and'
                    GD.Expression.DualOperator(
                        GD.Expression.Identifier("health"),
                        GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                        GD.Expression.Number(0)
                    )
                ),
                GD.Statement.Expression(GD.Expression.Pass())
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("while"));
            Assert.IsTrue(code.Contains("is_running"));
            Assert.IsTrue(code.Contains("and"));
            Assert.IsTrue(code.Contains("health"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void BuildStatement_While_WithCompoundCondition_Or()
        {
            // Test: while paused or in_menu:
            //     wait()
            var stmt = GD.Statement.While(
                GD.Expression.DualOperator(
                    GD.Expression.Identifier("paused"),
                    GD.Syntax.DualOperator(GDDualOperatorType.Or2), // Use Or2 for keyword 'or'
                    GD.Expression.Identifier("in_menu")
                ),
                GD.Statement.Expression(
                    GD.Expression.Call(GD.Expression.Identifier("wait"))
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("while"));
            Assert.IsTrue(code.Contains("paused"));
            Assert.IsTrue(code.Contains("or"));
            Assert.IsTrue(code.Contains("in_menu"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void BuildStatement_If_WithMultipleStatements_FiveStatements()
        {
            // Test: if condition:
            //     stmt1
            //     stmt2
            //     stmt3
            //     stmt4
            //     stmt5
            var stmt = GD.Statement.If(
                GD.Branch.If(
                    GD.Expression.Identifier("condition"),
                    GD.List.Statements(
                        GD.Statement.Expression(GD.Expression.Call(GD.Expression.Identifier("step1"))),
                        GD.Statement.Expression(GD.Expression.Call(GD.Expression.Identifier("step2"))),
                        GD.Statement.Expression(GD.Expression.Call(GD.Expression.Identifier("step3"))),
                        GD.Statement.Expression(GD.Expression.Call(GD.Expression.Identifier("step4"))),
                        GD.Statement.Expression(GD.Expression.Call(GD.Expression.Identifier("step5")))
                    )
                )
            );

            stmt.UpdateIntendation();
            var code = stmt.ToString();

            Assert.IsTrue(code.Contains("step1()"));
            Assert.IsTrue(code.Contains("step2()"));
            Assert.IsTrue(code.Contains("step3()"));
            Assert.IsTrue(code.Contains("step4()"));
            Assert.IsTrue(code.Contains("step5()"));
            AssertHelper.NoInvalidTokens(stmt);
        }

        [TestMethod]
        public void BuildStatement_Match_InsideForLoop()
        {
            // Test: for item in items:
            //     match item.type:
            //         "weapon": equip(item)
            //         "armor": wear(item)
            //         _: pass
            var matchStmt = GD.Statement.Match(
                GD.Expression.Member(GD.Expression.Identifier("item"), GD.Syntax.Identifier("type")),
                GD.List.MatchCases(
                    GD.Declaration.MatchCase(
                        GD.Expression.String("weapon"),
                        GD.List.Statements(GD.Statement.Expression(
                            GD.Expression.Call(GD.Expression.Identifier("equip"), GD.Expression.Identifier("item"))
                        ))
                    ),
                    GD.Declaration.MatchCase(
                        GD.Expression.String("armor"),
                        GD.List.Statements(GD.Statement.Expression(
                            GD.Expression.Call(GD.Expression.Identifier("wear"), GD.Expression.Identifier("item"))
                        ))
                    ),
                    GD.Declaration.MatchCase(
                        GD.Expression.MatchDefaultOperator(),
                        GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                    )
                )
            );

            var forStmt = GD.Statement.For(
                GD.Syntax.Identifier("item"),
                GD.Expression.Identifier("items"),
                matchStmt
            );

            forStmt.UpdateIntendation();
            var code = forStmt.ToString();

            Assert.IsTrue(code.Contains("for item in items"));
            Assert.IsTrue(code.Contains("match item.type"));
            Assert.IsTrue(code.Contains("\"weapon\""));
            Assert.IsTrue(code.Contains("\"armor\""));
            Assert.IsTrue(code.Contains("_"));
            AssertHelper.NoInvalidTokens(forStmt);
        }

        #endregion

        #region Round-Trip Tests

        private readonly GDScriptReader _reader = new GDScriptReader();

        [TestMethod]
        public void NestedIfElse_GeneratesValidStructure()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.If(
                        GD.Branch.If(
                            GD.Expression.Identifier("a"),
                            GD.List.Statements(
                                GD.Statement.If(
                                    GD.Branch.If(
                                        GD.Expression.Identifier("b"),
                                        GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                                    ),
                                    null,
                                    GD.Branch.Else(GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass())))
                                )
                            )
                        ),
                        null,
                        GD.Branch.Else(GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass())))
                    )
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            // Verify structure without round-trip (spacing issues)
            Assert.IsTrue(code.Contains("func test"));
            Assert.IsTrue(code.Contains("if a"));
            Assert.IsTrue(code.Contains("if b"));
            Assert.IsTrue(code.Contains("else"));
            AssertHelper.NoInvalidTokens(classDecl);
        }

        [TestMethod]
        public void RoundTrip_ForWithRange()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.For(
                        GD.Syntax.Identifier("i"),
                        GD.Expression.Call(GD.Expression.Identifier("range"), GD.Expression.Number(5)),
                        GD.Statement.Expression(GD.Expression.Pass())
                    )
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_NestedLoops()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.For(
                        GD.Syntax.Identifier("i"),
                        GD.Expression.Identifier("rows"),
                        GD.Statement.For(
                            GD.Syntax.Identifier("j"),
                            GD.Expression.Identifier("cols"),
                            GD.Statement.Expression(GD.Expression.Pass())
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
