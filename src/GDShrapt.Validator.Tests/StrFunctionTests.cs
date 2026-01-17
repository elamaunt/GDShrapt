using FluentAssertions;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for str() function with variadic arguments.
    /// str() in GDScript accepts any number of arguments (varargs).
    /// </summary>
    [TestClass]
    public class StrFunctionTests
    {
        private GDValidator _validator;
        private GDGodotTypesProvider _godotProvider;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
            _godotProvider = new GDGodotTypesProvider();
        }

        [TestMethod]
        public void Str_NoArgs_Valid()
        {
            var code = @"
func test():
    var s = str()
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckCalls = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.WrongArgumentCount && e.Message.Contains("'str'"))
                .Should().BeEmpty("str() should accept 0 arguments");
        }

        [TestMethod]
        public void Str_OneArg_Valid()
        {
            var code = @"
func test():
    var s = str(42)
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckCalls = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.WrongArgumentCount && e.Message.Contains("'str'"))
                .Should().BeEmpty("str() should accept 1 argument");
        }

        [TestMethod]
        public void Str_MultipleArgs_Valid()
        {
            var code = @"
func test():
    var s = str(""Value: "", 42, "" items"")
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckCalls = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.WrongArgumentCount && e.Message.Contains("'str'"))
                .Should().BeEmpty("str() should accept multiple arguments");
        }

        [TestMethod]
        public void Str_ManyArgs_Valid()
        {
            var code = @"
func test():
    var s = str(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckCalls = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.WrongArgumentCount && e.Message.Contains("'str'"))
                .Should().BeEmpty("str() should accept any number of arguments");
        }

        [TestMethod]
        public void Print_MultipleArgs_Valid()
        {
            var code = @"
func test():
    print(""a"", ""b"", ""c"", 1, 2, 3)
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckCalls = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.WrongArgumentCount && e.Message.Contains("'print'"))
                .Should().BeEmpty("print() should accept any number of arguments");
        }

        [TestMethod]
        public void PushError_MultipleArgs_Valid()
        {
            var code = @"
func test():
    push_error(""Error: "", 42, "" at line "", 10)
";
            var options = new GDValidationOptions
            {
                RuntimeProvider = _godotProvider,
                CheckCalls = true
            };
            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(e => e.Code == GDDiagnosticCode.WrongArgumentCount && e.Message.Contains("'push_error'"))
                .Should().BeEmpty("push_error() should accept any number of arguments");
        }

        [TestMethod]
        public void StrFunctionInfo_IsVarArgs()
        {
            var funcInfo = _godotProvider.GetGlobalFunction("str");

            funcInfo.Should().NotBeNull("str function should exist");
            funcInfo.IsVarArgs.Should().BeTrue("str should be a varargs function");
        }

        [TestMethod]
        public void PrintFunctionInfo_IsVarArgs()
        {
            var funcInfo = _godotProvider.GetGlobalFunction("print");

            funcInfo.Should().NotBeNull("print function should exist");
            funcInfo.IsVarArgs.Should().BeTrue("print should be a varargs function");
        }
    }
}
