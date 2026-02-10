using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using GDShrapt.CLI;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class FormatCommandBuilderTests
{
    private Command _command = null!;
    private Option<string> _formatOption = null!;
    private Option<bool> _verboseOption = null!;
    private Option<bool> _debugOption = null!;
    private Option<bool> _quietOption = null!;
    private Option<string?> _logLevelOption = null!;

    [TestInitialize]
    public void Setup()
    {
        _formatOption = new Option<string>("--format", () => "text");
        _verboseOption = new Option<bool>("--verbose");
        _debugOption = new Option<bool>("--debug");
        _quietOption = new Option<bool>("--quiet");
        _logLevelOption = new Option<string?>("--log-level");
        _command = FormatCommandBuilder.Build(_formatOption, _verboseOption, _debugOption, _quietOption, _logLevelOption);
    }

    [TestMethod]
    public void Build_ReturnsCommand_WithCorrectName()
    {
        _command.Name.Should().Be("format");
    }

    [TestMethod]
    public void Build_HasPathArgument()
    {
        _command.Arguments.Where(a => a.Name == "path").Should().ContainSingle();
    }

    [TestMethod]
    public void Build_HasModeOptions()
    {
        _command.Options.Should().Contain(o => o.Name == "dry-run");
        _command.Options.Should().Contain(o => o.Name == "check");
    }

    [TestMethod]
    public void Build_HasIndentationOptions()
    {
        _command.Options.Should().Contain(o => o.Name == "indent-style");
        _command.Options.Should().Contain(o => o.Name == "indent-size");
    }

    [TestMethod]
    public void Build_HasLineEndingOption()
    {
        _command.Options.Should().Contain(o => o.Name == "line-ending");
    }

    [TestMethod]
    public void Build_HasLineWrappingOptions()
    {
        _command.Options.Should().Contain(o => o.Name == "max-line-length");
        _command.Options.Should().Contain(o => o.Name == "wrap-long-lines");
        _command.Options.Should().Contain(o => o.Name == "line-wrap-style");
        _command.Options.Should().Contain(o => o.Name == "continuation-indent");
        _command.Options.Should().Contain(o => o.Name == "use-backslash");
    }

    [TestMethod]
    public void Build_HasSpacingOptions()
    {
        _command.Options.Should().Contain(o => o.Name == "space-around-operators");
        _command.Options.Should().Contain(o => o.Name == "space-after-comma");
        _command.Options.Should().Contain(o => o.Name == "space-after-colon");
        _command.Options.Should().Contain(o => o.Name == "space-before-colon");
        _command.Options.Should().Contain(o => o.Name == "space-inside-parens");
        _command.Options.Should().Contain(o => o.Name == "space-inside-brackets");
        _command.Options.Should().Contain(o => o.Name == "space-inside-braces");
    }

    [TestMethod]
    public void Build_HasBlankLinesOptions()
    {
        _command.Options.Should().Contain(o => o.Name == "blank-lines-between-functions");
        _command.Options.Should().Contain(o => o.Name == "blank-lines-after-class");
        _command.Options.Should().Contain(o => o.Name == "blank-lines-between-members");
    }

    [TestMethod]
    public void Build_HasCleanupOptions()
    {
        _command.Options.Should().Contain(o => o.Name == "remove-trailing-whitespace");
        _command.Options.Should().Contain(o => o.Name == "ensure-trailing-newline");
        _command.Options.Should().Contain(o => o.Name == "remove-multiple-newlines");
    }

    [TestMethod]
    public void Parse_IndentStyleOption_ParsesCorrectly()
    {
        var result = _command.Parse("format . --indent-style spaces");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_IndentSizeOption_ParsesCorrectly()
    {
        var result = _command.Parse("format . --indent-size 2");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_MultipleOptions_ParsesCorrectly()
    {
        var result = _command.Parse("format . --indent-style spaces --indent-size 2 --line-ending lf");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_DryRunOption_ParsesCorrectly()
    {
        var result = _command.Parse("format . --dry-run");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_ShortDryRunOption_ParsesCorrectly()
    {
        var result = _command.Parse("format . -n");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_CheckOption_ParsesCorrectly()
    {
        var result = _command.Parse("format . --check");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_ShortCheckOption_ParsesCorrectly()
    {
        var result = _command.Parse("format . -c");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_AllFormattingOptions_ParsesCorrectly()
    {
        var result = _command.Parse("format . --indent-style tabs --indent-size 4 --line-ending lf --max-line-length 100 --wrap-long-lines --space-around-operators --blank-lines-between-functions 2");
        result.Errors.Should().BeEmpty();
    }
}
