using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Formatting
{
    /// <summary>
    /// Tests for the GDFormatter core functionality.
    /// </summary>
    [TestClass]
    public class FormatterTests
    {
        private GDFormatter _formatter;

        [TestInitialize]
        public void Setup()
        {
            _formatter = new GDFormatter();
        }

        #region Basic Formatting

        [TestMethod]
        public void FormatCode_NullOrEmpty_ReturnsInput()
        {
            _formatter.FormatCode(null).Should().BeNull();
            _formatter.FormatCode("").Should().Be("");
        }

        [TestMethod]
        public void FormatCode_SimpleFunction_PreservesStructure()
        {
            var code = @"func test():
	pass
";

            var result = _formatter.FormatCode(code);

            result.Should().Contain("func test()");
            result.Should().Contain("pass");
        }

        [TestMethod]
        public void Format_ParsedNode_AppliesRules()
        {
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent("func test():\n\tpass");

            var formatted = _formatter.Format(tree);

            formatted.Should().NotBeNull();
        }

        #endregion

        #region Options and Rules

        [TestMethod]
        public void Formatter_DefaultOptions_HasDefaultRules()
        {
            _formatter.Rules.Should().NotBeEmpty();
            _formatter.Options.Should().NotBeNull();
        }

        [TestMethod]
        public void Formatter_CustomOptions_AreApplied()
        {
            var options = new GDFormatterOptions
            {
                IndentStyle = IndentStyle.Spaces,
                IndentSize = 2
            };

            var formatter = new GDFormatter(options);

            formatter.Options.IndentStyle.Should().Be(IndentStyle.Spaces);
            formatter.Options.IndentSize.Should().Be(2);
        }

        [TestMethod]
        public void CreateEmpty_NoRules()
        {
            var formatter = GDFormatter.CreateEmpty();

            formatter.Rules.Should().BeEmpty();
        }

        [TestMethod]
        public void AddRule_AddsCustomRule()
        {
            var formatter = GDFormatter.CreateEmpty();
            var initialCount = formatter.Rules.Count;

            formatter.AddRule(new GDIndentationFormatRule());

            formatter.Rules.Count.Should().Be(initialCount + 1);
        }

        [TestMethod]
        public void RemoveRule_RemovesRuleById()
        {
            var initialCount = _formatter.Rules.Count;

            // Use GDF005 (newline rule) which is registered by default
            var removed = _formatter.RemoveRule("GDF005");

            removed.Should().BeTrue();
            _formatter.Rules.Count.Should().Be(initialCount - 1);
        }

        [TestMethod]
        public void GetRule_ReturnsRuleById()
        {
            // Use GDF005 (newline rule) which is registered by default
            var rule = _formatter.GetRule("GDF005");

            rule.Should().NotBeNull();
            rule.RuleId.Should().Be("GDF005");
        }

        [TestMethod]
        public void GetEnabledRules_ReturnsEnabledRules()
        {
            var enabled = _formatter.GetEnabledRules().ToList();

            enabled.Should().NotBeEmpty();
        }

        #endregion

        #region Line Ending Conversion

        [TestMethod]
        public void FormatCode_LFLineEndings_ConvertsToLF()
        {
            var options = new GDFormatterOptions { LineEnding = LineEndingStyle.LF };
            var formatter = new GDFormatter(options);
            var code = "func test():\r\n\tpass\r\n";

            var result = formatter.FormatCode(code);

            result.Should().NotContain("\r\n");
            result.Should().Contain("\n");
        }

        [TestMethod]
        public void FormatCode_CRLFLineEndings_ConvertsToCRLF()
        {
            var options = new GDFormatterOptions { LineEnding = LineEndingStyle.CRLF };
            var formatter = new GDFormatter(options);
            var code = "func test():\n\tpass\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("\r\n");
        }

        #endregion

        #region Rule Enable/Disable

        [TestMethod]
        public void DisableRule_RuleNotApplied()
        {
            // Use GDF005 (newline rule) which is registered by default
            _formatter.Options.DisableRule("GDF005");

            var disabled = _formatter.GetDisabledRules().ToList();

            disabled.Should().Contain(r => r.RuleId == "GDF005");
        }

        [TestMethod]
        public void EnableRule_RuleIsApplied()
        {
            // Use GDF005 (newline rule) which is registered by default
            _formatter.Options.DisableRule("GDF005");
            _formatter.Options.EnableRule("GDF005");

            var enabled = _formatter.GetEnabledRules().ToList();

            enabled.Should().Contain(r => r.RuleId == "GDF005");
        }

        #endregion
    }
}
