using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Linting
{
    /// <summary>
    /// Tests for comment-based rule suppression (gdlint:ignore, gdlint:disable/enable).
    /// </summary>
    [TestClass]
    public class SuppressionTests
    {
        private GDLinter _linter;

        [TestInitialize]
        public void Setup()
        {
            _linter = new GDLinter();
        }

        #region gdlint:ignore (next line suppression)

        [TestMethod]
        public void Ignore_NextLine_SuppressesIssue()
        {
            var code = @"
# gdlint:ignore = variable-name-case
var MyVariable = 10
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_NextLine_ByRuleId_SuppressesIssue()
        {
            var code = @"
# gdlint:ignore = GDL003
var MyVariable = 10
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_Inline_SuppressesCurrentLine()
        {
            var code = @"
var MyVariable = 10  # gdlint:ignore = variable-name-case
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_WithoutRules_SuppressesAllRules()
        {
            var code = @"
# gdlint:ignore
var MyVariable = 10
";

            var result = _linter.LintCode(code);

            // All issues on line 3 should be suppressed
            result.Issues.Where(i => i.StartLine == 3).Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_MultipleRules_SuppressesAll()
        {
            var options = new GDLinterOptions { MaxLineLength = 50 };
            var linter = new GDLinter(options);

            // Comment line itself is under limit; the next line exceeds it
            var code = @"# gdlint:ignore = variable-name-case, GDL101
var My_Very_Long_Variable_Name_That_Exceeds_Line_Limit = 10";

            var result = linter.LintCode(code);

            // Both naming and line length issues on line 1 should be suppressed
            result.Issues.Where(i => (i.RuleId == "GDL003" || i.RuleId == "GDL101") && i.StartLine == 1).Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_OnlyAffectsNextLine()
        {
            // Line 0: comment, Line 1: first var (suppressed), Line 2: second var (not suppressed)
            var code = @"# gdlint:ignore = variable-name-case
var MyVariable = 10
var AnotherBadName = 20";

            var result = _linter.LintCode(code);

            // First variable suppressed (line 1), second should still have issue (line 2)
            result.Issues.Where(i => i.RuleId == "GDL003" && i.StartLine == 1).Should().BeEmpty();
            result.Issues.Where(i => i.RuleId == "GDL003" && i.StartLine == 2).Should().NotBeEmpty();
        }

        [TestMethod]
        public void Ignore_CaseInsensitive()
        {
            var code = @"
# GDLINT:IGNORE = variable-name-case
var MyVariable = 10
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        #endregion

        #region gdlint:disable/enable (block suppression)

        [TestMethod]
        public void Disable_SuppressesUntilEndOfFile()
        {
            var code = @"
# gdlint: disable=variable-name-case
var MyVariable = 10
var AnotherBadName = 20
var ThirdBadName = 30
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_Enable_SuppressesOnlyBlock()
        {
            // Line 0: disable, Line 1: var1, Line 2: var2, Line 3: enable, Line 4: var3
            var code = @"# gdlint: disable=variable-name-case
var MyVariable = 10
var AnotherBadName = 20
# gdlint: enable=variable-name-case
var ThirdBadName = 30";

            var result = _linter.LintCode(code);

            // First two suppressed, third should have issue
            result.Issues.Where(i => i.RuleId == "GDL003" && i.StartLine == 1).Should().BeEmpty();
            result.Issues.Where(i => i.RuleId == "GDL003" && i.StartLine == 2).Should().BeEmpty();
            result.Issues.Where(i => i.RuleId == "GDL003" && i.StartLine == 4).Should().NotBeEmpty();
        }

        [TestMethod]
        public void Disable_WithoutRules_SuppressesAllRules()
        {
            var code = @"
# gdlint: disable
var MyVariable = 10
func BadFunctionName():
    pass
";

            var result = _linter.LintCode(code);

            // All issues after disable should be suppressed
            result.Issues.Where(i => i.StartLine >= 3).Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_ByRuleId_Works()
        {
            var code = @"
# gdlint: disable=GDL003
var MyVariable = 10
var AnotherBadName = 20
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_MultipleRules_SuppressesAll()
        {
            var options = new GDLinterOptions { MaxLineLength = 20 };
            var linter = new GDLinter(options);

            var code = @"
# gdlint: disable=variable-name-case, GDL101
var My_Very_Long_Variable_Name = 10
var Another_Very_Long_Variable_Name = 20
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003" || i.RuleId == "GDL101").Should().BeEmpty();
        }

        [TestMethod]
        public void Enable_ReEnablesRule()
        {
            // Line 0: disable, Line 1: var1, Line 2: enable, Line 3: var2
            var code = @"# gdlint: disable=variable-name-case
var MyVariable = 10
# gdlint: enable=variable-name-case
var AnotherBadName = 20";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003" && i.StartLine == 1).Should().BeEmpty();
            result.Issues.Where(i => i.RuleId == "GDL003" && i.StartLine == 3).Should().NotBeEmpty();
        }

        #endregion

        #region Suppression disabled option

        [TestMethod]
        public void SuppressionDisabled_IgnoresDirectives()
        {
            var options = new GDLinterOptions { EnableCommentSuppression = false };
            var linter = new GDLinter(options);

            var code = @"
# gdlint:ignore = variable-name-case
var MyVariable = 10
";

            var result = linter.LintCode(code);

            // Should still report issue when suppression is disabled
            result.Issues.Where(i => i.RuleId == "GDL003").Should().NotBeEmpty();
        }

        [TestMethod]
        public void SuppressionDisabled_DisableDirectiveIgnored()
        {
            var options = new GDLinterOptions { EnableCommentSuppression = false };
            var linter = new GDLinter(options);

            var code = @"
# gdlint: disable=variable-name-case
var MyVariable = 10
var AnotherBadName = 20
";

            var result = linter.LintCode(code);

            // Should still report issues when suppression is disabled
            result.Issues.Where(i => i.RuleId == "GDL003").Should().HaveCount(2);
        }

        #endregion

        #region Edge cases

        [TestMethod]
        public void Ignore_WithSpacesAroundEquals()
        {
            var code = @"
# gdlint:ignore = variable-name-case
var MyVariable = 10
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_WithNoSpacesAroundEquals()
        {
            var code = @"
# gdlint:ignore=variable-name-case
var MyVariable = 10
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_WithSpaceAfterColon()
        {
            var code = @"
# gdlint: disable=variable-name-case
var MyVariable = 10
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_WithNoSpaceAfterColon()
        {
            var code = @"
# gdlint:disable=variable-name-case
var MyVariable = 10
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void Comment_NotDirective_DoesNotSuppresses()
        {
            var code = @"
# This is just a comment about gdlint
var MyVariable = 10
";

            var result = _linter.LintCode(code);

            // Should still report issue because it's not a directive
            result.Issues.Where(i => i.RuleId == "GDL003").Should().NotBeEmpty();
        }

        [TestMethod]
        public void MultipleIgnore_OnConsecutiveLines()
        {
            var code = @"
# gdlint:ignore = variable-name-case
var MyVariable = 10
# gdlint:ignore = variable-name-case
var AnotherBadName = 20
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void Nested_Disable_Enable()
        {
            // Line 0: disable var, Line 1: var1, Line 2: disable func
            // Line 3: func1, Line 4: pass, Line 5: enable var
            // Line 6: var2, Line 7: enable func, Line 8: func2, Line 9: pass
            var code = @"# gdlint: disable=variable-name-case
var MyVariable = 10
# gdlint: disable=function-name-case
func BadFunctionName():
    pass
# gdlint: enable=variable-name-case
var AnotherBadName = 20
# gdlint: enable=function-name-case
func another_function():
    pass";

            var result = _linter.LintCode(code);

            // Variable naming should be suppressed at line 1, re-enabled at line 6
            result.Issues.Where(i => i.RuleId == "GDL003" && i.StartLine == 1).Should().BeEmpty();
            result.Issues.Where(i => i.RuleId == "GDL003" && i.StartLine == 6).Should().NotBeEmpty();
            // Function naming should be disabled at line 3
            result.Issues.Where(i => i.RuleId == "GDL002" && i.StartLine == 3).Should().BeEmpty();
        }

        #endregion
    }
}
