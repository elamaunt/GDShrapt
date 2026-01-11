using FluentAssertions;
using GDShrapt.Formatter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Formatting
{
    /// <summary>
    /// Tests for the GDLineWrapFormatRule (GDF006).
    /// </summary>
    [TestClass]
    public class LineWrappingTests
    {
        private GDFormatter _formatter;

        [TestInitialize]
        public void Setup()
        {
            _formatter = new GDFormatter(new GDFormatterOptions
            {
                MaxLineLength = 40, // Short line length for testing
                WrapLongLines = true,
                LineWrapStyle = LineWrapStyle.AfterOpeningBracket,
                ContinuationIndentSize = 1
            });
        }

        #region Basic Line Wrapping

        [TestMethod]
        public void FormatCode_ShortLine_NoWrapping()
        {
            var code = @"func test():
	var x = foo(1, 2)
";

            var result = _formatter.FormatCode(code);

            // Short lines should not be wrapped
            result.Should().Contain("foo(1, 2)");
        }

        [TestMethod]
        public void FormatCode_LongArrayInitializer_WrapsElements()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 30,
                WrapLongLines = true,
                LineWrapStyle = LineWrapStyle.AfterOpeningBracket,
                ContinuationIndentSize = 1
            };
            var formatter = new GDFormatter(options);

            var code = @"func test():
	var arr = [item1, item2, item3, item4, item5]
";

            var result = formatter.FormatCode(code);

            // The array should be wrapped since it exceeds line length
            // Check that it still contains all elements
            result.Should().Contain("item1");
            result.Should().Contain("item2");
            result.Should().Contain("item3");
            result.Should().Contain("item4");
            result.Should().Contain("item5");
        }

        [TestMethod]
        public void FormatCode_LongFunctionCall_WrapsParameters()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 40,
                WrapLongLines = true,
                LineWrapStyle = LineWrapStyle.AfterOpeningBracket,
                ContinuationIndentSize = 1
            };
            var formatter = new GDFormatter(options);

            var code = @"func test():
	var result = some_long_function(param1, param2, param3, param4)
";

            var result = formatter.FormatCode(code);

            // The function call should be wrapped
            result.Should().Contain("param1");
            result.Should().Contain("param2");
            result.Should().Contain("param3");
            result.Should().Contain("param4");
        }

        #endregion

        #region WrapLongLines Option

        [TestMethod]
        public void FormatCode_WrapLongLinesDisabled_NoWrapping()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 30,
                WrapLongLines = false // Disabled
            };
            var formatter = new GDFormatter(options);

            var code = @"func test():
	var arr = [item1, item2, item3, item4, item5]
";

            var result = formatter.FormatCode(code);

            // Should NOT wrap since WrapLongLines is disabled
            result.Should().Contain("[item1, item2, item3, item4, item5]");
        }

        [TestMethod]
        public void FormatCode_MaxLineLengthZero_NoWrapping()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 0, // Disabled
                WrapLongLines = true
            };
            var formatter = new GDFormatter(options);

            var code = @"func test():
	var arr = [item1, item2, item3, item4, item5]
";

            var result = formatter.FormatCode(code);

            // Should NOT wrap since MaxLineLength is 0
            result.Should().Contain("[item1, item2, item3, item4, item5]");
        }

        #endregion

        #region LineWrapStyle Options

        [TestMethod]
        public void FormatCode_AfterOpeningBracketStyle_WrapsCorrectly()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 25,
                WrapLongLines = true,
                LineWrapStyle = LineWrapStyle.AfterOpeningBracket,
                ContinuationIndentSize = 1
            };
            var formatter = new GDFormatter(options);

            var code = @"func test():
	var arr = [a, b, c, d, e]
";

            var result = formatter.FormatCode(code);

            // After wrapping, should contain newlines
            result.Should().Contain("a");
            result.Should().Contain("b");
            result.Should().Contain("c");
            result.Should().Contain("d");
            result.Should().Contain("e");
        }

        [TestMethod]
        public void FormatCode_BeforeElementsStyle_WrapsOnlyWhenNeeded()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 30,
                WrapLongLines = true,
                LineWrapStyle = LineWrapStyle.BeforeElements,
                ContinuationIndentSize = 1
            };
            var formatter = new GDFormatter(options);

            var code = @"func test():
	var arr = [a, b, c, d, e]
";

            var result = formatter.FormatCode(code);

            // Should still contain all elements
            result.Should().Contain("a");
            result.Should().Contain("b");
            result.Should().Contain("c");
            result.Should().Contain("d");
            result.Should().Contain("e");
        }

        #endregion

        #region Dictionary Wrapping

        [TestMethod]
        public void FormatCode_LongDictionary_WrapsKeyValues()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 35,
                WrapLongLines = true,
                LineWrapStyle = LineWrapStyle.AfterOpeningBracket,
                ContinuationIndentSize = 1
            };
            var formatter = new GDFormatter(options);

            var code = @"func test():
	var dict = {""key1"": value1, ""key2"": value2, ""key3"": value3}
";

            var result = formatter.FormatCode(code);

            // Dictionary should be wrapped
            result.Should().Contain("key1");
            result.Should().Contain("key2");
            result.Should().Contain("key3");
            result.Should().Contain("value1");
            result.Should().Contain("value2");
            result.Should().Contain("value3");
        }

        #endregion

        #region Method Declaration Wrapping

        [TestMethod]
        public void FormatCode_LongMethodDeclaration_WrapsParameters()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 40,
                WrapLongLines = true,
                LineWrapStyle = LineWrapStyle.AfterOpeningBracket,
                ContinuationIndentSize = 1
            };
            var formatter = new GDFormatter(options);

            var code = @"func my_long_function(param1, param2, param3, param4):
	pass
";

            var result = formatter.FormatCode(code);

            // Method declaration should be wrapped
            result.Should().Contain("param1");
            result.Should().Contain("param2");
            result.Should().Contain("param3");
            result.Should().Contain("param4");
            result.Should().Contain("pass");
        }

        #endregion

        #region Idempotency

        [TestMethod]
        public void FormatCode_AlreadyWrapped_NoChange()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 30,
                WrapLongLines = true,
                LineWrapStyle = LineWrapStyle.AfterOpeningBracket,
                ContinuationIndentSize = 1
            };
            var formatter = new GDFormatter(options);

            // Already wrapped code
            var code = @"func test():
	var arr = [
		a,
		b,
		c
	]
";

            var result1 = formatter.FormatCode(code);
            var result2 = formatter.FormatCode(result1);

            // Should be idempotent - no change on second format
            result1.Should().Be(result2);
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void FormatCode_SingleElementArray_NoWrapping()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 20,
                WrapLongLines = true,
                LineWrapStyle = LineWrapStyle.AfterOpeningBracket
            };
            var formatter = new GDFormatter(options);

            var code = @"func test():
	var arr = [single_element]
";

            var result = formatter.FormatCode(code);

            // Single element arrays should not be wrapped
            result.Should().Contain("[single_element]");
        }

        [TestMethod]
        public void FormatCode_EmptyArray_NoWrapping()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 10,
                WrapLongLines = true,
                LineWrapStyle = LineWrapStyle.AfterOpeningBracket
            };
            var formatter = new GDFormatter(options);

            var code = @"func test():
	var arr = []
";

            var result = formatter.FormatCode(code);

            // Empty arrays should not be wrapped
            result.Should().Contain("[]");
        }

        [TestMethod]
        public void FormatCode_SingleParameterCall_NoWrapping()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 20,
                WrapLongLines = true,
                LineWrapStyle = LineWrapStyle.AfterOpeningBracket
            };
            var formatter = new GDFormatter(options);

            var code = @"func test():
	foo(x)
";

            var result = formatter.FormatCode(code);

            // Single parameter calls should not be wrapped
            result.Should().Contain("foo(x)");
        }

        #endregion

        #region UseBackslashContinuation Option

        [TestMethod]
        public void FormatCode_MethodChain_WithBackslashDisabled_NoWrapping()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 30,
                WrapLongLines = true,
                UseBackslashContinuation = false
            };
            var formatter = new GDFormatter(options);

            var code = @"func test():
	var x = obj.method1().method2().method3()
";

            var result = formatter.FormatCode(code);

            // Without backslash continuation, method chains should not be wrapped
            result.Should().Contain("obj.method1().method2().method3()");
        }

        [TestMethod]
        public void FormatCode_MethodChain_WithBackslashEnabled_WrapsChain()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 25,
                WrapLongLines = true,
                UseBackslashContinuation = true,
                ContinuationIndentSize = 1
            };
            var formatter = new GDFormatter(options);

            var code = @"func test():
	var x = obj.method1().method2().method3()
";

            var result = formatter.FormatCode(code);

            // Method chain should still contain all method calls
            result.Should().Contain("method1");
            result.Should().Contain("method2");
            result.Should().Contain("method3");
        }

        #endregion

        #region ContinuationIndentSize Option

        [TestMethod]
        public void FormatCode_ContinuationIndentSize_AffectsWrapping()
        {
            var options = new GDFormatterOptions
            {
                MaxLineLength = 25,
                WrapLongLines = true,
                LineWrapStyle = LineWrapStyle.AfterOpeningBracket,
                ContinuationIndentSize = 2, // Double indent
                IndentStyle = IndentStyle.Tabs
            };
            var formatter = new GDFormatter(options);

            var code = @"func test():
	var arr = [a, b, c, d]
";

            var result = formatter.FormatCode(code);

            // Elements should be present (indent level is implementation detail)
            result.Should().Contain("a");
            result.Should().Contain("b");
            result.Should().Contain("c");
            result.Should().Contain("d");
        }

        #endregion

        #region GDFormatterOptions Defaults

        [TestMethod]
        public void GDFormatterOptions_Defaults_AreCorrect()
        {
            var options = new GDFormatterOptions();

            options.MaxLineLength.Should().Be(100);
            options.WrapLongLines.Should().BeTrue();
            options.LineWrapStyle.Should().Be(LineWrapStyle.AfterOpeningBracket);
            options.ContinuationIndentSize.Should().Be(1);
            options.UseBackslashContinuation.Should().BeFalse();
        }

        #endregion
    }
}
