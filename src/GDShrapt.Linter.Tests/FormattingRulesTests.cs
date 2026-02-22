using FluentAssertions;
using GDShrapt.Linter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Linting
{
    /// <summary>
    /// Tests for formatting rules (text-based).
    /// </summary>
    [TestClass]
    public class FormattingRulesTests
    {
        private GDLinter _linter;

        [TestInitialize]
        public void Setup()
        {
            _linter = new GDLinter();
        }

        #region GDIndentationConsistencyRule (GDL501)

        [TestMethod]
        public void IndentationConsistency_TabsCorrect_NoIssue()
        {
            var code = "func foo():\n\tpass\n";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL501").Should().BeEmpty();
        }

        [TestMethod]
        public void IndentationConsistency_SpacesWhenTabsExpected_ReportsIssue()
        {
            var code = "func foo():\n    pass\n";
            var options = new GDLinterOptions { IndentationStyle = GDIndentationStyle.Tabs };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL501" &&
                i.Message.Contains("Spaces used for indentation"));
        }

        [TestMethod]
        public void IndentationConsistency_TabsWhenSpacesExpected_ReportsIssue()
        {
            var code = "func foo():\n\tpass\n";
            var options = new GDLinterOptions { IndentationStyle = GDIndentationStyle.Spaces };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL501" &&
                i.Message.Contains("Tabs used for indentation"));
        }

        [TestMethod]
        public void IndentationConsistency_MixedIndentation_ReportsIssue()
        {
            var code = "func foo():\n\t pass\n"; // tab followed by space
            var options = new GDLinterOptions { IndentationStyle = GDIndentationStyle.Tabs };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL501" &&
                i.Message.Contains("Mixed tabs and spaces"));
        }

        [TestMethod]
        public void IndentationConsistency_CorrectPosition()
        {
            var code = "func foo():\n    pass\n";
            var options = new GDLinterOptions { IndentationStyle = GDIndentationStyle.Tabs };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            var issue = result.Issues.FirstOrDefault(i => i.RuleId == "GDL501");
            issue.Should().NotBeNull();
            issue!.StartLine.Should().Be(2); // 1-based
            issue.StartColumn.Should().Be(1); // 1-based
        }

        #endregion

        #region GDTrailingWhitespaceRule (GDL502)

        [TestMethod]
        public void TrailingWhitespace_NoTrailing_NoIssue()
        {
            var code = "func foo():\n\tpass\n";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL502").Should().BeEmpty();
        }

        [TestMethod]
        public void TrailingWhitespace_WithTrailing_ReportsIssue()
        {
            var code = "func foo():  \n\tpass\n"; // trailing spaces on first line

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL502");
        }

        [TestMethod]
        public void TrailingWhitespace_Disabled_NoIssue()
        {
            var code = "func foo():  \n\tpass\n";
            var options = new GDLinterOptions { CheckTrailingWhitespace = false };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL502").Should().BeEmpty();
        }

        [TestMethod]
        public void TrailingWhitespace_CorrectPosition()
        {
            var code = "func foo():  \n\tpass\n";

            var result = _linter.LintCode(code);

            var issue = result.Issues.FirstOrDefault(i => i.RuleId == "GDL502");
            issue.Should().NotBeNull();
            issue!.StartLine.Should().Be(1); // 1-based
            issue.StartColumn.Should().Be(12); // After the colon, before spaces
        }

        #endregion

        #region GDTrailingNewlineRule (GDL503)

        [TestMethod]
        public void TrailingNewline_SingleNewline_NoIssue()
        {
            var code = "func foo():\n\tpass\n";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL503").Should().BeEmpty();
        }

        [TestMethod]
        public void TrailingNewline_NoNewline_ReportsIssue()
        {
            var code = "func foo():\n\tpass"; // no trailing newline

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL503" &&
                i.Message.Contains("should end with a newline"));
        }

        [TestMethod]
        public void TrailingNewline_MultipleNewlines_ReportsIssue()
        {
            var code = "func foo():\n\tpass\n\n\n"; // 3 trailing newlines

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL503" &&
                i.Message.Contains("trailing newlines"));
        }

        [TestMethod]
        public void TrailingNewline_Disabled_NoIssue()
        {
            var code = "func foo():\n\tpass";
            var options = new GDLinterOptions { CheckTrailingNewline = false };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL503").Should().BeEmpty();
        }

        #endregion

        #region GDSpaceAroundOperatorsRule (GDL510)

        [TestMethod]
        public void SpaceAroundOperators_Correct_NoIssue()
        {
            var code = "var x = 1\nvar y = x + 1\n";
            var options = new GDLinterOptions { CheckSpaceAroundOperators = true };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL510").Should().BeEmpty();
        }

        [TestMethod]
        public void SpaceAroundOperators_MissingSpaceBefore_ReportsIssue()
        {
            var code = "var x= 1\n";
            var options = new GDLinterOptions { CheckSpaceAroundOperators = true };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL510" &&
                i.Message.Contains("Missing space before"));
        }

        [TestMethod]
        public void SpaceAroundOperators_MissingSpaceAfter_ReportsIssue()
        {
            var code = "var x =1\n";
            var options = new GDLinterOptions { CheckSpaceAroundOperators = true };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL510" &&
                i.Message.Contains("Missing space after"));
        }

        [TestMethod]
        public void SpaceAroundOperators_DisabledByDefault_NoIssue()
        {
            var code = "var x=1\n";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL510").Should().BeEmpty();
        }

        #endregion

        #region GDSpaceAfterCommaRule (GDL511)

        [TestMethod]
        public void SpaceAfterComma_Correct_NoIssue()
        {
            var code = "func foo(a, b, c):\n\tpass\n";
            var options = new GDLinterOptions { CheckSpaceAfterComma = true };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL511").Should().BeEmpty();
        }

        [TestMethod]
        public void SpaceAfterComma_MissingSpace_ReportsIssue()
        {
            var code = "func foo(a,b,c):\n\tpass\n";
            var options = new GDLinterOptions { CheckSpaceAfterComma = true };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL511" &&
                i.Message.Contains("Missing space after comma"));
        }

        [TestMethod]
        public void SpaceAfterComma_DisabledByDefault_NoIssue()
        {
            var code = "func foo(a,b,c):\n\tpass\n";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL511").Should().BeEmpty();
        }

        #endregion

        #region GDEmptyLinesRule (GDL513)

        [TestMethod]
        public void EmptyLines_CorrectSpacing_NoIssue()
        {
            var code = @"func foo():
	pass


func bar():
	pass
";
            var options = new GDLinterOptions { EmptyLinesBetweenFunctions = 2 };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL513").Should().BeEmpty();
        }

        [TestMethod]
        public void EmptyLines_NotEnough_ReportsIssue()
        {
            var code = @"func foo():
	pass

func bar():
	pass
";
            var options = new GDLinterOptions { EmptyLinesBetweenFunctions = 2 };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL513" &&
                i.Message.Contains("Expected"));
        }

        [TestMethod]
        public void EmptyLines_TooManyConsecutive_ReportsIssue()
        {
            var code = @"func foo():
	pass




func bar():
	pass
";
            var options = new GDLinterOptions { MaxConsecutiveEmptyLines = 3 };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL513" &&
                i.Message.Contains("Too many consecutive blank lines"));
        }

        [TestMethod]
        public void EmptyLines_Disabled_NoIssue()
        {
            var code = @"func foo():
	pass
func bar():
	pass
";
            var options = new GDLinterOptions { EmptyLinesBetweenFunctions = 0, MaxConsecutiveEmptyLines = 0 };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL513").Should().BeEmpty();
        }

        [TestMethod]
        public void EmptyLines_WithCommentBeforeFunction_NoIssue()
        {
            var code = "func foo():\n\tpass\n\n\n# Comment for next function\nfunc bar():\n\tpass\n";
            var options = new GDLinterOptions { EmptyLinesBetweenFunctions = 2 };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL513").Should().BeEmpty();
        }

        [TestMethod]
        public void EmptyLines_MultipleCommentsBeforeFunction_NoIssue()
        {
            var code = "func foo():\n\tpass\n\n\n# Category header\n# Detailed description\nfunc bar():\n\tpass\n";
            var options = new GDLinterOptions { EmptyLinesBetweenFunctions = 2 };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL513").Should().BeEmpty();
        }

        [TestMethod]
        public void EmptyLines_CommentButNotEnoughBlankLines_ReportsIssue()
        {
            var code = "func foo():\n\tpass\n\n# Comment\nfunc bar():\n\tpass\n";
            var options = new GDLinterOptions { EmptyLinesBetweenFunctions = 2 };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL513" &&
                i.Message.Contains("Expected"));
        }

        [TestMethod]
        public void EmptyLines_ManyFunctionsWithComments_NoIssue()
        {
            var code = @"func _ready():
	super._ready()


# Method without type annotations
func take_damage(amount):
	pass


# Dodge check
func _try_dodge():
	return randf() < 0.15


# Show dodge effect
func _show_dodge_effect():
	pass


# Afterimages handling
func _update_afterimages():
	pass
";
            var options = new GDLinterOptions { EmptyLinesBetweenFunctions = 2 };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL513").Should().BeEmpty();
        }

        [TestMethod]
        public void EmptyLines_MultilineString_BetweenFunctions_NoFalsePositive()
        {
            // Multiline string between functions should not inflate empty line count
            var code = "func foo():\n\tvar x = \"line1\\nline2\\nline3\"\n\tpass\n\n\nfunc bar():\n\tpass\n";
            var options = new GDLinterOptions { EmptyLinesBetweenFunctions = 2 };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL513").Should().BeEmpty();
        }

        [TestMethod]
        public void EmptyLines_NormalCase_StillReports()
        {
            // Normal case: 1 empty line when 2 required should still report
            var code = "func foo():\n\tpass\n\nfunc bar():\n\tpass\n";
            var options = new GDLinterOptions { EmptyLinesBetweenFunctions = 2 };
            var linter = new GDLinter(options);

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL513" &&
                i.Message.Contains("Expected"));
        }

        #endregion
    }
}
