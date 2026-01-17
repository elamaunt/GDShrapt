using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for GD3007: IncompatibleReturnType validation.
    /// </summary>
    [TestClass]
    public class ReturnTypeValidationTests
    {
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
        }

        #region Return Type Matches - No Warning

        [TestMethod]
        public void ReturnIntFromIntFunction_NoWarning()
        {
            var code = @"
func get_value() -> int:
    return 42
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.IncompatibleReturnType)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void ReturnFloatFromFloatFunction_NoWarning()
        {
            var code = @"
func get_value() -> float:
    return 3.14
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.IncompatibleReturnType)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void ReturnStringFromStringFunction_NoWarning()
        {
            var code = @"
func get_value() -> String:
    return ""hello""
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.IncompatibleReturnType)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void ReturnBoolFromBoolFunction_NoWarning()
        {
            var code = @"
func is_valid() -> bool:
    return true
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.IncompatibleReturnType)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void ReturnNullFromTypedFunction_NoWarning()
        {
            // null is compatible with any reference type
            var code = @"
func get_node() -> Node:
    return null
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.IncompatibleReturnType)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void ReturnFromUntypedFunction_NoWarning()
        {
            // Functions without declared return type can return anything
            var code = @"
func get_value():
    return ""anything""
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.IncompatibleReturnType)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void ReturnNothingFromVoidFunction_NoWarning()
        {
            var code = @"
func do_something() -> void:
    print(""done"")
    return
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.IncompatibleReturnType)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void NoReturnFromVoidFunction_NoWarning()
        {
            // Implicit return is fine for void
            var code = @"
func do_something() -> void:
    print(""done"")
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.IncompatibleReturnType)
                .Should().BeEmpty();
        }

        #endregion

        #region Return Type Mismatch - Warning Expected

        [TestMethod]
        public void ReturnStringFromIntFunction_ReportsWarning()
        {
            var code = @"
func get_value() -> int:
    return ""hello""
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Should().Contain(d =>
                d.Code == GDDiagnosticCode.IncompatibleReturnType &&
                d.Message.Contains("String") &&
                d.Message.Contains("int"));
        }

        [TestMethod]
        public void ReturnIntFromStringFunction_ReportsWarning()
        {
            var code = @"
func get_text() -> String:
    return 42
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Should().Contain(d =>
                d.Code == GDDiagnosticCode.IncompatibleReturnType &&
                d.Message.Contains("int") &&
                d.Message.Contains("String"));
        }

        [TestMethod]
        public void ReturnBoolFromIntFunction_ReportsWarning()
        {
            var code = @"
func get_count() -> int:
    return false
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Should().Contain(d =>
                d.Code == GDDiagnosticCode.IncompatibleReturnType &&
                d.Message.Contains("bool") &&
                d.Message.Contains("int"));
        }

        [TestMethod]
        public void ReturnNothingFromTypedFunction_ReportsWarning()
        {
            var code = @"
func get_value() -> int:
    return
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Should().Contain(d =>
                d.Code == GDDiagnosticCode.IncompatibleReturnType &&
                d.Message.Contains("returns nothing"));
        }

        [TestMethod]
        public void ReturnValueFromVoidFunction_ReportsWarning()
        {
            var code = @"
func do_something() -> void:
    return 42
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Should().Contain(d =>
                d.Code == GDDiagnosticCode.IncompatibleReturnType &&
                d.Message.Contains("void") &&
                d.Message.Contains("int"));
        }

        [TestMethod]
        public void ReturnArrayFromIntFunction_ReportsWarning()
        {
            var code = @"
func get_count() -> int:
    return [1, 2, 3]
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Should().Contain(d =>
                d.Code == GDDiagnosticCode.IncompatibleReturnType &&
                d.Message.Contains("Array") &&
                d.Message.Contains("int"));
        }

        #endregion

        #region Multiple Returns

        [TestMethod]
        public void MultipleReturns_OneIncompatible_ReportsWarningForIncompatibleOnly()
        {
            var code = @"
func get_value(flag: bool) -> int:
    if flag:
        return 42
    else:
        return ""error""
";
            var result = _validator.ValidateCode(code);
            var returnWarnings = result.Warnings
                .Where(d => d.Code == GDDiagnosticCode.IncompatibleReturnType)
                .ToList();

            returnWarnings.Should().HaveCount(1);
            returnWarnings[0].Message.Should().Contain("String");
        }

        [TestMethod]
        public void MultipleReturns_AllCompatible_NoWarning()
        {
            var code = @"
func get_value(flag: bool) -> int:
    if flag:
        return 42
    else:
        return 0
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.IncompatibleReturnType)
                .Should().BeEmpty();
        }

        #endregion

        #region Nested Methods/Lambdas

        [TestMethod]
        public void NestedLambda_ReturnTypeDoesNotAffectOuter()
        {
            // Lambda inside function should not affect the outer function's return type check
            var code = @"
func get_value() -> int:
    var f = func(): return ""hello""
    return 42
";
            var result = _validator.ValidateCode(code);
            // The outer function returns int correctly
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.IncompatibleReturnType)
                .Should().BeEmpty();
        }

        #endregion

        #region Numeric Compatibility

        [TestMethod]
        public void ReturnIntFromFloatFunction_MayBeCompatible()
        {
            // In GDScript, int can be implicitly converted to float
            var code = @"
func get_value() -> float:
    return 42
";
            var result = _validator.ValidateCode(code);
            // This depends on runtime provider behavior - int to float may be allowed
            // For now, we're lenient - no warning expected
            // If stricter checking is wanted, this test should expect a warning
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.IncompatibleReturnType)
                .Should().BeEmpty();
        }

        #endregion
    }
}
