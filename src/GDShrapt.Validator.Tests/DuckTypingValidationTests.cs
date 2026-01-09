using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for duck typing validation (GD7xxx codes).
    /// </summary>
    [TestClass]
    public class DuckTypingValidationTests
    {
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
        }

        private GDValidationOptions DuckTypingOptions => new GDValidationOptions
        {
            CheckDuckTyping = true,
            DuckTypingSeverity = GDDiagnosticSeverity.Warning,
            CheckSyntax = false,
            CheckScope = true, // Required for scope/symbol lookup
            CheckTypes = false,
            CheckCalls = false,
            CheckControlFlow = false,
            CheckIndentation = false
        };

        #region Unguarded Access Tests

        [TestMethod]
        public void UntypedVariable_PropertyAccess_WithoutGuard_Reports()
        {
            var code = @"
func process(obj):
    var x = obj.health
";
            var result = _validator.ValidateCode(code, DuckTypingOptions);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void UntypedVariable_MethodCall_WithoutGuard_Reports()
        {
            var code = @"
func process(obj):
    obj.attack()
";
            var result = _validator.ValidateCode(code, DuckTypingOptions);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedMethodCall)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void TypedVariable_PropertyAccess_NoReport()
        {
            var code = @"
func process(obj: Node2D):
    var x = obj.position
";
            var result = _validator.ValidateCode(code, DuckTypingOptions);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void SelfAccess_NoReport()
        {
            var code = @"
var health = 100

func process():
    var x = self.health
";
            var result = _validator.ValidateCode(code, DuckTypingOptions);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess)
                .Should().BeEmpty();
        }

        #endregion

        #region Type Guard Tests - Is Check

        [TestMethod]
        public void IsCheck_NarrowsType_NoReport()
        {
            var code = @"
func process(obj):
    if obj is Node2D:
        var x = obj.position
";
            var result = _validator.ValidateCode(code, DuckTypingOptions);

            // Inside if with 'is' check, the type is narrowed
            var duckDiags = result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess)
                .ToList();

            // Debug output
            foreach (var d in duckDiags)
            {
                Console.WriteLine($"Unexpected: {d}");
            }

            duckDiags.Should().BeEmpty();
        }

        [TestMethod]
        public void IsCheck_OutsideBranch_StillReports()
        {
            var code = @"
func process(obj):
    if obj is Node2D:
        pass
    obj.position = Vector2.ZERO
";
            var result = _validator.ValidateCode(code, DuckTypingOptions);

            // Outside the if branch, type is not narrowed
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess)
                .Should().NotBeEmpty();
        }

        #endregion

        #region Type Guard Tests - has_method

        [TestMethod]
        public void HasMethod_GuardsMethodCall_NoReport()
        {
            var code = @"
func process(obj):
    if obj.has_method(""attack""):
        obj.attack()
";
            var result = _validator.ValidateCode(code, DuckTypingOptions);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedMethodCall)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void HasMethod_DifferentMethod_StillReports()
        {
            var code = @"
func process(obj):
    if obj.has_method(""attack""):
        obj.defend()
";
            var result = _validator.ValidateCode(code, DuckTypingOptions);

            // has_method("attack") doesn't guard defend()
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedMethodCall)
                .Should().NotBeEmpty();
        }

        #endregion

        #region Compound Conditions

        [TestMethod]
        public void AndCondition_BothGuards_NoReport()
        {
            var code = @"
func process(obj):
    if obj is Entity and obj.has_method(""attack""):
        obj.attack()
";
            var result = _validator.ValidateCode(code, DuckTypingOptions);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedMethodCall)
                .Should().BeEmpty();
        }

        #endregion

        #region Severity Tests

        [TestMethod]
        public void DuckTypingSeverity_Error_ReportsAsError()
        {
            var code = @"
func process(obj):
    obj.health = 100
";
            var options = new GDValidationOptions
            {
                CheckDuckTyping = true,
                DuckTypingSeverity = GDDiagnosticSeverity.Error
            };

            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void DuckTypingSeverity_Hint_ReportsAsHint()
        {
            var code = @"
func process(obj):
    obj.health = 100
";
            var options = new GDValidationOptions
            {
                CheckDuckTyping = true,
                DuckTypingSeverity = GDDiagnosticSeverity.Hint
            };

            var result = _validator.ValidateCode(code, options);

            result.Hints
                .Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess)
                .Should().NotBeEmpty();
        }

        #endregion

        #region Disabled by Default

        [TestMethod]
        public void DefaultOptions_DuckTypingDisabled()
        {
            var code = @"
func process(obj):
    obj.attack()
";
            var result = _validator.ValidateCode(code, GDValidationOptions.Default);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedMethodCall)
                .Should().BeEmpty("duck typing check is disabled by default");
        }

        #endregion
    }
}
