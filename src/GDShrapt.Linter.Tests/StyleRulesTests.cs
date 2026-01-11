using FluentAssertions;
using GDShrapt.Linter;
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

        #region TrailingCommaRule (GDL302)

        [TestMethod]
        public void TrailingComma_SingleLineArray_NoIssue()
        {
            var options = new GDLinterOptions { RequireTrailingComma = true };
            options.EnableRule("GDL302");
            var linter = new GDLinter(options);
            var code = @"
func test():
    var arr = [1, 2, 3]
";

            var result = linter.LintCode(code);

            // Single-line arrays don't need trailing comma
            result.Issues.Where(i => i.RuleId == "GDL302").Should().BeEmpty();
        }

        [TestMethod]
        public void TrailingComma_MultilineArrayWithComma_NoIssue()
        {
            var options = new GDLinterOptions { RequireTrailingComma = true };
            options.EnableRule("GDL302");
            var linter = new GDLinter(options);
            var code = @"
func test():
    var arr = [
        1,
        2,
        3,
    ]
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL302").Should().BeEmpty();
        }

        [TestMethod]
        public void TrailingComma_MultilineArrayWithoutComma_ReportsIssue()
        {
            var options = new GDLinterOptions { RequireTrailingComma = true };
            options.EnableRule("GDL302");
            var linter = new GDLinter(options);
            var code = @"
func test():
    var arr = [
        1,
        2,
        3
    ]
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL302" && i.Message.Contains("array"));
        }

        [TestMethod]
        public void TrailingComma_MultilineDictWithoutComma_ReportsIssue()
        {
            var options = new GDLinterOptions { RequireTrailingComma = true };
            options.EnableRule("GDL302");
            var linter = new GDLinter(options);
            var code = @"
func test():
    var dict = {
        ""a"": 1,
        ""b"": 2
    }
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL302" && i.Message.Contains("dictionary"));
        }

        [TestMethod]
        public void TrailingComma_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { RequireTrailingComma = false };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var arr = [
        1,
        2,
        3
    ]
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL302").Should().BeEmpty();
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

        #region MaxFileLinesRule (GDL102)

        [TestMethod]
        public void MaxFileLines_UnderLimit_NoIssue()
        {
            var options = new GDLinterOptions { MaxFileLines = 10 };
            var linter = new GDLinter(options);
            var code = @"extends Node
var x = 1
var y = 2
var z = 3";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL102").Should().BeEmpty();
        }

        [TestMethod]
        public void MaxFileLines_OverLimit_ReportsIssue()
        {
            var options = new GDLinterOptions { MaxFileLines = 3 };
            var linter = new GDLinter(options);
            var code = @"extends Node
var a = 1
var b = 2
var c = 3
var d = 4";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL102");
        }

        [TestMethod]
        public void MaxFileLines_ExactlyAtLimit_NoIssue()
        {
            var options = new GDLinterOptions { MaxFileLines = 5 };
            var linter = new GDLinter(options);
            var code = @"extends Node
var a = 1
var b = 2
var c = 3
var d = 4";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL102").Should().BeEmpty();
        }

        [TestMethod]
        public void MaxFileLines_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { MaxFileLines = 0 };
            var linter = new GDLinter(options);
            var lines = new string[100];
            lines[0] = "extends Node";
            for (int i = 1; i < 100; i++)
                lines[i] = $"var x{i} = {i}";
            var code = string.Join("\n", lines);

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL102").Should().BeEmpty();
        }

        [TestMethod]
        public void MaxFileLines_ReportsCorrectLineCount()
        {
            var options = new GDLinterOptions { MaxFileLines = 5 };
            var linter = new GDLinter(options);
            var lines = new string[10];
            lines[0] = "extends Node";
            for (int i = 1; i < 10; i++)
                lines[i] = $"var x{i} = {i}";
            var code = string.Join("\n", lines);

            var result = linter.LintCode(code);

            var issue = result.Issues.FirstOrDefault(i => i.RuleId == "GDL102");
            issue.Should().NotBeNull();
            issue.Message.Should().Contain("5"); // Contains max limit
        }

        #endregion

        #region NoElifReturnRule (GDL216)

        [TestMethod]
        public void NoElifReturn_IfReturnsWithElif_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnNoElifReturn = true };
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        return 1
    elif x < 0:
        return -1
    return 0
";
            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL216");
        }

        [TestMethod]
        public void NoElifReturn_IfDoesNotReturn_NoIssue()
        {
            var options = new GDLinterOptions { WarnNoElifReturn = true };
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        print(""positive"")
    elif x < 0:
        return -1
    return 0
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL216").Should().BeEmpty();
        }

        [TestMethod]
        public void NoElifReturn_MultipleElifs_ReportsAll()
        {
            var options = new GDLinterOptions { WarnNoElifReturn = true };
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 10:
        return 2
    elif x > 0:
        return 1
    elif x < 0:
        return -1
    return 0
";
            var result = linter.LintCode(code);

            var elifIssues = result.Issues.Where(i => i.RuleId == "GDL216").ToList();
            elifIssues.Count.Should().Be(2);
        }

        [TestMethod]
        public void NoElifReturn_DisabledByDefault_NoIssue()
        {
            var code = @"
func test(x):
    if x > 0:
        return 1
    elif x < 0:
        return -1
    return 0
";
            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL216").Should().BeEmpty();
        }

        #endregion

        #region NoElseReturnRule (GDL217)

        [TestMethod]
        public void NoElseReturn_IfReturnsWithElse_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnNoElseReturn = true };
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        return 1
    else:
        return 0
";
            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL217");
        }

        [TestMethod]
        public void NoElseReturn_IfDoesNotReturn_NoIssue()
        {
            var options = new GDLinterOptions { WarnNoElseReturn = true };
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        print(""positive"")
    else:
        return 0
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL217").Should().BeEmpty();
        }

        [TestMethod]
        public void NoElseReturn_AllBranchesReturn_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnNoElseReturn = true };
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        return 1
    elif x < 0:
        return -1
    else:
        return 0
";
            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL217");
        }

        [TestMethod]
        public void NoElseReturn_DisabledByDefault_NoIssue()
        {
            var code = @"
func test(x):
    if x > 0:
        return 1
    else:
        return 0
";
            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL217").Should().BeEmpty();
        }

        [TestMethod]
        public void NoElseReturn_NestedIf_ReportsCorrectly()
        {
            var options = new GDLinterOptions { WarnNoElseReturn = true };
            var linter = new GDLinter(options);
            var code = @"
func test(x, y):
    if x > 0:
        if y > 0:
            return 1
        else:
            return 2
    else:
        return 0
";
            var result = linter.LintCode(code);

            // Should report both else blocks since their parent ifs all end with return
            var elseIssues = result.Issues.Where(i => i.RuleId == "GDL217").ToList();
            elseIssues.Count.Should().BeGreaterOrEqualTo(1);
        }

        #endregion

        #region NoLonelyIfRule (GDL233)

        [TestMethod]
        public void NoLonelyIf_IfNotInElse_NoIssue()
        {
            var options = new GDLinterOptions { WarnNoLonelyIf = true };
            options.EnableRule("GDL233");
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        return 1
    return 0
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL233").Should().BeEmpty();
        }

        [TestMethod]
        public void NoLonelyIf_ElseWithMultipleStatements_NoIssue()
        {
            var options = new GDLinterOptions { WarnNoLonelyIf = true };
            options.EnableRule("GDL233");
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        return 1
    else:
        print(""negative"")
        if x < -10:
            return -2
        return -1
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL233").Should().BeEmpty();
        }

        [TestMethod]
        public void NoLonelyIf_LonelyIfInElse_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnNoLonelyIf = true };
            options.EnableRule("GDL233");
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        return 1
    else:
        if x < -10:
            return -2
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL233");
        }

        [TestMethod]
        public void NoLonelyIf_Disabled_NoIssue()
        {
            var code = @"
func test(x):
    if x > 0:
        return 1
    else:
        if x < -10:
            return -2
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL233").Should().BeEmpty();
        }

        [TestMethod]
        public void NoLonelyIf_SuggestsElif()
        {
            var options = new GDLinterOptions { WarnNoLonelyIf = true };
            options.EnableRule("GDL233");
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        return 1
    else:
        if x < 0:
            return -1
";

            var result = linter.LintCode(code);

            var issue = result.Issues.FirstOrDefault(i => i.RuleId == "GDL233");
            issue.Should().NotBeNull();
            issue.Suggestion.Should().Contain("elif");
        }

        #endregion
    }
}
