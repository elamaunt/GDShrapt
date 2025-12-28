using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for indentation validation.
    /// </summary>
    [TestClass]
    public class IndentationValidationTests
    {
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
        }

        #region Consistent Indentation Tests

        [TestMethod]
        public void Indentation_ConsistentTabs_NoWarning()
        {
            var code = "func test():\n\tvar x = 1\n\tif true:\n\t\tprint(x)\n";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().BeEmpty();
        }

        [TestMethod]
        public void Indentation_ConsistentSpaces_NoWarning()
        {
            var code = "func test():\n    var x = 1\n    if true:\n        print(x)\n";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().BeEmpty();
        }

        [TestMethod]
        public void Indentation_ConsistentTwoSpaces_NoWarning()
        {
            var code = "func test():\n  var x = 1\n  if true:\n    print(x)\n";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().BeEmpty();
        }

        #endregion

        #region Mixed Indentation Tests

        [TestMethod]
        public void Indentation_MixedTabsAndSpaces_ReportsWarning()
        {
            // Line with both tab and space
            var code = "func test():\n\t var x = 1\n";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().NotBeEmpty();
        }

        [TestMethod]
        public void Indentation_SpacesAfterTabs_ReportsWarning()
        {
            var code = "func test():\n\t  var x = 1\n";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().NotBeEmpty();
        }

        [TestMethod]
        public void Indentation_TabsAfterSpaces_ReportsWarning()
        {
            var code = "func test():\n  \tvar x = 1\n";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().NotBeEmpty();
        }

        [TestMethod]
        public void Indentation_TabsThenSpaces_ReportsWarning()
        {
            // File uses tabs, then encounters spaces
            var code = "func test():\n\tvar x = 1\n    var y = 2\n";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().NotBeEmpty();
        }

        [TestMethod]
        public void Indentation_SpacesThenTabs_ReportsWarning()
        {
            // File uses spaces, then encounters tabs
            var code = "func test():\n    var x = 1\n\tvar y = 2\n";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().NotBeEmpty();
        }

        #endregion

        #region Indentation Level Jump Tests

        [TestMethod]
        public void Indentation_JumpTwoLevels_ReportsError()
        {
            // Jumps from level 0 to level 2
            var code = "func test():\n\t\tvar x = 1\n";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UnexpectedIndent).Should().NotBeEmpty();
        }

        [TestMethod]
        public void Indentation_JumpThreeLevels_ReportsError()
        {
            var code = "func test():\n\t\t\tvar x = 1\n";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UnexpectedIndent).Should().NotBeEmpty();
        }

        [TestMethod]
        public void Indentation_GradualIncrease_NoError()
        {
            var code = "func test():\n\tif true:\n\t\tif false:\n\t\t\tprint(1)\n";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UnexpectedIndent).Should().BeEmpty();
        }

        #endregion

        #region Dedent Tests

        [TestMethod]
        public void Indentation_ValidDedent_NoError()
        {
            var code = "func test():\n\tif true:\n\t\tprint(1)\n\tprint(2)\n";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.IndentationMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void Indentation_DedentToZero_NoError()
        {
            var code = "func test():\n\tprint(1)\n\nfunc test2():\n\tprint(2)\n";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.IndentationMismatch).Should().BeEmpty();
        }

        [TestMethod]
        public void Indentation_MultipleDedent_NoError()
        {
            var code = "func test():\n\tif true:\n\t\tif false:\n\t\t\tprint(1)\n\tprint(2)\n";
            var result = _validator.ValidateCode(code);
            result.Errors.Where(d => d.Code == GDDiagnosticCode.IndentationMismatch).Should().BeEmpty();
        }

        #endregion

        #region Validation Options Tests

        [TestMethod]
        public void Indentation_DisabledCheck_NoWarning()
        {
            var code = "func test():\n\t var x = 1\n"; // Mixed tabs and spaces
            var options = new GDValidationOptions { CheckIndentation = false };
            var result = _validator.ValidateCode(code, options);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().BeEmpty();
        }

        [TestMethod]
        public void Indentation_SyntaxOnlyOptions_NoWarning()
        {
            var code = "func test():\n\t var x = 1\n"; // Mixed tabs and spaces
            var result = _validator.ValidateCode(code, GDValidationOptions.SyntaxOnly);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().BeEmpty();
        }

        [TestMethod]
        public void Indentation_NoneOptions_NoWarning()
        {
            var code = "func test():\n\t var x = 1\n"; // Mixed tabs and spaces
            var result = _validator.ValidateCode(code, GDValidationOptions.None);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().BeEmpty();
        }

        #endregion

        #region Complex Cases

        [TestMethod]
        public void Indentation_NestedIfElse_ConsistentTabs_NoWarning()
        {
            var code = @"func test():
	if true:
		print(1)
	elif false:
		print(2)
	else:
		print(3)
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().BeEmpty();
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UnexpectedIndent).Should().BeEmpty();
        }

        [TestMethod]
        public void Indentation_NestedLoops_ConsistentTabs_NoWarning()
        {
            var code = @"func test():
	for i in range(10):
		for j in range(10):
			print(i, j)
		print(i)
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().BeEmpty();
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UnexpectedIndent).Should().BeEmpty();
        }

        [TestMethod]
        public void Indentation_MatchStatement_ConsistentTabs_NoWarning()
        {
            var code = @"func test(x):
	match x:
		1:
			print(""one"")
		2:
			print(""two"")
		_:
			print(""other"")
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().BeEmpty();
            result.Errors.Where(d => d.Code == GDDiagnosticCode.UnexpectedIndent).Should().BeEmpty();
        }

        [TestMethod]
        public void Indentation_MultipleClasses_ConsistentTabs_NoWarning()
        {
            var code = @"class A:
	var x = 1
	func test():
		print(x)

class B:
	var y = 2
	func test():
		print(y)
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().BeEmpty();
        }

        [TestMethod]
        public void Indentation_EmptyLines_NoWarning()
        {
            var code = "func test():\n\tvar x = 1\n\n\tvar y = 2\n";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().BeEmpty();
        }

        [TestMethod]
        public void Indentation_WithComments_NoWarning()
        {
            var code = @"func test():
	# This is a comment
	var x = 1
	# Another comment
	print(x)
";
            var result = _validator.ValidateCode(code);
            result.Warnings.Where(d => d.Code == GDDiagnosticCode.InconsistentIndentation).Should().BeEmpty();
        }

        #endregion

        #region Diagnostic Code Format Tests

        [TestMethod]
        public void DiagnosticCode_InconsistentIndentation_FormatsCorrectly()
        {
            GDDiagnosticCode.InconsistentIndentation.ToCodeString().Should().Be("GD6001");
        }

        [TestMethod]
        public void DiagnosticCode_UnexpectedIndent_FormatsCorrectly()
        {
            GDDiagnosticCode.UnexpectedIndent.ToCodeString().Should().Be("GD6002");
        }

        [TestMethod]
        public void DiagnosticCode_ExpectedIndent_FormatsCorrectly()
        {
            GDDiagnosticCode.ExpectedIndent.ToCodeString().Should().Be("GD6003");
        }

        [TestMethod]
        public void DiagnosticCode_UnexpectedDedent_FormatsCorrectly()
        {
            GDDiagnosticCode.UnexpectedDedent.ToCodeString().Should().Be("GD6004");
        }

        [TestMethod]
        public void DiagnosticCode_IndentationMismatch_FormatsCorrectly()
        {
            GDDiagnosticCode.IndentationMismatch.ToCodeString().Should().Be("GD6005");
        }

        #endregion
    }
}
