using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using GDShrapt.CLI;
using Xunit;

namespace GDShrapt.CLI.Tests;

public class FormatCommandBuilderTests
{
    private readonly Command _command;
    private readonly Option<string> _formatOption;

    public FormatCommandBuilderTests()
    {
        _formatOption = new Option<string>("--format", () => "text");
        _command = FormatCommandBuilder.Build(_formatOption);
    }

    [Fact]
    public void Build_ReturnsCommand_WithCorrectName()
    {
        Assert.Equal("format", _command.Name);
    }

    [Fact]
    public void Build_HasPathArgument()
    {
        Assert.Single(_command.Arguments.Where(a => a.Name == "path"));
    }

    [Fact]
    public void Build_HasModeOptions()
    {
        Assert.Contains(_command.Options, o => o.Name == "dry-run");
        Assert.Contains(_command.Options, o => o.Name == "check");
    }

    [Fact]
    public void Build_HasIndentationOptions()
    {
        Assert.Contains(_command.Options, o => o.Name == "indent-style");
        Assert.Contains(_command.Options, o => o.Name == "indent-size");
    }

    [Fact]
    public void Build_HasLineEndingOption()
    {
        Assert.Contains(_command.Options, o => o.Name == "line-ending");
    }

    [Fact]
    public void Build_HasLineWrappingOptions()
    {
        Assert.Contains(_command.Options, o => o.Name == "max-line-length");
        Assert.Contains(_command.Options, o => o.Name == "wrap-long-lines");
        Assert.Contains(_command.Options, o => o.Name == "line-wrap-style");
        Assert.Contains(_command.Options, o => o.Name == "continuation-indent");
        Assert.Contains(_command.Options, o => o.Name == "use-backslash");
    }

    [Fact]
    public void Build_HasSpacingOptions()
    {
        Assert.Contains(_command.Options, o => o.Name == "space-around-operators");
        Assert.Contains(_command.Options, o => o.Name == "space-after-comma");
        Assert.Contains(_command.Options, o => o.Name == "space-after-colon");
        Assert.Contains(_command.Options, o => o.Name == "space-before-colon");
        Assert.Contains(_command.Options, o => o.Name == "space-inside-parens");
        Assert.Contains(_command.Options, o => o.Name == "space-inside-brackets");
        Assert.Contains(_command.Options, o => o.Name == "space-inside-braces");
    }

    [Fact]
    public void Build_HasBlankLinesOptions()
    {
        Assert.Contains(_command.Options, o => o.Name == "blank-lines-between-functions");
        Assert.Contains(_command.Options, o => o.Name == "blank-lines-after-class");
        Assert.Contains(_command.Options, o => o.Name == "blank-lines-between-members");
    }

    [Fact]
    public void Build_HasCleanupOptions()
    {
        Assert.Contains(_command.Options, o => o.Name == "remove-trailing-whitespace");
        Assert.Contains(_command.Options, o => o.Name == "ensure-trailing-newline");
        Assert.Contains(_command.Options, o => o.Name == "remove-multiple-newlines");
    }

    [Fact]
    public void Parse_IndentStyleOption_ParsesCorrectly()
    {
        var result = _command.Parse("format . --indent-style spaces");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_IndentSizeOption_ParsesCorrectly()
    {
        var result = _command.Parse("format . --indent-size 2");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_MultipleOptions_ParsesCorrectly()
    {
        var result = _command.Parse("format . --indent-style spaces --indent-size 2 --line-ending lf");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_DryRunOption_ParsesCorrectly()
    {
        var result = _command.Parse("format . --dry-run");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_ShortDryRunOption_ParsesCorrectly()
    {
        var result = _command.Parse("format . -n");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_CheckOption_ParsesCorrectly()
    {
        var result = _command.Parse("format . --check");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_ShortCheckOption_ParsesCorrectly()
    {
        var result = _command.Parse("format . -c");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_AllFormattingOptions_ParsesCorrectly()
    {
        var result = _command.Parse("format . --indent-style tabs --indent-size 4 --line-ending lf --max-line-length 100 --wrap-long-lines --space-around-operators --blank-lines-between-functions 2");
        Assert.Empty(result.Errors);
    }
}
