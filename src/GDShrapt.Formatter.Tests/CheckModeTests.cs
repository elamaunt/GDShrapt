using FluentAssertions;
using GDShrapt.Formatter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Formatting
{
    /// <summary>
    /// Tests for formatter check mode functionality.
    /// </summary>
    [TestClass]
    public class CheckModeTests
    {
        private GDFormatter _formatter;

        [TestInitialize]
        public void Setup()
        {
            _formatter = new GDFormatter();
        }

        [TestMethod]
        public void IsFormatted_NullOrEmpty_ReturnsTrue()
        {
            _formatter.IsFormatted(null).Should().BeTrue();
            _formatter.IsFormatted("").Should().BeTrue();
        }

        [TestMethod]
        public void IsFormatted_AlreadyFormatted_ReturnsTrue()
        {
            // First format some code to get a known-good baseline
            var rawCode = @"func test():
	pass
";
            var formattedCode = _formatter.FormatCode(rawCode);

            // Now check that the formatted code is considered "already formatted"
            var result = _formatter.IsFormatted(formattedCode);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsFormatted_NeedsFormatting_ReturnsFalse()
        {
            // Missing trailing newline
            var code = "func test():\n\tpass";

            var result = _formatter.IsFormatted(code);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void Check_NullOrEmpty_ReturnsAlreadyFormatted()
        {
            var result = _formatter.Check(null);

            result.IsFormatted.Should().BeTrue();
            result.DifferingLineCount.Should().Be(0);
        }

        [TestMethod]
        public void Check_AlreadyFormatted_ReturnsCorrectResult()
        {
            // First format some code to get a known-good baseline
            var rawCode = @"func test():
	pass
";
            var formattedCode = _formatter.FormatCode(rawCode);

            // Now check that the formatted code returns correct result
            var result = _formatter.Check(formattedCode);

            result.IsFormatted.Should().BeTrue();
            result.OriginalCode.Should().Be(formattedCode);
            result.FormattedCode.Should().Be(formattedCode);
            result.DifferingLineCount.Should().Be(0);
        }

        [TestMethod]
        public void Check_NeedsFormatting_ReturnsCorrectResult()
        {
            // Missing trailing newline
            var code = "func test():\n\tpass";

            var result = _formatter.Check(code);

            result.IsFormatted.Should().BeFalse();
            result.OriginalCode.Should().Be(code);
            result.FormattedCode.Should().EndWith("\n");
            result.DifferingLineCount.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public void Check_MultipleLinesDiffer_CountsCorrectly()
        {
            // Code with trailing spaces on multiple lines
            var code = "var x = 1   \nvar y = 2   \nvar z = 3   ";

            var result = _formatter.Check(code);

            result.IsFormatted.Should().BeFalse();
            result.DifferingLineCount.Should().BeGreaterThan(0);
        }
    }
}
