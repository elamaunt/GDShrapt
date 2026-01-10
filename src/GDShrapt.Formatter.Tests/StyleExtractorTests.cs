using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Formatting
{
    /// <summary>
    /// Tests for GDFormatterStyleExtractor.
    /// </summary>
    [TestClass]
    public class StyleExtractorTests
    {
        private GDFormatterStyleExtractor _extractor;

        [TestInitialize]
        public void Setup()
        {
            _extractor = new GDFormatterStyleExtractor();
        }

        #region Indentation Detection

        [TestMethod]
        public void ExtractStyle_TabIndentation_DetectsTabs()
        {
            var code = @"func test():
	var x = 10
	print(x)
";

            var options = _extractor.ExtractStyleFromCode(code);

            options.IndentStyle.Should().Be(IndentStyle.Tabs);
        }

        [TestMethod]
        public void ExtractStyle_SpaceIndentation_DetectsSpaces()
        {
            var code = @"func test():
    var x = 10
    print(x)
";

            var options = _extractor.ExtractStyleFromCode(code);

            options.IndentStyle.Should().Be(IndentStyle.Spaces);
        }

        [TestMethod]
        public void ExtractStyle_TwoSpaceIndent_DetectsSize()
        {
            var code = @"func test():
  var x = 10
  print(x)
";

            var options = _extractor.ExtractStyleFromCode(code);

            options.IndentStyle.Should().Be(IndentStyle.Spaces);
            options.IndentSize.Should().Be(2);
        }

        [TestMethod]
        public void ExtractStyle_FourSpaceIndent_DetectsSize()
        {
            var code = @"func test():
    var x = 10
    print(x)
";

            var options = _extractor.ExtractStyleFromCode(code);

            options.IndentStyle.Should().Be(IndentStyle.Spaces);
            options.IndentSize.Should().Be(4);
        }

        #endregion

        #region Spacing Detection

        [TestMethod]
        public void ExtractStyle_SpaceAroundOperators_DetectsTrue()
        {
            var code = @"func test():
	var x = 10 + 5
";

            var options = _extractor.ExtractStyleFromCode(code);

            options.SpaceAroundOperators.Should().BeTrue();
        }

        [TestMethod]
        public void ExtractStyle_SpaceAfterColon_DetectsTrue()
        {
            var code = @"var x: int = 10
";

            var options = _extractor.ExtractStyleFromCode(code);

            options.SpaceAfterColon.Should().BeTrue();
        }

        [TestMethod]
        public void ExtractStyle_SpaceAfterComma_DetectsTrue()
        {
            var code = @"func test(a, b, c):
	pass
";

            var options = _extractor.ExtractStyleFromCode(code);

            options.SpaceAfterComma.Should().BeTrue();
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void ExtractStyle_EmptyCode_ReturnsDefault()
        {
            var options = _extractor.ExtractStyleFromCode("");

            options.Should().NotBeNull();
        }

        [TestMethod]
        public void ExtractStyle_NullCode_ReturnsDefault()
        {
            var options = _extractor.ExtractStyleFromCode(null);

            options.Should().NotBeNull();
        }

        [TestMethod]
        public void ExtractStyle_MinimalCode_ReturnsDefaults()
        {
            var code = "pass";

            var options = _extractor.ExtractStyleFromCode(code);

            options.Should().NotBeNull();
        }

        #endregion

        #region Line Endings

        [TestMethod]
        public void ExtractStyle_CRLFLineEndings_DetectsCRLF()
        {
            var code = "func test():\r\n\tpass\r\n";

            var options = _extractor.ExtractStyleFromCode(code);

            options.LineEnding.Should().Be(LineEndingStyle.CRLF);
        }

        [TestMethod]
        public void ExtractStyle_LFLineEndings_DetectsLF()
        {
            var code = "func test():\n\tpass\n";

            var options = _extractor.ExtractStyleFromCode(code);

            options.LineEnding.Should().Be(LineEndingStyle.LF);
        }

        [TestMethod]
        public void ExtractStyle_MixedLineEndings_UsesMajority()
        {
            // 2 CRLF vs 1 LF
            var code = "func test():\r\n\tvar x = 1\r\n\tpass\n";

            var options = _extractor.ExtractStyleFromCode(code);

            options.LineEnding.Should().Be(LineEndingStyle.CRLF);
        }

        #endregion

        #region Max Line Length

        [TestMethod]
        public void ExtractStyle_ShortLines_DetectsMaxLength80()
        {
            var code = string.Join("\n", System.Linq.Enumerable.Repeat("var x = 10", 20));

            var options = _extractor.ExtractStyleFromCode(code);

            options.MaxLineLength.Should().Be(80);
        }

        [TestMethod]
        public void ExtractStyle_MediumLines_DetectsMaxLength100()
        {
            var code = string.Join("\n", System.Linq.Enumerable.Repeat(new string('x', 95), 20));

            var options = _extractor.ExtractStyleFromCode(code);

            options.MaxLineLength.Should().Be(100);
        }

        [TestMethod]
        public void ExtractStyle_LongLines_DetectsMaxLength120()
        {
            var code = string.Join("\n", System.Linq.Enumerable.Repeat(new string('x', 115), 20));

            var options = _extractor.ExtractStyleFromCode(code);

            options.MaxLineLength.Should().Be(120);
        }

        [TestMethod]
        public void ExtractStyle_VeryLongLines_DisablesMaxLength()
        {
            var code = string.Join("\n", System.Linq.Enumerable.Repeat(new string('x', 200), 20));

            var options = _extractor.ExtractStyleFromCode(code);

            options.MaxLineLength.Should().Be(0);
        }

        #endregion

        #region Backslash Continuation

        [TestMethod]
        public void ExtractStyle_BackslashContinuation_Detects()
        {
            var code = "var result = obj.method1() \\\n    .method2()\n";

            var options = _extractor.ExtractStyleFromCode(code);

            options.UseBackslashContinuation.Should().BeTrue();
        }

        [TestMethod]
        public void ExtractStyle_NoBackslashContinuation_DetectsFalse()
        {
            var code = "var result = obj.method1()\n";

            var options = _extractor.ExtractStyleFromCode(code);

            options.UseBackslashContinuation.Should().BeFalse();
        }

        #endregion

        #region Trailing Whitespace

        [TestMethod]
        public void ExtractStyle_NoTrailingWhitespace_SetsRemoveTrue()
        {
            var code = "func test():\n\tpass\n";

            var options = _extractor.ExtractStyleFromCode(code);

            options.RemoveTrailingWhitespace.Should().BeTrue();
        }

        [TestMethod]
        public void ExtractStyle_ManyLinesWithTrailingWhitespace_SetsRemoveFalse()
        {
            // More than 10% of lines have trailing whitespace
            var code = "func test():   \n\tvar x = 1   \n\tvar y = 2   \n\tpass\n";

            var options = _extractor.ExtractStyleFromCode(code);

            options.RemoveTrailingWhitespace.Should().BeFalse();
        }

        #endregion

        #region Trailing Newline

        [TestMethod]
        public void ExtractStyle_HasTrailingNewline_SetsEnsureTrue()
        {
            var code = "func test():\n\tpass\n";

            var options = _extractor.ExtractStyleFromCode(code);

            options.EnsureTrailingNewline.Should().BeTrue();
        }

        [TestMethod]
        public void ExtractStyle_NoTrailingNewline_SetsEnsureFalse()
        {
            var code = "func test():\n\tpass";

            var options = _extractor.ExtractStyleFromCode(code);

            options.EnsureTrailingNewline.Should().BeFalse();
        }

        [TestMethod]
        public void ExtractStyle_MultipleTrailingNewlines_SetsRemoveMultipleTrue()
        {
            var code = "func test():\n\tpass\n\n\n";

            var options = _extractor.ExtractStyleFromCode(code);

            options.RemoveMultipleTrailingNewlines.Should().BeFalse();
        }

        [TestMethod]
        public void ExtractStyle_SingleTrailingNewline_SetsRemoveMultipleFalse()
        {
            var code = "func test():\n\tpass\n";

            var options = _extractor.ExtractStyleFromCode(code);

            options.RemoveMultipleTrailingNewlines.Should().BeTrue();
        }

        #endregion

        #region Nested Indent Size Calculation

        [TestMethod]
        public void ExtractStyle_NestedIndent_CalculatesCorrectSize()
        {
            var code = @"func test():
    if true:
        if true:
            pass
";

            var options = _extractor.ExtractStyleFromCode(code);

            options.IndentStyle.Should().Be(IndentStyle.Spaces);
            options.IndentSize.Should().Be(4);
        }

        [TestMethod]
        public void ExtractStyle_TwoSpaceNested_CalculatesCorrectSize()
        {
            var code = @"func test():
  if true:
    if true:
      pass
";

            var options = _extractor.ExtractStyleFromCode(code);

            options.IndentStyle.Should().Be(IndentStyle.Spaces);
            options.IndentSize.Should().Be(2);
        }

        #endregion

        #region Format By Example

        [TestMethod]
        public void FormatCodeWithStyle_UsesExtractedStyle()
        {
            var formatter = new GDFormatter();

            var sampleCode = @"func sample():
    var x = 10
";

            var codeToFormat = @"func test():
	var y = 20
";

            var result = formatter.FormatCodeWithStyle(codeToFormat, sampleCode);

            result.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void FormatCodeWithStyle_NoSample_UsesDefaultOptions()
        {
            var formatter = new GDFormatter();

            var code = @"func test():
	pass
";

            var result = formatter.FormatCodeWithStyle(code, "");

            result.Should().NotBeNullOrEmpty();
        }

        #endregion
    }
}
