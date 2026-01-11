using FluentAssertions;
using GDShrapt.Formatter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Formatting
{
    /// <summary>
    /// Tests for GDFormatterOptions.
    /// </summary>
    [TestClass]
    public class FormatterOptionsTests
    {
        #region Default Options

        [TestMethod]
        public void Default_HasCorrectValues()
        {
            var options = GDFormatterOptions.Default;

            options.IndentStyle.Should().Be(IndentStyle.Tabs);
            options.IndentSize.Should().Be(4);
            options.LineEnding.Should().Be(LineEndingStyle.LF);
            options.BlankLinesBetweenFunctions.Should().Be(2);
            options.SpaceAroundOperators.Should().BeTrue();
            options.SpaceAfterComma.Should().BeTrue();
            options.RemoveTrailingWhitespace.Should().BeTrue();
        }

        [TestMethod]
        public void GDScriptStyleGuide_HasCorrectValues()
        {
            var options = GDFormatterOptions.GDScriptStyleGuide;

            options.IndentStyle.Should().Be(IndentStyle.Tabs);
            options.BlankLinesBetweenFunctions.Should().Be(2);
            options.SpaceAroundOperators.Should().BeTrue();
            options.SpaceAfterComma.Should().BeTrue();
            options.SpaceAfterColon.Should().BeTrue();
            options.SpaceBeforeColon.Should().BeFalse();
        }

        [TestMethod]
        public void Minimal_HasCorrectValues()
        {
            var options = GDFormatterOptions.Minimal;

            options.RemoveTrailingWhitespace.Should().BeTrue();
            options.EnsureTrailingNewline.Should().BeTrue();
            options.RemoveMultipleTrailingNewlines.Should().BeTrue();
        }

        #endregion

        #region IndentPattern

        [TestMethod]
        public void IndentPattern_Tabs_ReturnsTab()
        {
            var options = new GDFormatterOptions { IndentStyle = IndentStyle.Tabs };

            options.IndentPattern.Should().Be("\t");
        }

        [TestMethod]
        public void IndentPattern_Spaces2_ReturnsTwoSpaces()
        {
            var options = new GDFormatterOptions
            {
                IndentStyle = IndentStyle.Spaces,
                IndentSize = 2
            };

            options.IndentPattern.Should().Be("  ");
        }

        [TestMethod]
        public void IndentPattern_Spaces4_ReturnsFourSpaces()
        {
            var options = new GDFormatterOptions
            {
                IndentStyle = IndentStyle.Spaces,
                IndentSize = 4
            };

            options.IndentPattern.Should().Be("    ");
        }

        #endregion

        #region Rule Management

        [TestMethod]
        public void DisableRule_DisablesRule()
        {
            var options = new GDFormatterOptions();
            var rule = new GDIndentationFormatRule();

            options.DisableRule(rule.RuleId);

            options.IsRuleEnabled(rule).Should().BeFalse();
        }

        [TestMethod]
        public void EnableRule_EnablesRule()
        {
            var options = new GDFormatterOptions();
            var rule = new GDIndentationFormatRule();

            options.DisableRule(rule.RuleId);
            options.EnableRule(rule.RuleId);

            options.IsRuleEnabled(rule).Should().BeTrue();
        }

        [TestMethod]
        public void IsRuleEnabled_EnabledByDefault_ReturnsTrue()
        {
            var options = new GDFormatterOptions();
            var rule = new GDIndentationFormatRule();

            options.IsRuleEnabled(rule).Should().BeTrue();
        }

        #endregion
    }
}
