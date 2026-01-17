using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Linter.Tests
{
    /// <summary>
    /// Integration tests for lint rules generating fix descriptors.
    /// </summary>
    [TestClass]
    public class RuleFixIntegrationTests
    {
        private readonly GDLinter _linter = new GDLinter();

        #region Class Name Case

        [TestMethod]
        public void ClassNameCaseRule_GeneratesRenameFix()
        {
            var code = "class_name badClassName";
            var result = _linter.LintCode(code);

            var issue = result.Issues.FirstOrDefault(i => i.RuleId == "GDL001");
            issue.Should().NotBeNull("Expected GDL001 issue for class name case violation");

            issue.FixDescriptors.Should().NotBeNullOrEmpty("Expected fix descriptors");

            var renameFix = issue.FixDescriptors.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
            renameFix.Should().NotBeNull("Expected rename fix descriptor");
            renameFix.NewText.Should().Be("BadClassName");
        }

        #endregion

        #region Function Name Case

        [TestMethod]
        public void FunctionNameCaseRule_GeneratesRenameFix()
        {
            var code = @"func BadFunction():
    pass";
            var result = _linter.LintCode(code);

            var issue = result.Issues.FirstOrDefault(i => i.RuleId == "GDL002");
            issue.Should().NotBeNull("Expected GDL002 issue for function name case violation");

            issue.FixDescriptors.Should().NotBeNullOrEmpty("Expected fix descriptors");

            var renameFix = issue.FixDescriptors.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
            renameFix.Should().NotBeNull("Expected rename fix descriptor");
            renameFix.NewText.Should().Be("bad_function");
        }

        #endregion

        #region Variable Name Case

        [TestMethod]
        public void VariableNameCaseRule_GeneratesRenameFix()
        {
            var code = "var BadVariable = 1";
            var result = _linter.LintCode(code);

            var issue = result.Issues.FirstOrDefault(i => i.RuleId == "GDL003");
            issue.Should().NotBeNull("Expected GDL003 issue for variable name case violation");

            issue.FixDescriptors.Should().NotBeNullOrEmpty("Expected fix descriptors");

            var renameFix = issue.FixDescriptors.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
            renameFix.Should().NotBeNull("Expected rename fix descriptor");
            renameFix.NewText.Should().Be("bad_variable");
        }

        #endregion

        #region Constant Name Case

        [TestMethod]
        public void ConstantNameCaseRule_GeneratesRenameFix()
        {
            var code = "const myConstant = 42";
            var result = _linter.LintCode(code);

            var issue = result.Issues.FirstOrDefault(i => i.RuleId == "GDL004");
            issue.Should().NotBeNull("Expected GDL004 issue for constant name case violation");

            issue.FixDescriptors.Should().NotBeNullOrEmpty("Expected fix descriptors");

            var renameFix = issue.FixDescriptors.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
            renameFix.Should().NotBeNull("Expected rename fix descriptor");
            renameFix.NewText.Should().Be("MY_CONSTANT");
        }

        #endregion

        #region Fix Position Validation

        [TestMethod]
        public void ClassNameCaseFix_HasValidPosition()
        {
            var code = "class_name badClassName";
            var result = _linter.LintCode(code);

            var issue = result.Issues.FirstOrDefault(i => i.RuleId == "GDL001");
            var fix = issue?.FixDescriptors?.OfType<GDTextEditFixDescriptor>().FirstOrDefault();

            fix.Should().NotBeNull();
            fix.NewText.Should().Be("BadClassName");
            // Position should be valid
            fix.Line.Should().BeGreaterThanOrEqualTo(0);
            fix.StartColumn.Should().BeGreaterThanOrEqualTo(0);
            fix.EndColumn.Should().BeGreaterThanOrEqualTo(fix.StartColumn);
        }

        [TestMethod]
        public void FunctionNameCaseFix_HasValidPosition()
        {
            var code = @"func BadFunction():
    pass";
            var result = _linter.LintCode(code);

            var issue = result.Issues.FirstOrDefault(i => i.RuleId == "GDL002");
            var fix = issue?.FixDescriptors?.OfType<GDTextEditFixDescriptor>().FirstOrDefault();

            fix.Should().NotBeNull();
            fix.NewText.Should().Be("bad_function");
            // Position should be valid
            fix.Line.Should().BeGreaterThanOrEqualTo(0);
            fix.StartColumn.Should().BeGreaterThanOrEqualTo(0);
            fix.EndColumn.Should().BeGreaterThanOrEqualTo(fix.StartColumn);
        }

        #endregion

        #region Multiple Fixes

        [TestMethod]
        public void MultipleIssues_EachHasFixes()
        {
            var code = @"class_name badClassName

var BadVariable = 1
const myConstant = 42

func BadFunction():
    pass";

            var result = _linter.LintCode(code);

            // Each naming issue should have fix descriptors
            foreach (var issue in result.Issues.Where(i =>
                i.RuleId == "GDL001" ||
                i.RuleId == "GDL002" ||
                i.RuleId == "GDL003" ||
                i.RuleId == "GDL004"))
            {
                issue.FixDescriptors.Should().NotBeNullOrEmpty($"Issue {issue.RuleId} should have fix descriptors");
            }
        }

        #endregion

    }
}
