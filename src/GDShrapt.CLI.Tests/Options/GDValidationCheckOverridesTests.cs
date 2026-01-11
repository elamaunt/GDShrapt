using GDShrapt.CLI.Core;
using Xunit;

namespace GDShrapt.CLI.Tests;

public class GDValidationCheckOverridesTests
{
    [Fact]
    public void ApplyTo_EnableSyntax_AddsFlag()
    {
        // Arrange
        var checks = GDValidationChecks.None;
        var overrides = new GDValidationCheckOverrides { CheckSyntax = true };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        Assert.True(result.HasFlag(GDValidationChecks.Syntax));
    }

    [Fact]
    public void ApplyTo_DisableSyntax_RemovesFlag()
    {
        // Arrange
        var checks = GDValidationChecks.All;
        var overrides = new GDValidationCheckOverrides { CheckSyntax = false };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        Assert.False(result.HasFlag(GDValidationChecks.Syntax));
    }

    [Fact]
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
        Assert.True(result.HasFlag(GDValidationChecks.Syntax));
        Assert.True(result.HasFlag(GDValidationChecks.Scope));
        Assert.True(result.HasFlag(GDValidationChecks.Types));
    }

    [Fact]
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
        Assert.False(result.HasFlag(GDValidationChecks.Types));
        Assert.False(result.HasFlag(GDValidationChecks.Calls));
        Assert.False(result.HasFlag(GDValidationChecks.ControlFlow));
        // Others should be preserved
        Assert.True(result.HasFlag(GDValidationChecks.Syntax));
        Assert.True(result.HasFlag(GDValidationChecks.Scope));
        Assert.True(result.HasFlag(GDValidationChecks.Indentation));
    }

    [Fact]
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
        Assert.False(result.HasFlag(GDValidationChecks.Syntax));
        Assert.True(result.HasFlag(GDValidationChecks.Scope)); // Preserved
        Assert.True(result.HasFlag(GDValidationChecks.Types));
        Assert.True(result.HasFlag(GDValidationChecks.Calls));
    }

    [Fact]
    public void ApplyTo_NoOverrides_PreservesOriginalFlags()
    {
        // Arrange
        var checks = GDValidationChecks.Syntax | GDValidationChecks.Scope;
        var overrides = new GDValidationCheckOverrides(); // No overrides set

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert - should be unchanged
        Assert.Equal(checks, result);
    }

    [Fact]
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
        Assert.Equal(GDValidationChecks.All, result);
    }

    [Fact]
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
        Assert.Equal(GDValidationChecks.None, result);
    }

    [Fact]
    public void ApplyTo_ControlFlowCheck_WorksCorrectly()
    {
        // Arrange
        var checks = GDValidationChecks.None;
        var overrides = new GDValidationCheckOverrides { CheckControlFlow = true };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        Assert.True(result.HasFlag(GDValidationChecks.ControlFlow));
    }

    [Fact]
    public void ApplyTo_IndentationCheck_WorksCorrectly()
    {
        // Arrange
        var checks = GDValidationChecks.All;
        var overrides = new GDValidationCheckOverrides { CheckIndentation = false };

        // Act
        var result = overrides.ApplyTo(checks);

        // Assert
        Assert.False(result.HasFlag(GDValidationChecks.Indentation));
    }
}
