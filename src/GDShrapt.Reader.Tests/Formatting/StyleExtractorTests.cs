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
