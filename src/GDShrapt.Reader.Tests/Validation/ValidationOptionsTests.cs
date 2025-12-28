using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for validation options and result handling.
    /// </summary>
    [TestClass]
    public class ValidationOptionsTests
    {
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
        }

        [TestMethod]
        public void ValidCode_NoErrors()
        {
            var code = @"
extends Node

var x = 10

func _ready():
    print(x)
";
            var result = _validator.ValidateCode(code);

            result.IsValid.Should().BeTrue();
            result.HasErrors.Should().BeFalse();
        }

        [TestMethod]
        public void SyntaxOnly_SkipsOtherChecks()
        {
            var code = @"
func test():
    print(undefined_var)
    break
";
            var result = _validator.ValidateCode(code, GDValidationOptions.SyntaxOnly);

            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().BeEmpty();
            result.Errors.Where(d => d.Code == GDDiagnosticCode.BreakOutsideLoop).Should().BeEmpty();
        }

        [TestMethod]
        public void None_NoChecks()
        {
            var code = @"
func test():
    print(undefined_var)
    break
    return @@@
";
            var result = _validator.ValidateCode(code, GDValidationOptions.None);

            result.Diagnostics.Should().BeEmpty();
        }

        [TestMethod]
        public void CustomOptions_SelectiveChecks()
        {
            var code = @"
func test():
    break
";
            var options = new GDValidationOptions
            {
                CheckSyntax = false,
                CheckScope = false,
                CheckTypes = false,
                CheckCalls = false,
                CheckControlFlow = true
            };

            var result = _validator.ValidateCode(code, options);

            result.Errors.Where(d => d.Code == GDDiagnosticCode.BreakOutsideLoop).Should().NotBeEmpty();
        }

        [TestMethod]
        public void GetDiagnosticsAtLine_ReturnsCorrect()
        {
            var code = @"
func test():
    break
";
            var result = _validator.ValidateCode(code);

            result.HasErrors.Should().BeTrue();
            var breakError = result.Errors.First(d => d.Code == GDDiagnosticCode.BreakOutsideLoop);
            breakError.StartLine.Should().BeGreaterThan(0);

            var diagnosticsAtLine = result.GetDiagnosticsAtLine(breakError.StartLine).ToList();
            diagnosticsAtLine.Should().NotBeEmpty();
        }

        [TestMethod]
        public void GetDiagnosticsByCode_ReturnsCorrect()
        {
            var code = @"
func test():
    break
    continue
";
            var result = _validator.ValidateCode(code);

            var breakErrors = result.GetDiagnosticsByCode(GDDiagnosticCode.BreakOutsideLoop).ToList();
            breakErrors.Should().NotBeEmpty();
        }

        [TestMethod]
        public void InvalidToken_ReportsError()
        {
            var code = @"var x = @@@";
            var result = _validator.ValidateCode(code);

            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.InvalidToken).Should().NotBeEmpty();
        }
    }
}
