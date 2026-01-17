using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for comment-based diagnostic suppression (gd:ignore, gd:disable/enable).
    /// </summary>
    [TestClass]
    public class ValidatorSuppressionTests
    {
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
        }

        #region gd:ignore (next line suppression)

        [TestMethod]
        public void Ignore_NextLine_SuppressesDiagnostic()
        {
            var code = @"
func test():
    # gd:ignore = GD2001
    print(undefined_var)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_Inline_SuppressesCurrentLine()
        {
            var code = @"
func test():
    print(undefined_var)  # gd:ignore = GD2001
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_WithoutRules_SuppressesAllDiagnostics()
        {
            var code = @"
func test():
    # gd:ignore
    print(undefined_var)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            // All issues on line 3 should be suppressed (0-based: line 3 = 4th line = print(undefined_var))
            result.Diagnostics.Where(d => d.StartLine == 3).Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_MultipleRules_SuppressesAll()
        {
            var code = @"
func test():
    # gd:ignore = GD2001, GD2002
    undefined_func(undefined_var)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            result.Diagnostics.Where(d =>
                d.Code == GDDiagnosticCode.UndefinedVariable ||
                d.Code == GDDiagnosticCode.UndefinedFunction).Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_OnlyAffectsNextLine()
        {
            var code = @"
func test():
    # gd:ignore = GD2001
    print(undefined_var1)
    print(undefined_var2)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            // First variable suppressed (line 3), second should still have issue (line 4) - 0-based
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.StartLine == 3).Should().BeEmpty();
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.StartLine == 4).Should().NotBeEmpty();
        }

        [TestMethod]
        public void Ignore_CaseInsensitive()
        {
            var code = @"
func test():
    # GD:IGNORE = GD2001
    print(undefined_var)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().BeEmpty();
        }

        #endregion

        #region gd:disable/enable (block suppression)

        [TestMethod]
        public void Disable_SuppressesUntilEndOfFile()
        {
            var code = @"
func test():
    # gd:disable = GD2001
    print(undefined_var1)
    print(undefined_var2)
    print(undefined_var3)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_Enable_SuppressesOnlyBlock()
        {
            var code = @"
func test():
    # gd:disable = GD2001
    print(undefined_var1)
    print(undefined_var2)
    # gd:enable = GD2001
    print(undefined_var3)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            // First two suppressed, third should have issue - 0-based line numbers
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.StartLine == 3).Should().BeEmpty();
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.StartLine == 4).Should().BeEmpty();
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.StartLine == 6).Should().NotBeEmpty();
        }

        [TestMethod]
        public void Disable_WithoutRules_SuppressesAll()
        {
            var code = @"
func test():
    # gd:disable
    print(undefined_var)
    undefined_func()
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            // All issues after disable should be suppressed - 0-based
            result.Diagnostics.Where(d => d.StartLine >= 3).Should().BeEmpty();
        }

        [TestMethod]
        public void Enable_ReEnablesRule()
        {
            var code = @"
func test():
    # gd:disable = GD2001
    print(undefined_var1)
    # gd:enable = GD2001
    print(undefined_var2)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            // Line 3 suppressed (0-based), line 5 should report
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.StartLine == 3).Should().BeEmpty();
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.StartLine == 5).Should().NotBeEmpty();
        }

        #endregion

        #region Suppression disabled option

        [TestMethod]
        public void SuppressionDisabled_IgnoresDirectives()
        {
            var code = @"
func test():
    # gd:ignore = GD2001
    print(undefined_var)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = false });

            // Should still report issue when suppression is disabled
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().NotBeEmpty();
        }

        [TestMethod]
        public void SuppressionDisabled_DisableDirectiveIgnored()
        {
            var code = @"
func test():
    # gd:disable = GD2001
    print(undefined_var1)
    print(undefined_var2)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = false });

            // Should still report issues when suppression is disabled
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().HaveCount(2);
        }

        [TestMethod]
        public void SuppressionEnabled_ByDefault()
        {
            // Default options should have suppression enabled
            var options = GDValidationOptions.Default;
            options.EnableCommentSuppression.Should().BeTrue();
        }

        #endregion

        #region Different diagnostic codes

        [TestMethod]
        public void Ignore_BreakOutsideLoop_SuppressesGD5001()
        {
            var code = @"
func test():
    # gd:ignore = GD5001
    break
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.BreakOutsideLoop).Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_DuplicateDeclaration_SuppressesGD2003()
        {
            var code = @"
func test():
    var x = 1
    # gd:ignore = GD2003
    var x = 2
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.DuplicateDeclaration).Should().BeEmpty();
        }

        [TestMethod]
        public void Disable_ControlFlowErrors_SuppressesRange()
        {
            var code = @"
func test():
    # gd:disable = GD5001, GD5002
    break
    continue
    # gd:enable = GD5001, GD5002
    break
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            // First break and continue suppressed, last break should report - 0-based
            result.Diagnostics.Where(d =>
                (d.Code == GDDiagnosticCode.BreakOutsideLoop || d.Code == GDDiagnosticCode.ContinueOutsideLoop) &&
                d.StartLine <= 4).Should().BeEmpty();
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.BreakOutsideLoop && d.StartLine == 6).Should().NotBeEmpty();
        }

        #endregion

        #region Edge cases

        [TestMethod]
        public void Ignore_WithSpacesAroundEquals()
        {
            var code = @"
func test():
    # gd:ignore = GD2001
    print(undefined_var)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().BeEmpty();
        }

        [TestMethod]
        public void Ignore_WithNoSpacesAroundEquals()
        {
            var code = @"
func test():
    # gd:ignore=GD2001
    print(undefined_var)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().BeEmpty();
        }

        [TestMethod]
        public void Comment_NotDirective_DoesNotSuppress()
        {
            var code = @"
func test():
    # This is just a comment about gd errors
    print(undefined_var)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            // Should still report issue because it's not a directive
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().NotBeEmpty();
        }

        [TestMethod]
        public void MultipleIgnore_OnConsecutiveLines()
        {
            var code = @"
func test():
    # gd:ignore = GD2001
    print(undefined_var1)
    # gd:ignore = GD2001
    print(undefined_var2)
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().BeEmpty();
        }

        [TestMethod]
        public void Nested_Disable_Enable()
        {
            var code = @"
func test():
    # gd:disable = GD2001
    print(undefined_var1)
    # gd:disable = GD5001
    break
    # gd:enable = GD2001
    print(undefined_var2)
    # gd:enable = GD5001
    break
";
            var result = _validator.ValidateCode(code, new GDValidationOptions { EnableCommentSuppression = true });

            // UndefinedVariable suppressed at line 3, re-enabled at line 7 - 0-based
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.StartLine == 3).Should().BeEmpty();
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.StartLine == 7).Should().NotBeEmpty();
            // BreakOutsideLoop suppressed at line 5, re-enabled at line 9 - 0-based
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.BreakOutsideLoop && d.StartLine == 5).Should().BeEmpty();
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.BreakOutsideLoop && d.StartLine == 9).Should().NotBeEmpty();
        }

        #endregion
    }
}
