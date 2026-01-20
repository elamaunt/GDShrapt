using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDValidateCommandTests
{
    private string? _tempProjectPath;

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempProjectPath != null)
        {
            TestProjectHelper.DeleteTempProject(_tempProjectPath);
        }
    }

    // === Exit code tests ===

    [TestMethod]
    public async Task ExecuteAsync_CleanProject_ReturnsZero()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateCleanProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDValidateCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInvalidPath_ReturnsFatal()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDValidateCommand("/nonexistent/path", formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        // Exit code 3 = Fatal (project not found)
        result.Should().Be(3);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithErrors_ReturnsTwo()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithBreakOutsideLoop();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDValidateCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        // Exit code 2 = Errors found
        result.Should().Be(2);
    }

    // === CheckControlFlow flag tests ===

    [TestMethod]
    public async Task ExecuteAsync_BreakOutsideLoop_CheckControlFlowEnabled_ReportsError()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithBreakOutsideLoop();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDValidateCommand(
            _tempProjectPath,
            formatter,
            output,
            checks: GDValidationChecks.ControlFlow);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(2); // Errors
        output.ToString().Should().Contain("GD5001"); // BreakOutsideLoop
    }

    [TestMethod]
    public async Task ExecuteAsync_BreakOutsideLoop_CheckControlFlowDisabled_NoError()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithBreakOutsideLoop();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        // All checks except ControlFlow
        var checks = GDValidationChecks.Syntax | GDValidationChecks.Scope;
        var command = new GDValidateCommand(
            _tempProjectPath,
            formatter,
            output,
            checks: checks);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        // Should not report GD5001 when ControlFlow is disabled
        output.ToString().Should().NotContain("GD5001");
    }

    // === CheckAbstract flag tests ===

    [TestMethod]
    public async Task ExecuteAsync_MissingAbstractClass_CheckAbstractEnabled_ReportsError()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithMissingAbstractClass();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDValidateCommand(
            _tempProjectPath,
            formatter,
            output,
            checks: GDValidationChecks.Abstract);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(2); // Errors
        output.ToString().Should().Contain("GD8002"); // ClassNotAbstract
    }

    [TestMethod]
    public async Task ExecuteAsync_MissingAbstractClass_CheckAbstractDisabled_NoError()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithMissingAbstractClass();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        // Only basic checks, no Abstract
        var checks = GDValidationChecks.Syntax | GDValidationChecks.Scope;
        var command = new GDValidateCommand(
            _tempProjectPath,
            formatter,
            output,
            checks: checks);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        // Should not report GD8002 when Abstract is disabled
        output.ToString().Should().NotContain("GD8002");
    }

    // === CheckSyntax flag tests ===

    [TestMethod]
    public async Task ExecuteAsync_SyntaxError_CheckSyntaxEnabled_ReportsError()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithSyntaxError();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDValidateCommand(
            _tempProjectPath,
            formatter,
            output,
            checks: GDValidationChecks.Syntax);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(2); // Errors
        // Should contain either GD0001 or GD0002 (parse errors)
        var outputText = output.ToString();
        (outputText.Contains("GD0001") || outputText.Contains("GD0002")).Should().BeTrue();
    }

    [TestMethod]
    public async Task ExecuteAsync_SyntaxError_CheckSyntaxDisabled_NoSyntaxError()
    {
        // Arrange
        _tempProjectPath = TestProjectHelper.CreateProjectWithSyntaxError();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        // Only scope check, no Syntax flag
        var command = new GDValidateCommand(
            _tempProjectPath,
            formatter,
            output,
            checks: GDValidationChecks.Scope);

        // Act
        await command.ExecuteAsync();

        // Assert
        // Invalid tokens (GD0002) should NOT be reported when CheckSyntax is disabled
        var outputText = output.ToString();
        outputText.Should().NotContain("GD0002");
    }

    // === Basic vs All checks ===

    [TestMethod]
    public async Task ExecuteAsync_BasicChecks_OnlyRunsBasicValidation()
    {
        // Arrange - project with abstract error (not in Basic checks)
        _tempProjectPath = TestProjectHelper.CreateProjectWithMissingAbstractClass();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDValidateCommand(
            _tempProjectPath,
            formatter,
            output,
            checks: GDValidationChecks.Basic);

        // Act
        await command.ExecuteAsync();

        // Assert
        // Abstract errors should not be reported with Basic checks
        output.ToString().Should().NotContain("GD8002");
    }

    [TestMethod]
    public async Task ExecuteAsync_AllChecks_RunsAllValidation()
    {
        // Arrange - project with abstract error
        _tempProjectPath = TestProjectHelper.CreateProjectWithMissingAbstractClass();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDValidateCommand(
            _tempProjectPath,
            formatter,
            output,
            checks: GDValidationChecks.All);

        // Act
        await command.ExecuteAsync();

        // Assert
        // Abstract errors should be reported with All checks
        output.ToString().Should().Contain("GD8002");
    }
}
