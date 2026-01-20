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
            CheckIndentation = true,
            CheckMemberAccess = true,
            CheckAbstract = true,
            CheckSignals = true,
            CheckResourcePaths = true
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
            CheckIndentation = false,
            CheckMemberAccess = false,
            CheckAbstract = false,
            CheckSignals = false,
            CheckResourcePaths = false
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

    [TestMethod]
    public void ApplyTo_MemberAccessCheck_EnablesFlag()
    {
        // Arrange
        var checks = GDValidationChecks.None;
        var overrides = new GDValidationCheckOverrides { CheckMemberAccess = true };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.MemberAccess).Should().BeTrue();
    }

    [TestMethod]
    public void ApplyTo_MemberAccessCheck_DisablesFlag()
    {
        // Arrange
        var checks = GDValidationChecks.All;
        var overrides = new GDValidationCheckOverrides { CheckMemberAccess = false };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.MemberAccess).Should().BeFalse();
    }

    [TestMethod]
    public void ApplyTo_AbstractCheck_EnablesFlag()
    {
        // Arrange
        var checks = GDValidationChecks.None;
        var overrides = new GDValidationCheckOverrides { CheckAbstract = true };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.Abstract).Should().BeTrue();
    }

    [TestMethod]
    public void ApplyTo_AbstractCheck_DisablesFlag()
    {
        // Arrange
        var checks = GDValidationChecks.All;
        var overrides = new GDValidationCheckOverrides { CheckAbstract = false };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.Abstract).Should().BeFalse();
    }

    [TestMethod]
    public void ApplyTo_SignalsCheck_EnablesFlag()
    {
        // Arrange
        var checks = GDValidationChecks.None;
        var overrides = new GDValidationCheckOverrides { CheckSignals = true };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.Signals).Should().BeTrue();
    }

    [TestMethod]
    public void ApplyTo_SignalsCheck_DisablesFlag()
    {
        // Arrange
        var checks = GDValidationChecks.All;
        var overrides = new GDValidationCheckOverrides { CheckSignals = false };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.Signals).Should().BeFalse();
    }

    [TestMethod]
    public void ApplyTo_ResourcePathsCheck_EnablesFlag()
    {
        // Arrange
        var checks = GDValidationChecks.None;
        var overrides = new GDValidationCheckOverrides { CheckResourcePaths = true };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.ResourcePaths).Should().BeTrue();
    }

    [TestMethod]
    public void ApplyTo_ResourcePathsCheck_DisablesFlag()
    {
        // Arrange
        var checks = GDValidationChecks.All;
        var overrides = new GDValidationCheckOverrides { CheckResourcePaths = false };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.ResourcePaths).Should().BeFalse();
    }

    [TestMethod]
    public void ApplyTo_BasicChecks_EqualsExpectedFlags()
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
        result.Should().Be(GDValidationChecks.Basic);
    }

    [TestMethod]
    public void ApplyTo_AdvancedChecksOnly_AddsCorrectFlags()
    {
        // Arrange
        var checks = GDValidationChecks.None;
        var overrides = new GDValidationCheckOverrides
        {
            CheckMemberAccess = true,
            CheckAbstract = true,
            CheckSignals = true,
            CheckResourcePaths = true
        };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        result.HasFlag(GDValidationChecks.MemberAccess).Should().BeTrue();
        result.HasFlag(GDValidationChecks.Abstract).Should().BeTrue();
        result.HasFlag(GDValidationChecks.Signals).Should().BeTrue();
        result.HasFlag(GDValidationChecks.ResourcePaths).Should().BeTrue();
        // Basic checks should not be set
        result.HasFlag(GDValidationChecks.Syntax).Should().BeFalse();
        result.HasFlag(GDValidationChecks.Scope).Should().BeFalse();
    }
}
