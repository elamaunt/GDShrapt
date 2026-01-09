using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Linting
{
    /// <summary>
    /// Tests for .gdlintrc configuration file parsing.
    /// </summary>
    [TestClass]
    public class GDLintrcParserTests
    {
        #region Basic Parsing

        [TestMethod]
        public void ParseContent_EmptyContent_ReturnsDefault()
        {
            var options = GDLintrcParser.ParseContent("");

            options.Should().NotBeNull();
            options.MaxLineLength.Should().Be(100); // Default value
        }

        [TestMethod]
        public void ParseContent_MaxLineLength_SetsValue()
        {
            var content = "max-line-length: 120";

            var options = GDLintrcParser.ParseContent(content);

            options.MaxLineLength.Should().Be(120);
        }

        [TestMethod]
        public void ParseContent_MaxFileLines_SetsValue()
        {
            var content = "max-file-lines: 500";

            var options = GDLintrcParser.ParseContent(content);

            options.MaxFileLines.Should().Be(500);
        }

        [TestMethod]
        public void ParseContent_FunctionArgumentsNumber_SetsValue()
        {
            var content = "function-arguments-number: 8";

            var options = GDLintrcParser.ParseContent(content);

            options.MaxParameters.Should().Be(8);
        }

        [TestMethod]
        public void ParseContent_Comments_AreIgnored()
        {
            var content = @"
# This is a comment
max-line-length: 80
# Another comment
";

            var options = GDLintrcParser.ParseContent(content);

            options.MaxLineLength.Should().Be(80);
        }

        [TestMethod]
        public void ParseContent_MultipleOptions_AllParsed()
        {
            var content = @"
max-line-length: 120
max-file-lines: 800
function-arguments-number: 6
";

            var options = GDLintrcParser.ParseContent(content);

            options.MaxLineLength.Should().Be(120);
            options.MaxFileLines.Should().Be(800);
            options.MaxParameters.Should().Be(6);
        }

        #endregion

        #region Disable Rules

        [TestMethod]
        public void ParseContent_DisableSingleRule_DisablesRule()
        {
            var content = "disable: [class-name]";

            var options = GDLintrcParser.ParseContent(content);

            // GDL001 should be disabled
            var linter = new GDLinter(options);
            options.DisableRule("GDL001");
            options.IsRuleEnabled(linter.GetRule("GDL001")).Should().BeFalse();
        }

        [TestMethod]
        public void ParseContent_DisableMultipleRules_DisablesAll()
        {
            var content = "disable: [class-name, function-name, signal-name]";

            var options = GDLintrcParser.ParseContent(content);

            // Rules should be disabled
            var linter = new GDLinter(options);
            options.IsRuleEnabled(linter.GetRule("GDL001")).Should().BeFalse(); // class-name
            options.IsRuleEnabled(linter.GetRule("GDL002")).Should().BeFalse(); // function-name
            options.IsRuleEnabled(linter.GetRule("GDL005")).Should().BeFalse(); // signal-name
        }

        [TestMethod]
        public void ParseContent_DisableWithQuotes_Works()
        {
            var content = "disable: [\"class-name\", 'function-name']";

            var options = GDLintrcParser.ParseContent(content);

            var linter = new GDLinter(options);
            options.IsRuleEnabled(linter.GetRule("GDL001")).Should().BeFalse();
            options.IsRuleEnabled(linter.GetRule("GDL002")).Should().BeFalse();
        }

        [TestMethod]
        public void ParseContent_DisableByRuleId_Works()
        {
            var content = "disable: [GDL101, GDL201]";

            var options = GDLintrcParser.ParseContent(content);

            var linter = new GDLinter(options);
            options.IsRuleEnabled(linter.GetRule("GDL101")).Should().BeFalse();
            options.IsRuleEnabled(linter.GetRule("GDL201")).Should().BeFalse();
        }

        #endregion

        #region Naming Conventions

        [TestMethod]
        public void ParseContent_ClassNamePascalCase_SetsConvention()
        {
            var content = "class-name: ([A-Z][a-z0-9]*)+";

            var options = GDLintrcParser.ParseContent(content);

            options.ClassNameCase.Should().Be(NamingCase.PascalCase);
        }

        [TestMethod]
        public void ParseContent_FunctionNameSnakeCase_SetsConvention()
        {
            var content = "function-name: _?[a-z][a-z0-9_]*";

            var options = GDLintrcParser.ParseContent(content);

            options.FunctionNameCase.Should().Be(NamingCase.SnakeCase);
        }

        [TestMethod]
        public void ParseContent_ConstantNameScreamingSnake_SetsConvention()
        {
            var content = "constant-name: [A-Z][A-Z0-9_]*";

            var options = GDLintrcParser.ParseContent(content);

            options.ConstantNameCase.Should().Be(NamingCase.ScreamingSnakeCase);
        }

        #endregion

        #region Rule Name Mapping

        [TestMethod]
        public void GetRuleId_KnownRuleName_ReturnsId()
        {
            GDLintrcParser.GetRuleId("class-name").Should().Be("GDL001");
            GDLintrcParser.GetRuleId("function-name").Should().Be("GDL002");
            GDLintrcParser.GetRuleId("unused-argument").Should().Be("GDL202");
            GDLintrcParser.GetRuleId("duplicated-load").Should().Be("GDL219");
        }

        [TestMethod]
        public void GetRuleId_UnknownRuleName_ReturnsNull()
        {
            GDLintrcParser.GetRuleId("unknown-rule").Should().BeNull();
        }

        [TestMethod]
        public void GetKnownRuleNames_ReturnsAllMappedNames()
        {
            var names = GDLintrcParser.GetKnownRuleNames();

            names.Should().Contain("class-name");
            names.Should().Contain("function-name");
            names.Should().Contain("duplicated-load");
            names.Should().Contain("private-method-call");
        }

        #endregion

        #region Real World Examples

        [TestMethod]
        public void ParseContent_RealGdlintrc_ParsesCorrectly()
        {
            var content = @"
# GDScript style configuration

class-name: ([A-Z][a-z0-9]*)+
function-name: (_on_([A-Z][a-z0-9]*)+_[a-z0-9_]+|_?[a-z][a-z0-9_]*)
class-variable-name: (_?[a-z][a-z0-9_]*|_?[A-Z][a-z0-9]*)
constant-name: [A-Z][A-Z0-9_]*
signal-name: [a-z][a-z0-9_]*

max-line-length: 100
max-file-lines: 1000

disable: [
    unnecessary-pass,
    max-public-methods
]
";

            var options = GDLintrcParser.ParseContent(content);

            options.MaxLineLength.Should().Be(100);
            options.MaxFileLines.Should().Be(1000);
            options.ClassNameCase.Should().Be(NamingCase.PascalCase);
        }

        #endregion
    }
}
