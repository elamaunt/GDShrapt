using GDShrapt.CLI.Core;
using GDShrapt.Reader;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class OptionParsersTests
{
    #region ParseNamingCase

    [DataTestMethod]
    [DataRow("pascal", NamingCase.PascalCase)]
    [DataRow("PascalCase", NamingCase.PascalCase)]
    [DataRow("snake", NamingCase.SnakeCase)]
    [DataRow("snake_case", NamingCase.SnakeCase)]
    [DataRow("snakecase", NamingCase.SnakeCase)]
    [DataRow("camel", NamingCase.CamelCase)]
    [DataRow("camelCase", NamingCase.CamelCase)]
    [DataRow("screaming", NamingCase.ScreamingSnakeCase)]
    [DataRow("screamingsnake", NamingCase.ScreamingSnakeCase)]
    [DataRow("screaming_snake_case", NamingCase.ScreamingSnakeCase)]
    [DataRow("any", NamingCase.Any)]
    public void ParseNamingCase_ValidInput_ReturnsCorrectCase(string input, NamingCase expected)
    {
        var result = OptionParsers.ParseNamingCase(input);
        result.Should().Be(expected);
    }

    [TestMethod]
    public void ParseNamingCase_NullInput_ReturnsSnakeCase()
    {
        var result = OptionParsers.ParseNamingCase(null);
        result.Should().Be(NamingCase.SnakeCase);
    }

    [TestMethod]
    public void ParseNamingCase_EmptyInput_ReturnsSnakeCase()
    {
        var result = OptionParsers.ParseNamingCase("");
        result.Should().Be(NamingCase.SnakeCase);
    }

    [TestMethod]
    public void ParseNamingCase_InvalidInput_ReturnsSnakeCase()
    {
        var result = OptionParsers.ParseNamingCase("invalid");
        result.Should().Be(NamingCase.SnakeCase);
    }

    #endregion

    #region ParseIndentStyle

    [DataTestMethod]
    [DataRow("tabs", IndentStyle.Tabs)]
    [DataRow("tab", IndentStyle.Tabs)]
    [DataRow("spaces", IndentStyle.Spaces)]
    [DataRow("space", IndentStyle.Spaces)]
    public void ParseIndentStyle_ValidInput_ReturnsCorrectStyle(string input, IndentStyle expected)
    {
        var result = OptionParsers.ParseIndentStyle(input);
        result.Should().Be(expected);
    }

    [TestMethod]
    public void ParseIndentStyle_NullInput_ReturnsTabs()
    {
        var result = OptionParsers.ParseIndentStyle(null);
        result.Should().Be(IndentStyle.Tabs);
    }

    [TestMethod]
    public void ParseIndentStyle_InvalidInput_ReturnsTabs()
    {
        var result = OptionParsers.ParseIndentStyle("invalid");
        result.Should().Be(IndentStyle.Tabs);
    }

    #endregion

    #region ParseLineEnding

    [DataTestMethod]
    [DataRow("lf", LineEndingStyle.LF)]
    [DataRow("unix", LineEndingStyle.LF)]
    [DataRow("crlf", LineEndingStyle.CRLF)]
    [DataRow("windows", LineEndingStyle.CRLF)]
    [DataRow("platform", LineEndingStyle.Platform)]
    [DataRow("auto", LineEndingStyle.Platform)]
    public void ParseLineEnding_ValidInput_ReturnsCorrectStyle(string input, LineEndingStyle expected)
    {
        var result = OptionParsers.ParseLineEnding(input);
        result.Should().Be(expected);
    }

    [TestMethod]
    public void ParseLineEnding_NullInput_ReturnsLF()
    {
        var result = OptionParsers.ParseLineEnding(null);
        result.Should().Be(LineEndingStyle.LF);
    }

    #endregion

    #region ParseSeverity

    [DataTestMethod]
    [DataRow("error", GDLintSeverity.Error)]
    [DataRow("warning", GDLintSeverity.Warning)]
    [DataRow("warn", GDLintSeverity.Warning)]
    [DataRow("info", GDLintSeverity.Info)]
    [DataRow("information", GDLintSeverity.Info)]
    [DataRow("hint", GDLintSeverity.Hint)]
    public void ParseSeverity_ValidInput_ReturnsCorrectSeverity(string input, GDLintSeverity expected)
    {
        var result = OptionParsers.ParseSeverity(input);
        result.Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow("off")]
    [DataRow("none")]
    [DataRow("disable")]
    public void ParseSeverity_DisableKeywords_ReturnsNull(string input)
    {
        var result = OptionParsers.ParseSeverity(input);
        result.Should().BeNull();
    }

    [TestMethod]
    public void ParseSeverity_NullInput_ReturnsNull()
    {
        var result = OptionParsers.ParseSeverity(null);
        result.Should().BeNull();
    }

    [TestMethod]
    public void ParseSeverity_InvalidInput_ReturnsNull()
    {
        var result = OptionParsers.ParseSeverity("invalid");
        result.Should().BeNull();
    }

    #endregion

    #region ParseCategories

    [TestMethod]
    public void ParseCategories_SingleCategory_ReturnsArray()
    {
        var result = OptionParsers.ParseCategories("naming");
        result.Should().ContainSingle();
        result[0].Should().Be(GDLintCategory.Naming);
    }

    [TestMethod]
    public void ParseCategories_MultipleCategories_ReturnsArray()
    {
        var result = OptionParsers.ParseCategories("naming,style,best-practices");
        result.Length.Should().Be(3);
        result.Should().Contain(GDLintCategory.Naming);
        result.Should().Contain(GDLintCategory.Style);
        result.Should().Contain(GDLintCategory.BestPractices);
    }

    [TestMethod]
    public void ParseCategories_WithSpaces_TrimsCorrectly()
    {
        var result = OptionParsers.ParseCategories("naming , style , organization");
        result.Length.Should().Be(3);
        result.Should().Contain(GDLintCategory.Naming);
        result.Should().Contain(GDLintCategory.Style);
        result.Should().Contain(GDLintCategory.Organization);
    }

    [TestMethod]
    public void ParseCategories_NullInput_ReturnsEmptyArray()
    {
        var result = OptionParsers.ParseCategories(null);
        result.Should().BeEmpty();
    }

    [TestMethod]
    public void ParseCategories_EmptyInput_ReturnsEmptyArray()
    {
        var result = OptionParsers.ParseCategories("");
        result.Should().BeEmpty();
    }

    #endregion

    #region ParseValidationChecks

    [TestMethod]
    public void ParseValidationChecks_All_ReturnsAll()
    {
        var result = OptionParsers.ParseValidationChecks("all");
        result.Should().Be(GDValidationChecks.All);
    }

    [TestMethod]
    public void ParseValidationChecks_AllCaseInsensitive_ReturnsAll()
    {
        var result = OptionParsers.ParseValidationChecks("ALL");
        result.Should().Be(GDValidationChecks.All);
    }

    [TestMethod]
    public void ParseValidationChecks_SingleCheck_ReturnsFlag()
    {
        var result = OptionParsers.ParseValidationChecks("syntax");
        result.Should().Be(GDValidationChecks.Syntax);
    }

    [TestMethod]
    public void ParseValidationChecks_MultipleChecks_ReturnsCombinedFlags()
    {
        var result = OptionParsers.ParseValidationChecks("syntax,scope,types");
        result.HasFlag(GDValidationChecks.Syntax).Should().BeTrue();
        result.HasFlag(GDValidationChecks.Scope).Should().BeTrue();
        result.HasFlag(GDValidationChecks.Types).Should().BeTrue();
    }

    [TestMethod]
    public void ParseValidationChecks_ControlFlow_BothFormats()
    {
        var result1 = OptionParsers.ParseValidationChecks("controlflow");
        var result2 = OptionParsers.ParseValidationChecks("control-flow");
        result1.Should().Be(GDValidationChecks.ControlFlow);
        result2.Should().Be(GDValidationChecks.ControlFlow);
    }

    [TestMethod]
    public void ParseValidationChecks_NullInput_ReturnsAll()
    {
        var result = OptionParsers.ParseValidationChecks(null);
        result.Should().Be(GDValidationChecks.All);
    }

    [TestMethod]
    public void ParseValidationChecks_EmptyInput_ReturnsAll()
    {
        var result = OptionParsers.ParseValidationChecks("");
        result.Should().Be(GDValidationChecks.All);
    }

    #endregion

    #region ParseLineWrapStyle

    [DataTestMethod]
    [DataRow("afteropen", LineWrapStyle.AfterOpeningBracket)]
    [DataRow("afteropening", LineWrapStyle.AfterOpeningBracket)]
    [DataRow("afteropeningbracket", LineWrapStyle.AfterOpeningBracket)]
    [DataRow("before", LineWrapStyle.BeforeElements)]
    [DataRow("beforeelements", LineWrapStyle.BeforeElements)]
    public void ParseLineWrapStyle_ValidInput_ReturnsCorrectStyle(string input, LineWrapStyle expected)
    {
        var result = OptionParsers.ParseLineWrapStyle(input);
        result.Should().Be(expected);
    }

    [TestMethod]
    public void ParseLineWrapStyle_NullInput_ReturnsAfterOpeningBracket()
    {
        var result = OptionParsers.ParseLineWrapStyle(null);
        result.Should().Be(LineWrapStyle.AfterOpeningBracket);
    }

    #endregion
}
