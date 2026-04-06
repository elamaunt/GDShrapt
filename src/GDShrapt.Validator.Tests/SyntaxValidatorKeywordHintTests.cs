using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    [TestClass]
    public class SyntaxValidatorKeywordHintTests
    {
        #region Keyword Hints — Prefix Match

        [TestMethod]
        public void Validator_VaR_ShowsDidYouMeanVar()
        {
            var validator = new GDValidator();
            var result = validator.ValidateCode("va r x = 5");

            result.Diagnostics.Should().NotBeEmpty();
            var syntaxErrors = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).ToList();
            syntaxErrors.Should().NotBeEmpty();
            syntaxErrors.First().Message.Should().Contain("Did you mean 'var'?");
        }

        [TestMethod]
        public void Validator_FuNc_ShowsDidYouMeanFunc()
        {
            var validator = new GDValidator();
            var result = validator.ValidateCode("fu nc test():\n\tpass");

            var syntaxErrors = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).ToList();
            syntaxErrors.Should().NotBeEmpty();
            syntaxErrors.First().Message.Should().Contain("Did you mean 'func'?");
        }

        #endregion

        #region Keyword Hints — Glued (Missing Space)

        [TestMethod]
        public void Validator_Varx_ShowsMissingSpace()
        {
            var validator = new GDValidator();
            var result = validator.ValidateCode("varx = 5");

            var syntaxErrors = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).ToList();
            syntaxErrors.Should().NotBeEmpty();
            syntaxErrors.First().Message.Should().Contain("Missing space between 'var' and 'x'?");
        }

        [TestMethod]
        public void Validator_Functest_ShowsMissingSpace()
        {
            var validator = new GDValidator();
            var result = validator.ValidateCode("functest():\n\tpass");

            var syntaxErrors = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).ToList();
            syntaxErrors.Should().NotBeEmpty();
            syntaxErrors.First().Message.Should().Contain("Missing space between 'func'");
        }

        #endregion

        #region ShowKeywordHints = false

        [TestMethod]
        public void Validator_ShowKeywordHintsDisabled_NoHintMessage()
        {
            var validator = new GDValidator();
            var options = new GDValidationOptions { ShowKeywordHints = false };
            var result = validator.ValidateCode("va r x = 5", options);

            var syntaxErrors = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).ToList();
            syntaxErrors.Should().NotBeEmpty();
            syntaxErrors.First().Message.Should().NotContain("Did you mean");
            syntaxErrors.First().Message.Should().Contain("Invalid token:");
        }

        #endregion

        #region Grouping — Consecutive Invalid Tokens

        [TestMethod]
        public void Validator_ConsecutiveInvalidTokens_OnlyFirstReported()
        {
            var validator = new GDValidator();
            var result = validator.ValidateCode("va r x = 5");

            // "va" and "r" (and possibly "x") should be grouped into one zone
            // Only the first invalid token in the zone should be reported
            var syntaxErrors = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).ToList();
            syntaxErrors.Count.Should().Be(1, "consecutive invalid tokens should be grouped");
        }

        [TestMethod]
        public void Validator_TwoSeparateZones_TwoReports()
        {
            var validator = new GDValidator();
            // Two invalid tokens on different lines
            var result = validator.ValidateCode("§§§\nvar x = 5\n§§§");

            var syntaxErrors = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).ToList();
            syntaxErrors.Count.Should().Be(2, "two zones on different lines should produce two reports");
        }

        [TestMethod]
        public void Validator_NewlineSeparatesZones()
        {
            var validator = new GDValidator();
            var result = validator.ValidateCode("§§§\n§§§");

            var syntaxErrors = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).ToList();
            syntaxErrors.Count.Should().Be(2, "newline should separate error zones");
        }

        [TestMethod]
        public void Validator_SpaceDoesNotSeparateZone()
        {
            var validator = new GDValidator();
            var result = validator.ValidateCode("§§§ §§§");

            var syntaxErrors = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).ToList();
            syntaxErrors.Count.Should().Be(1, "space should not separate error zones");
        }

        #endregion

        #region Valid Code — No Errors

        [TestMethod]
        public void Validator_ValidVar_NoSyntaxErrors()
        {
            var validator = new GDValidator();
            var result = validator.ValidateCode("var x = 5");

            var syntaxErrors = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).ToList();
            syntaxErrors.Should().BeEmpty();
        }

        [TestMethod]
        public void Validator_ValidFunc_NoSyntaxErrors()
        {
            var validator = new GDValidator();
            var result = validator.ValidateCode("func test():\n\tpass");

            var syntaxErrors = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).ToList();
            syntaxErrors.Should().BeEmpty();
        }

        #endregion

        #region Statement Level

        [TestMethod]
        public void Validator_StatementWhile_ShowsDidYouMean()
        {
            var validator = new GDValidator();
            var result = validator.ValidateCode("func t():\n\twh ile true:\n\t\tpass");

            var syntaxErrors = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).ToList();
            syntaxErrors.Should().NotBeEmpty();
            syntaxErrors.Any(e => e.Message.Contains("Did you mean 'while'?")).Should().BeTrue();
        }

        [TestMethod]
        public void Validator_StatementReturn_ShowsDidYouMean()
        {
            var validator = new GDValidator();
            var result = validator.ValidateCode("func t():\n\tret urn 5");

            var syntaxErrors = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).ToList();
            syntaxErrors.Should().NotBeEmpty();
            syntaxErrors.Any(e => e.Message.Contains("Did you mean 'return'?")).Should().BeTrue();
        }

        #endregion
    }
}
