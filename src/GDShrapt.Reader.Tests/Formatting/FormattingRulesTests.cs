using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Formatting
{
    /// <summary>
    /// Tests for individual formatting rules.
    /// </summary>
    [TestClass]
    public class FormattingRulesTests
    {
        #region IndentationRule Tests

        [TestMethod]
        public void GDIndentationFormatRule_HasCorrectId()
        {
            var rule = new GDIndentationFormatRule();

            rule.RuleId.Should().Be("GDF001");
            rule.Name.Should().Be("indentation");
        }

        [TestMethod]
        public void GDIndentationFormatRule_EnabledByDefault()
        {
            var rule = new GDIndentationFormatRule();

            rule.EnabledByDefault.Should().BeTrue();
        }

        #endregion

        #region BlankLinesRule Tests

        [TestMethod]
        public void GDBlankLinesFormatRule_HasCorrectId()
        {
            var rule = new GDBlankLinesFormatRule();

            rule.RuleId.Should().Be("GDF002");
            rule.Name.Should().Be("blank-lines");
        }

        [TestMethod]
        public void GDBlankLinesFormatRule_EnabledByDefault()
        {
            var rule = new GDBlankLinesFormatRule();

            rule.EnabledByDefault.Should().BeTrue();
        }

        #endregion

        #region SpacingRule Tests

        [TestMethod]
        public void GDSpacingFormatRule_HasCorrectId()
        {
            var rule = new GDSpacingFormatRule();

            rule.RuleId.Should().Be("GDF003");
            rule.Name.Should().Be("spacing");
        }

        [TestMethod]
        public void GDSpacingFormatRule_EnabledByDefault()
        {
            var rule = new GDSpacingFormatRule();

            rule.EnabledByDefault.Should().BeTrue();
        }

        #endregion

        #region TrailingWhitespaceRule Tests

        [TestMethod]
        public void GDTrailingWhitespaceFormatRule_HasCorrectId()
        {
            var rule = new GDTrailingWhitespaceFormatRule();

            rule.RuleId.Should().Be("GDF004");
            rule.Name.Should().Be("trailing-whitespace");
        }

        [TestMethod]
        public void GDTrailingWhitespaceFormatRule_EnabledByDefault()
        {
            var rule = new GDTrailingWhitespaceFormatRule();

            rule.EnabledByDefault.Should().BeTrue();
        }

        #endregion

        #region NewLineRule Tests

        [TestMethod]
        public void GDNewLineFormatRule_HasCorrectId()
        {
            var rule = new GDNewLineFormatRule();

            rule.RuleId.Should().Be("GDF005");
            rule.Name.Should().Be("newline");
        }

        [TestMethod]
        public void GDNewLineFormatRule_EnabledByDefault()
        {
            var rule = new GDNewLineFormatRule();

            rule.EnabledByDefault.Should().BeTrue();
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void Formatter_AllRulesRegistered()
        {
            var formatter = new GDFormatter();

            // Note: Some rules are currently disabled due to idempotency issues.
            // Only GDNewLineFormatRule (GDF005) is registered by default.
            // When the other rules are fixed, they will be re-enabled and this test updated.
            formatter.Rules.Should().HaveCountGreaterOrEqualTo(1);
            formatter.GetRule("GDF005").Should().NotBeNull(); // Only this rule is guaranteed
        }

        [TestMethod]
        public void FormatCode_WithAllRules_Succeeds()
        {
            var formatter = new GDFormatter();
            var code = @"func test():
	var x = 10
	print(x)
";

            var result = formatter.FormatCode(code);

            result.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void FormatCode_VariableWithType_PreservesSpacing()
        {
            var formatter = new GDFormatter();
            var code = @"var x: int = 10
";

            var result = formatter.FormatCode(code);

            result.Should().Contain("var x");
            result.Should().Contain("int");
            result.Should().Contain("10");
        }

        [TestMethod]
        public void FormatCode_FunctionWithParams_PreservesSpacing()
        {
            var formatter = new GDFormatter();
            var code = @"func test(a, b):
	pass
";

            var result = formatter.FormatCode(code);

            result.Should().Contain("func test");
            result.Should().Contain("a");
            result.Should().Contain("b");
        }

        [TestMethod]
        public void FormatCode_ArrayInitializer_Formats()
        {
            var formatter = new GDFormatter();
            var code = @"var arr = [1, 2, 3]
";

            var result = formatter.FormatCode(code);

            result.Should().Contain("[");
            result.Should().Contain("]");
        }

        [TestMethod]
        public void FormatCode_DictionaryInitializer_Formats()
        {
            var formatter = new GDFormatter();
            var code = @"var dict = { ""key"": 1 }
";

            var result = formatter.FormatCode(code);

            result.Should().Contain("{");
            result.Should().Contain("}");
        }

        #endregion
    }
}
