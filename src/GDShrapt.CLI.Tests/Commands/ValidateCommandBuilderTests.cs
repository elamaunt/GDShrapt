using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using GDShrapt.CLI;
using Xunit;

namespace GDShrapt.CLI.Tests;

public class ValidateCommandBuilderTests
{
    private readonly Command _command;
    private readonly Option<string> _formatOption;

    public ValidateCommandBuilderTests()
    {
        _formatOption = new Option<string>("--format", () => "text");
        _command = ValidateCommandBuilder.Build(_formatOption);
    }

    [Fact]
    public void Build_ReturnsCommand_WithCorrectName()
    {
        Assert.Equal("validate", _command.Name);
    }

    [Fact]
    public void Build_HasPathArgument()
    {
        Assert.Single(_command.Arguments.Where(a => a.Name == "project-path"));
    }

    [Fact]
    public void Build_HasChecksOption()
    {
        Assert.Contains(_command.Options, o => o.Name == "checks");
    }

    [Fact]
    public void Build_HasStrictOption()
    {
        Assert.Contains(_command.Options, o => o.Name == "strict");
    }

    [Fact]
    public void Build_HasIndividualCheckOptions()
    {
        Assert.Contains(_command.Options, o => o.Name == "check-syntax");
        Assert.Contains(_command.Options, o => o.Name == "check-scope");
        Assert.Contains(_command.Options, o => o.Name == "check-types");
        Assert.Contains(_command.Options, o => o.Name == "check-calls");
        Assert.Contains(_command.Options, o => o.Name == "check-control-flow");
        Assert.Contains(_command.Options, o => o.Name == "check-indentation");
    }

    [Fact]
    public void Parse_ChecksOption_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --checks syntax,scope");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_StrictOption_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --strict");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_IndividualCheckOption_True_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --check-types true");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_IndividualCheckOption_False_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --check-types false");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_MultipleIndividualChecks_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --check-syntax true --check-types false --check-scope true");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_ChecksAll_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --checks all");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_CombinedOptions_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --checks syntax,scope --strict --check-types false");
        Assert.Empty(result.Errors);
    }
}
