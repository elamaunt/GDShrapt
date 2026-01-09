using GDShrapt.CLI.Core;
using GDShrapt.Reader;
using Xunit;

namespace GDShrapt.CLI.Tests.Helpers;

public class OptionParsersTests
{
    #region ParseNamingCase

    [Theory]
    [InlineData("pascal", NamingCase.PascalCase)]
    [InlineData("PascalCase", NamingCase.PascalCase)]
    [InlineData("snake", NamingCase.SnakeCase)]
    [InlineData("snake_case", NamingCase.SnakeCase)]
    [InlineData("snakecase", NamingCase.SnakeCase)]
    [InlineData("camel", NamingCase.CamelCase)]
    [InlineData("camelCase", NamingCase.CamelCase)]
    [InlineData("screaming", NamingCase.ScreamingSnakeCase)]
    [InlineData("screamingsnake", NamingCase.ScreamingSnakeCase)]
    [InlineData("screaming_snake_case", NamingCase.ScreamingSnakeCase)]
    [InlineData("any", NamingCase.Any)]
    public void ParseNamingCase_ValidInput_ReturnsCorrectCase(string input, NamingCase expected)
    {
        var result = OptionParsers.ParseNamingCase(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseNamingCase_NullInput_ReturnsSnakeCase()
    {
        var result = OptionParsers.ParseNamingCase(null);
        Assert.Equal(NamingCase.SnakeCase, result);
    }

    [Fact]
    public void ParseNamingCase_EmptyInput_ReturnsSnakeCase()
    {
        var result = OptionParsers.ParseNamingCase("");
        Assert.Equal(NamingCase.SnakeCase, result);
    }

    [Fact]
    public void ParseNamingCase_InvalidInput_ReturnsSnakeCase()
    {
        var result = OptionParsers.ParseNamingCase("invalid");
        Assert.Equal(NamingCase.SnakeCase, result);
    }

    #endregion

    #region ParseIndentStyle

    [Theory]
    [InlineData("tabs", IndentStyle.Tabs)]
    [InlineData("tab", IndentStyle.Tabs)]
    [InlineData("spaces", IndentStyle.Spaces)]
    [InlineData("space", IndentStyle.Spaces)]
    public void ParseIndentStyle_ValidInput_ReturnsCorrectStyle(string input, IndentStyle expected)
    {
        var result = OptionParsers.ParseIndentStyle(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseIndentStyle_NullInput_ReturnsTabs()
    {
        var result = OptionParsers.ParseIndentStyle(null);
        Assert.Equal(IndentStyle.Tabs, result);
    }

    [Fact]
    public void ParseIndentStyle_InvalidInput_ReturnsTabs()
    {
        var result = OptionParsers.ParseIndentStyle("invalid");
        Assert.Equal(IndentStyle.Tabs, result);
    }

    #endregion

    #region ParseLineEnding

    [Theory]
    [InlineData("lf", LineEndingStyle.LF)]
    [InlineData("unix", LineEndingStyle.LF)]
    [InlineData("crlf", LineEndingStyle.CRLF)]
    [InlineData("windows", LineEndingStyle.CRLF)]
    [InlineData("platform", LineEndingStyle.Platform)]
    [InlineData("auto", LineEndingStyle.Platform)]
    public void ParseLineEnding_ValidInput_ReturnsCorrectStyle(string input, LineEndingStyle expected)
    {
        var result = OptionParsers.ParseLineEnding(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseLineEnding_NullInput_ReturnsLF()
    {
        var result = OptionParsers.ParseLineEnding(null);
        Assert.Equal(LineEndingStyle.LF, result);
    }

    #endregion

    #region ParseSeverity

    [Theory]
    [InlineData("error", GDLintSeverity.Error)]
    [InlineData("warning", GDLintSeverity.Warning)]
    [InlineData("warn", GDLintSeverity.Warning)]
    [InlineData("info", GDLintSeverity.Info)]
    [InlineData("information", GDLintSeverity.Info)]
    [InlineData("hint", GDLintSeverity.Hint)]
    public void ParseSeverity_ValidInput_ReturnsCorrectSeverity(string input, GDLintSeverity expected)
    {
        var result = OptionParsers.ParseSeverity(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("off")]
    [InlineData("none")]
    [InlineData("disable")]
    public void ParseSeverity_DisableKeywords_ReturnsNull(string input)
    {
        var result = OptionParsers.ParseSeverity(input);
        Assert.Null(result);
    }

    [Fact]
    public void ParseSeverity_NullInput_ReturnsNull()
    {
        var result = OptionParsers.ParseSeverity(null);
        Assert.Null(result);
    }

    [Fact]
    public void ParseSeverity_InvalidInput_ReturnsNull()
    {
        var result = OptionParsers.ParseSeverity("invalid");
        Assert.Null(result);
    }

    #endregion

    #region ParseCategories

    [Fact]
    public void ParseCategories_SingleCategory_ReturnsArray()
    {
        var result = OptionParsers.ParseCategories("naming");
        Assert.Single(result);
        Assert.Equal(GDLintCategory.Naming, result[0]);
    }

    [Fact]
    public void ParseCategories_MultipleCategories_ReturnsArray()
    {
        var result = OptionParsers.ParseCategories("naming,style,best-practices");
        Assert.Equal(3, result.Length);
        Assert.Contains(GDLintCategory.Naming, result);
        Assert.Contains(GDLintCategory.Style, result);
        Assert.Contains(GDLintCategory.BestPractices, result);
    }

    [Fact]
    public void ParseCategories_WithSpaces_TrimsCorrectly()
    {
        var result = OptionParsers.ParseCategories("naming , style , organization");
        Assert.Equal(3, result.Length);
        Assert.Contains(GDLintCategory.Naming, result);
        Assert.Contains(GDLintCategory.Style, result);
        Assert.Contains(GDLintCategory.Organization, result);
    }

    [Fact]
    public void ParseCategories_NullInput_ReturnsEmptyArray()
    {
        var result = OptionParsers.ParseCategories(null);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseCategories_EmptyInput_ReturnsEmptyArray()
    {
        var result = OptionParsers.ParseCategories("");
        Assert.Empty(result);
    }

    #endregion

    #region ParseValidationChecks

    [Fact]
    public void ParseValidationChecks_All_ReturnsAll()
    {
        var result = OptionParsers.ParseValidationChecks("all");
        Assert.Equal(GDValidationChecks.All, result);
    }

    [Fact]
    public void ParseValidationChecks_AllCaseInsensitive_ReturnsAll()
    {
        var result = OptionParsers.ParseValidationChecks("ALL");
        Assert.Equal(GDValidationChecks.All, result);
    }

    [Fact]
    public void ParseValidationChecks_SingleCheck_ReturnsFlag()
    {
        var result = OptionParsers.ParseValidationChecks("syntax");
        Assert.Equal(GDValidationChecks.Syntax, result);
    }

    [Fact]
    public void ParseValidationChecks_MultipleChecks_ReturnsCombinedFlags()
    {
        var result = OptionParsers.ParseValidationChecks("syntax,scope,types");
        Assert.True(result.HasFlag(GDValidationChecks.Syntax));
        Assert.True(result.HasFlag(GDValidationChecks.Scope));
        Assert.True(result.HasFlag(GDValidationChecks.Types));
    }

    [Fact]
    public void ParseValidationChecks_ControlFlow_BothFormats()
    {
        var result1 = OptionParsers.ParseValidationChecks("controlflow");
        var result2 = OptionParsers.ParseValidationChecks("control-flow");
        Assert.Equal(GDValidationChecks.ControlFlow, result1);
        Assert.Equal(GDValidationChecks.ControlFlow, result2);
    }

    [Fact]
    public void ParseValidationChecks_NullInput_ReturnsAll()
    {
        var result = OptionParsers.ParseValidationChecks(null);
        Assert.Equal(GDValidationChecks.All, result);
    }

    [Fact]
    public void ParseValidationChecks_EmptyInput_ReturnsAll()
    {
        var result = OptionParsers.ParseValidationChecks("");
        Assert.Equal(GDValidationChecks.All, result);
    }

    #endregion

    #region ParseLineWrapStyle

    [Theory]
    [InlineData("afteropen", LineWrapStyle.AfterOpeningBracket)]
    [InlineData("afteropening", LineWrapStyle.AfterOpeningBracket)]
    [InlineData("afteropeningbracket", LineWrapStyle.AfterOpeningBracket)]
    [InlineData("before", LineWrapStyle.BeforeElements)]
    [InlineData("beforeelements", LineWrapStyle.BeforeElements)]
    public void ParseLineWrapStyle_ValidInput_ReturnsCorrectStyle(string input, LineWrapStyle expected)
    {
        var result = OptionParsers.ParseLineWrapStyle(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseLineWrapStyle_NullInput_ReturnsAfterOpeningBracket()
    {
        var result = OptionParsers.ParseLineWrapStyle(null);
        Assert.Equal(LineWrapStyle.AfterOpeningBracket, result);
    }

    #endregion
}
