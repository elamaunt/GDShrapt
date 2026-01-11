using FluentAssertions;
using GDShrapt.Linter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Linting
{
    /// <summary>
    /// Tests for linter options and configuration.
    /// </summary>
    [TestClass]
    public class LinterOptionsTests
    {
        #region Default Options

        [TestMethod]
        public void DefaultOptions_HasCorrectDefaults()
        {
            var options = GDLinterOptions.Default;

            options.ClassNameCase.Should().Be(NamingCase.PascalCase);
            options.FunctionNameCase.Should().Be(NamingCase.SnakeCase);
            options.VariableNameCase.Should().Be(NamingCase.SnakeCase);
            options.ConstantNameCase.Should().Be(NamingCase.ScreamingSnakeCase);
            options.SignalNameCase.Should().Be(NamingCase.SnakeCase);
            options.EnumNameCase.Should().Be(NamingCase.PascalCase);
            options.EnumValueCase.Should().Be(NamingCase.ScreamingSnakeCase);
            options.MaxLineLength.Should().Be(100);
            options.WarnUnusedVariables.Should().BeTrue();
            options.WarnUnusedParameters.Should().BeTrue();
            options.WarnEmptyFunctions.Should().BeTrue();
            options.SuggestTypeHints.Should().BeFalse();
        }

        #endregion

        #region Strict Options

        [TestMethod]
        public void StrictOptions_HasAllEnabled()
        {
            var options = GDLinterOptions.Strict;

            options.SuggestTypeHints.Should().BeTrue();
            options.WarnUnusedSignals.Should().BeTrue();
            options.EnforceMemberOrdering.Should().BeTrue();
        }

        #endregion

        #region Minimal Options

        [TestMethod]
        public void MinimalOptions_HasMostDisabled()
        {
            var options = GDLinterOptions.Minimal;

            options.WarnUnusedVariables.Should().BeFalse();
            options.WarnUnusedParameters.Should().BeFalse();
            options.WarnEmptyFunctions.Should().BeFalse();
            options.MaxLineLength.Should().Be(0);
        }

        #endregion

        #region Rule Enable/Disable

        [TestMethod]
        public void DisableRule_DisablesRule()
        {
            var options = new GDLinterOptions();
            var rule = new GDVariableNameCaseRule();

            options.DisableRule("GDL003");

            options.IsRuleEnabled(rule).Should().BeFalse();
        }

        [TestMethod]
        public void EnableRule_EnablesRule()
        {
            var options = new GDLinterOptions();
            var rule = new GDVariableNameCaseRule();
            options.DisableRule("GDL003");

            options.EnableRule("GDL003");

            options.IsRuleEnabled(rule).Should().BeTrue();
        }

        [TestMethod]
        public void IsRuleEnabled_DefaultsToRuleDefault()
        {
            var options = new GDLinterOptions();
            var rule = new GDVariableNameCaseRule();

            options.IsRuleEnabled(rule).Should().Be(rule.EnabledByDefault);
        }

        #endregion

        #region Severity Override

        [TestMethod]
        public void SetRuleSeverity_OverridesSeverity()
        {
            var options = new GDLinterOptions();
            var rule = new GDVariableNameCaseRule();

            options.SetRuleSeverity("GDL003", GDLintSeverity.Error);

            options.GetRuleSeverity(rule).Should().Be(GDLintSeverity.Error);
        }

        [TestMethod]
        public void GetRuleSeverity_DefaultsToRuleDefault()
        {
            var options = new GDLinterOptions();
            var rule = new GDVariableNameCaseRule();

            options.GetRuleSeverity(rule).Should().Be(rule.DefaultSeverity);
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void Linter_WithDisabledRule_SkipsRule()
        {
            var options = new GDLinterOptions();
            options.DisableRule("GDL003");
            var linter = new GDLinter(options);
            var code = @"var BadName = 10";

            var result = linter.LintCode(code);

            result.Issues.Should().NotContain(i => i.RuleId == "GDL003");
        }

        [TestMethod]
        public void Linter_WithCustomNamingCase_UsesCustomCase()
        {
            var options = new GDLinterOptions { VariableNameCase = NamingCase.CamelCase };
            var linter = new GDLinter(options);
            var code = @"var myVariable = 10";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void Linter_WithNamingCaseAny_AcceptsAnyCase()
        {
            var options = new GDLinterOptions { VariableNameCase = NamingCase.Any };
            var linter = new GDLinter(options);
            var code = @"
var snake_case = 1
var PascalCase = 2
var camelCase = 3
var SCREAMING = 4
";

            var result = linter.LintCode(code);

            result.Issues.Where(i => i.RuleId == "GDL003").Should().BeEmpty();
        }

        [TestMethod]
        public void Linter_OptionsCanBeModifiedAfterConstruction()
        {
            var linter = new GDLinter();
            linter.Options.DisableRule("GDL003");
            var code = @"var BadName = 10";

            var result = linter.LintCode(code);

            result.Issues.Should().NotContain(i => i.RuleId == "GDL003");
        }

        [TestMethod]
        public void Linter_OptionsCanBeReplaced()
        {
            var linter = new GDLinter();
            linter.Options = GDLinterOptions.Minimal;
            var code = @"
func test(unused):
    var unused_var = 10
";

            var result = linter.LintCode(code);

            // Minimal options disable unused checks
            result.Issues.Where(i => i.RuleId == "GDL201").Should().BeEmpty();
            result.Issues.Where(i => i.RuleId == "GDL202").Should().BeEmpty();
        }

        #endregion

        #region LintResult Tests

        [TestMethod]
        public void LintResult_GetErrors_ReturnsOnlyErrors()
        {
            var linter = new GDLinter();
            var code = @"var BadName = 10";

            var result = linter.LintCode(code);

            // This test verifies the filtering works
            result.GetErrors().All(i => i.Severity == GDLintSeverity.Error).Should().BeTrue();
        }

        [TestMethod]
        public void LintResult_GetWarnings_ReturnsOnlyWarnings()
        {
            var linter = new GDLinter();
            var code = @"var BadName = 10";

            var result = linter.LintCode(code);

            result.GetWarnings().All(i => i.Severity == GDLintSeverity.Warning).Should().BeTrue();
        }

        [TestMethod]
        public void LintResult_GetIssuesBySeverity_Hints_ReturnsOnlyHints()
        {
            var linter = new GDLinter();
            var code = @"var some_var = 10";

            var result = linter.LintCode(code);

            result.GetIssuesBySeverity(GDLintSeverity.Hint).All(i => i.Severity == GDLintSeverity.Hint).Should().BeTrue();
        }

        [TestMethod]
        public void LintResult_HasErrors_TrueWhenHasErrors()
        {
            var linter = new GDLinter();
            var code = @"var BadName = 10";

            var result = linter.LintCode(code);

            // Currently rules report their own DefaultSeverity
            result.HasErrors.Should().Be(result.GetErrors().Any());
        }

        [TestMethod]
        public void LintResult_HasWarnings_TrueWhenHasWarnings()
        {
            var linter = new GDLinter();
            var code = @"var BadName = 10";

            var result = linter.LintCode(code);

            result.HasWarnings.Should().BeTrue();
        }

        [TestMethod]
        public void LintResult_TotalCount_ReturnsCorrectCount()
        {
            var linter = new GDLinter();
            var code = @"
var BadName1 = 10
var BadName2 = 20
";

            var result = linter.LintCode(code);

            result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        }

        #endregion
    }
}
