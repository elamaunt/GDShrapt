using GDShrapt.CLI.Core;
using GDShrapt.Reader;
using Xunit;

namespace GDShrapt.CLI.Tests;

public class GDLinterOptionsOverridesTests
{
    [Fact]
    public void ApplyTo_NamingConventions_OverridesValues()
    {
        // Arrange
        var options = new GDLinterOptions();
        var overrides = new GDLinterOptionsOverrides
        {
            ClassNameCase = NamingCase.SnakeCase,
            FunctionNameCase = NamingCase.PascalCase,
            VariableNameCase = NamingCase.CamelCase,
            ConstantNameCase = NamingCase.PascalCase
        };

        // Act
        overrides.ApplyTo(options);

        // Assert
        Assert.Equal(NamingCase.SnakeCase, options.ClassNameCase);
        Assert.Equal(NamingCase.PascalCase, options.FunctionNameCase);
        Assert.Equal(NamingCase.CamelCase, options.VariableNameCase);
        Assert.Equal(NamingCase.PascalCase, options.ConstantNameCase);
    }

    [Fact]
    public void ApplyTo_Limits_OverridesValues()
    {
        // Arrange
        var options = new GDLinterOptions();
        var overrides = new GDLinterOptionsOverrides
        {
            MaxLineLength = 80,
            MaxFileLines = 500,
            MaxParameters = 3,
            MaxFunctionLength = 25,
            MaxCyclomaticComplexity = 5
        };

        // Act
        overrides.ApplyTo(options);

        // Assert
        Assert.Equal(80, options.MaxLineLength);
        Assert.Equal(500, options.MaxFileLines);
        Assert.Equal(3, options.MaxParameters);
        Assert.Equal(25, options.MaxFunctionLength);
        Assert.Equal(5, options.MaxCyclomaticComplexity);
    }

    [Fact]
    public void ApplyTo_Warnings_OverridesValues()
    {
        // Arrange
        var options = new GDLinterOptions();
        var overrides = new GDLinterOptionsOverrides
        {
            WarnUnusedVariables = false,
            WarnNoElifReturn = true,
            WarnNoElseReturn = true,
            WarnPrivateMethodCall = true,
            WarnDuplicatedLoad = false
        };

        // Act
        overrides.ApplyTo(options);

        // Assert
        Assert.False(options.WarnUnusedVariables);
        Assert.True(options.WarnNoElifReturn);
        Assert.True(options.WarnNoElseReturn);
        Assert.True(options.WarnPrivateMethodCall);
        Assert.False(options.WarnDuplicatedLoad);
    }

    [Fact]
    public void ApplyTo_StrictTyping_OverridesValues()
    {
        // Arrange
        var options = new GDLinterOptions();
        var overrides = new GDLinterOptionsOverrides
        {
            StrictTypingClassVariables = GDLintSeverity.Error,
            StrictTypingLocalVariables = GDLintSeverity.Warning,
            StrictTypingParameters = GDLintSeverity.Error,
            StrictTypingReturnTypes = GDLintSeverity.Warning
        };

        // Act
        overrides.ApplyTo(options);

        // Assert
        Assert.Equal(GDLintSeverity.Error, options.StrictTypingClassVariables);
        Assert.Equal(GDLintSeverity.Warning, options.StrictTypingLocalVariables);
        Assert.Equal(GDLintSeverity.Error, options.StrictTypingParameters);
        Assert.Equal(GDLintSeverity.Warning, options.StrictTypingReturnTypes);
    }

    [Fact]
    public void ApplyTo_NullOverrides_PreservesOriginalValues()
    {
        // Arrange
        var options = new GDLinterOptions
        {
            MaxLineLength = 100,
            WarnUnusedVariables = true
        };
        var overrides = new GDLinterOptionsOverrides
        {
            // Only override one property
            MaxFileLines = 200
        };

        // Act
        overrides.ApplyTo(options);

        // Assert - other values preserved
        Assert.Equal(100, options.MaxLineLength);
        Assert.True(options.WarnUnusedVariables);
        Assert.Equal(200, options.MaxFileLines);
    }

    [Fact]
    public void ApplyTo_CommentSuppression_OverridesValue()
    {
        // Arrange
        var options = new GDLinterOptions { EnableCommentSuppression = true };
        var overrides = new GDLinterOptionsOverrides
        {
            EnableCommentSuppression = false
        };

        // Act
        overrides.ApplyTo(options);

        // Assert
        Assert.False(options.EnableCommentSuppression);
    }
}
