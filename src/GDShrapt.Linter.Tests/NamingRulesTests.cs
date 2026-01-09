using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Linting
{
    /// <summary>
    /// Tests for naming convention rules.
    /// </summary>
    [TestClass]
    public class NamingRulesTests
    {
        private GDLinter _linter;

        [TestInitialize]
        public void Setup()
        {
            _linter = new GDLinter();
        }

        #region ClassNameCaseRule (GDL001)

        [TestMethod]
        public void ClassNameCase_PascalCase_NoIssue()
        {
            var code = @"class_name MyClass";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL001").Should().BeEmpty();
        }

        [TestMethod]
        public void ClassNameCase_SnakeCase_ReportsIssue()
        {
            var code = @"class_name my_class";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL001");
        }

        [TestMethod]
        public void ClassNameCase_CamelCase_ReportsIssue()
        {
            var code = @"class_name myClass";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL001");
        }

        [TestMethod]
        public void ClassNameCase_CustomCase_NoIssue()
        {
            var options = new GDLinterOptions { ClassNameCase = NamingCase.Any };
            var linter = new GDLinter(options);
            var code = @"class_name any_name_works";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL001").Should().BeEmpty();
        }

        #endregion

        #region FunctionNameCaseRule (GDL002)

        [TestMethod]
        public void FunctionNameCase_SnakeCase_NoIssue()
        {
            var code = @"
func my_function():
    pass
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL002").Should().BeEmpty();
        }

        [TestMethod]
        public void FunctionNameCase_PascalCase_ReportsIssue()
        {
            var code = @"
func MyFunction():
    pass
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL002");
        }

        [TestMethod]
        public void FunctionNameCase_CamelCase_ReportsIssue()
        {
            var code = @"
func myFunction():
    pass
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL002");
        }

        [TestMethod]
        public void FunctionNameCase_VirtualMethod_NoIssue()
        {
            var code = @"
func _ready():
    pass

func _process(delta):
    pass
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL002").Should().BeEmpty();
        }

        #endregion

        #region VariableNameCaseRule (GDL003)

        [TestMethod]
        public void VariableNameCase_SnakeCase_NoIssue()
        {
            var code = @"var my_variable = 10";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void VariableNameCase_PascalCase_ReportsIssue()
        {
            var code = @"var MyVariable = 10";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL003");
        }

        [TestMethod]
        public void VariableNameCase_CamelCase_ReportsIssue()
        {
            var code = @"var myVariable = 10";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL003");
        }

        [TestMethod]
        public void VariableNameCase_LocalVariable_SnakeCase_NoIssue()
        {
            var code = @"
func test():
    var local_var = 10
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void VariableNameCase_LocalVariable_PascalCase_ReportsIssue()
        {
            var code = @"
func test():
    var LocalVar = 10
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL003");
        }

        #endregion

        #region ConstantNameCaseRule (GDL004)

        [TestMethod]
        public void ConstantNameCase_ScreamingSnakeCase_NoIssue()
        {
            var code = @"const MY_CONSTANT = 10";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL004").Should().BeEmpty();
        }

        [TestMethod]
        public void ConstantNameCase_SnakeCase_ReportsIssue()
        {
            var code = @"const my_constant = 10";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL004");
        }

        [TestMethod]
        public void ConstantNameCase_PascalCase_ReportsIssue()
        {
            var code = @"const MyConstant = 10";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL004");
        }

        #endregion

        #region SignalNameCaseRule (GDL005)

        [TestMethod]
        public void SignalNameCase_SnakeCase_NoIssue()
        {
            var code = @"signal my_signal";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL005").Should().BeEmpty();
        }

        [TestMethod]
        public void SignalNameCase_PascalCase_ReportsIssue()
        {
            var code = @"signal MySignal";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL005");
        }

        [TestMethod]
        public void SignalNameCase_CamelCase_ReportsIssue()
        {
            var code = @"signal mySignal";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL005");
        }

        [TestMethod]
        public void SignalNameCase_WithParameters_NoIssue()
        {
            var code = @"signal my_signal(value: int, name: String)";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL005").Should().BeEmpty();
        }

        #endregion

        #region EnumNameCaseRule (GDL006)

        [TestMethod]
        public void EnumNameCase_PascalCase_NoIssue()
        {
            var code = @"enum MyEnum { VALUE }";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL006").Should().BeEmpty();
        }

        [TestMethod]
        public void EnumNameCase_SnakeCase_ReportsIssue()
        {
            var code = @"enum my_enum { VALUE }";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL006");
        }

        [TestMethod]
        public void EnumNameCase_Anonymous_NoIssue()
        {
            var code = @"enum { VALUE_ONE, VALUE_TWO }";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL006").Should().BeEmpty();
        }

        #endregion

        #region EnumValueCaseRule (GDL007)

        [TestMethod]
        public void EnumValueCase_ScreamingSnakeCase_NoIssue()
        {
            var code = @"enum State { IDLE, RUNNING, GAME_OVER }";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL007").Should().BeEmpty();
        }

        [TestMethod]
        public void EnumValueCase_SnakeCase_ReportsIssue()
        {
            var code = @"enum State { idle, running }";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL007");
        }

        [TestMethod]
        public void EnumValueCase_PascalCase_ReportsIssue()
        {
            var code = @"enum State { Idle, Running }";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL007");
        }

        #endregion

        #region PrivatePrefixRule (GDL008)

        [TestMethod]
        public void PrivatePrefix_MethodWithUnderscore_NoHint()
        {
            var code = @"
func _private_method():
    pass
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL008" && i.Message.Contains("_private_method")).Should().BeEmpty();
        }

        [TestMethod]
        public void PrivatePrefix_VirtualMethods_NoHint()
        {
            var code = @"
func _ready():
    pass

func _process(delta):
    pass
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL008").Should().BeEmpty();
        }

        [TestMethod]
        public void PrivatePrefix_VariableWithoutUnderscore_ReportsHint()
        {
            // PrivatePrefixRule is disabled by default, so we need to enable it
            _linter.Options.EnableRule("GDL008");
            var code = @"var private_var = 10";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL008" && i.Severity == GDLintSeverity.Hint);
        }

        [TestMethod]
        public void PrivatePrefix_VariableWithUnderscore_NoHint()
        {
            var code = @"var _private_var = 10";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL008" && i.Message.Contains("_private_var")).Should().BeEmpty();
        }

        [TestMethod]
        public void PrivatePrefix_Disabled_NoHint()
        {
            var options = new GDLinterOptions { RequireUnderscoreForPrivate = false };
            var linter = new GDLinter(options);
            var code = @"var some_var = 10";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL008").Should().BeEmpty();
        }

        #endregion

        #region Naming Helper Tests

        [TestMethod]
        public void NamingHelper_MatchesCase_SnakeCase()
        {
            NamingHelper.MatchesCase("my_variable_name", NamingCase.SnakeCase).Should().BeTrue();
            NamingHelper.MatchesCase("MyVariable", NamingCase.SnakeCase).Should().BeFalse();
            NamingHelper.MatchesCase("myVariable", NamingCase.SnakeCase).Should().BeFalse();
        }

        [TestMethod]
        public void NamingHelper_MatchesCase_PascalCase()
        {
            NamingHelper.MatchesCase("MyClassName", NamingCase.PascalCase).Should().BeTrue();
            NamingHelper.MatchesCase("my_class", NamingCase.PascalCase).Should().BeFalse();
            NamingHelper.MatchesCase("myClass", NamingCase.PascalCase).Should().BeFalse();
        }

        [TestMethod]
        public void NamingHelper_MatchesCase_CamelCase()
        {
            NamingHelper.MatchesCase("myVariable", NamingCase.CamelCase).Should().BeTrue();
            NamingHelper.MatchesCase("MyVariable", NamingCase.CamelCase).Should().BeFalse();
            NamingHelper.MatchesCase("my_variable", NamingCase.CamelCase).Should().BeFalse();
        }

        [TestMethod]
        public void NamingHelper_MatchesCase_ScreamingSnakeCase()
        {
            NamingHelper.MatchesCase("MY_CONSTANT", NamingCase.ScreamingSnakeCase).Should().BeTrue();
            NamingHelper.MatchesCase("my_constant", NamingCase.ScreamingSnakeCase).Should().BeFalse();
            NamingHelper.MatchesCase("MyConstant", NamingCase.ScreamingSnakeCase).Should().BeFalse();
        }

        [TestMethod]
        public void NamingHelper_SuggestCorrectName_ToSnakeCase()
        {
            NamingHelper.SuggestCorrectName("MyVariable", NamingCase.SnakeCase).Should().Be("my_variable");
            NamingHelper.SuggestCorrectName("myVariable", NamingCase.SnakeCase).Should().Be("my_variable");
        }

        [TestMethod]
        public void NamingHelper_SuggestCorrectName_ToPascalCase()
        {
            NamingHelper.SuggestCorrectName("my_class", NamingCase.PascalCase).Should().Be("MyClass");
        }

        [TestMethod]
        public void NamingHelper_SuggestCorrectName_ToScreamingSnakeCase()
        {
            NamingHelper.SuggestCorrectName("myConstant", NamingCase.ScreamingSnakeCase).Should().Be("MY_CONSTANT");
        }

        #endregion

        #region InnerClassNameCaseRule (GDL009)

        [TestMethod]
        public void InnerClassName_PascalCase_NoIssue()
        {
            var code = @"
class MyInnerClass:
    var value = 0
";
            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL009").Should().BeEmpty();
        }

        [TestMethod]
        public void InnerClassName_SnakeCase_ReportsIssue()
        {
            var code = @"
class my_inner_class:
    pass
";
            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL009");
        }

        [TestMethod]
        public void InnerClassName_CamelCase_ReportsIssue()
        {
            var code = @"
class myInnerClass:
    pass
";
            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL009");
        }

        [TestMethod]
        public void InnerClassName_WithExtends_PascalCase_NoIssue()
        {
            var code = @"
class MyInnerClass extends Node:
    pass
";
            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL009").Should().BeEmpty();
        }

        [TestMethod]
        public void InnerClassName_PrivateWithUnderscore_NoIssue()
        {
            var code = @"
class _PrivateInner:
    pass
";
            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL009").Should().BeEmpty();
        }

        [TestMethod]
        public void InnerClassName_CustomCase_NoIssue()
        {
            var options = new GDLinterOptions { InnerClassNameCase = NamingCase.Any };
            var linter = new GDLinter(options);
            var code = @"
class any_name_works:
    pass
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL009").Should().BeEmpty();
        }

        #endregion
    }
}
