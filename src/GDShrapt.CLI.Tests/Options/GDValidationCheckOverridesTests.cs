using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDValidationCheckOverridesTests
{
    [TestMethod]
    public void ApplyTo_EnableSyntax_AddsFlag()
    {
        // Arrange
        var checks = GDValidationChecks.None;
        var overrides = new GDValidationCheckOverrides { CheckSyntax = true };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.Syntax).Should().BeTrue();
    }

    [TestMethod]
    public void ApplyTo_DisableSyntax_RemovesFlag()
    {
        // Arrange
        var checks = GDValidationChecks.All;
        var overrides = new GDValidationCheckOverrides { CheckSyntax = false };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.Syntax).Should().BeFalse();
    }

    [TestMethod]
    public void ApplyTo_EnableMultipleChecks_AddsFlags()
    {
        // Arrange
        var checks = GDValidationChecks.None;
        var overrides = new GDValidationCheckOverrides
        {
            CheckSyntax = true,
            CheckScope = true,
            CheckTypes = true
        };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.Syntax).Should().BeTrue();
        result.HasFlag(GDValidationChecks.Scope).Should().BeTrue();
        result.HasFlag(GDValidationChecks.Types).Should().BeTrue();
    }

    [TestMethod]
    public void ApplyTo_DisableMultipleChecks_RemovesFlags()
    {
        // Arrange
        var checks = GDValidationChecks.All;
        var overrides = new GDValidationCheckOverrides
        {
            CheckTypes = false,
            CheckCalls = false,
            CheckControlFlow = false
        };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.Types).Should().BeFalse();
        result.HasFlag(GDValidationChecks.Calls).Should().BeFalse();
        result.HasFlag(GDValidationChecks.ControlFlow).Should().BeFalse();
        // Others should be preserved
        result.HasFlag(GDValidationChecks.Syntax).Should().BeTrue();
        result.HasFlag(GDValidationChecks.Scope).Should().BeTrue();
        result.HasFlag(GDValidationChecks.Indentation).Should().BeTrue();
    }

    [TestMethod]
    public void ApplyTo_MixedEnableDisable_CorrectlyModifiesFlags()
    {
        // Arrange
        var checks = GDValidationChecks.Syntax | GDValidationChecks.Scope;
        var overrides = new GDValidationCheckOverrides
        {
            CheckSyntax = false,  // Remove
            CheckTypes = true,     // Add
            CheckCalls = true      // Add
        };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.Syntax).Should().BeFalse();
        result.HasFlag(GDValidationChecks.Scope).Should().BeTrue(); // Preserved
        result.HasFlag(GDValidationChecks.Types).Should().BeTrue();
        result.HasFlag(GDValidationChecks.Calls).Should().BeTrue();
    }

    [TestMethod]
    public void ApplyTo_NoOverrides_PreservesOriginalFlags()
    {
        // Arrange
        var checks = GDValidationChecks.Syntax | GDValidationChecks.Scope;
        var overrides = new GDValidationCheckOverrides(); // No overrides set

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert - should be unchanged
        result.Should().Be(checks);
    }

    [TestMethod]
    public void ApplyTo_AllChecksEnabled_ReturnsAllFlags()
    {
        // Arrange
        var checks = GDValidationChecks.None;
        var overrides = new GDValidationCheckOverrides
        {
            CheckSyntax = true,
            CheckScope = true,
            CheckTypes = true,
            CheckCalls = true,
            CheckControlFlow = true,
            CheckIndentation = true
        };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.Should().Be(GDValidationChecks.All);
    }

    [TestMethod]
    public void ApplyTo_AllChecksDisabled_ReturnsNone()
    {
        // Arrange
        var checks = GDValidationChecks.All;
        var overrides = new GDValidationCheckOverrides
        {
            CheckSyntax = false,
            CheckScope = false,
            CheckTypes = false,
            CheckCalls = false,
            CheckControlFlow = false,
            CheckIndentation = false
        };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.Should().Be(GDValidationChecks.None);
    }

    [TestMethod]
    public void ApplyTo_ControlFlowCheck_WorksCorrectly()
    {
        // Arrange
        var checks = GDValidationChecks.None;
        var overrides = new GDValidationCheckOverrides { CheckControlFlow = true };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.ControlFlow).Should().BeTrue();
    }

    [TestMethod]
    public void ApplyTo_IndentationCheck_WorksCorrectly()
    {
        // Arrange
        var checks = GDValidationChecks.All;
        var overrides = new GDValidationCheckOverrides { CheckIndentation = false };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.Indentation).Should().BeFalse();
    }
}
