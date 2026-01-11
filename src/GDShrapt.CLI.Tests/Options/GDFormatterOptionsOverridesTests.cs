using GDShrapt.CLI.Core;
using GDShrapt.Reader;
using Xunit;

namespace GDShrapt.CLI.Tests;

public class GDFormatterOptionsOverridesTests
{
    [Fact]
    public void ApplyTo_Indentation_OverridesValues()
    {
        // Arrange
        var options = new GDFormatterOptions();
        var overrides = new GDFormatterOptionsOverrides
        {
            IndentStyle = IndentStyle.Spaces,
            IndentSize = 2
        };

        // Act
        overrides.ApplyTo(options);

        // Assert
        Assert.Equal(IndentStyle.Spaces, options.IndentStyle);
        Assert.Equal(2, options.IndentSize);
    }

    [Fact]
    public void ApplyTo_LineEnding_OverridesValue()
    {
        // Arrange
        var options = new GDFormatterOptions();
        var overrides = new GDFormatterOptionsOverrides
        {
            LineEnding = LineEndingStyle.CRLF
        };

        // Act
        overrides.ApplyTo(options);

        // Assert
        Assert.Equal(LineEndingStyle.CRLF, options.LineEnding);
    }

    [Fact]
    public void ApplyTo_LineWrapping_OverridesValues()
    {
        // Arrange
        var options = new GDFormatterOptions();
        var overrides = new GDFormatterOptionsOverrides
        {
            MaxLineLength = 80,
            WrapLongLines = true,
            LineWrapStyle = LineWrapStyle.BeforeElements,
            ContinuationIndentSize = 2,
            UseBackslashContinuation = true
        };

        // Act
        overrides.ApplyTo(options);

        // Assert
        Assert.Equal(80, options.MaxLineLength);
        Assert.True(options.WrapLongLines);
        Assert.Equal(LineWrapStyle.BeforeElements, options.LineWrapStyle);
        Assert.Equal(2, options.ContinuationIndentSize);
        Assert.True(options.UseBackslashContinuation);
    }

    [Fact]
    public void ApplyTo_Spacing_OverridesValues()
    {
        // Arrange
        var options = new GDFormatterOptions();
        var overrides = new GDFormatterOptionsOverrides
        {
            SpaceAroundOperators = false,
            SpaceAfterComma = false,
            SpaceAfterColon = false,
            SpaceBeforeColon = true,
            SpaceInsideParentheses = true,
            SpaceInsideBrackets = true,
            SpaceInsideBraces = false
        };

        // Act
        overrides.ApplyTo(options);

        // Assert
        Assert.False(options.SpaceAroundOperators);
        Assert.False(options.SpaceAfterComma);
        Assert.False(options.SpaceAfterColon);
        Assert.True(options.SpaceBeforeColon);
        Assert.True(options.SpaceInsideParentheses);
        Assert.True(options.SpaceInsideBrackets);
        Assert.False(options.SpaceInsideBraces);
    }

    [Fact]
    public void ApplyTo_BlankLines_OverridesValues()
    {
        // Arrange
        var options = new GDFormatterOptions();
        var overrides = new GDFormatterOptionsOverrides
        {
            BlankLinesBetweenFunctions = 1,
            BlankLinesAfterClassDeclaration = 2,
            BlankLinesBetweenMemberTypes = 0
        };

        // Act
        overrides.ApplyTo(options);

        // Assert
        Assert.Equal(1, options.BlankLinesBetweenFunctions);
        Assert.Equal(2, options.BlankLinesAfterClassDeclaration);
        Assert.Equal(0, options.BlankLinesBetweenMemberTypes);
    }

    [Fact]
    public void ApplyTo_Cleanup_OverridesValues()
    {
        // Arrange
        var options = new GDFormatterOptions();
        var overrides = new GDFormatterOptionsOverrides
        {
            RemoveTrailingWhitespace = false,
            EnsureTrailingNewline = false,
            RemoveMultipleTrailingNewlines = false
        };

        // Act
        overrides.ApplyTo(options);

        // Assert
        Assert.False(options.RemoveTrailingWhitespace);
        Assert.False(options.EnsureTrailingNewline);
        Assert.False(options.RemoveMultipleTrailingNewlines);
    }

    [Fact]
    public void ApplyTo_NullOverrides_PreservesOriginalValues()
    {
        // Arrange
        var options = new GDFormatterOptions
        {
            IndentStyle = IndentStyle.Tabs,
            IndentSize = 4,
            MaxLineLength = 100
        };
        var overrides = new GDFormatterOptionsOverrides
        {
            // Only override one property
            BlankLinesBetweenFunctions = 3
        };

        // Act
        overrides.ApplyTo(options);

        // Assert - other values preserved
        Assert.Equal(IndentStyle.Tabs, options.IndentStyle);
        Assert.Equal(4, options.IndentSize);
        Assert.Equal(100, options.MaxLineLength);
        Assert.Equal(3, options.BlankLinesBetweenFunctions);
    }
}
