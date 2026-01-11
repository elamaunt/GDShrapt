using GDShrapt.CLI.Core;
using GDShrapt.Reader;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDLinterOptionsOverridesTests
{
    [TestMethod]
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
        options.ClassNameCase.Should().Be(NamingCase.SnakeCase);
        options.FunctionNameCase.Should().Be(NamingCase.PascalCase);
        options.VariableNameCase.Should().Be(NamingCase.CamelCase);
        options.ConstantNameCase.Should().Be(NamingCase.PascalCase);
    }

    [TestMethod]
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
        options.MaxLineLength.Should().Be(80);
        options.MaxFileLines.Should().Be(500);
        options.MaxParameters.Should().Be(3);
        options.MaxFunctionLength.Should().Be(25);
        options.MaxCyclomaticComplexity.Should().Be(5);
    }

    [TestMethod]
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
        options.WarnUnusedVariables.Should().BeFalse();
        options.WarnNoElifReturn.Should().BeTrue();
        options.WarnNoElseReturn.Should().BeTrue();
        options.WarnPrivateMethodCall.Should().BeTrue();
        options.WarnDuplicatedLoad.Should().BeFalse();
    }

    [TestMethod]
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
        options.StrictTypingClassVariables.Should().Be(GDLintSeverity.Error);
        options.StrictTypingLocalVariables.Should().Be(GDLintSeverity.Warning);
        options.StrictTypingParameters.Should().Be(GDLintSeverity.Error);
        options.StrictTypingReturnTypes.Should().Be(GDLintSeverity.Warning);
    }

    [TestMethod]
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
        options.MaxLineLength.Should().Be(100);
        options.WarnUnusedVariables.Should().BeTrue();
        options.MaxFileLines.Should().Be(200);
    }

    [TestMethod]
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
        options.EnableCommentSuppression.Should().BeFalse();
    }
}
