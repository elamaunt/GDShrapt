using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for argument type validation using GDSemanticModel as IGDArgumentTypeAnalyzer.
    /// Tests verify that type mismatches are properly detected and reported with detailed messages.
    /// </summary>
    [TestClass]
    public class ArgumentTypeValidationTests
    {
        private GDValidator _validator;
        private GDScriptReader _reader;
        private IGDRuntimeProvider _runtimeProvider;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
            _reader = new GDScriptReader();
            _runtimeProvider = new GDGodotTypesProvider();
        }

        /// <summary>
        /// Helper method to create a script file from code.
        /// </summary>
        private GDScriptFile CreateScriptFile(string code)
        {
            var reference = new GDScriptReference("test://virtual/test_script.gd");
            var scriptFile = new GDScriptFile(reference);
            scriptFile.Reload(code);
            return scriptFile;
        }

        /// <summary>
        /// Helper method to validate code with argument type checking enabled.
        /// </summary>
        private GDValidationResult ValidateWithArgumentTypes(string code)
        {
            var scriptFile = CreateScriptFile(code);
            var semanticModel = GDSemanticModel.Create(scriptFile, _runtimeProvider);

            var options = new GDValidationOptions
            {
                CheckArgumentTypes = true,
                ArgumentTypeAnalyzer = semanticModel,
                ArgumentTypeSeverity = GDDiagnosticSeverity.Warning,
                RuntimeProvider = _runtimeProvider
            };

            return _validator.Validate(scriptFile.Class, options);
        }

        #region Debug Tests

        [TestMethod]
        public void SemanticModel_GetAllArgumentTypeDiffs_ReturnsData()
        {
            // Debug test: verify that the semantic model returns type diffs
            var code = @"
func f(x: int) -> void:
    pass

func test():
    f(""hello"")
";
            var scriptFile = CreateScriptFile(code);
            var semanticModel = GDSemanticModel.Create(scriptFile, _runtimeProvider);

            // Find the call expression
            var callExpr = scriptFile.Class!.AllNodes.OfType<GDCallExpression>()
                .FirstOrDefault(c => c.CallerExpression is GDIdentifierExpression id && id.Identifier?.Sequence == "f");

            callExpr.Should().NotBeNull("Should find call to f()");

            // Get the argument type diffs
            var diffs = ((IGDArgumentTypeAnalyzer)semanticModel).GetAllArgumentTypeDiffs(callExpr!).ToList();

            // Debug info
            if (diffs.Count == 0)
            {
                Assert.Fail("GetAllArgumentTypeDiffs returned no diffs - check ResolveCalledMethod and GetExpressionType");
            }

            var diff = diffs.First();
            diff.Should().NotBeNull();
            diff.ActualType.Should().Be("String", "Argument type should be String");
            diff.ExpectedTypes.Should().Contain("int", "Expected type should be int");
            diff.IsCompatible.Should().BeFalse("String is not compatible with int");
        }

        #endregion

        #region Basic Type Mismatch Tests

        [TestMethod]
        public void BasicTypeMismatch_IntVsString()
        {
            var code = @"
func f(x: int) -> void:
    pass

func test():
    f(""hello"")
";
            var result = ValidateWithArgumentTypes(code);

            // ArgumentTypeMismatch is reported as Warning by default
            var allDiagnostics = result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).ToList();
            allDiagnostics.Should().NotBeEmpty("ArgumentTypeMismatch should be reported");
            var error = allDiagnostics.First();
            error.Message.Should().Contain("String").And.Contain("int");
        }

        [TestMethod]
        public void BasicTypeMismatch_StringVsInt()
        {
            var code = @"
func f(x: String) -> void:
    pass

func test():
    f(42)
";
            var result = ValidateWithArgumentTypes(code);
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().NotBeEmpty();
            var error = result.Diagnostics.First(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
            error.Message.Should().Contain("int").And.Contain("String");
        }

        [TestMethod]
        public void CompatibleTypes_SameType()
        {
            var code = @"
func f(x: int) -> void:
    pass

func test():
    f(42)
";
            var result = ValidateWithArgumentTypes(code);
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void CompatibleTypes_IntToFloat()
        {
            var code = @"
func f(x: float) -> void:
    pass

func test():
    f(42)
";
            var result = ValidateWithArgumentTypes(code);
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void NullToTyped_IsAllowed()
        {
            var code = @"
func f(x: Node2D) -> void:
    pass

func test():
    f(null)
";
            var result = ValidateWithArgumentTypes(code);
            // null should be assignable to any reference type
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void VariantParameter_NoCheck()
        {
            var code = @"
func f(x) -> void:
    pass

func test():
    f(""any"")
    f(42)
    f(null)
";
            var result = ValidateWithArgumentTypes(code);
            // Variant parameters should not be checked
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        #endregion

        #region Subtype Compatibility Tests

        [TestMethod]
        public void CompatibleTypes_SubtypeOk()
        {
            // Using typed variable instead of inferred from .new() to avoid hang
            var code = @"
extends Node

func f(x: Node) -> void:
    pass

func test():
    var child: Node2D = null
    f(child)
";
            var result = ValidateWithArgumentTypes(code);
            // Node2D extends Node, so should be compatible
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void IncompatibleTypes_ParentToChild()
        {
            // Simplified test - using typed variable instead of inferred from .new()
            var code = @"
extends Node

func f(x: Node2D) -> void:
    pass

func test():
    var parent: Node = null
    f(parent)
";
            var result = ValidateWithArgumentTypes(code);
            // Node does NOT extend Node2D, so should be incompatible
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().NotBeEmpty();
        }

        #endregion

        #region Optional Parameter Tests

        [TestMethod]
        public void OptionalParam_DefaultUsed()
        {
            var code = @"
func f(x: int = 0) -> void:
    pass

func test():
    f()
";
            var result = ValidateWithArgumentTypes(code);
            // When default is used, no argument type check needed
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void OptionalParam_WrongType()
        {
            var code = @"
func f(x: int = 0) -> void:
    pass

func test():
    f(""a"")
";
            var result = ValidateWithArgumentTypes(code);
            // Even though param is optional, wrong type should still be reported
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().NotBeEmpty();
        }

        #endregion

        #region User Function Call Tests

        [TestMethod]
        public void UserFunction_TypedParam_Valid()
        {
            var code = @"
func process_number(n: int) -> int:
    return n * 2

func test():
    var result = process_number(10)
";
            var result = ValidateWithArgumentTypes(code);
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void UserFunction_TypedParam_Invalid()
        {
            var code = @"
func process_number(n: int) -> int:
    return n * 2

func test():
    var result = process_number(""not a number"")
";
            var result = ValidateWithArgumentTypes(code);
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().NotBeEmpty();
        }

        [TestMethod]
        public void UserFunction_MultipleParams_MixedValid()
        {
            var code = @"
func calculate(a: int, b: int, name: String) -> void:
    pass

func test():
    calculate(1, 2, ""result"")
";
            var result = ValidateWithArgumentTypes(code);
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void UserFunction_MultipleParams_SecondInvalid()
        {
            var code = @"
func calculate(a: int, b: int, name: String) -> void:
    pass

func test():
    calculate(1, ""two"", ""result"")
";
            var result = ValidateWithArgumentTypes(code);
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().NotBeEmpty();
            var error = result.Diagnostics.First(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
            // Should mention position 2 or parameter 'b'
            (error.Message.Contains("position 2") || error.Message.Contains("'b'")).Should().BeTrue();
        }

        #endregion

        #region Built-in Function Call Tests

        [TestMethod]
        public void BuiltinFunction_ClampValid()
        {
            var code = @"
func test():
    var x = clamp(5, 0, 10)
";
            var result = ValidateWithArgumentTypes(code);
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void BuiltinFunction_ClampInvalid()
        {
            var code = @"
func test():
    var x = clamp(""five"", 0, 10)
";
            var result = ValidateWithArgumentTypes(code);
            // clamp expects numeric types
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().NotBeEmpty();
        }

        [TestMethod]
        public void BuiltinFunction_StrValid()
        {
            var code = @"
func test():
    var s = str(42)
    var s2 = str(3.14)
    var s3 = str(true)
";
            var result = ValidateWithArgumentTypes(code);
            // str() accepts Variant, should accept anything
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        #endregion

        #region Method Call Tests

        [TestMethod]
        public void MethodCall_AddChildValid()
        {
            // Using typed variable instead of inferred from .new() to avoid hang
            var code = @"
extends Node

func test():
    var child: Node2D = null
    add_child(child)
";
            var result = ValidateWithArgumentTypes(code);
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void MethodCall_AddChildInvalid()
        {
            var code = @"
extends Node

func test():
    add_child(""not a node"")
";
            var result = ValidateWithArgumentTypes(code);
            // add_child expects Node, String is not compatible
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().NotBeEmpty();
        }

        #endregion

        #region Self Method Call Tests

        [TestMethod]
        public void SelfMethodCall_Valid()
        {
            var code = @"
extends Node

func my_func(value: int) -> void:
    pass

func test():
    self.my_func(42)
";
            var result = ValidateWithArgumentTypes(code);
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void SelfMethodCall_Invalid()
        {
            var code = @"
extends Node

func my_func(value: int) -> void:
    pass

func test():
    self.my_func(""not an int"")
";
            var result = ValidateWithArgumentTypes(code);
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().NotBeEmpty();
        }

        #endregion

        #region Type Guard Tests

        [TestMethod]
        public void TypeGuard_NarrowsExpected()
        {
            var code = @"
func f(x) -> void:
    if x is int:
        var doubled = x * 2

func test():
    f(42)
";
            var result = ValidateWithArgumentTypes(code);
            // Type guard should narrow expected type to int
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void TypeGuard_MultipleGuards()
        {
            var code = @"
func f(x) -> void:
    if x is int:
        pass
    elif x is String:
        pass

func test():
    f(42)
    f(""hello"")
";
            var result = ValidateWithArgumentTypes(code);
            // Both int and String should be valid
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        #endregion

        #region Detailed Message Tests

        [TestMethod]
        public void DetailedMessage_ContainsActualType()
        {
            var code = @"
func f(x: int) -> void:
    pass

func test():
    f(""hello"")
";
            var result = ValidateWithArgumentTypes(code);
            var error = result.Diagnostics.FirstOrDefault(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
            error.Should().NotBeNull();
            error!.Message.Should().Contain("String");
        }

        [TestMethod]
        public void DetailedMessage_ContainsExpectedType()
        {
            var code = @"
func f(x: int) -> void:
    pass

func test():
    f(""hello"")
";
            var result = ValidateWithArgumentTypes(code);
            var error = result.Diagnostics.FirstOrDefault(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
            error.Should().NotBeNull();
            error!.Message.Should().Contain("int");
        }

        [TestMethod]
        public void DetailedMessage_ContainsParameterName()
        {
            var code = @"
func attack(target: Node2D, damage: int) -> void:
    pass

func test():
    attack(""enemy"", 10)
";
            var result = ValidateWithArgumentTypes(code);
            var error = result.Diagnostics.FirstOrDefault(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
            error.Should().NotBeNull();
            // Should contain either parameter name 'target' or position info
            (error!.Message.Contains("target") || error.Message.Contains("position 1")).Should().BeTrue();
        }

        #endregion

        #region Edge Case Tests

        [TestMethod]
        public void EdgeCase_NoArgumentsPassed()
        {
            var code = @"
func f(x: int) -> void:
    pass

func test():
    f()
";
            // This should be caught by WrongArgumentCount, not ArgumentTypeMismatch
            var result = ValidateWithArgumentTypes(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.WrongArgumentCount).Should().NotBeEmpty();
        }

        [TestMethod]
        public void EdgeCase_ExtraArgumentsPassed()
        {
            var code = @"
func f(x: int) -> void:
    pass

func test():
    f(1, 2, 3)
";
            // This should be caught by WrongArgumentCount, not ArgumentTypeMismatch
            var result = ValidateWithArgumentTypes(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.WrongArgumentCount).Should().NotBeEmpty();
        }

        [TestMethod]
        public void EdgeCase_UnknownFunctionCall()
        {
            var code = @"
func test():
    unknown_function(42)
";
            // Unknown function - should not cause ArgumentTypeMismatch
            var result = ValidateWithArgumentTypes(code);
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void EdgeCase_ChainedMethodCall()
        {
            var code = @"
extends Node

func test():
    get_node(""Player"").set_position(Vector2.ZERO)
";
            var result = ValidateWithArgumentTypes(code);
            // Should not crash on chained calls
            result.Should().NotBeNull();
        }

        #endregion

        #region Severity Tests

        [TestMethod]
        public void Severity_DefaultIsWarning()
        {
            var code = @"
func f(x: int) -> void:
    pass

func test():
    f(""hello"")
";
            var result = ValidateWithArgumentTypes(code);
            var diagnostic = result.Diagnostics.FirstOrDefault(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
            diagnostic.Should().NotBeNull();
            diagnostic!.Severity.Should().Be(GDDiagnosticSeverity.Warning);
        }

        [TestMethod]
        public void Severity_CanBeConfiguredAsError()
        {
            var code = @"
func f(x: int) -> void:
    pass

func test():
    f(""hello"")
";
            var scriptFile = CreateScriptFile(code);
            var semanticModel = GDSemanticModel.Create(scriptFile, _runtimeProvider);

            var options = new GDValidationOptions
            {
                CheckArgumentTypes = true,
                ArgumentTypeAnalyzer = semanticModel,
                ArgumentTypeSeverity = GDDiagnosticSeverity.Error,
                RuntimeProvider = _runtimeProvider
            };

            var result = _validator.Validate(scriptFile.Class, options);
            var error = result.Errors.FirstOrDefault(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch);
            error.Should().NotBeNull();
            error!.Severity.Should().Be(GDDiagnosticSeverity.Error);
        }

        #endregion

        #region Disabled Validation Tests

        [TestMethod]
        public void DisabledValidation_NoErrors()
        {
            var code = @"
func f(x: int) -> void:
    pass

func test():
    f(""hello"")
";
            // Validate without argument type checking
            var result = _validator.ValidateCode(code);
            result.Diagnostics.Where(d => d.Code == GDDiagnosticCode.ArgumentTypeMismatch).Should().BeEmpty();
        }

        #endregion
    }
}
