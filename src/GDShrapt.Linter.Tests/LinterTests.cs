using FluentAssertions;
using GDShrapt.Linter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Linting
{
    /// <summary>
    /// Tests for the core GDLinter functionality.
    /// </summary>
    [TestClass]
    public class LinterTests
    {
        [TestMethod]
        public void Linter_DefaultConstructor_RegistersDefaultRules()
        {
            var linter = new GDLinter();

            linter.Rules.Should().NotBeEmpty();
            linter.Rules.Count.Should().BeGreaterThanOrEqualTo(12);
        }

        [TestMethod]
        public void Linter_CreateEmpty_HasNoRules()
        {
            var linter = GDLinter.CreateEmpty();

            linter.Rules.Should().BeEmpty();
        }

        [TestMethod]
        public void Linter_AddRule_AddsCustomRule()
        {
            var linter = GDLinter.CreateEmpty();
            var rule = new GDVariableNameCaseRule();

            linter.AddRule(rule);

            linter.Rules.Should().ContainSingle();
            linter.Rules[0].Should().Be(rule);
        }

        [TestMethod]
        public void Linter_RemoveRule_RemovesRuleById()
        {
            var linter = new GDLinter();
            var initialCount = linter.Rules.Count;

            var removed = linter.RemoveRule("GDL003"); // VariableNameCaseRule

            removed.Should().BeTrue();
            linter.Rules.Count.Should().Be(initialCount - 1);
            linter.GetRule("GDL003").Should().BeNull();
        }

        [TestMethod]
        public void Linter_GetRule_ReturnsRuleById()
        {
            var linter = new GDLinter();

            var rule = linter.GetRule("GDL001");

            rule.Should().NotBeNull();
            rule.Should().BeOfType<GDClassNameCaseRule>();
        }

        [TestMethod]
        public void Linter_GetRulesByCategory_ReturnsCorrectRules()
        {
            var linter = new GDLinter();

            var namingRules = linter.GetRulesByCategory(GDLintCategory.Naming).ToList();
            var styleRules = linter.GetRulesByCategory(GDLintCategory.Style).ToList();
            var bestPracticesRules = linter.GetRulesByCategory(GDLintCategory.BestPractices).ToList();

            namingRules.Should().NotBeEmpty();
            styleRules.Should().NotBeEmpty();
            bestPracticesRules.Should().NotBeEmpty();
        }

        [TestMethod]
        public void Linter_LintCode_ReturnsResult()
        {
            var linter = new GDLinter();
            var code = @"
func test():
    pass
";

            var result = linter.LintCode(code);

            result.Should().NotBeNull();
        }

        [TestMethod]
        public void Linter_LintCode_EmptyCode_ReturnsEmptyResult()
        {
            var linter = new GDLinter();

            var result = linter.LintCode("");

            result.Should().NotBeNull();
            result.Issues.Should().BeEmpty();
        }

        [TestMethod]
        public void Linter_LintCode_NullCode_ReturnsEmptyResult()
        {
            var linter = new GDLinter();

            var result = linter.LintCode(null);

            result.Should().NotBeNull();
            result.Issues.Should().BeEmpty();
        }

        [TestMethod]
        public void Linter_Lint_NullNode_ReturnsEmptyResult()
        {
            var linter = new GDLinter();

            var result = linter.Lint(null);

            result.Should().NotBeNull();
            result.Issues.Should().BeEmpty();
        }

        [TestMethod]
        public void Linter_GetEnabledRules_ReturnsOnlyEnabled()
        {
            var linter = new GDLinter();
            linter.Options.DisableRule("GDL001");

            var enabledRules = linter.GetEnabledRules().ToList();

            enabledRules.Should().NotContain(r => r.RuleId == "GDL001");
        }

        [TestMethod]
        public void Linter_GetDisabledRules_ReturnsOnlyDisabled()
        {
            var linter = new GDLinter();
            linter.Options.DisableRule("GDL001");

            var disabledRules = linter.GetDisabledRules().ToList();

            disabledRules.Should().ContainSingle(r => r.RuleId == "GDL001");
        }

        [TestMethod]
        public void Linter_LintCode_DisabledRule_DoesNotRun()
        {
            var linter = new GDLinter();
            linter.Options.DisableRule("GDL003"); // Variable naming rule

            var code = @"
func test():
    var BadName = 10
";

            var result = linter.LintCode(code);

            result.Issues.Should().NotContain(i => i.RuleId == "GDL003");
        }

        [TestMethod]
        public void Linter_WithOptions_UsesProvidedOptions()
        {
            var options = new GDLinterOptions { MaxLineLength = 50 };
            var linter = new GDLinter(options);

            linter.Options.MaxLineLength.Should().Be(50);
        }

        [TestMethod]
        public void LintResult_TotalCount_ReturnsTrueWhenHasIssues()
        {
            var linter = new GDLinter();
            var code = @"
func test():
    var BadName = 10
";

            var result = linter.LintCode(code);

            result.TotalCount.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public void LintResult_GetIssuesByCategory_FiltersCorrectly()
        {
            var linter = new GDLinter();
            var code = @"
class_name badClassName
func test():
    var BadName = 10
";

            var result = linter.LintCode(code);
            var namingIssues = result.GetIssuesByCategory(GDLintCategory.Naming);

            namingIssues.Should().NotBeEmpty();
            namingIssues.All(i => i.Category == GDLintCategory.Naming).Should().BeTrue();
        }

        [TestMethod]
        public void LintResult_GetIssuesBySeverity_FiltersCorrectly()
        {
            var linter = new GDLinter();
            var code = @"
func test():
    var BadName = 10
";

            var result = linter.LintCode(code);
            var warnings = result.GetIssuesBySeverity(GDLintSeverity.Warning);

            warnings.All(i => i.Severity == GDLintSeverity.Warning).Should().BeTrue();
        }

        [TestMethod]
        public void LintResult_GetIssuesByRule_FiltersCorrectly()
        {
            var linter = new GDLinter();
            var code = @"
func test():
    var BadName = 10
    var AnotherBad = 20
";

            var result = linter.LintCode(code);
            var varNameIssues = result.GetIssuesByRule("GDL003");

            varNameIssues.All(i => i.RuleId == "GDL003").Should().BeTrue();
        }
    }
}
