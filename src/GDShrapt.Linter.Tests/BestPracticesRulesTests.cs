using FluentAssertions;
using GDShrapt.Linter;
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

        #region CyclomaticComplexityRule (GDL208)

        [TestMethod]
        public void CyclomaticComplexity_SimpleFunction_NoIssue()
        {
            var options = new GDLinterOptions { MaxCyclomaticComplexity = 10 };
            options.EnableRule("GDL208");
            var linter = new GDLinter(options);
            var code = @"
func simple():
    var x = 10
    return x
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL208").Should().BeEmpty();
        }

        [TestMethod]
        public void CyclomaticComplexity_ComplexFunction_ReportsIssue()
        {
            var options = new GDLinterOptions { MaxCyclomaticComplexity = 3 };
            options.EnableRule("GDL208");
            var linter = new GDLinter(options);
            var code = @"
func complex(a, b, c):
    if a > 0:
        if b > 0:
            if c > 0:
                return 1
            else:
                return 2
        else:
            return 3
    else:
        return 4
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL208" && i.Message.Contains("complex"));
        }

        [TestMethod]
        public void CyclomaticComplexity_CountsAndOr_ReportsIssue()
        {
            var options = new GDLinterOptions { MaxCyclomaticComplexity = 2 };
            options.EnableRule("GDL208");
            var linter = new GDLinter(options);
            var code = @"
func check(a, b, c):
    if a > 0 and b > 0 or c > 0:
        return true
    return false
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL208");
        }

        [TestMethod]
        public void CyclomaticComplexity_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { MaxCyclomaticComplexity = 0 };
            var linter = new GDLinter(options);
            var code = @"
func complex(a, b, c, d, e):
    if a: pass
    if b: pass
    if c: pass
    if d: pass
    if e: pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL208").Should().BeEmpty();
        }

        #endregion

        #region MagicNumberRule (GDL209)

        [TestMethod]
        public void MagicNumber_AllowedNumber_NoIssue()
        {
            var options = new GDLinterOptions { WarnMagicNumbers = true };
            options.EnableRule("GDL209");
            var linter = new GDLinter(options);
            var code = @"
func test():
    var x = 0
    var y = 1
    var z = -1
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL209").Should().BeEmpty();
        }

        [TestMethod]
        public void MagicNumber_MagicNumber_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnMagicNumbers = true };
            options.EnableRule("GDL209");
            var linter = new GDLinter(options);
            var code = @"
func test():
    var timeout = 3600
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL209" && i.Message.Contains("3600"));
        }

        [TestMethod]
        public void MagicNumber_InConstant_NoIssue()
        {
            var options = new GDLinterOptions { WarnMagicNumbers = true };
            options.EnableRule("GDL209");
            var linter = new GDLinter(options);
            var code = @"const TIMEOUT = 3600";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL209").Should().BeEmpty();
        }

        [TestMethod]
        public void MagicNumber_ArrayIndex_NoIssue()
        {
            var options = new GDLinterOptions { WarnMagicNumbers = true };
            options.EnableRule("GDL209");
            var linter = new GDLinter(options);
            var code = @"
func test():
    var arr = [1, 2, 3]
    return arr[5]
";

            var result = linter.LintCode(code);

            // Array index with magic number should be allowed
            result.Issues.Where(i => i.RuleId == "GDL209" && i.Message.Contains("5")).Should().BeEmpty();
        }

        [TestMethod]
        public void MagicNumber_InEnum_NoIssue()
        {
            var options = new GDLinterOptions { WarnMagicNumbers = true };
            options.EnableRule("GDL209");
            var linter = new GDLinter(options);
            var code = @"
enum Values {
    FIRST = 100,
    SECOND = 200
}
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL209").Should().BeEmpty();
        }

        [TestMethod]
        public void MagicNumber_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { WarnMagicNumbers = false };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var x = 12345
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL209").Should().BeEmpty();
        }

        #endregion

        #region DeadCodeRule (GDL210)

        [TestMethod]
        public void DeadCode_NoDeadCode_NoIssue()
        {
            var code = @"
func test():
    var x = 10
    return x
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL210").Should().BeEmpty();
        }

        [TestMethod]
        public void DeadCode_AfterReturn_ReportsIssue()
        {
            var code = @"
func test():
    return 10
    var x = 20
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL210");
        }

        [TestMethod]
        public void DeadCode_AfterBreak_ReportsIssue()
        {
            var code = @"
func test():
    for i in range(10):
        break
        print(i)
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL210");
        }

        [TestMethod]
        public void DeadCode_AfterContinue_ReportsIssue()
        {
            var code = @"
func test():
    for i in range(10):
        continue
        print(i)
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL210");
        }

        [TestMethod]
        public void DeadCode_ReturnInIfBranch_NoIssue()
        {
            var code = @"
func test(x):
    if x > 0:
        return x
    return 0
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL210").Should().BeEmpty();
        }

        #endregion

        #region VariableShadowingRule (GDL211)

        [TestMethod]
        public void VariableShadowing_NoShadowing_NoIssue()
        {
            var options = new GDLinterOptions { WarnVariableShadowing = true };
            var linter = new GDLinter(options);
            var code = @"
var class_var = 10

func test():
    var local_var = 20
    print(local_var)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL211").Should().BeEmpty();
        }

        [TestMethod]
        public void VariableShadowing_LocalShadows_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnVariableShadowing = true };
            var linter = new GDLinter(options);
            var code = @"
var my_var = 10

func test():
    var my_var = 20
    print(my_var)
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL211" && i.Message.Contains("my_var"));
        }

        [TestMethod]
        public void VariableShadowing_ForLoopShadows_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnVariableShadowing = true };
            var linter = new GDLinter(options);
            var code = @"
var i = 10

func test():
    for i in range(10):
        print(i)
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL211" && i.Message.Contains("'i'"));
        }

        [TestMethod]
        public void VariableShadowing_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { WarnVariableShadowing = false };
            var linter = new GDLinter(options);
            var code = @"
var my_var = 10

func test():
    var my_var = 20
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL211").Should().BeEmpty();
        }

        #endregion

        #region AwaitInLoopRule (GDL212)

        [TestMethod]
        public void AwaitInLoop_OutsideLoop_NoIssue()
        {
            var options = new GDLinterOptions { WarnAwaitInLoop = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    await some_signal
    print(""done"")
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL212").Should().BeEmpty();
        }

        [TestMethod]
        public void AwaitInLoop_InsideForLoop_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnAwaitInLoop = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    for i in range(10):
        await some_signal
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL212");
        }

        [TestMethod]
        public void AwaitInLoop_InsideWhileLoop_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnAwaitInLoop = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    while true:
        await some_signal
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL212");
        }

        [TestMethod]
        public void AwaitInLoop_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { WarnAwaitInLoop = false };
            var linter = new GDLinter(options);
            var code = @"
func test():
    for i in range(10):
        await some_signal
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL212").Should().BeEmpty();
        }

        #endregion

        #region SelfComparisonRule (GDL213)

        [TestMethod]
        public void SelfComparison_DifferentOperands_NoIssue()
        {
            var code = @"
func test():
    var a = 10
    var b = 20
    if a == b:
        pass
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL213").Should().BeEmpty();
        }

        [TestMethod]
        public void SelfComparison_SameVariable_ReportsIssue()
        {
            var code = @"
func test():
    var x = 10
    if x == x:
        pass
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL213" && i.Message.Contains("x"));
        }

        [TestMethod]
        public void SelfComparison_NotEqual_ReportsIssue()
        {
            var code = @"
func test():
    var x = 10
    if x != x:
        pass
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL213");
        }

        [TestMethod]
        public void SelfComparison_MemberAccess_ReportsIssue()
        {
            var code = @"
func test():
    if self.value == self.value:
        pass
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL213");
        }

        #endregion

        #region DuplicateDictKeyRule (GDL214)

        [TestMethod]
        public void DuplicateDictKey_UniqueKeys_NoIssue()
        {
            var code = @"
func test():
    var dict = {
        ""a"": 1,
        ""b"": 2,
        ""c"": 3
    }
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL214").Should().BeEmpty();
        }

        [TestMethod]
        public void DuplicateDictKey_DuplicateStringKey_ReportsIssue()
        {
            var code = @"
func test():
    var dict = {
        ""key"": 1,
        ""key"": 2
    }
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL214" && i.Message.Contains("key"));
        }

        [TestMethod]
        public void DuplicateDictKey_DuplicateNumericKey_ReportsIssue()
        {
            var code = @"
func test():
    var dict = {
        1: ""a"",
        1: ""b""
    }
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL214");
        }

        [TestMethod]
        public void DuplicateDictKey_InlineDict_ReportsIssue()
        {
            var code = @"
func test():
    var dict = {""a"": 1, ""a"": 2}
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL214");
        }

        #endregion

        #region StrictTypingRule (GDL215)

        [TestMethod]
        public void StrictTyping_DisabledByDefault_NoIssue()
        {
            var code = @"
var my_var = 10

func test(x):
    var local = 20
    return x + local
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL215").Should().BeEmpty();
        }

        [TestMethod]
        public void StrictTyping_ClassVariable_WithType_NoIssue()
        {
            var options = new GDLinterOptions
            {
                StrictTypingClassVariables = GDLintSeverity.Error
            };
            var linter = new GDLinter(options);
            var code = @"var my_var: int = 10";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL215").Should().BeEmpty();
        }

        [TestMethod]
        public void StrictTyping_ClassVariable_WithoutType_ReportsIssue()
        {
            var options = new GDLinterOptions
            {
                StrictTypingClassVariables = GDLintSeverity.Error
            };
            var linter = new GDLinter(options);
            var code = @"var my_var = 10";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i =>
                i.RuleId == "GDL215" &&
                i.Severity == GDLintSeverity.Error &&
                i.Message.Contains("my_var"));
        }

        [TestMethod]
        public void StrictTyping_Constant_NoIssue()
        {
            var options = new GDLinterOptions
            {
                StrictTypingClassVariables = GDLintSeverity.Error
            };
            var linter = new GDLinter(options);
            var code = @"const MY_CONST = 10";

            var result = linter.LintCode(code);

            // Constants are skipped
            result.Issues.Where(i => i.RuleId == "GDL215").Should().BeEmpty();
        }

        [TestMethod]
        public void StrictTyping_LocalVariable_WithType_NoIssue()
        {
            var options = new GDLinterOptions
            {
                StrictTypingLocalVariables = GDLintSeverity.Warning
            };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var x: int = 10
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL215").Should().BeEmpty();
        }

        [TestMethod]
        public void StrictTyping_LocalVariable_WithoutType_ReportsIssue()
        {
            var options = new GDLinterOptions
            {
                StrictTypingLocalVariables = GDLintSeverity.Warning
            };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var x = 10
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i =>
                i.RuleId == "GDL215" &&
                i.Severity == GDLintSeverity.Warning &&
                i.Message.Contains("x"));
        }

        [TestMethod]
        public void StrictTyping_Parameter_WithType_NoIssue()
        {
            var options = new GDLinterOptions
            {
                StrictTypingParameters = GDLintSeverity.Error
            };
            var linter = new GDLinter(options);
            var code = @"
func test(x: int):
    print(x)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL215").Should().BeEmpty();
        }

        [TestMethod]
        public void StrictTyping_Parameter_WithoutType_ReportsIssue()
        {
            var options = new GDLinterOptions
            {
                StrictTypingParameters = GDLintSeverity.Error
            };
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    print(x)
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i =>
                i.RuleId == "GDL215" &&
                i.Severity == GDLintSeverity.Error &&
                i.Message.Contains("x"));
        }

        [TestMethod]
        public void StrictTyping_ReturnType_WithType_NoIssue()
        {
            var options = new GDLinterOptions
            {
                StrictTypingReturnTypes = GDLintSeverity.Error
            };
            var linter = new GDLinter(options);
            var code = @"
func test() -> int:
    return 42
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL215").Should().BeEmpty();
        }

        [TestMethod]
        public void StrictTyping_ReturnType_WithoutType_ReportsIssue()
        {
            var options = new GDLinterOptions
            {
                StrictTypingReturnTypes = GDLintSeverity.Error
            };
            var linter = new GDLinter(options);
            var code = @"
func my_func():
    return 42
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i =>
                i.RuleId == "GDL215" &&
                i.Severity == GDLintSeverity.Error &&
                i.Message.Contains("my_func"));
        }

        [TestMethod]
        public void StrictTyping_VirtualMethod_NoIssue()
        {
            var options = new GDLinterOptions
            {
                StrictTypingReturnTypes = GDLintSeverity.Error
            };
            var linter = new GDLinter(options);
            var code = @"
func _ready():
    pass

func _process(delta):
    pass
";

            var result = linter.LintCode(code);

            // Virtual methods should not report missing return types
            result.Issues.Where(i => i.RuleId == "GDL215").Should().BeEmpty();
        }

        [TestMethod]
        public void StrictTyping_MixedSeverities()
        {
            var options = new GDLinterOptions
            {
                StrictTypingParameters = GDLintSeverity.Error,
                StrictTypingReturnTypes = GDLintSeverity.Error,
                StrictTypingLocalVariables = GDLintSeverity.Hint,
                StrictTypingClassVariables = null // Disabled
            };
            var linter = new GDLinter(options);
            var code = @"
var class_var = 10

func my_func(param):
    var local = 20
    return param + local
";

            var result = linter.LintCode(code);

            // Class variable should not be reported (disabled)
            result.Issues.Where(i => i.RuleId == "GDL215" && i.Message.Contains("class_var")).Should().BeEmpty();

            // Parameter should be Error
            result.Issues.Should().Contain(i =>
                i.RuleId == "GDL215" &&
                i.Severity == GDLintSeverity.Error &&
                i.Message.Contains("param"));

            // Return type should be Error
            result.Issues.Should().Contain(i =>
                i.RuleId == "GDL215" &&
                i.Severity == GDLintSeverity.Error &&
                i.Message.Contains("my_func"));

            // Local variable should be Hint
            result.Issues.Should().Contain(i =>
                i.RuleId == "GDL215" &&
                i.Severity == GDLintSeverity.Hint &&
                i.Message.Contains("local"));
        }

        [TestMethod]
        public void StrictTyping_EnableStrictTypingWarnings()
        {
            var options = new GDLinterOptions();
            options.EnableStrictTypingWarnings();
            var linter = new GDLinter(options);
            var code = @"
var my_var = 10

func test(x):
    var local = 20
    return x
";

            var result = linter.LintCode(code);

            // All should be warnings
            var issues = result.Issues.Where(i => i.RuleId == "GDL215").ToList();
            issues.Should().NotBeEmpty();
            issues.Should().OnlyContain(i => i.Severity == GDLintSeverity.Warning);
        }

        [TestMethod]
        public void StrictTyping_EnableStrictTypingForMethods()
        {
            var options = new GDLinterOptions();
            options.EnableStrictTypingForMethods();
            var linter = new GDLinter(options);
            var code = @"
var my_var = 10

func test(x):
    var local = 20
    return x
";

            var result = linter.LintCode(code);

            // Only parameter and return type should be reported
            result.Issues.Where(i => i.RuleId == "GDL215" && i.Message.Contains("my_var")).Should().BeEmpty();
            result.Issues.Where(i => i.RuleId == "GDL215" && i.Message.Contains("local")).Should().BeEmpty();
            result.Issues.Should().Contain(i => i.RuleId == "GDL215" && i.Severity == GDLintSeverity.Error && i.Message.Contains("x"));
            result.Issues.Should().Contain(i => i.RuleId == "GDL215" && i.Severity == GDLintSeverity.Error && i.Message.Contains("test"));
        }

        [TestMethod]
        public void StrictTyping_InferredType_NoIssue()
        {
            var options = new GDLinterOptions
            {
                StrictTypingClassVariables = GDLintSeverity.Error
            };
            var linter = new GDLinter(options);
            var code = @"var my_var := 10";

            var result = linter.LintCode(code);

            // Inferred types have type info
            result.Issues.Where(i => i.RuleId == "GDL215").Should().BeEmpty();
        }

        #endregion

#region PrivateMethodCallRule (GDL218)

        [TestMethod]
        public void PrivateMethodCall_ExternalCall_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnPrivateMethodCall = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var node = get_node(""."")
    node._private_method()
";
            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL218");
        }

        [TestMethod]
        public void PrivateMethodCall_InternalCall_NoIssue()
        {
            var options = new GDLinterOptions { WarnPrivateMethodCall = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    _my_private_method()

func _my_private_method():
    pass
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL218").Should().BeEmpty();
        }

        [TestMethod]
        public void PrivateMethodCall_SelfCall_NoIssue()
        {
            var options = new GDLinterOptions { WarnPrivateMethodCall = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    self._my_private_method()
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL218").Should().BeEmpty();
        }

        [TestMethod]
        public void PrivateMethodCall_DisabledByDefault_NoIssue()
        {
            var code = @"
func test():
    var node = get_node(""."")
    node._private_method()
";
            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL218").Should().BeEmpty();
        }

        [TestMethod]
        public void PrivateMethodCall_PublicMethod_NoIssue()
        {
            var options = new GDLinterOptions { WarnPrivateMethodCall = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var node = get_node(""."")
    node.public_method()
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL218").Should().BeEmpty();
        }

        [TestMethod]
        public void PrivateMethodCall_ChainedCall_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnPrivateMethodCall = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    get_node(""."").get_child(0)._private_method()
";
            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL218");
        }

        #endregion

        #region DuplicatedLoadRule (GDL219)

        [TestMethod]
        public void DuplicatedLoad_SamePathTwice_ReportsIssue()
        {
            var code = @"
var Scene1 = load(""res://scene.tscn"")
var Scene2 = load(""res://scene.tscn"")
";
            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL219");
        }

        [TestMethod]
        public void DuplicatedLoad_DifferentPaths_NoIssue()
        {
            var code = @"
var Scene1 = load(""res://scene1.tscn"")
var Scene2 = load(""res://scene2.tscn"")
";
            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL219").Should().BeEmpty();
        }

        [TestMethod]
        public void DuplicatedLoad_PreloadSamePath_ReportsIssue()
        {
            var code = @"
const Scene1 = preload(""res://scene.tscn"")
const Scene2 = preload(""res://scene.tscn"")
";
            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL219");
        }

        [TestMethod]
        public void DuplicatedLoad_LoadAndPreloadSamePath_ReportsIssue()
        {
            var code = @"
const Scene1 = preload(""res://scene.tscn"")
var Scene2 = load(""res://scene.tscn"")
";
            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL219");
        }

        [TestMethod]
        public void DuplicatedLoad_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { WarnDuplicatedLoad = false };
            var linter = new GDLinter(options);
            var code = @"
var Scene1 = load(""res://scene.tscn"")
var Scene2 = load(""res://scene.tscn"")
";
            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL219").Should().BeEmpty();
        }

        [TestMethod]
        public void DuplicatedLoad_SingleLoad_NoIssue()
        {
            var code = @"
var Scene = load(""res://scene.tscn"")
";
            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL219").Should().BeEmpty();
        }

        [TestMethod]
        public void DuplicatedLoad_MultipleLoads_ReportsCorrectPath()
        {
            var code = @"
var Scene1 = load(""res://first.tscn"")
var Scene2 = load(""res://duplicate.tscn"")
var Scene3 = load(""res://duplicate.tscn"")
";
            var result = _linter.LintCode(code);

            var duplicateIssue = result.Issues.FirstOrDefault(i => i.RuleId == "GDL219");
            duplicateIssue.Should().NotBeNull();
            duplicateIssue.Message.Should().Contain("duplicate.tscn");
        }

        [TestMethod]
        public void DuplicatedLoad_InsideFunction_ReportsIssue()
        {
            var code = @"
func test():
    var a = load(""res://resource.tres"")
    var b = load(""res://resource.tres"")
";
            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL219");
        }

        #endregion

        #region NoSelfAssignRule (GDL230)

        [TestMethod]
        public void NoSelfAssign_DifferentValues_NoIssue()
        {
            var code = @"
func test():
    var x = 10
    x = 20
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL230").Should().BeEmpty();
        }

        [TestMethod]
        public void NoSelfAssign_SameVariable_ReportsIssue()
        {
            var code = @"
func test():
    var x = 10
    x = x
";

            var result = _linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL230" && i.Message.Contains("x"));
        }

        [TestMethod]
        public void NoSelfAssign_CompoundAssignment_NoIssue()
        {
            var code = @"
func test():
    var x = 10
    x += x
";

            var result = _linter.LintCode(code);

            // Compound assignment (+=) with self is typically intentional
            result.Issues.Where(i => i.RuleId == "GDL230").Should().BeEmpty();
        }

        #endregion

        #region ExpressionNotAssignedRule (GDL224)

        [TestMethod]
        public void ExpressionNotAssigned_Assignment_NoIssue()
        {
            var options = new GDLinterOptions { WarnExpressionNotAssigned = true };
            options.EnableRule("GDL224");
            var linter = new GDLinter(options);
            var code = @"
func test():
    var x = 10
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL224").Should().BeEmpty();
        }

        [TestMethod]
        public void ExpressionNotAssigned_FunctionCall_NoIssue()
        {
            var options = new GDLinterOptions { WarnExpressionNotAssigned = true };
            options.EnableRule("GDL224");
            var linter = new GDLinter(options);
            var code = @"
func test():
    print(""hello"")
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL224").Should().BeEmpty();
        }

        [TestMethod]
        public void ExpressionNotAssigned_UnusedLiteral_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnExpressionNotAssigned = true };
            options.EnableRule("GDL224");
            var linter = new GDLinter(options);
            var code = @"
func test():
    42
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL224");
        }

        [TestMethod]
        public void ExpressionNotAssigned_Disabled_NoIssue()
        {
            var code = @"
func test():
    42
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL224").Should().BeEmpty();
        }

        #endregion

        #region ConsistentReturnRule (GDL234)

        [TestMethod]
        public void ConsistentReturn_AllReturnValues_NoIssue()
        {
            var options = new GDLinterOptions { WarnInconsistentReturn = true };
            options.EnableRule("GDL234");
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        return 1
    return 0
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL234").Should().BeEmpty();
        }

        [TestMethod]
        public void ConsistentReturn_NoReturns_NoIssue()
        {
            var options = new GDLinterOptions { WarnInconsistentReturn = true };
            options.EnableRule("GDL234");
            var linter = new GDLinter(options);
            var code = @"
func test():
    print(""hello"")
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL234").Should().BeEmpty();
        }

        [TestMethod]
        public void ConsistentReturn_MixedReturns_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnInconsistentReturn = true };
            options.EnableRule("GDL234");
            var linter = new GDLinter(options);
            var code = @"
func test(x):
    if x > 0:
        return 1
    return
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL234");
        }

        [TestMethod]
        public void ConsistentReturn_VoidReturnType_NoIssue()
        {
            var options = new GDLinterOptions { WarnInconsistentReturn = true };
            options.EnableRule("GDL234");
            var linter = new GDLinter(options);
            var code = @"
func test(x) -> void:
    if x > 0:
        return
    print(x)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL234").Should().BeEmpty();
        }

        [TestMethod]
        public void ConsistentReturn_Disabled_NoIssue()
        {
            var code = @"
func test(x):
    if x > 0:
        return 1
    return
";

            var result = _linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL234").Should().BeEmpty();
        }

        #endregion

        // NOTE: GDL220 (AbstractMethodBodyRule) and GDL221 (AbstractClassRequiredRule) tests removed.
        // Abstract method validation is now handled exclusively by GDAbstractValidator (GD8001, GD8002)
        // to avoid duplicate diagnostics for the same issues.

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

        #region AllocationInLoopRule (GDL240)

        [TestMethod]
        public void AllocationInLoop_NewOutsideLoop_NoIssue()
        {
            var options = new GDLinterOptions { WarnAllocationInLoop = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var vec = Vector2.new(0, 0)
    for i in range(10):
        print(i)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL240").Should().BeEmpty();
        }

        [TestMethod]
        public void AllocationInLoop_NewInsideForLoop_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnAllocationInLoop = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    for i in range(10):
        var vec = Vector2.new(i, 0)
        print(vec)
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL240");
        }

        [TestMethod]
        public void AllocationInLoop_DictInsideLoop_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnAllocationInLoop = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    for i in range(10):
        var dict = {""key"": i}
        print(dict)
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL240");
        }

        [TestMethod]
        public void AllocationInLoop_ArrayInsideLoop_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnAllocationInLoop = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    for i in range(10):
        var arr = [1, 2, 3]
        print(arr)
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL240");
        }

        [TestMethod]
        public void AllocationInLoop_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { WarnAllocationInLoop = false };
            var linter = new GDLinter(options);
            var code = @"
func test():
    for i in range(10):
        var vec = Vector2.new(i, 0)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL240").Should().BeEmpty();
        }

        #endregion

        #region ProcessGetNodeRule (GDL241)

        [TestMethod]
        public void ProcessGetNode_OutsideProcess_NoIssue()
        {
            var options = new GDLinterOptions { WarnProcessGetNode = true };
            var linter = new GDLinter(options);
            var code = @"
func _ready():
    var node = get_node(""Child"")
    print(node)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL241").Should().BeEmpty();
        }

        [TestMethod]
        public void ProcessGetNode_InProcess_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnProcessGetNode = true };
            var linter = new GDLinter(options);
            var code = @"
func _process(delta):
    var node = get_node(""Child"")
    print(node)
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL241");
        }

        [TestMethod]
        public void ProcessGetNode_InPhysicsProcess_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnProcessGetNode = true };
            var linter = new GDLinter(options);
            var code = @"
func _physics_process(delta):
    var node = get_node(""Child"")
    print(node)
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL241");
        }

        [TestMethod]
        public void ProcessGetNode_DollarPath_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnProcessGetNode = true };
            var linter = new GDLinter(options);
            var code = @"
func _process(delta):
    var node = $Child
    print(node)
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL241");
        }

        [TestMethod]
        public void ProcessGetNode_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { WarnProcessGetNode = false };
            var linter = new GDLinter(options);
            var code = @"
func _process(delta):
    var node = get_node(""Child"")
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL241").Should().BeEmpty();
        }

        #endregion

        #region StringConcatLoopRule (GDL242)

        [TestMethod]
        public void StringConcatLoop_OutsideLoop_NoIssue()
        {
            var options = new GDLinterOptions { WarnStringConcatInLoop = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var result = """"
    result += ""hello""
    print(result)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL242").Should().BeEmpty();
        }

        [TestMethod]
        public void StringConcatLoop_InsideLoop_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnStringConcatInLoop = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var result = """"
    for i in range(10):
        result += ""item""
    print(result)
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL242");
        }

        [TestMethod]
        public void StringConcatLoop_IntegerConcat_NoIssue()
        {
            var options = new GDLinterOptions { WarnStringConcatInLoop = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var sum = 0
    for i in range(10):
        sum += i
    print(sum)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL242").Should().BeEmpty();
        }

        [TestMethod]
        public void StringConcatLoop_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { WarnStringConcatInLoop = false };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var result = """"
    for i in range(10):
        result += ""item""
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL242").Should().BeEmpty();
        }

        #endregion

        #region OrphanNodeRule (GDL245)

        [TestMethod]
        public void OrphanNode_AddedToTree_NoIssue()
        {
            var options = new GDLinterOptions { WarnOrphanNode = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var node = Node.new()
    add_child(node)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL245").Should().BeEmpty();
        }

        [TestMethod]
        public void OrphanNode_NotAddedOrFreed_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnOrphanNode = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var node = Node.new()
    print(node)
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL245");
        }

        [TestMethod]
        public void OrphanNode_QueueFreed_NoIssue()
        {
            var options = new GDLinterOptions { WarnOrphanNode = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var node = Node.new()
    node.queue_free()
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL245").Should().BeEmpty();
        }

        [TestMethod]
        public void OrphanNode_Returned_NoIssue()
        {
            var options = new GDLinterOptions { WarnOrphanNode = true };
            var linter = new GDLinter(options);
            var code = @"
func create_node():
    var node = Node.new()
    return node
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL245").Should().BeEmpty();
        }

        [TestMethod]
        public void OrphanNode_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { WarnOrphanNode = false };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var node = Node.new()
    print(node)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL245").Should().BeEmpty();
        }

        #endregion

        #region UninitializedVariableRule (GDL250)

        [TestMethod]
        public void UninitializedVariable_Initialized_NoIssue()
        {
            var options = new GDLinterOptions { WarnUninitializedVariable = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var x = 10
    print(x)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL250").Should().BeEmpty();
        }

        [TestMethod]
        public void UninitializedVariable_UsedBeforeAssign_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnUninitializedVariable = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var x
    print(x)
    x = 10
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL250" && i.Message.Contains("x"));
        }

        [TestMethod]
        public void UninitializedVariable_AssignedThenUsed_NoIssue()
        {
            var options = new GDLinterOptions { WarnUninitializedVariable = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var x
    x = 10
    print(x)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL250").Should().BeEmpty();
        }

        [TestMethod]
        public void UninitializedVariable_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { WarnUninitializedVariable = false };
            var linter = new GDLinter(options);
            var code = @"
func test():
    var x
    print(x)
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL250").Should().BeEmpty();
        }

        #endregion

        #region UnusedFunctionRule (GDL252)

        [TestMethod]
        public void UnusedFunction_PrivateCalled_NoIssue()
        {
            var options = new GDLinterOptions { WarnUnusedFunctions = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    _helper()

func _helper():
    print(""helping"")
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL252").Should().BeEmpty();
        }

        [TestMethod]
        public void UnusedFunction_PrivateNeverCalled_ReportsIssue()
        {
            var options = new GDLinterOptions { WarnUnusedFunctions = true };
            var linter = new GDLinter(options);
            var code = @"
func test():
    print(""hello"")

func _unused_helper():
    print(""never called"")
";

            var result = linter.LintCode(code);

            result.Issues.Should().Contain(i => i.RuleId == "GDL252" && i.Message.Contains("_unused_helper"));
        }

        [TestMethod]
        public void UnusedFunction_BuiltinCallback_NoIssue()
        {
            var options = new GDLinterOptions { WarnUnusedFunctions = true };
            var linter = new GDLinter(options);
            var code = @"
func _ready():
    print(""ready"")

func _process(delta):
    pass
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL252").Should().BeEmpty();
        }

        [TestMethod]
        public void UnusedFunction_PublicNeverCalled_NoIssue()
        {
            var options = new GDLinterOptions { WarnUnusedFunctions = true };
            var linter = new GDLinter(options);
            var code = @"
func public_method():
    print(""public"")
";

            var result = linter.LintCode(code);

            // Public methods are not flagged
            result.Issues.Where(i => i.RuleId == "GDL252").Should().BeEmpty();
        }

        [TestMethod]
        public void UnusedFunction_Disabled_NoIssue()
        {
            var options = new GDLinterOptions { WarnUnusedFunctions = false };
            var linter = new GDLinter(options);
            var code = @"
func _unused_helper():
    print(""never called"")
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL252").Should().BeEmpty();
        }

        #endregion
    }
}
