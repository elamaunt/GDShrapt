using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using GDShrapt.CLI;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class ValidateCommandBuilderTests
{
    private Command _command = null!;
    private Option<string> _formatOption = null!;

    [TestInitialize]
    public void Setup()
    {
        _formatOption = new Option<string>("--format", () => "text");
        _command = ValidateCommandBuilder.Build(_formatOption);
    }

    [TestMethod]
    public void Build_ReturnsCommand_WithCorrectName()
    {
        _command.Name.Should().Be("validate");
    }

    [TestMethod]
    public void Build_HasPathArgument()
    {
        _command.Arguments.Where(a => a.Name == "project-path").Should().ContainSingle();
    }

    [TestMethod]
    public void Build_HasChecksOption()
    {
        _command.Options.Should().Contain(o => o.Name == "checks");
    }

    [TestMethod]
    public void Build_HasStrictOption()
    {
        _command.Options.Should().Contain(o => o.Name == "strict");
    }

    [TestMethod]
    public void Build_HasIndividualCheckOptions()
    {
        _command.Options.Should().Contain(o => o.Name == "check-syntax");
        _command.Options.Should().Contain(o => o.Name == "check-scope");
        _command.Options.Should().Contain(o => o.Name == "check-types");
        _command.Options.Should().Contain(o => o.Name == "check-calls");
        _command.Options.Should().Contain(o => o.Name == "check-control-flow");
        _command.Options.Should().Contain(o => o.Name == "check-indentation");
    }

    [TestMethod]
    public void Parse_ChecksOption_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --checks syntax,scope");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_StrictOption_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --strict");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_IndividualCheckOption_True_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --check-types true");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_IndividualCheckOption_False_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --check-types false");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_MultipleIndividualChecks_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --check-syntax true --check-types false --check-scope true");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_ChecksAll_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --checks all");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_CombinedOptions_ParsesCorrectly()
    {
        var result = _command.Parse("validate . --checks syntax,scope --strict --check-types false");
        result.Errors.Should().BeEmpty();
    }
}
