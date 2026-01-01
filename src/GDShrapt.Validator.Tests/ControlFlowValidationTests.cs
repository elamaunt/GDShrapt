using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for control flow validation: break, continue, return, await, yield.
    /// </summary>
    [TestClass]
    public class ControlFlowValidationTests
    {
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
        }

        [TestMethod]
        public void BreakInLoop_NoError()
        {
            var code = @"
func test():
    for i in range(10):
        if i == 5:
            break
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.BreakOutsideLoop).Should().BeEmpty();
        }

        [TestMethod]
        public void BreakOutsideLoop_ReportsError()
        {
            var code = @"
func test():
    break
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.BreakOutsideLoop).Should().NotBeEmpty();
        }

        [TestMethod]
        public void ContinueInLoop_NoError()
        {
            var code = @"
func test():
    while true:
        continue
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.ContinueOutsideLoop).Should().BeEmpty();
        }

        [TestMethod]
        public void ContinueOutsideLoop_ReportsError()
        {
            var code = @"
func test():
    continue
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.ContinueOutsideLoop).Should().NotBeEmpty();
        }

        [TestMethod]
        public void ReturnInFunction_NoError()
        {
            var code = @"
func test():
    return 42
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.ReturnOutsideFunction).Should().BeEmpty();
        }

        [TestMethod]
        public void ReturnOutsideFunction_ReportsError()
        {
            var result = _validator.ValidateStatement("return 42");
            result.Errors.Where(d => d.Code == GDDiagnosticCode.ReturnOutsideFunction).Should().NotBeEmpty();
        }

        [TestMethod]
        public void AwaitInFunction_NoError()
        {
            var code = @"
func test():
    await get_tree().create_timer(1.0).timeout
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.AwaitOutsideFunction).Should().BeEmpty();
        }

        [TestMethod]
        public void ReturnInLambda_NoError()
        {
            var code = @"
func test():
    var f = func(): return 42
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.ReturnOutsideFunction).Should().BeEmpty();
        }

        [TestMethod]
        public void BreakInNestedLoop_NoError()
        {
            var code = @"
func test():
    for i in range(10):
        for j in range(10):
            if i == j:
                break
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.BreakOutsideLoop).Should().BeEmpty();
        }
    }
}
