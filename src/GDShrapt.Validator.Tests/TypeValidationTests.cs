using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for type validation: operator type checking.
    /// </summary>
    [TestClass]
    public class TypeValidationTests
    {
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
        }

        [TestMethod]
        public void BitwiseOnIntegers_NoWarning()
        {
            var code = @"
func test():
    var x = 5 & 3
    var y = 5 | 3
    var z = 5 ^ 3
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InvalidOperandType && d.Message.Contains("Bitwise")).Should().BeEmpty();
        }

        [TestMethod]
        public void ArithmeticOnNumbers_NoWarning()
        {
            var code = @"
func test():
    var a = 5 + 3
    var b = 5.0 - 3.0
    var c = 5 * 3.0
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InvalidOperandType).Should().BeEmpty();
        }

        [TestMethod]
        public void StringConcatenation_NoWarning()
        {
            var code = @"
func test():
    var s = ""hello"" + "" world""
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InvalidOperandType).Should().BeEmpty();
        }
    }
}
