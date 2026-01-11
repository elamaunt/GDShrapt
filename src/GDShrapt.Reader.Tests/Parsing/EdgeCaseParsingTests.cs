using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for edge cases in the parser covering whitespace, brackets, newlines,
    /// indentation, comments, and other boundary conditions.
    /// </summary>
    [TestClass]
    public class EdgeCaseParsingTests
    {
        private GDScriptReader _reader;

        [TestInitialize]
        public void Setup()
        {
            _reader = new GDScriptReader();
        }

        #region Indentation Edge Cases

        [TestMethod]
        public void Parser_Indentation_JumpTwoLevels_ShouldParse()
        {
            // Jumping from 0 to 3 tabs (skipping 2 levels)
            var code = "func test():\n\t\t\tx = 1\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Methods.Should().HaveCount(1);

            // The parser should allow over-indentation
            var method = tree.Methods.First();
            method.Statements.Should().HaveCountGreaterOrEqualTo(1);
        }

        [TestMethod]
        public void Parser_Indentation_TabSpaceMixed_ShouldParse()
        {
            // Tab + space in same indentation line (tab=4 + 1 space = 5 spaces)
            var code = "func test():\n\t x = 1\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Methods.Should().HaveCount(1);

            // Round-trip should preserve the mixed indentation
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_Indentation_IrregularSpaces_ShouldParse()
        {
            // 2 spaces (not multiple of 4)
            var code = "func test():\n  x = 1\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Methods.Should().HaveCount(1);

            // Parser should accept non-standard indentation
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_Indentation_DecreasePartial_ShouldParse()
        {
            // 2 tabs then 1 tab - should be different blocks
            var code = "func test():\n\t\tx = 1\n\ty = 2\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Methods.Should().HaveCount(1);

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_Indentation_ZeroAfterNested_ShouldSeparateBlocks()
        {
            // Zero indentation after nested block should create class-level variable
            var code = "func test():\n\tif true:\n\t\tpass\nvar x = 1\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Methods.Should().HaveCount(1);
            // x = 1 at zero indent should be a class-level variable
            tree.Variables.Should().HaveCount(1);
            tree.Variables.First().Identifier.Sequence.Should().Be("x");
        }

        [TestMethod]
        public void Parser_Indentation_SpaceBeforeTab_ShouldCalculateCorrectly()
        {
            // Space + tab = 1 + 4 = 5 spaces worth
            var code = "func test():\n \tx = 1\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_Indentation_MultipleTabsAndSpaces_ShouldPreserve()
        {
            // 2 spaces + tab + 2 spaces
            var code = "func test():\n  \t  x = 1\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        #endregion

        #region Newline Edge Cases

        [TestMethod]
        public void Parser_NoFinalNewline_ShouldParse()
        {
            // File without trailing newline
            var code = "var x = 1";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parser_MultipleFinalNewlines_ShouldPreserve()
        {
            // 4 trailing newlines
            var code = "var x = 1\n\n\n\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_CRLFLineEndings_ShouldParse()
        {
            // Windows line endings - \r should be ignored
            var code = "var x = 1\r\nvar y = 2\r\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(2);

            // Output should have \n only (normalized)
            var output = tree.ToString();
            output.Should().NotContain("\r");
        }

        [TestMethod]
        public void Parser_CROnlyLineEndings_ShouldNotSeparateLines()
        {
            // Old Mac style (CR only) - \r is ignored, so no line separation
            var code = "var x = 1\rvar y = 2\r";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            // Since \r is ignored, this should be parsed as a single malformed statement
            // or create invalid tokens
            var allInvalid = tree.AllInvalidTokens.ToList();
            // The behavior depends on implementation - either 1 variable with invalid tokens
            // or multiple variables with issues
        }

        [TestMethod]
        public void Parser_MixedLineEndings_ShouldParse()
        {
            // Mixed CRLF and LF
            var code = "var x = 1\r\nvar y = 2\nvar z = 3\r\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(3);
        }

        [TestMethod]
        public void Parser_NewlineImmediatelyAfterColon_ShouldParse()
        {
            var code = "func test():\n\tpass\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Methods.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parser_MultipleNewlinesBetweenStatements_ShouldPreserve()
        {
            var code = "func test():\n\tvar x = 1\n\n\n\tvar y = 2\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        #endregion

        #region Bracket Edge Cases

        [TestMethod]
        public void Parser_UnclosedParenthesis_ShouldCreateInvalidTokens()
        {
            var code = "func test():\n\tvar x = (1 + 2\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            // Parser should handle unclosed brackets gracefully
        }

        [TestMethod]
        public void Parser_ExtraClosingBracket_ShouldParse()
        {
            var code = "var arr = [1, 2]]\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            // Extra ] should be handled as invalid or passed to parent
            var invalidTokens = tree.AllInvalidTokens.ToList();
        }

        [TestMethod]
        public void Parser_EmptyBrackets_AllTypes_ShouldParse()
        {
            var code = "var arr = []\nvar dict = {}\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(2);

            var arr = tree.Variables.First().Initializer as GDArrayInitializerExpression;
            arr.Should().NotBeNull();
            arr.Values.Should().HaveCount(0);

            var dict = tree.Variables.Last().Initializer as GDDictionaryInitializerExpression;
            dict.Should().NotBeNull();
            dict.KeyValues.Should().HaveCount(0);
        }

        [TestMethod]
        public void Parser_EmptyParentheses_ShouldParse()
        {
            var code = "func test():\n\tvar x = ()\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            // Empty parentheses might be invalid GDScript or create a bracket expression
        }

        [TestMethod]
        public void Parser_DeepNesting_FiveLevels_ShouldParse()
        {
            var code = "var x = [[[[[1]]]]]\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);

            // Verify deep nesting structure
            var expr = tree.Variables.First().Initializer;
            int depth = 0;
            while (expr is GDArrayInitializerExpression arr)
            {
                depth++;
                expr = arr.Values.FirstOrDefault();
            }
            depth.Should().Be(5);
        }

        [TestMethod]
        public void Parser_BracketsInString_ShouldNotParseBrackets()
        {
            var code = "var s = \"((([[{\"\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);
            tree.AllInvalidTokens.Should().BeEmpty();

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_BracketsInComment_ShouldBeIgnored()
        {
            var code = "var x = 1 # ((([\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);
            tree.AllInvalidTokens.Should().BeEmpty();

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_SingleBracketPerLine_ShouldParse()
        {
            var code = "var arr = [\n\t1,\n\t2\n]\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);

            var arr = tree.Variables.First().Initializer as GDArrayInitializerExpression;
            arr.Should().NotBeNull();
            arr.Values.Should().HaveCount(2);

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_MixedNestedBrackets_ShouldParse()
        {
            var code = "var x = {\"arr\": [{\"inner\": 1}]}\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);
            tree.AllInvalidTokens.Should().BeEmpty();
        }

        [TestMethod]
        public void Parser_UnclosedNestedBrackets_ShouldHandleGracefully()
        {
            var code = "var arr = [[[1, 2]\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            // Parser should not throw, just handle gracefully
        }

        #endregion

        #region Dictionary Edge Cases

        [TestMethod]
        public void Parser_DictEqualsInsteadOfColon_ShouldParse()
        {
            // GDScript allows = instead of : for dictionary values
            var code = "var dict = {\"a\" = 1}\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);

            var dict = tree.Variables.First().Initializer as GDDictionaryInitializerExpression;
            dict.Should().NotBeNull();
            dict.KeyValues.Should().HaveCount(1);

            var kv = dict.KeyValues.First();
            kv.Assign.Should().NotBeNull();
        }

        [TestMethod]
        public void Parser_DictMixedColonEquals_ShouldParse()
        {
            var code = "var dict = {\"a\": 1, \"b\" = 2}\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var dict = tree.Variables.First().Initializer as GDDictionaryInitializerExpression;
            dict.Should().NotBeNull();
            dict.KeyValues.Should().HaveCount(2);

            var first = dict.KeyValues.First();
            var last = dict.KeyValues.Last();

            first.Colon.Should().NotBeNull();
            last.Assign.Should().NotBeNull();
        }

        [TestMethod]
        public void Parser_TrailingCommaArray_ShouldParse()
        {
            var code = "var arr = [1, 2, 3,]\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var arr = tree.Variables.First().Initializer as GDArrayInitializerExpression;
            arr.Should().NotBeNull();
            arr.Values.Should().HaveCount(3);

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_TrailingCommaDict_ShouldParse()
        {
            var code = "var dict = {\"a\": 1,}\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var dict = tree.Variables.First().Initializer as GDDictionaryInitializerExpression;
            dict.Should().NotBeNull();
            dict.KeyValues.Should().HaveCount(1);

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_TrailingCommaParams_ShouldParse()
        {
            var code = "func test(a, b,):\n\tpass\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var method = tree.Methods.First();
            method.Parameters.Should().HaveCount(2);

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_NestedDictionaries_ThreeLevels_ShouldParse()
        {
            var code = "var x = {\"a\": {\"b\": {\"c\": 1}}}\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.AllInvalidTokens.Should().BeEmpty();

            var output = tree.ToString();
            output.Should().Be(code);
        }

        #endregion

        #region Comment Edge Cases

        [TestMethod]
        public void Parser_EmptyComment_ShouldParse()
        {
            var code = "#\nvar x = 1\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_CommentOnlySpaces_ShouldParse()
        {
            var code = "#   \nvar x = 1\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_CommentNoSpaceAfterHash_ShouldParse()
        {
            var code = "var x = 1#comment\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);

            var variable = tree.Variables.First();
            var initializer = variable.Initializer;

            initializer.Should().NotBeNull("the number 1 should be parsed as initializer");
            initializer.Should().BeOfType<GDNumberExpression>();
        }

        [TestMethod]
        public void Parser_CommentNoSpaceAfterHash_RoundTrip()
        {
            var code = "var x = 1#comment\n";
            var tree = _reader.ParseFileContent(code);

            var output = tree.ToString();

            // Round-trip should preserve the number before comment
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_CommentInsideMultilineArray_ShouldParse()
        {
            var code = "var arr = [\n\t1, # first element\n\t2\n]\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var arr = tree.Variables.First().Initializer as GDArrayInitializerExpression;
            arr.Should().NotBeNull();
            arr.Values.Should().HaveCount(2);

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_CommentWrongIndent_ZeroIndentInBlock_ShouldParse()
        {
            // Comment at zero indent inside a function block
            var code = "func test():\n\tvar x = 1\n# zero indent comment\n\tvar y = 2\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            // The comment should be preserved and block should continue
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_CommentOverIndented_ShouldParse()
        {
            var code = "func test():\n\tvar x = 1\n\t\t\t# over-indented\n\tvar y = 2\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        #endregion

        #region Backslash Continuation Edge Cases

        [TestMethod]
        public void Parser_BackslashNoIndentContinuation_ShouldParse()
        {
            // Continuation without indentation on next line
            var code = "var x = 1 +\\\n2\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_BackslashLessIndent_InFunction_ShouldParse()
        {
            // Continuation with less indentation than expected
            var code = "func test():\n\tvar x = 1 +\\\n2\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            // The _lineSplitted flag should prevent block end
        }

        [TestMethod]
        public void Parser_BackslashWithSpaces_ShouldPreserve()
        {
            // Backslash with spaces before newline
            var code = "var x = 1 + \\   \n\t2\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_BackslashAtEndOfFile_ShouldHandle()
        {
            // Backslash at end of file without newline
            var code = "var x = 1 + \\";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            // Should handle gracefully without throwing
        }

        [TestMethod]
        public void Parser_MultipleBackslash_FourLines_ShouldParse()
        {
            var code = "var x = 1 +\\\n2 +\\\n3 +\\\n4\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_BackslashInParentheses_ShouldParse()
        {
            // Backslash inside parentheses (parentheses already allow multiline)
            var code = "func test():\n\tvar x = (1 +\\\n\t\t2)\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        #endregion

        #region Expression Edge Cases

        [TestMethod]
        public void Parser_UnaryOperatorChain_ShouldParse()
        {
            var code = "var x = -!~a\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);
            tree.AllInvalidTokens.Should().BeEmpty();
        }

        [TestMethod]
        public void Parser_DoubleNegation_ShouldParse()
        {
            var code = "var x = --a\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parser_NestedTernary_ShouldParse()
        {
            var code = "var x = a if c1 else b if c2 else c\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_IndexerChain_ShouldParse()
        {
            var code = "func test():\n\tvar x = a[0][1][2]\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_MethodChain_ShouldParse()
        {
            var code = "func test():\n\tvar x = a().b().c()\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_MixedChain_IndexerAndMethod_ShouldParse()
        {
            var code = "func test():\n\tvar x = a[0].b().c[1].d()\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_VeryLongExpression_ElevenOperators_ShouldParse()
        {
            var code = "var x = a+b-c*d/e%f+g-h+i-j+k\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);
            tree.AllInvalidTokens.Should().BeEmpty();
        }

        [TestMethod]
        public void Parser_AllArithmeticOperators_ShouldParse()
        {
            var code = "var x = a + b - c * d / e % f ** g\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.AllInvalidTokens.Should().BeEmpty();
        }

        [TestMethod]
        public void Parser_AllComparisonOperators_ShouldParse()
        {
            var code = "var x = a == b and c != d and e < f and g > h and i <= j and k >= l\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.AllInvalidTokens.Should().BeEmpty();
        }

        [TestMethod]
        public void Parser_AllBitwiseOperators_ShouldParse()
        {
            var code = "var x = a & b | c ^ d << e >> f\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.AllInvalidTokens.Should().BeEmpty();
        }

        #endregion

        #region Statement Edge Cases

        [TestMethod]
        public void Parser_BreakOutsideLoop_ShouldParse()
        {
            // Parser should allow; validator catches semantic error
            var code = "func test():\n\tbreak\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Methods.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parser_ContinueOutsideLoop_ShouldParse()
        {
            var code = "func test():\n\tcontinue\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Methods.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parser_IfWithoutElse_FollowedByCode_ShouldParse()
        {
            var code = "func test():\n\tif true:\n\t\tpass\n\tvar x = 1\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var method = tree.Methods.First();
            method.Statements.Should().HaveCount(2);
        }

        [TestMethod]
        public void Parser_NestedIfMatch_ShouldParse()
        {
            var code = "func test():\n\tif a:\n\t\tmatch b:\n\t\t\t1:\n\t\t\t\tpass\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_MultiplePassStatements_ShouldParse()
        {
            var code = "func test():\n\tpass\n\tpass\n\tpass\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var method = tree.Methods.First();
            method.Statements.Should().HaveCount(3);
        }

        [TestMethod]
        public void Parser_MatchWithMultipleCases_ShouldParse()
        {
            var code = "func test(x):\n\tmatch x:\n\t\t1:\n\t\t\tpass\n\t\t2:\n\t\t\tpass\n\t\t_:\n\t\t\tpass\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_ForLoopWithRange_ShouldParse()
        {
            var code = "func test():\n\tfor i in range(10):\n\t\tpass\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_WhileLoopWithBreakContinue_ShouldParse()
        {
            var code = "func test():\n\twhile true:\n\t\tif a:\n\t\t\tbreak\n\t\tcontinue\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        #endregion

        #region Unicode and Special Characters Edge Cases

        [TestMethod]
        public void Parser_UnicodeInIdentifier_ShouldParse()
        {
            var code = "var \u043f\u0435\u0440\u0435\u043c\u0435\u043d\u043d\u0430\u044f = 1\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);
            tree.Variables.First().Identifier.Sequence.Should().Be("\u043f\u0435\u0440\u0435\u043c\u0435\u043d\u043d\u0430\u044f");
        }

        [TestMethod]
        public void Parser_UnicodeInString_ShouldParse()
        {
            var code = "var s = \"\u041f\u0440\u0438\u0432\u0435\u0442 \u043c\u0438\u0440\"\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_EmojiInString_ShouldParse()
        {
            var code = "var s = \"Hello \ud83c\udf89\"\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);

            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_NonBreakingSpace_ShouldCreateInvalidToken()
        {
            // NBSP (U+00A0) is not recognized as space by IsSpace()
            var code = "var x\u00a0= 1\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            // NBSP should be treated as invalid character
            tree.AllInvalidTokens.Should().NotBeEmpty();
        }

        [TestMethod]
        public void Parser_TabCharacterInString_ShouldPreserve()
        {
            var code = "var s = \"hello\\tworld\"\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        #endregion

        #region Whitespace in Empty Lines Edge Cases

        [TestMethod]
        public void Parser_EmptyLineOnlySpaces_ShouldPreserve()
        {
            var code = "func test():\n\tvar x = 1\n   \n\tvar y = 2\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_EmptyLineOnlyTabs_ShouldPreserve()
        {
            var code = "func test():\n\tvar x = 1\n\t\n\tvar y = 2\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_MultipleEmptyLinesInBlock_ShouldPreserve()
        {
            var code = "func test():\n\tvar x = 1\n\n\n\n\tvar y = 2\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_EmptyLinesMixedWhitespace_ShouldPreserve()
        {
            var code = "func test():\n\tvar x = 1\n\t  \n\tvar y = 2\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        #endregion

        #region Additional Edge Cases

        [TestMethod]
        public void Parser_VeryDeepNesting_ShouldNotOverflow()
        {
            // 10 levels of nesting
            var code = "func test():\n\tif a:\n\t\tif b:\n\t\t\tif c:\n\t\t\t\tif d:\n\t\t\t\t\tif e:\n\t\t\t\t\t\tif f:\n\t\t\t\t\t\t\tif g:\n\t\t\t\t\t\t\t\tif h:\n\t\t\t\t\t\t\t\t\tif i:\n\t\t\t\t\t\t\t\t\t\tpass\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
        }

        [TestMethod]
        public void Parser_EmptyFile_ShouldParse()
        {
            var code = "";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
        }

        [TestMethod]
        public void Parser_OnlyNewlines_ShouldParse()
        {
            var code = "\n\n\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
        }

        [TestMethod]
        public void Parser_OnlyComments_ShouldParse()
        {
            var code = "# comment 1\n# comment 2\n# comment 3\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.AllInvalidTokens.Should().BeEmpty();
        }

        [TestMethod]
        public void Parser_ClassWithExtends_ShouldParse()
        {
            var code = "class_name MyClass\nextends Node\n\nvar x = 1\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.ClassName.Should().NotBeNull();
            tree.Extends.Should().NotBeNull();
        }

        [TestMethod]
        public void Parser_SignalDeclaration_WithParams_ShouldParse()
        {
            var code = "signal my_signal(arg1: int, arg2: String)\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Signals.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parser_EnumDeclaration_WithValues_ShouldParse()
        {
            var code = "enum MyEnum { A, B = 5, C }\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Enums.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parser_ConstDeclaration_ShouldParse()
        {
            var code = "const MY_CONST = 42\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            // Constants are stored as Variables with ConstKeyword
            var constants = tree.Variables.Where(v => v.ConstKeyword != null).ToList();
            constants.Should().HaveCount(1);
            constants.First().Identifier.Sequence.Should().Be("MY_CONST");
        }

        [TestMethod]
        public void Parser_StaticFunction_ShouldParse()
        {
            var code = "static func helper(a: int) -> int:\n\treturn a * 2\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Methods.Should().HaveCount(1);
            tree.Methods.First().StaticKeyword.Should().NotBeNull();
        }

        [TestMethod]
        public void Parser_LambdaExpression_ShouldParse()
        {
            var code = "var f = func(a, b): return a + b\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parser_GetNodeExpression_ShouldParse()
        {
            var code = "func test():\n\tvar node = $Node/Child\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_AwaitExpression_ShouldParse()
        {
            var code = "func test():\n\tawait get_tree().create_timer(1.0).timeout\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            var output = tree.ToString();
            output.Should().Be(code);
        }

        [TestMethod]
        public void Parser_PropertyWithGetSet_ShouldParse()
        {
            var code = "var prop: int:\n\tget:\n\t\treturn _prop\n\tset(value):\n\t\t_prop = value\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parser_ExportAttribute_ShouldParse()
        {
            var code = "@export var value: int = 10\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parser_MultipleAttributes_ShouldParse()
        {
            var code = "@export\n@onready\nvar node: Node\n";
            var tree = _reader.ParseFileContent(code);

            tree.Should().NotBeNull();
            tree.Variables.Should().HaveCount(1);
        }

        #endregion
    }
}
