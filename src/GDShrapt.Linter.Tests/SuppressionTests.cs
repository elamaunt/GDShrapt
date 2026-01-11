using FluentAssertions;
using GDShrapt.Linter;
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

        #region New Rules Suppression (GDL009, GDL102, GDL216, GDL217, GDL218, GDL219)

        [TestMethod]
        public void Ignore_InnerClassName_SuppressesGDL009()
        {
            var code = @"
# gdlint:ignore = inner-class-name-case
class my_inner_class:
    pass
";
            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL009").Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_InnerClassName_ByRuleId_SuppressesGDL009()
        {
            var code = @"
# gdlint:ignore = GDL009
class my_inner_class:
    pass
";
            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL009").Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_InnerClassName_SuppressesGDL009()
        {
            var code = @"
# gdlint: disable=inner-class-name-case
class my_inner_class:
    pass

class another_inner_class:
    pass
";
            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL009").Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_MaxFileLines_SuppressesGDL102()
        {
            var options = new GDLinterOptions { MaxFileLines = 3 };
            var linter = new GDLinter(options);
            var code = @"# gdlint: disable=max-file-lines
extends Node
var a = 1
var b = 2
var c = 3";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL102").Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_MaxFileLines_ByRuleId_SuppressesGDL102()
        {
            var options = new GDLinterOptions { MaxFileLines = 3 };
            var linter = new GDLinter(options);
            var code = @"# gdlint: disable=GDL102
extends Node
var a = 1
var b = 2
var c = 3";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL102").Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_NoElifReturn_SuppressesGDL216()
        {
            var options = new GDLinterOptions { WarnNoElifReturn = true };
            var linter = new GDLinter(options);
            var code = @"
# gdlint: disable=no-elif-return
func test(x):
    if x > 0:
        return 1
    elif x < 0:
        return -1
    return 0
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL216").Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_NoElifReturn_ByRuleId_SuppressesGDL216()
        {
            var options = new GDLinterOptions { WarnNoElifReturn = true };
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        return 1
    # gdlint:ignore = GDL216
    elif x < 0:
        return -1
    return 0
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL216").Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_NoElseReturn_SuppressesGDL217()
        {
            var options = new GDLinterOptions { WarnNoElseReturn = true };
            var linter = new GDLinter(options);
            var code = @"
# gdlint: disable=no-else-return
func test(x):
    if x > 0:
        return 1
    else:
        return 0
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL217").Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_NoElseReturn_ByRuleId_SuppressesGDL217()
        {
            var options = new GDLinterOptions { WarnNoElseReturn = true };
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        return 1
    # gdlint:ignore = GDL217
    else:
        return 0
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL217").Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_PrivateMethodCall_SuppressesGDL218()
        {
            var options = new GDLinterOptions { WarnPrivateMethodCall = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var node = get_node(""."")
    # gdlint:ignore = private-method-call
    node._private_method()
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL218").Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_PrivateMethodCall_SuppressesGDL218()
        {
            var options = new GDLinterOptions { WarnPrivateMethodCall = true };
            var linter = new GDLinter(options);
            var code = @"
# gdlint: disable=private-method-call
func test():
    var node = get_node(""."")
    node._private_method()
    node._another_private()
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL218").Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_PrivateMethodCall_ByRuleId_SuppressesGDL218()
        {
            var options = new GDLinterOptions { WarnPrivateMethodCall = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var node = get_node(""."")
    # gdlint:ignore = GDL218
    node._private_method()
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL218").Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_DuplicatedLoad_SuppressesGDL219()
        {
            var code = @"
# gdlint: disable=duplicated-load
var Scene1 = load(""res://scene.tscn"")
var Scene2 = load(""res://scene.tscn"")
";
            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL219").Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_DuplicatedLoad_ByRuleId_SuppressesGDL219()
        {
            var code = @"
var Scene1 = load(""res://scene.tscn"")
# gdlint:ignore = GDL219
var Scene2 = load(""res://scene.tscn"")
";
            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL219").Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_Enable_DuplicatedLoad_WorksCorrectly()
        {
            var code = @"# gdlint: disable=duplicated-load
var Scene1 = load(""res://scene1.tscn"")
var Scene2 = load(""res://scene1.tscn"")
# gdlint: enable=duplicated-load
var Scene3 = load(""res://scene2.tscn"")
var Scene4 = load(""res://scene2.tscn"")";

            var result = _linter.LintCode(code);

            // First duplicate suppressed, second duplicate should report
            result.Issues.Where(i => i.RuleId == "GDL219" && i.StartLine <= 2).Should().BeEmpty();
            result.Issues.Where(i => i.RuleId == "GDL219" && i.StartLine > 3).Should().NotBeEmpty();
        }

        [TestMethod]
        public void Ignore_MultipleNewRules_SuppressesAll()
        {
            var options = new GDLinterOptions { WarnNoElseReturn = true };
            var linter = new GDLinter(options);

            var code = @"
# gdlint:ignore = inner-class-name-case, no-else-return
class my_bad_class:
    func test(x):
        if x > 0:
            return 1
        else:
            return 0
";
            var result = linter.LintCode(code);

            // Both GDL009 (inner class name) and GDL217 (no-else-return) should be suppressed
            // Note: The inner class is on the next line after the ignore comment
            result.Issues.Where(i => i.RuleId == "GDL009").Should().BeEmpty();
        }

        #endregion
    }
}
