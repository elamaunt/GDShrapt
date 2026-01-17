using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for member access validation (GD7xxx codes).
    /// These tests use mock analyzers to test the validator behavior.
    /// Real semantic analysis is tested in GDShrapt.Semantics.Tests.
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

        /// <summary>
        /// Mock analyzer that reports all member access as unguarded (NameMatch).
        /// Used to test that the validator correctly reports unguarded access.
        /// </summary>
        private class UnguardedAccessAnalyzer : IGDMemberAccessAnalyzer
        {
            public GDReferenceConfidence GetMemberAccessConfidence(object memberAccess) => GDReferenceConfidence.NameMatch;
            public string? GetExpressionType(object expression) => null;
        }

        /// <summary>
        /// Mock analyzer that reports all member access as guarded (Potential).
        /// Used to test that guarded access is not reported.
        /// </summary>
        private class GuardedAccessAnalyzer : IGDMemberAccessAnalyzer
        {
            public GDReferenceConfidence GetMemberAccessConfidence(object memberAccess) => GDReferenceConfidence.Potential;
            public string? GetExpressionType(object expression) => "Node2D";
        }

        private GDValidationOptions UnguardedOptions => new GDValidationOptions
        {
            CheckMemberAccess = true,
            MemberAccessAnalyzer = new UnguardedAccessAnalyzer(),
            MemberAccessSeverity = GDDiagnosticSeverity.Warning,
            CheckSyntax = false,
            CheckScope = true, // Required for scope/symbol lookup
            CheckTypes = false,
            CheckCalls = false,
            CheckControlFlow = false,
            CheckIndentation = false
        };

        private GDValidationOptions GuardedOptions => new GDValidationOptions
        {
            CheckMemberAccess = true,
            MemberAccessAnalyzer = new GuardedAccessAnalyzer(),
            MemberAccessSeverity = GDDiagnosticSeverity.Warning,
            CheckSyntax = false,
            CheckScope = true,
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
            var result = _validator.ValidateCode(code, UnguardedOptions);

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
            var result = _validator.ValidateCode(code, UnguardedOptions);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedMethodCall)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void TypedVariable_PropertyAccess_WithGuard_NoReport()
        {
            var code = @"
func process(obj: Node2D):
    var x = obj.position
";
            var result = _validator.ValidateCode(code, GuardedOptions);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void SelfAccess_NoReport()
        {
            // self access is always skipped in the validator
            var code = @"
var health = 100

func process():
    var x = self.health
";
            var result = _validator.ValidateCode(code, UnguardedOptions);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess)
                .Should().BeEmpty();
        }

        #endregion

        #region Type Guard Tests - Using Mock Analyzer

        [TestMethod]
        public void GuardedAnalyzer_NarrowsType_NoReport()
        {
            var code = @"
func process(obj):
    if obj is Node2D:
        var x = obj.position
";
            var result = _validator.ValidateCode(code, GuardedOptions);

            // With guarded analyzer, all member access is reported as Potential
            var duckDiags = result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess)
                .ToList();

            duckDiags.Should().BeEmpty();
        }

        [TestMethod]
        public void UnguardedAnalyzer_OutsideBranch_StillReports()
        {
            var code = @"
func process(obj):
    if obj is Node2D:
        pass
    obj.position = Vector2.ZERO
";
            var result = _validator.ValidateCode(code, UnguardedOptions);

            // Unguarded analyzer always reports NameMatch
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess)
                .Should().NotBeEmpty();
        }

        #endregion

        #region Type Guard Tests - has_method

        [TestMethod]
        public void GuardedAnalyzer_MethodCall_NoReport()
        {
            var code = @"
func process(obj):
    if obj.has_method(""attack""):
        obj.attack()
";
            var result = _validator.ValidateCode(code, GuardedOptions);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedMethodCall)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void UnguardedAnalyzer_DifferentMethod_StillReports()
        {
            var code = @"
func process(obj):
    if obj.has_method(""attack""):
        obj.defend()
";
            var result = _validator.ValidateCode(code, UnguardedOptions);

            // Unguarded analyzer reports NameMatch for all access
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedMethodCall)
                .Should().NotBeEmpty();
        }

        #endregion

        #region Compound Conditions

        [TestMethod]
        public void GuardedAnalyzer_CompoundCondition_NoReport()
        {
            var code = @"
func process(obj):
    if obj is Entity and obj.has_method(""attack""):
        obj.attack()
";
            var result = _validator.ValidateCode(code, GuardedOptions);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedMethodCall)
                .Should().BeEmpty();
        }

        #endregion

        #region Severity Tests

        [TestMethod]
        public void MemberAccessSeverity_Error_ReportsAsError()
        {
            var code = @"
func process(obj):
    obj.health = 100
";
            var options = new GDValidationOptions
            {
                CheckMemberAccess = true,
                MemberAccessAnalyzer = new UnguardedAccessAnalyzer(),
                MemberAccessSeverity = GDDiagnosticSeverity.Error
            };

            var result = _validator.ValidateCode(code, options);

            result.Errors
                .Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void MemberAccessSeverity_Hint_ReportsAsHint()
        {
            var code = @"
func process(obj):
    obj.health = 100
";
            var options = new GDValidationOptions
            {
                CheckMemberAccess = true,
                MemberAccessAnalyzer = new UnguardedAccessAnalyzer(),
                MemberAccessSeverity = GDDiagnosticSeverity.Hint
            };

            var result = _validator.ValidateCode(code, options);

            result.Hints
                .Where(d => d.Code == GDDiagnosticCode.UnguardedPropertyAccess)
                .Should().NotBeEmpty();
        }

        #endregion

        #region Disabled by Default

        [TestMethod]
        public void DefaultOptions_MemberAccessDisabled()
        {
            var code = @"
func process(obj):
    obj.attack()
";
            var result = _validator.ValidateCode(code, GDValidationOptions.Default);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedMethodCall)
                .Should().BeEmpty("member access check is disabled by default");
        }

        [TestMethod]
        public void NoAnalyzer_MemberAccessDisabled()
        {
            var code = @"
func process(obj):
    obj.attack()
";
            // Even with CheckMemberAccess = true, without analyzer nothing happens
            var options = new GDValidationOptions
            {
                CheckMemberAccess = true,
                MemberAccessAnalyzer = null
            };

            var result = _validator.ValidateCode(code, options);

            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.UnguardedMethodCall)
                .Should().BeEmpty("member access requires an analyzer to work");
        }

        #endregion
    }
}
