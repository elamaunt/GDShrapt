using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Linting
{
    /// <summary>
    /// Tests for complexity rules (max methods, max returns, max nesting, etc.).
    /// </summary>
    [TestClass]
    public class ComplexityRulesTests
    {
        private GDLinter _linter;

        [TestInitialize]
        public void Setup()
        {
            _linter = new GDLinter();
        }

        #region MaxPublicMethodsRule (GDL222)

        [TestMethod]
        public void MaxPublicMethods_UnderLimit_NoIssue()
        {
            var options = new GDLinterOptions { MaxPublicMethods = 5 };
            options.EnableRule("GDL222");
            var linter = new GDLinter(options);
            var code = @"
func method1():
    pass

func method2():
    pass

func method3():
    pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL222").Should().BeEmpty();
        }

        [TestMethod]
        public void MaxPublicMethods_OverLimit_ReportsIssue()
        {
            var options = new GDLinterOptions { MaxPublicMethods = 2 };
            options.EnableRule("GDL222");
            var linter = new GDLinter(options);
            var code = @"
func method1():
    pass

func method2():
    pass

func method3():
    pass
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL222" && i.Message.Contains("3") && i.Message.Contains("2"));
        }

        [TestMethod]
        public void MaxPublicMethods_PrivateNotCounted_NoIssue()
        {
            var options = new GDLinterOptions { MaxPublicMethods = 2 };
            options.EnableRule("GDL222");
            var linter = new GDLinter(options);
            var code = @"
func method1():
    pass

func method2():
    pass

func _private():
    pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL222").Should().BeEmpty();
        }

        [TestMethod]
        public void MaxPublicMethods_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { MaxPublicMethods = 0 };
            var linter = new GDLinter(options);
            var code = @"
func m1(): pass
func m2(): pass
func m3(): pass
func m4(): pass
func m5(): pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL222").Should().BeEmpty();
        }

        #endregion

        #region MaxReturnsRule (GDL223)

        [TestMethod]
        public void MaxReturns_UnderLimit_NoIssue()
        {
            var options = new GDLinterOptions { MaxReturns = 3 };
            options.EnableRule("GDL223");
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        return 1
    return 0
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL223").Should().BeEmpty();
        }

        [TestMethod]
        public void MaxReturns_OverLimit_ReportsIssue()
        {
            var options = new GDLinterOptions { MaxReturns = 2 };
            options.EnableRule("GDL223");
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

            result.Issues.Should().Contain(i => i.RuleId == "GDL223" && i.Message.Contains("3") && i.Message.Contains("2"));
        }

        [TestMethod]
        public void MaxReturns_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { MaxReturns = 0 };
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x == 1: return 1
    if x == 2: return 2
    if x == 3: return 3
    if x == 4: return 4
    return 0
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL223").Should().BeEmpty();
        }

        #endregion

        #region MaxNestingDepthRule (GDL225)

        [TestMethod]
        public void MaxNestingDepth_UnderLimit_NoIssue()
        {
            var options = new GDLinterOptions { MaxNestingDepth = 3 };
            options.EnableRule("GDL225");
            var linter = new GDLinter(options);
            var code = @"
func test():
    if true:
        if true:
            pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL225").Should().BeEmpty();
        }

        [TestMethod]
        public void MaxNestingDepth_OverLimit_ReportsIssue()
        {
            var options = new GDLinterOptions { MaxNestingDepth = 2 };
            options.EnableRule("GDL225");
            var linter = new GDLinter(options);
            var code = @"
func test():
    if true:
        if true:
            if true:
                pass
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL225" && i.Message.Contains("3") && i.Message.Contains("2"));
        }

        [TestMethod]
        public void MaxNestingDepth_ForLoop_CountsAsNesting()
        {
            var options = new GDLinterOptions { MaxNestingDepth = 2 };
            options.EnableRule("GDL225");
            var linter = new GDLinter(options);
            var code = @"
func test():
    for i in range(10):
        for j in range(10):
            for k in range(10):
                pass
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL225");
        }

        [TestMethod]
        public void MaxNestingDepth_WhileLoop_CountsAsNesting()
        {
            var options = new GDLinterOptions { MaxNestingDepth = 2 };
            options.EnableRule("GDL225");
            var linter = new GDLinter(options);
            var code = @"
func test():
    while true:
        while true:
            while true:
                pass
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL225");
        }

        [TestMethod]
        public void MaxNestingDepth_MixedNesting_CountsCorrectly()
        {
            var options = new GDLinterOptions { MaxNestingDepth = 2 };
            options.EnableRule("GDL225");
            var linter = new GDLinter(options);
            var code = @"
func test():
    if true:
        for i in range(10):
            while true:
                pass
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL225");
        }

        [TestMethod]
        public void MaxNestingDepth_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { MaxNestingDepth = 0 };
            var linter = new GDLinter(options);
            var code = @"
func test():
    if true:
        if true:
            if true:
                if true:
                    if true:
                        pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL225").Should().BeEmpty();
        }

        #endregion

        #region MaxLocalVariablesRule (GDL226)

        [TestMethod]
        public void MaxLocalVariables_UnderLimit_NoIssue()
        {
            var options = new GDLinterOptions { MaxLocalVariables = 5 };
            options.EnableRule("GDL226");
            var linter = new GDLinter(options);
            var code = @"
func test():
    var a = 1
    var b = 2
    var c = 3
    return a + b + c
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL226").Should().BeEmpty();
        }

        [TestMethod]
        public void MaxLocalVariables_OverLimit_ReportsIssue()
        {
            var options = new GDLinterOptions { MaxLocalVariables = 2 };
            options.EnableRule("GDL226");
            var linter = new GDLinter(options);
            var code = @"
func test():
    var a = 1
    var b = 2
    var c = 3
    return a + b + c
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL226" && i.Message.Contains("3") && i.Message.Contains("2"));
        }

        [TestMethod]
        public void MaxLocalVariables_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { MaxLocalVariables = 0 };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var a = 1
    var b = 2
    var c = 3
    var d = 4
    var e = 5
    var f = 6
    return 0
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL226").Should().BeEmpty();
        }

        #endregion

        #region MaxClassVariablesRule (GDL227)

        [TestMethod]
        public void MaxClassVariables_UnderLimit_NoIssue()
        {
            var options = new GDLinterOptions { MaxClassVariables = 5 };
            options.EnableRule("GDL227");
            var linter = new GDLinter(options);
            var code = @"
var a = 1
var b = 2
var c = 3
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL227").Should().BeEmpty();
        }

        [TestMethod]
        public void MaxClassVariables_OverLimit_ReportsIssue()
        {
            var options = new GDLinterOptions { MaxClassVariables = 2 };
            options.EnableRule("GDL227");
            var linter = new GDLinter(options);
            var code = @"
var a = 1
var b = 2
var c = 3
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL227" && i.Message.Contains("3") && i.Message.Contains("2"));
        }

        [TestMethod]
        public void MaxClassVariables_ConstantsNotCounted_NoIssue()
        {
            var options = new GDLinterOptions { MaxClassVariables = 2 };
            options.EnableRule("GDL227");
            var linter = new GDLinter(options);
            var code = @"
var a = 1
var b = 2
const C = 3
const D = 4
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL227").Should().BeEmpty();
        }

        [TestMethod]
        public void MaxClassVariables_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { MaxClassVariables = 0 };
            var linter = new GDLinter(options);
            var code = @"
var a = 1
var b = 2
var c = 3
var d = 4
var e = 5
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL227").Should().BeEmpty();
        }

        #endregion

        #region MaxBranchesRule (GDL228)

        [TestMethod]
        public void MaxBranches_UnderLimit_NoIssue()
        {
            var options = new GDLinterOptions { MaxBranches = 5 };
            options.EnableRule("GDL228");
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x == 1:
        return 1
    elif x == 2:
        return 2
    return 0
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL228").Should().BeEmpty();
        }

        [TestMethod]
        public void MaxBranches_OverLimit_ReportsIssue()
        {
            var options = new GDLinterOptions { MaxBranches = 3 };
            options.EnableRule("GDL228");
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x == 1:
        return 1
    elif x == 2:
        return 2
    elif x == 3:
        return 3
    elif x == 4:
        return 4
    return 0
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL228");
        }

        [TestMethod]
        public void MaxBranches_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { MaxBranches = 0 };
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x == 1: return 1
    elif x == 2: return 2
    elif x == 3: return 3
    elif x == 4: return 4
    elif x == 5: return 5
    return 0
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL228").Should().BeEmpty();
        }

        #endregion

        #region MaxBooleanExpressionsRule (GDL229)

        [TestMethod]
        public void MaxBooleanExpressions_UnderLimit_NoIssue()
        {
            var options = new GDLinterOptions { MaxBooleanExpressions = 3 };
            options.EnableRule("GDL229");
            var linter = new GDLinter(options);
            var code = @"
func test(a, b):
    if a and b:
        pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL229").Should().BeEmpty();
        }

        [TestMethod]
        public void MaxBooleanExpressions_OverLimit_ReportsIssue()
        {
            var options = new GDLinterOptions { MaxBooleanExpressions = 2 };
            options.EnableRule("GDL229");
            var linter = new GDLinter(options);
            var code = @"
func test(a, b, c, d):
    if a and b and c and d:
        pass
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL229");
        }

        [TestMethod]
        public void MaxBooleanExpressions_OrOperator_CountsCorrectly()
        {
            var options = new GDLinterOptions { MaxBooleanExpressions = 2 };
            options.EnableRule("GDL229");
            var linter = new GDLinter(options);
            var code = @"
func test(a, b, c, d):
    if a or b or c or d:
        pass
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL229");
        }

        [TestMethod]
        public void MaxBooleanExpressions_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { MaxBooleanExpressions = 0 };
            var linter = new GDLinter(options);
            var code = @"
func test(a, b, c, d, e, f):
    if a and b and c and d and e and f:
        pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL229").Should().BeEmpty();
        }

        #endregion

        #region MaxInnerClassesRule (GDL232)

        [TestMethod]
        public void MaxInnerClasses_UnderLimit_NoIssue()
        {
            var options = new GDLinterOptions { MaxInnerClasses = 3 };
            options.EnableRule("GDL232");
            var linter = new GDLinter(options);
            var code = @"
class Inner1:
    pass

class Inner2:
    pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL232").Should().BeEmpty();
        }

        [TestMethod]
        public void MaxInnerClasses_OverLimit_ReportsIssue()
        {
            var options = new GDLinterOptions { MaxInnerClasses = 2 };
            options.EnableRule("GDL232");
            var linter = new GDLinter(options);
            var code = @"
class Inner1:
    pass

class Inner2:
    pass

class Inner3:
    pass
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL232" && i.Message.Contains("3") && i.Message.Contains("2"));
        }

        [TestMethod]
        public void MaxInnerClasses_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { MaxInnerClasses = 0 };
            var linter = new GDLinter(options);
            var code = @"
class A: pass
class B: pass
class C: pass
class D: pass
class E: pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL232").Should().BeEmpty();
        }

        #endregion
    }
}
