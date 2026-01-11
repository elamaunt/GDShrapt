using GDShrapt.CLI.Core;
using GDShrapt.Reader;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDFormatterOptionsOverridesTests
{
    [TestMethod]
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
        options.IndentStyle.Should().Be(IndentStyle.Spaces);
        options.IndentSize.Should().Be(2);
    }

    [TestMethod]
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
        options.LineEnding.Should().Be(LineEndingStyle.CRLF);
    }

    [TestMethod]
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
        options.MaxLineLength.Should().Be(80);
        options.WrapLongLines.Should().BeTrue();
        options.LineWrapStyle.Should().Be(LineWrapStyle.BeforeElements);
        options.ContinuationIndentSize.Should().Be(2);
        options.UseBackslashContinuation.Should().BeTrue();
    }

    [TestMethod]
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
        options.SpaceAroundOperators.Should().BeFalse();
        options.SpaceAfterComma.Should().BeFalse();
        options.SpaceAfterColon.Should().BeFalse();
        options.SpaceBeforeColon.Should().BeTrue();
        options.SpaceInsideParentheses.Should().BeTrue();
        options.SpaceInsideBrackets.Should().BeTrue();
        options.SpaceInsideBraces.Should().BeFalse();
    }

    [TestMethod]
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
        options.BlankLinesBetweenFunctions.Should().Be(1);
        options.BlankLinesAfterClassDeclaration.Should().Be(2);
        options.BlankLinesBetweenMemberTypes.Should().Be(0);
    }

    [TestMethod]
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
        options.RemoveTrailingWhitespace.Should().BeFalse();
        options.EnsureTrailingNewline.Should().BeFalse();
        options.RemoveMultipleTrailingNewlines.Should().BeFalse();
    }

    [TestMethod]
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
        options.IndentStyle.Should().Be(IndentStyle.Tabs);
        options.IndentSize.Should().Be(4);
        options.MaxLineLength.Should().Be(100);
        options.BlankLinesBetweenFunctions.Should().Be(3);
    }
}
