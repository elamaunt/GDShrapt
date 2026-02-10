using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using GDShrapt.CLI;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class LintCommandBuilderTests
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
        _command = LintCommandBuilder.Build(_formatOption, _verboseOption, _debugOption, _quietOption, _logLevelOption);
    }

    [TestMethod]
    public void Build_ReturnsCommand_WithCorrectName()
    {
        _command.Name.Should().Be("lint");
    }

    [TestMethod]
    public void Build_HasPathArgument()
    {
        _command.Arguments.Where(a => a.Name == "project-path").Should().ContainSingle();
    }

    [TestMethod]
    public void Build_HasRulesOption()
    {
        _command.Options.Should().Contain(o => o.Name == "rules");
    }

    [TestMethod]
    public void Build_HasCategoryOption()
    {
        _command.Options.Should().Contain(o => o.Name == "category");
    }

    [TestMethod]
    public void Build_HasNamingCaseOptions()
    {
        _command.Options.Should().Contain(o => o.Name == "class-name-case");
        _command.Options.Should().Contain(o => o.Name == "function-name-case");
        _command.Options.Should().Contain(o => o.Name == "variable-name-case");
        _command.Options.Should().Contain(o => o.Name == "constant-name-case");
        _command.Options.Should().Contain(o => o.Name == "signal-name-case");
        _command.Options.Should().Contain(o => o.Name == "enum-name-case");
        _command.Options.Should().Contain(o => o.Name == "enum-value-case");
        _command.Options.Should().Contain(o => o.Name == "inner-class-name-case");
    }

    [TestMethod]
    public void Build_HasLimitOptions()
    {
        _command.Options.Should().Contain(o => o.Name == "max-line-length");
        _command.Options.Should().Contain(o => o.Name == "max-file-lines");
        _command.Options.Should().Contain(o => o.Name == "max-parameters");
        _command.Options.Should().Contain(o => o.Name == "max-function-length");
        _command.Options.Should().Contain(o => o.Name == "max-complexity");
    }

    [TestMethod]
    public void Build_HasWarningOptions()
    {
        _command.Options.Should().Contain(o => o.Name == "warn-unused-variables");
        _command.Options.Should().Contain(o => o.Name == "warn-unused-parameters");
        _command.Options.Should().Contain(o => o.Name == "warn-unused-signals");
        _command.Options.Should().Contain(o => o.Name == "warn-empty-functions");
        _command.Options.Should().Contain(o => o.Name == "warn-magic-numbers");
        _command.Options.Should().Contain(o => o.Name == "warn-variable-shadowing");
        _command.Options.Should().Contain(o => o.Name == "warn-await-in-loop");
        _command.Options.Should().Contain(o => o.Name == "warn-no-elif-return");
        _command.Options.Should().Contain(o => o.Name == "warn-no-else-return");
        _command.Options.Should().Contain(o => o.Name == "warn-private-method-call");
        _command.Options.Should().Contain(o => o.Name == "warn-duplicated-load");
    }

    [TestMethod]
    public void Build_HasStrictTypingOptions()
    {
        _command.Options.Should().Contain(o => o.Name == "strict-typing");
        _command.Options.Should().Contain(o => o.Name == "strict-typing-class-vars");
        _command.Options.Should().Contain(o => o.Name == "strict-typing-local-vars");
        _command.Options.Should().Contain(o => o.Name == "strict-typing-params");
        _command.Options.Should().Contain(o => o.Name == "strict-typing-return");
    }

    [TestMethod]
    public void Build_HasSuppressionOption()
    {
        _command.Options.Should().Contain(o => o.Name == "enable-suppression");
    }

    [TestMethod]
    public void Parse_RulesOption_ParsesCorrectly()
    {
        var result = _command.Parse("lint . --rules GDL001,GDL003");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_MaxLineLengthOption_ParsesCorrectly()
    {
        var result = _command.Parse("lint . --max-line-length 120");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_BooleanWarningOption_ParsesCorrectly()
    {
        var result = _command.Parse("lint . --warn-no-elif-return");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_MultipleOptions_ParsesCorrectly()
    {
        var result = _command.Parse("lint . --max-line-length 80 --warn-no-elif-return --class-name-case pascal");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_ShortRulesOption_ParsesCorrectly()
    {
        var result = _command.Parse("lint . -r GDL001");
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_StrictTypingOption_ParsesCorrectly()
    {
        var result = _command.Parse("lint . --strict-typing warning");
        result.Errors.Should().BeEmpty();
    }
}
