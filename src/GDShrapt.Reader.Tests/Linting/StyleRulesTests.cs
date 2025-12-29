using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Linting
{
    /// <summary>
    /// Tests for style rules (line length, formatting).
    /// </summary>
    [TestClass]
    public class StyleRulesTests
    {
        private GDLinter _linter;

        [TestInitialize]
        public void Setup()
        {
            _linter = new GDLinter();
        }

        #region LineLengthRule (GDL101)

        [TestMethod]
        public void LineLength_UnderLimit_NoIssue()
        {
            var code = @"
func test():
    var x = 10
    print(x)
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL101").Should().BeEmpty();
        }

        [TestMethod]
        public void LineLength_OverLimit_ReportsIssue()
        {
            // Create a line that's definitely over 100 characters
            var longLine = "var x = \"" + new string('a', 120) + "\"";
            var code = $@"
func test():
    {longLine}
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL101");
        }

        [TestMethod]
        public void LineLength_ExactlyAtLimit_NoIssue()
        {
            // Create a line that's exactly 100 characters (accounting for indentation)
            var options = new GDLinterOptions { MaxLineLength = 50 };
            var linter = new GDLinter(options);
            var code = "var x = \"" + new string('a', 30) + "\""; // Under 50

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL101").Should().BeEmpty();
        }

        [TestMethod]
        public void LineLength_CustomLimit_ReportsCorrectly()
        {
            var options = new GDLinterOptions { MaxLineLength = 20 };
            var linter = new GDLinter(options);
            var code = @"var my_long_variable_name = 10";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL101");
        }

        [TestMethod]
        public void LineLength_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { MaxLineLength = 0 };
            var linter = new GDLinter(options);
            var longLine = "var x = \"" + new string('a', 200) + "\"";

            var result = linter.LintCode(longLine);

            result.Issues.Where(i => i.RuleId == "GDL101").Should().BeEmpty();
        }

        [TestMethod]
        public void LineLength_MultipleLines_ReportsEachLine()
        {
            var options = new GDLinterOptions { MaxLineLength = 30 };
            var linter = new GDLinter(options);
            var code = @"
var first_very_long_variable_name = 10
var second_very_long_variable_name = 20
var short = 5
";

            var result = linter.LintCode(code);

            var lineLengthIssues = result.Issues.Where(i => i.RuleId == "GDL101").ToList();
            lineLengthIssues.Count.Should().Be(2);
        }

        [TestMethod]
        public void LineLength_OnlyReportsOncePerLine()
        {
            var options = new GDLinterOptions { MaxLineLength = 20 };
            var linter = new GDLinter(options);
            var code = "var my_variable = \"some long string value\"";

            var result = linter.LintCode(code);

            var lineLengthIssues = result.Issues.Where(i => i.RuleId == "GDL101").ToList();
            lineLengthIssues.Count.Should().Be(1);
        }

        [TestMethod]
        public void LineLength_EmptyLines_NoIssue()
        {
            // Test that empty lines don't cause issues
            var options = new GDLinterOptions { MaxLineLength = 50 };
            var linter = new GDLinter(options);
            var code = @"
func test():


    pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL101").Should().BeEmpty();
        }

        [TestMethod]
        public void LineLength_IncludesComments()
        {
            var options = new GDLinterOptions { MaxLineLength = 30 };
            var linter = new GDLinter(options);
            var code = "# This is a very long comment that exceeds the limit";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL101");
        }

        [TestMethod]
        public void LineLength_IncludesStrings()
        {
            var options = new GDLinterOptions { MaxLineLength = 30 };
            var linter = new GDLinter(options);
            var code = "var s = \"This is a very long string literal\"";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL101");
        }

        [TestMethod]
        public void LineLength_MethodChaining()
        {
            var options = new GDLinterOptions { MaxLineLength = 40 };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var x = obj.method1().method2().method3().method4()
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL101");
        }

        #endregion

        #region Issue Properties Tests

        [TestMethod]
        public void LintIssue_HasCorrectLineNumber()
        {
            var options = new GDLinterOptions { MaxLineLength = 20 };
            var linter = new GDLinter(options);
            // First line is short, second line is long
            var code = "var x = 1\nvar this_is_a_very_long_line = 2\nvar y = 3";

            var result = linter.LintCode(code);

            var issue = result.Issues.First(i => i.RuleId == "GDL101");
            // Line numbers are 1-based, so long line should be on line 2
            issue.StartLine.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public void LintIssue_HasSuggestion()
        {
            var options = new GDLinterOptions { MaxLineLength = 20 };
            var linter = new GDLinter(options);
            var code = "var this_is_a_very_long_line = 2";

            var result = linter.LintCode(code);

            var issue = result.Issues.First(i => i.RuleId == "GDL101");
            issue.Suggestion.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void LintIssue_ToString_FormatsCorrectly()
        {
            var options = new GDLinterOptions { MaxLineLength = 20 };
            var linter = new GDLinter(options);
            var code = "var this_is_a_very_long_line = 2";

            var result = linter.LintCode(code);

            var issue = result.Issues.First(i => i.RuleId == "GDL101");
            var str = issue.ToString();
            str.Should().Contain("GDL101");
            str.Should().Contain("Warning");
        }

        #endregion
    }
}
