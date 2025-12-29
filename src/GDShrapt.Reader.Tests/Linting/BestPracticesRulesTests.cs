using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Linting
{
    /// <summary>
    /// Tests for best practices rules (unused variables, empty functions, type hints).
    /// </summary>
    [TestClass]
    public class BestPracticesRulesTests
    {
        private GDLinter _linter;

        [TestInitialize]
        public void Setup()
        {
            _linter = new GDLinter();
        }

        #region UnusedVariableRule (GDL201)

        [TestMethod]
        public void UnusedVariable_Used_NoIssue()
        {
            var code = @"
func test():
    var x = 10
    print(x)
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL201").Should().BeEmpty();
        }

        [TestMethod]
        public void UnusedVariable_NotUsed_ReportsIssue()
        {
            var code = @"
func test():
    var unused_var = 10
    print(""hello"")
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL201" && i.Message.Contains("unused_var"));
        }

        [TestMethod]
        public void UnusedVariable_PrefixedWithUnderscore_NoIssue()
        {
            var code = @"
func test():
    var _unused = 10
    print(""hello"")
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL201" && i.Message.Contains("_unused")).Should().BeEmpty();
        }

        [TestMethod]
        public void UnusedVariable_MultipleVariables_ReportsOnlyUnused()
        {
            var code = @"
func test():
    var used = 10
    var unused = 20
    print(used)
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL201" && i.Message.Contains("unused"));
            result.Issues.Where(i => i.RuleId == "GDL201" && i.Message.Contains("'used'")).Should().BeEmpty();
        }

        [TestMethod]
        public void UnusedVariable_UsedInExpression_NoIssue()
        {
            var code = @"
func test():
    var x = 10
    var y = x + 5
    return y
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL201" && i.Message.Contains("'x'")).Should().BeEmpty();
        }

        [TestMethod]
        public void UnusedVariable_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { WarnUnusedVariables = false };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var unused = 10
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL201").Should().BeEmpty();
        }

        #endregion

        #region UnusedParameterRule (GDL202)

        [TestMethod]
        public void UnusedParameter_Used_NoIssue()
        {
            var code = @"
func test(x):
    print(x)
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL202").Should().BeEmpty();
        }

        [TestMethod]
        public void UnusedParameter_NotUsed_ReportsIssue()
        {
            var code = @"
func test(unused_param):
    print(""hello"")
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL202" && i.Message.Contains("unused_param"));
        }

        [TestMethod]
        public void UnusedParameter_PrefixedWithUnderscore_NoIssue()
        {
            var code = @"
func test(_unused):
    print(""hello"")
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL202" && i.Message.Contains("_unused")).Should().BeEmpty();
        }

        [TestMethod]
        public void UnusedParameter_VirtualMethod_NoIssue()
        {
            var code = @"
func _process(delta):
    pass
";

            var result = _linter.LintCode(code);

            // Virtual methods should not report unused parameters
            result.Issues.Where(i => i.RuleId == "GDL202").Should().BeEmpty();
        }

        [TestMethod]
        public void UnusedParameter_MultipleParams_ReportsOnlyUnused()
        {
            var code = @"
func test(used, unused):
    print(used)
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL202" && i.Message.Contains("unused"));
            result.Issues.Where(i => i.RuleId == "GDL202" && i.Message.Contains("'used'")).Should().BeEmpty();
        }

        [TestMethod]
        public void UnusedParameter_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { WarnUnusedParameters = false };
            var linter = new GDLinter(options);
            var code = @"
func test(unused):
    pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL202").Should().BeEmpty();
        }

        #endregion

        #region EmptyFunctionRule (GDL203)

        [TestMethod]
        public void EmptyFunction_WithCode_NoIssue()
        {
            var code = @"
func test():
    print(""hello"")
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL203").Should().BeEmpty();
        }

        [TestMethod]
        public void EmptyFunction_OnlyPass_ReportsIssue()
        {
            var code = @"
func empty_func():
    pass
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL203" && i.Message.Contains("empty_func"));
        }

        [TestMethod]
        public void EmptyFunction_VirtualMethod_NoIssue()
        {
            var code = @"
func _ready():
    pass

func _process(delta):
    pass
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL203").Should().BeEmpty();
        }

        [TestMethod]
        public void EmptyFunction_SingleLineExpression_NoIssue()
        {
            var code = @"func get_value(): return 42";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL203").Should().BeEmpty();
        }

        [TestMethod]
        public void EmptyFunction_PassAndCode_NoIssue()
        {
            var code = @"
func test():
    pass
    print(""hello"")
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL203").Should().BeEmpty();
        }

        [TestMethod]
        public void EmptyFunction_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { WarnEmptyFunctions = false };
            var linter = new GDLinter(options);
            var code = @"
func empty_func():
    pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL203").Should().BeEmpty();
        }

        #endregion

        #region TypeHintRule (GDL204)

        [TestMethod]
        public void TypeHint_WithTypeHint_NoIssue()
        {
            var options = new GDLinterOptions { SuggestTypeHints = true };
            var linter = new GDLinter(options);
            var code = @"var my_var: int = 10";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL204" && i.Message.Contains("my_var")).Should().BeEmpty();
        }

        [TestMethod]
        public void TypeHint_ClassVariable_WithoutTypeHint_ReportsIssue()
        {
            var options = new GDLinterOptions { SuggestTypeHints = true };
            options.EnableRule("GDL204"); // TypeHintRule is disabled by default
            var linter = new GDLinter(options);
            var code = @"var my_var = 10";

            var result = linter.LintCode(code);

            // Class-level variable should report missing type hint
            result.Issues.Should().Contain(i => i.RuleId == "GDL204" && i.Message.Contains("my_var"));
        }

        [TestMethod]
        public void TypeHint_DisabledByDefault_NoIssue()
        {
            var code = @"var my_var = 10";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL204").Should().BeEmpty();
        }

        [TestMethod]
        public void TypeHint_InferredType_NoIssue()
        {
            var options = new GDLinterOptions { SuggestTypeHints = true };
            var linter = new GDLinter(options);
            var code = @"var my_var := 10";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL204" && i.Message.Contains("my_var")).Should().BeEmpty();
        }

        [TestMethod]
        public void TypeHint_Constant_NoIssue()
        {
            var options = new GDLinterOptions { SuggestTypeHints = true };
            var linter = new GDLinter(options);
            var code = @"const MY_CONST = 10";

            var result = linter.LintCode(code);

            // Constants typically don't need type hints
            result.Issues.Where(i => i.RuleId == "GDL204" && i.Message.Contains("MY_CONST")).Should().BeEmpty();
        }

        [TestMethod]
        public void TypeHint_ParameterWithType_NoIssue()
        {
            var options = new GDLinterOptions { SuggestTypeHints = true };
            var linter = new GDLinter(options);
            var code = @"
func test(x: int):
    print(x)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL204" && i.Message.Contains("'x'")).Should().BeEmpty();
        }

        [TestMethod]
        public void TypeHint_ParameterWithoutType_ReportsIssue()
        {
            var options = new GDLinterOptions { SuggestTypeHints = true };
            options.EnableRule("GDL204"); // TypeHintRule is disabled by default
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    print(x)
";

            var result = linter.LintCode(code);

            // Parameter 'x' should report missing type hint
            result.Issues.Should().Contain(i => i.RuleId == "GDL204" && i.Message.Contains("x"));
        }

        [TestMethod]
        public void TypeHint_ReturnType_NoIssue()
        {
            var options = new GDLinterOptions { SuggestTypeHints = true };
            var linter = new GDLinter(options);
            var code = @"
func test() -> int:
    return 42
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL204" && i.Message.Contains("return type")).Should().BeEmpty();
        }

        [TestMethod]
        public void TypeHint_NoReturnType_ReportsIssue()
        {
            var options = new GDLinterOptions { SuggestTypeHints = true };
            options.EnableRule("GDL204"); // TypeHintRule is disabled by default
            var linter = new GDLinter(options);
            var code = @"
func my_func():
    return 42
";

            var result = linter.LintCode(code);

            // Function should report missing return type hint
            result.Issues.Should().Contain(i => i.RuleId == "GDL204" && i.Message.Contains("my_func"));
        }

        #endregion

        #region Combined Tests

        [TestMethod]
        public void BestPractices_MultipleIssues_ReportsAll()
        {
            var code = @"
func test(unused_param):
    var unused_var = 10

func empty():
    pass
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL201"); // unused variable
            result.Issues.Should().Contain(i => i.RuleId == "GDL202"); // unused parameter
            result.Issues.Should().Contain(i => i.RuleId == "GDL203"); // empty function
        }

        [TestMethod]
        public void BestPractices_CleanCode_NoIssues()
        {
            var code = @"
func calculate(value):
    var result = value * 2
    return result

func process_data(data):
    for item in data:
        print(item)
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.Category == GDLintCategory.BestPractices).Should().BeEmpty();
        }

        #endregion
    }
}
