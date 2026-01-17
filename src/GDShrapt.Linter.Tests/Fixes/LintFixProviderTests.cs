using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Linter.Tests
{
    /// <summary>
    /// Tests for GDLintFixProvider functionality.
    /// </summary>
    [TestClass]
    public class LintFixProviderTests
    {
        private readonly GDLintFixProvider _provider = new GDLintFixProvider();

        #region Suppression Fixes

        [TestMethod]
        public void GetFixes_AnyIssue_AlwaysIncludesSuppression()
        {
            var issue = CreateIssue("GDL001", "class-name-case", 5, 0, 10);

            var fixes = _provider.GetFixes(issue).ToList();

            var suppression = fixes.OfType<GDSuppressionFixDescriptor>().FirstOrDefault();
            suppression.Should().NotBeNull();
            suppression.DiagnosticCode.Should().Be("GDL001");
            suppression.TargetLine.Should().Be(5);
        }

        #endregion

        #region Naming Rule Fixes

        [TestMethod]
        public void GetFixes_ClassNameCase_ReturnsRenameFix()
        {
            var issue = CreateNamingIssue("GDL001", "badClassName", "BadClassName", 3, 6, 18);

            var fixes = _provider.GetFixes(issue).ToList();

            var renameFix = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
            renameFix.Should().NotBeNull();
            renameFix.Kind.Should().Be(GDFixKind.ReplaceIdentifier);
            renameFix.NewText.Should().Be("BadClassName");
            renameFix.Line.Should().Be(3);
            renameFix.StartColumn.Should().Be(6);
            renameFix.EndColumn.Should().Be(18);
        }

        [TestMethod]
        public void GetFixes_FunctionNameCase_ReturnsRenameFix()
        {
            var issue = CreateNamingIssue("GDL002", "BadFunction", "bad_function", 10, 5, 16);

            var fixes = _provider.GetFixes(issue).ToList();

            var renameFix = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
            renameFix.Should().NotBeNull();
            renameFix.NewText.Should().Be("bad_function");
        }

        [TestMethod]
        public void GetFixes_VariableNameCase_ReturnsRenameFix()
        {
            var issue = CreateNamingIssue("GDL003", "BadVariable", "bad_variable", 8, 4, 15);

            var fixes = _provider.GetFixes(issue).ToList();

            var renameFix = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
            renameFix.Should().NotBeNull();
            renameFix.NewText.Should().Be("bad_variable");
        }

        [TestMethod]
        public void GetFixes_ConstantNameCase_ReturnsRenameFix()
        {
            var issue = CreateNamingIssue("GDL004", "myConstant", "MY_CONSTANT", 2, 6, 16);

            var fixes = _provider.GetFixes(issue).ToList();

            var renameFix = fixes.OfType<GDTextEditFixDescriptor>().FirstOrDefault();
            renameFix.Should().NotBeNull();
            renameFix.NewText.Should().Be("MY_CONSTANT");
        }

        #endregion

        #region No Fix for Non-Naming Rules

        [TestMethod]
        public void GetFixes_NonNamingRule_OnlyReturnsSuppression()
        {
            // GDL100 is a hypothetical non-naming rule
            var issue = CreateIssue("GDL100", "some-rule", 5, 0, 10);

            var fixes = _provider.GetFixes(issue).ToList();

            // Should only have suppression fix
            fixes.OfType<GDSuppressionFixDescriptor>().Should().HaveCount(1);
            fixes.OfType<GDTextEditFixDescriptor>().Should().BeEmpty();
        }

        #endregion

        #region Helper Methods

        private GDLintIssue CreateIssue(string ruleId, string ruleName, int line,
            int startCol, int endCol)
        {
            return new GDLintIssue(
                ruleId, ruleName, GDLintSeverity.Warning, GDLintCategory.Naming,
                "Test message",
                line, startCol, line, endCol);
        }

        private GDLintIssue CreateNamingIssue(string ruleId, string original, string suggested,
            int line, int startCol, int endCol)
        {
            return new GDLintIssue(
                ruleId, "naming", GDLintSeverity.Warning, GDLintCategory.Naming,
                $"'{original}' should use correct case",
                line, startCol, line, endCol,
                $"Rename to '{suggested}'");
        }

        #endregion
    }
}
