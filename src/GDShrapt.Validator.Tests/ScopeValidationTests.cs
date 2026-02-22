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
        public void DictionaryKeys_AssignSyntax_NoUndefinedError()
        {
            var code = @"
func test():
    var d = {id = ""0"", name = ""test""}
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && (d.Message.Contains("'id'") || d.Message.Contains("'name'"))).Should().BeEmpty();
        }

        [TestMethod]
        public void DictionaryKeys_NestedDictionary_NoUndefinedError()
        {
            var code = @"
func test():
    var d = {inner = {key = ""val""}}
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && (d.Message.Contains("'inner'") || d.Message.Contains("'key'"))).Should().BeEmpty();
        }

        [TestMethod]
        public void DictionaryKeys_MixedWithVariables_OnlyReportsUndefined()
        {
            var code = @"
func test():
    var val = 1
    var d = {key = val, name = undef}
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.Message.Contains("'key'")).Should().BeEmpty("dictionary key 'key' should not be reported");
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.Message.Contains("'name'")).Should().BeEmpty("dictionary key 'name' should not be reported");
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.Message.Contains("'val'")).Should().BeEmpty("declared variable 'val' should not be reported");
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.Message.Contains("'undef'")).Should().NotBeEmpty("undefined variable 'undef' should still be reported");
        }

        [TestMethod]
        public void StringLiteral_NoUndefinedIdentifier()
        {
            var code = @"
func test():
    var x = ""some_identifier""
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.Message.Contains("'some_identifier'")).Should().BeEmpty();
        }

        [TestMethod]
        public void MultilineString_NoUndefinedIdentifier()
        {
            var code = "func test():\n\tvar x = \"if some_var\\nprint(y)\"";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.Message.Contains("'some_var'")).Should().BeEmpty();
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.Message.Contains("'y'")).Should().BeEmpty();
        }

        [TestMethod]
        public void StringWithVariableReference_StillValidates()
        {
            var code = @"
func test():
    var x = 1
    print(""value: "" + str(x))
";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable).Should().BeEmpty();
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

        [TestMethod]
        public void TypedForLoop_VariableInScope_NoError()
        {
            var code = "func test():\n\tfor path: String in files:\n\t\tprint(path)\n";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UndefinedVariable && d.Message.Contains("path")).Should().BeEmpty();
            result.Errors.Where(d => d.Code == GDDiagnosticCode.InvalidToken).Should().BeEmpty();
        }

        [TestMethod]
        public void TypedForLoop_NoInvalidToken()
        {
            var code = "func test():\n\tfor x: int in range(10):\n\t\tprint(x)\n";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.InvalidToken).Should().BeEmpty();
        }

        [TestMethod]
        public void TypedForLoop_TwoLoops_SameVarName_NoDuplicate()
        {
            var code = "func test():\n\tfor node: Node in a:\n\t\tpass\n\tfor node: Node in b:\n\t\tpass\n";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.DuplicateDeclaration && d.Message.Contains("node")).Should().BeEmpty();
        }
    }
}
