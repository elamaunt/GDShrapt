using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using GDShrapt.CLI.Commands;
using Xunit;

namespace GDShrapt.CLI.Tests.Commands;

public class LintCommandBuilderTests
{
    private readonly Command _command;
    private readonly Option<string> _formatOption;

    public LintCommandBuilderTests()
    {
        _formatOption = new Option<string>("--format", () => "text");
        _command = LintCommandBuilder.Build(_formatOption);
    }

    [Fact]
    public void Build_ReturnsCommand_WithCorrectName()
    {
        Assert.Equal("lint", _command.Name);
    }

    [Fact]
    public void Build_HasPathArgument()
    {
        Assert.Single(_command.Arguments.Where(a => a.Name == "project-path"));
    }

    [Fact]
    public void Build_HasRulesOption()
    {
        Assert.Contains(_command.Options, o => o.Name == "rules");
    }

    [Fact]
    public void Build_HasCategoryOption()
    {
        Assert.Contains(_command.Options, o => o.Name == "category");
    }

    [Fact]
    public void Build_HasNamingCaseOptions()
    {
        Assert.Contains(_command.Options, o => o.Name == "class-name-case");
        Assert.Contains(_command.Options, o => o.Name == "function-name-case");
        Assert.Contains(_command.Options, o => o.Name == "variable-name-case");
        Assert.Contains(_command.Options, o => o.Name == "constant-name-case");
        Assert.Contains(_command.Options, o => o.Name == "signal-name-case");
        Assert.Contains(_command.Options, o => o.Name == "enum-name-case");
        Assert.Contains(_command.Options, o => o.Name == "enum-value-case");
        Assert.Contains(_command.Options, o => o.Name == "inner-class-name-case");
    }

    [Fact]
    public void Build_HasLimitOptions()
    {
        Assert.Contains(_command.Options, o => o.Name == "max-line-length");
        Assert.Contains(_command.Options, o => o.Name == "max-file-lines");
        Assert.Contains(_command.Options, o => o.Name == "max-parameters");
        Assert.Contains(_command.Options, o => o.Name == "max-function-length");
        Assert.Contains(_command.Options, o => o.Name == "max-complexity");
    }

    [Fact]
    public void Build_HasWarningOptions()
    {
        Assert.Contains(_command.Options, o => o.Name == "warn-unused-variables");
        Assert.Contains(_command.Options, o => o.Name == "warn-unused-parameters");
        Assert.Contains(_command.Options, o => o.Name == "warn-unused-signals");
        Assert.Contains(_command.Options, o => o.Name == "warn-empty-functions");
        Assert.Contains(_command.Options, o => o.Name == "warn-magic-numbers");
        Assert.Contains(_command.Options, o => o.Name == "warn-variable-shadowing");
        Assert.Contains(_command.Options, o => o.Name == "warn-await-in-loop");
        Assert.Contains(_command.Options, o => o.Name == "warn-no-elif-return");
        Assert.Contains(_command.Options, o => o.Name == "warn-no-else-return");
        Assert.Contains(_command.Options, o => o.Name == "warn-private-method-call");
        Assert.Contains(_command.Options, o => o.Name == "warn-duplicated-load");
    }

    [Fact]
    public void Build_HasStrictTypingOptions()
    {
        Assert.Contains(_command.Options, o => o.Name == "strict-typing");
        Assert.Contains(_command.Options, o => o.Name == "strict-typing-class-vars");
        Assert.Contains(_command.Options, o => o.Name == "strict-typing-local-vars");
        Assert.Contains(_command.Options, o => o.Name == "strict-typing-params");
        Assert.Contains(_command.Options, o => o.Name == "strict-typing-return");
    }

    [Fact]
    public void Build_HasSuppressionOption()
    {
        Assert.Contains(_command.Options, o => o.Name == "enable-suppression");
    }

    [Fact]
    public void Parse_RulesOption_ParsesCorrectly()
    {
        var result = _command.Parse("lint . --rules GDL001,GDL003");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_MaxLineLengthOption_ParsesCorrectly()
    {
        var result = _command.Parse("lint . --max-line-length 120");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_BooleanWarningOption_ParsesCorrectly()
    {
        var result = _command.Parse("lint . --warn-no-elif-return");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_MultipleOptions_ParsesCorrectly()
    {
        var result = _command.Parse("lint . --max-line-length 80 --warn-no-elif-return --class-name-case pascal");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_ShortRulesOption_ParsesCorrectly()
    {
        var result = _command.Parse("lint . -r GDL001");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_StrictTypingOption_ParsesCorrectly()
    {
        var result = _command.Parse("lint . --strict-typing warning");
        Assert.Empty(result.Errors);
    }
}
