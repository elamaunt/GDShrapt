using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for type validation: operator type checking and type annotation validation.
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

        #region Operator Type Validation

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

        #endregion

        #region Type Annotation Validation (UnknownType)

        [TestMethod]
        public void UnknownType_InVariableAnnotation_ReportsWarning()
        {
            var code = @"
var x: UnknownClass = null
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Should().Contain(d =>
                d.Code == GDDiagnosticCode.UnknownType &&
                d.Message.Contains("UnknownClass"));
        }

        [TestMethod]
        public void UnknownType_InParameterAnnotation_ReportsWarning()
        {
            var code = @"
func test(param: UnknownType) -> void:
    pass
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Should().Contain(d =>
                d.Code == GDDiagnosticCode.UnknownType &&
                d.Message.Contains("UnknownType"));
        }

        [TestMethod]
        public void UnknownType_InReturnType_ReportsWarning()
        {
            var code = @"
func test() -> UnknownReturnType:
    return null
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Should().Contain(d =>
                d.Code == GDDiagnosticCode.UnknownType &&
                d.Message.Contains("UnknownReturnType"));
        }

        [TestMethod]
        public void UnknownType_InLocalVariable_ReportsWarning()
        {
            var code = @"
func test():
    var local: UnknownLocalType = null
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Should().Contain(d =>
                d.Code == GDDiagnosticCode.UnknownType &&
                d.Message.Contains("UnknownLocalType"));
        }

        [TestMethod]
        public void UnknownType_InLambdaParameter_ReportsWarning()
        {
            var code = @"
func test():
    var f = func(x: UnknownLambdaType) -> void: pass
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Should().Contain(d =>
                d.Code == GDDiagnosticCode.UnknownType &&
                d.Message.Contains("UnknownLambdaType"));
        }

        [TestMethod]
        public void KnownPrimitiveType_InAnnotation_NoWarning()
        {
            var code = @"
var x: int = 5
var y: float = 3.14
var s: String = ""hello""
var b: bool = true
var arr: Array = []
var dict: Dictionary = {}
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.UnknownType).Should().BeEmpty();
        }

        [TestMethod]
        public void KnownBuiltInType_InAnnotation_NoWarning()
        {
            var code = @"
var vec: Vector2 = Vector2.ZERO
var node: Node = null
var color: Color = Color.RED
func test(path: NodePath) -> void:
    pass
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.UnknownType).Should().BeEmpty();
        }

        [TestMethod]
        public void Enum_AsType_NoWarning()
        {
            var code = @"
enum MyEnum { A, B, C }
var e: MyEnum = MyEnum.A
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.UnknownType).Should().BeEmpty();
        }

        #endregion

        #region Method Not Found Validation

        [TestMethod]
        public void MethodNotFound_OnGlobalSingleton_ReportsWarning()
        {
            var code = @"
func test():
    Input.nonexistent_method()
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Should().Contain(d =>
                d.Code == GDDiagnosticCode.MethodNotFound &&
                d.Message.Contains("nonexistent_method") &&
                d.Message.Contains("Input"));
        }

        #endregion
    }
}
