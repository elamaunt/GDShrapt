using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for scope validation: variable declarations, duplicates, undefined identifiers.
    /// </summary>
    [TestClass]
    public class ScopeValidationTests
    {
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
        }

        [TestMethod]
        public void DeclaredVariable_NoError()
        {
            var code = @"
func test():
    var x = 10
    print(x)
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().BeEmpty();
        }

        [TestMethod]
        public void UndeclaredVariable_ReportsError()
        {
            var code = @"
func test():
    print(undefined_var)
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().NotBeEmpty();
        }

        [TestMethod]
        public void BuiltInIdentifiers_NoError()
        {
            var code = @"
func test():
    print(self)
    print(null)
    print(true)
    print(false)
    print(PI)
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().BeEmpty();
        }

        [TestMethod]
        public void ForLoopIterator_NoError()
        {
            var code = @"
func test():
    for i in range(10):
        print(i)
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.Message.Contains("'i'")).Should().BeEmpty();
        }

        [TestMethod]
        public void DuplicateVariable_ReportsError()
        {
            var code = @"
func test():
    var x = 10
    var x = 20
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.DuplicateDeclaration).Should().NotBeEmpty();
        }

        [TestMethod]
        public void DuplicateMethod_ReportsError()
        {
            var code = @"
func test():
    pass

func test():
    pass
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.DuplicateDeclaration).Should().NotBeEmpty();
        }

        [TestMethod]
        public void MethodParameter_NoError()
        {
            var code = @"
func test(x, y, z):
    print(x)
    print(y)
    print(z)
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().BeEmpty();
        }

        [TestMethod]
        public void ClassMemberVariable_NoError()
        {
            var code = @"
var class_var = 10

func test():
    print(class_var)
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.Message.Contains("class_var")).Should().BeEmpty();
        }

        [TestMethod]
        public void Signal_NoError()
        {
            var code = @"
signal my_signal

func test():
    my_signal.emit()
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.Message.Contains("my_signal")).Should().BeEmpty();
        }

        [TestMethod]
        public void EnumValue_NoError()
        {
            var code = @"
enum State { IDLE, RUNNING, STOPPED }

func test():
    var s = IDLE
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.Message.Contains("IDLE")).Should().BeEmpty();
        }

        [TestMethod]
        public void LambdaParameters_NoError()
        {
            var code = @"
func test():
    var f = func(x, y): return x + y
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().BeEmpty();
        }

        [TestMethod]
        public void NotInOperator_NoError()
        {
            var code = @"
func test():
    var arr = [1, 2, 3]
    if 5 not in arr:
        print(""not found"")
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.InvalidNotKeywordUsage).Should().BeEmpty();
        }

        [TestMethod]
        public void InOperator_NoError()
        {
            var code = @"
func test():
    var arr = [1, 2, 3]
    if 5 in arr:
        print(""found"")
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.InvalidNotKeywordUsage).Should().BeEmpty();
        }

        [TestMethod]
        public void NotInOperator_NoDuplicateDiagnostics()
        {
            var code = @"
func test():
    var arr = [1, 2, 3]
    if 5 not in arr:
        print(""not found"")
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.InvalidNotKeywordUsage).Should().BeEmpty();
            result.Errors.Where(d => d.Code == GDDiagnosticCode.InvalidToken && d.Message.Contains("not")).Should().BeEmpty("'not' keyword should not produce InvalidToken GD1001");
        }
    }
}
