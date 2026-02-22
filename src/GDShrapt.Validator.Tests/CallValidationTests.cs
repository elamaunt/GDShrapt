using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for function call validation: argument counts.
    /// </summary>
    [TestClass]
    public class CallValidationTests
    {
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
        }

        [TestMethod]
        public void ValidateCall_RangeWithValidArgs()
        {
            var code = @"
func test():
    for i in range(10):
        pass
    for i in range(0, 10):
        pass
    for i in range(0, 10, 2):
        pass
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.WrongArgumentCount && d.Message.Contains("range")).Should().BeEmpty();
        }

        [TestMethod]
        public void ValidateCall_RangeWithNoArgs()
        {
            var code = @"
func test():
    for i in range():
        pass
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.WrongArgumentCount && d.Message.Contains("range")).Should().NotBeEmpty();
        }

        [TestMethod]
        public void ValidateCall_PreloadWithValidArgs()
        {
            var code = @"
func test():
    var scene = preload(""res://scene.tscn"")
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.WrongArgumentCount && d.Message.Contains("preload")).Should().BeEmpty();
        }

        [TestMethod]
        public void ValidateCall_ClampWithInvalidArgs()
        {
            var code = @"
func test():
    var x = clamp(5)
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.WrongArgumentCount && d.Message.Contains("clamp")).Should().NotBeEmpty();
        }

        [TestMethod]
        public void ValidateCall_AssertWithValidArgs()
        {
            var code = @"
func test():
    assert(true)
    assert(1 == 1, ""Should be equal"")
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.WrongArgumentCount && d.Message.Contains("assert")).Should().BeEmpty();
        }

        [TestMethod]
        public void ValidateCall_PrintWithAnyArgs()
        {
            var code = @"
func test():
    print()
    print(1)
    print(1, 2, 3, 4, 5)
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.WrongArgumentCount && d.Message.Contains("print")).Should().BeEmpty();
        }

        [TestMethod]
        public void ValidateCall_IsInstanceOf_TwoArgs_NoError()
        {
            var code = @"
func test():
    var obj = Node.new()
    var result = is_instance_of(obj, Node)
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.WrongArgumentCount && d.Message.Contains("is_instance_of")).Should().BeEmpty();
        }

        [TestMethod]
        public void ValidateCall_IsInstanceOf_OneArg_ReportsError()
        {
            var code = @"
func test():
    var obj = Node.new()
    var result = is_instance_of(obj)
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.WrongArgumentCount && d.Message.Contains("is_instance_of")).Should().NotBeEmpty();
        }

        [TestMethod]
        public void ValidateCall_IsInstanceOf_ThreeArgs_ReportsError()
        {
            var code = @"
func test():
    var result = is_instance_of(1, 2, 3)
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.WrongArgumentCount && d.Message.Contains("is_instance_of")).Should().NotBeEmpty();
        }

        [TestMethod]
        public void ValidateCall_MathFunctionsWithValidArgs()
        {
            var code = @"
func test():
    var a = abs(-5)
    var b = sqrt(16)
    var c = pow(2, 3)
    var d = lerp(0, 100, 0.5)
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.WrongArgumentCount).Should().BeEmpty();
        }
    }
}
