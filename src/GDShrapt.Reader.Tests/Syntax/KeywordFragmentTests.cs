using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    [TestClass]
    public class KeywordFragmentTests
    {
        #region Class Level — Prefix Match (extra space in keyword)

        [TestMethod]
        public void ClassLevel_VaR_SuggestsVar()
        {
            var reader = new GDScriptReader();
            var code = "va r x = 5";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("va");
            first.PossibleKeyword.Should().Be("var");
            first.StartsWithKeyword.Should().BeNull();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_FuNc_SuggestsFunc()
        {
            var reader = new GDScriptReader();
            var code = "fu nc test():\n\tpass";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("fu");
            first.PossibleKeyword.Should().Be("func");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_ClaSs_SuggestsClass()
        {
            var reader = new GDScriptReader();
            var code = "cla ss MyClass:\n\tpass";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("cla");
            first.PossibleKeyword.Should().Be("class");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_SigNal_SuggestsSignal()
        {
            var reader = new GDScriptReader();
            var code = "sig nal my_signal";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("sig");
            first.PossibleKeyword.Should().Be("signal");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_EnuM_SuggestsEnum()
        {
            var reader = new GDScriptReader();
            var code = "enu m MyEnum { A }";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("enu");
            first.PossibleKeyword.Should().Be("enum");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_ConSt_SuggestsConst()
        {
            var reader = new GDScriptReader();
            var code = "con st X = 5";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("con");
            first.PossibleKeyword.Should().Be("const");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_StaTic_SuggestsStatic()
        {
            var reader = new GDScriptReader();
            var code = "sta tic func test():\n\tpass";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("sta");
            first.PossibleKeyword.Should().Be("static");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_ExtenDs_SuggestsExtends()
        {
            var reader = new GDScriptReader();
            var code = "exten ds Node";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("exten");
            first.PossibleKeyword.Should().Be("extends");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_ClassUnderscoreName_SuggestsClassName()
        {
            var reader = new GDScriptReader();
            var code = "class_ name MyClass";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("class_");
            first.PossibleKeyword.Should().Be("class_name");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_ToOl_SuggestsTool()
        {
            var reader = new GDScriptReader();
            var code = "to ol";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("to");
            first.PossibleKeyword.Should().Be("tool");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_PaSs_SuggestsPass()
        {
            var reader = new GDScriptReader();
            var code = "pa ss";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("pa");
            first.PossibleKeyword.Should().Be("pass");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        #endregion

        #region Class Level — Glued Keyword+Identifier (missing space)

        [TestMethod]
        public void ClassLevel_Varx_StartsWithVar()
        {
            var reader = new GDScriptReader();
            var code = "varx = 5";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("varx");
            first.PossibleKeyword.Should().BeNull();
            first.StartsWithKeyword.Should().Be("var");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_Functest_StartsWithFunc()
        {
            var reader = new GDScriptReader();
            var code = "functest():\n\tpass";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            // Invalid token consumes all non-space/non-newline chars: "functest():"
            // StartsWithKeyword is set based on the resolver sequence "functest"
            var first = tree.AllInvalidTokens.First();
            first.StartsWithKeyword.Should().Be("func");
            first.PossibleKeyword.Should().BeNull();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_ClassMyClass_StartsWithClass()
        {
            var reader = new GDScriptReader();
            var code = "classMyClass:\n\tpass";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            // Invalid token consumes "classMyClass:" (colon is not space/newline)
            var first = tree.AllInvalidTokens.First();
            first.StartsWithKeyword.Should().Be("class");
            first.PossibleKeyword.Should().BeNull();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_ConstX_StartsWithConst()
        {
            var reader = new GDScriptReader();
            var code = "constX = 5";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("constX");
            first.PossibleKeyword.Should().BeNull();
            first.StartsWithKeyword.Should().Be("const");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_Signalmy_sig_StartsWithSignal()
        {
            var reader = new GDScriptReader();
            var code = "signalmy_sig";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("signalmy_sig");
            first.PossibleKeyword.Should().BeNull();
            first.StartsWithKeyword.Should().Be("signal");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ClassLevel_EnumMyEnum_StartsWithEnum()
        {
            var reader = new GDScriptReader();
            var code = "enumMyEnum { A }";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("enumMyEnum");
            first.PossibleKeyword.Should().BeNull();
            first.StartsWithKeyword.Should().Be("enum");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        #endregion

        #region Statement Level — Prefix Match

        [TestMethod]
        public void StatementLevel_WhIle_SuggestsWhile()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\twh ile true:\n\t\tpass";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            tree.AllInvalidTokens.Any(t => t.Sequence == "wh" && t.PossibleKeyword == "while").Should().BeTrue();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void StatementLevel_RetUrn_SuggestsReturn()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tret urn 5";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            tree.AllInvalidTokens.Any(t => t.Sequence == "ret" && t.PossibleKeyword == "return").Should().BeTrue();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void StatementLevel_FoR_SuggestsFor()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tfo r i in range(10):\n\t\tpass";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            tree.AllInvalidTokens.Any(t => t.Sequence == "fo" && t.PossibleKeyword == "for").Should().BeTrue();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void StatementLevel_BreAk_SuggestsBreak()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tbre ak";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            tree.AllInvalidTokens.Any(t => t.Sequence == "bre" && t.PossibleKeyword == "break").Should().BeTrue();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void StatementLevel_ContinUe_SuggestsContinue()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tcontin ue";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            tree.AllInvalidTokens.Any(t => t.Sequence == "contin" && t.PossibleKeyword == "continue").Should().BeTrue();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void StatementLevel_PaSs_SuggestsPass()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tpa ss";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            tree.AllInvalidTokens.Any(t => t.Sequence == "pa" && t.PossibleKeyword == "pass").Should().BeTrue();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        #endregion

        #region Negative Tests

        [TestMethod]
        public void SingleLetterPrefix_NoSuggestion()
        {
            var reader = new GDScriptReader();
            var code = "v ar x = 5";
            var tree = reader.ParseFileContent(code);

            var firstInvalid = tree.AllInvalidTokens.FirstOrDefault(t => t.Sequence == "v");
            if (firstInvalid != null)
            {
                firstInvalid.PossibleKeyword.Should().BeNull();
                firstInvalid.StartsWithKeyword.Should().BeNull();
            }

            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void UnrelatedSequence_NoSuggestion()
        {
            var reader = new GDScriptReader();
            var code = "xyz abc";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.PossibleKeyword.Should().BeNull();
            first.StartsWithKeyword.Should().BeNull();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ValidCode_NoInvalidTokens()
        {
            var reader = new GDScriptReader();
            var code = "var x = 5";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().BeEmpty();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void ValidFunc_NoInvalidTokens()
        {
            var reader = new GDScriptReader();
            var code = "func test():\n\tpass";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().BeEmpty();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        #endregion

        #region Settings Tests

        [TestMethod]
        public void DetectKeywordFragments_Disabled_NoPossibleKeyword()
        {
            var reader = new GDScriptReader(new GDReadSettings { DetectKeywordFragments = false });
            var code = "va r x = 5";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.PossibleKeyword.Should().BeNull();
            first.StartsWithKeyword.Should().BeNull();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void MinKeywordFragmentLength_3_ShortFragmentIgnored()
        {
            var reader = new GDScriptReader(new GDReadSettings { MinKeywordFragmentLength = 3 });
            var code = "va r x = 5";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("va");
            first.PossibleKeyword.Should().BeNull();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void MinKeywordFragmentLength_3_LongerFragmentMatches()
        {
            var reader = new GDScriptReader(new GDReadSettings { MinKeywordFragmentLength = 3 });
            var code = "fun c test():\n\tpass";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            var first = tree.AllInvalidTokens.First();
            first.Sequence.Should().Be("fun");
            first.PossibleKeyword.Should().Be("func");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        #endregion

        #region Split Identifiers in Declarations

        [TestMethod]
        public void SplitMethodName_ParsesWithTruncatedIdentifier()
        {
            var reader = new GDScriptReader();
            var code = "func my method():\n\tpass";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.FirstOrDefault();
            method.Should().NotBeNull();
            method.Identifier.Should().NotBeNull();
            method.Identifier.Sequence.Should().Be("my");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void SplitVariableName_ParsesWithTruncatedIdentifier()
        {
            var reader = new GDScriptReader();
            var code = "var my var = 5";
            var tree = reader.ParseFileContent(code);

            var variable = tree.Variables.FirstOrDefault();
            variable.Should().NotBeNull();
            variable.Identifier.Should().NotBeNull();
            variable.Identifier.Sequence.Should().Be("my");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void SplitClassName_ParsesWithTruncatedIdentifier()
        {
            var reader = new GDScriptReader();
            var code = "class My Class:\n\tpass";
            var tree = reader.ParseFileContent(code);

            var inner = tree.InnerClasses.FirstOrDefault();
            inner.Should().NotBeNull();
            inner.Identifier.Should().NotBeNull();
            inner.Identifier.Sequence.Should().Be("My");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void SplitSignalName_ParsesWithTruncatedIdentifier()
        {
            var reader = new GDScriptReader();
            var code = "signal my signal";
            var tree = reader.ParseFileContent(code);

            var sig = tree.Signals.FirstOrDefault();
            sig.Should().NotBeNull();
            sig.Identifier.Should().NotBeNull();
            sig.Identifier.Sequence.Should().Be("my");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        #endregion

        #region Inner Class Members

        [TestMethod]
        public void InnerClass_VaR_SuggestsVar()
        {
            var reader = new GDScriptReader();
            var code = "class Inner:\n\tva r x = 5";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            tree.AllInvalidTokens.First().PossibleKeyword.Should().Be("var");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void InnerClass_FuNc_SuggestsFunc()
        {
            var reader = new GDScriptReader();
            var code = "class Inner:\n\tfu nc test():\n\t\tpass";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            tree.AllInvalidTokens.First().PossibleKeyword.Should().Be("func");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void InnerClass_Varx_StartsWithVar()
        {
            var reader = new GDScriptReader();
            var code = "class Inner:\n\tvarx = 5";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().NotBeEmpty();
            tree.AllInvalidTokens.First().StartsWithKeyword.Should().Be("var");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        #endregion

        #region Lambda Body

        [TestMethod]
        public void LambdaBody_RetUrn_SuggestsReturn()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tvar f = func():\n\t\tret urn 5";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Any(t => t.Sequence == "ret" && t.PossibleKeyword == "return").Should().BeTrue();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void LambdaBody_PaSs_SuggestsPass()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tvar f = func():\n\t\tpa ss";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Any(t => t.Sequence == "pa" && t.PossibleKeyword == "pass").Should().BeTrue();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        #endregion

        #region Nested Statements

        [TestMethod]
        public void NestedIf_WhIle_SuggestsWhile()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tif true:\n\t\twh ile true:\n\t\t\tpass";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Any(t => t.Sequence == "wh" && t.PossibleKeyword == "while").Should().BeTrue();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void NestedFor_BreAk_SuggestsBreak()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tfor i in range(5):\n\t\tbre ak";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Any(t => t.Sequence == "bre" && t.PossibleKeyword == "break").Should().BeTrue();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        #endregion

        #region Match Case Body

        [TestMethod]
        public void MatchCase_RetUrn_SuggestsReturn()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tmatch x:\n\t\t1:\n\t\t\tret urn true";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Any(t => t.Sequence == "ret" && t.PossibleKeyword == "return").Should().BeTrue();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        #endregion

        #region Signature Resilience — Method

        [TestMethod]
        public void Method_InvalidCharsBeforeBracket_BracketStillFound()
        {
            var reader = new GDScriptReader();
            var code = "func §test():\n\tpass";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();
            method.OpenBracket.Should().NotBeNull();
            method.CloseBracket.Should().NotBeNull();
            method.Colon.Should().NotBeNull();
            tree.AllInvalidTokens.Should().NotBeEmpty();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void Method_NumberBeforeName_BracketStillFound()
        {
            var reader = new GDScriptReader();
            var code = "func 123test():\n\tpass";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();
            method.OpenBracket.Should().NotBeNull();
            method.CloseBracket.Should().NotBeNull();
            method.Colon.Should().NotBeNull();
            tree.AllInvalidTokens.Should().NotBeEmpty();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void Method_SpaceInName_BracketStillFound()
        {
            var reader = new GDScriptReader();
            var code = "func my test():\n\tpass";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();
            method.Identifier.Sequence.Should().Be("my");
            method.OpenBracket.Should().NotBeNull();
            method.CloseBracket.Should().NotBeNull();
            method.Colon.Should().NotBeNull();
            tree.AllInvalidTokens.Should().ContainSingle(t => t.Sequence == "test");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void Method_ExtraWordBeforeBracket_BracketStillFound()
        {
            var reader = new GDScriptReader();
            var code = "func test extra():\n\tpass";
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();
            method.Identifier.Sequence.Should().Be("test");
            method.OpenBracket.Should().NotBeNull();
            method.CloseBracket.Should().NotBeNull();
            method.Colon.Should().NotBeNull();
            tree.AllInvalidTokens.Should().ContainSingle(t => t.Sequence == "extra");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        #endregion

        #region Signature Resilience — Signal

        [TestMethod]
        public void Signal_SpaceInName_BracketStillFound()
        {
            var reader = new GDScriptReader();
            var code = "signal my signal()";
            var tree = reader.ParseFileContent(code);

            var sig = tree.Signals.First();
            sig.Identifier.Sequence.Should().Be("my");
            tree.AllInvalidTokens.Should().ContainSingle(t => t.Sequence == "signal");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void Signal_InvalidCharBeforeName_BracketStillFound()
        {
            var reader = new GDScriptReader();
            var code = "signal §my_signal()";
            var tree = reader.ParseFileContent(code);

            var sig = tree.Signals.First();
            tree.AllInvalidTokens.Should().NotBeEmpty();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        #endregion

        #region Signature Resilience — Lambda

        [TestMethod]
        public void Lambda_SpaceInName_BracketStillFound()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tvar f = func my name(): return 1";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().ContainSingle(t => t.Sequence == "name");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void Lambda_InvalidCharBeforeBracket_BracketStillFound()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tvar f = func §name(): return 1";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().ContainSingle(t => t.Sequence == "§name");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void Lambda_NumberBeforeBracket_BracketStillFound()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tvar f = func 123(): return 1";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().ContainSingle(t => t.Sequence == "123");
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        [TestMethod]
        public void Lambda_NoBracketIssue_NormalParsing()
        {
            var reader = new GDScriptReader();
            var code = "func t():\n\tvar f = func(): return 1";
            var tree = reader.ParseFileContent(code);

            tree.AllInvalidTokens.Should().BeEmpty();
            AssertHelper.CompareCodeStrings(code, tree.ToString());
        }

        #endregion
    }
}
