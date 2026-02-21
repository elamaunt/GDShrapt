using System.CommandLine;
using System.Linq;
using GDShrapt.CLI;

namespace GDShrapt.CLI.Tests;

/// <summary>
/// Tests for the 'config' parent command: structure, subcommands, options, and help text.
/// </summary>
[TestClass]
public class GDConfigCommandTests
{
    private Command _configCommand = null!;

    [TestInitialize]
    public void Setup()
    {
        _configCommand = ConfigCommandBuilder.Build();
    }

    [TestMethod]
    public void Config_HasThreeSubcommands()
    {
        _configCommand.Subcommands.Count.Should().Be(3);
        _configCommand.Subcommands.Select(c => c.Name).Should()
            .Contain("init")
            .And.Contain("show")
            .And.Contain("validate");
    }

    [TestMethod]
    public void Config_Description_ContainsManageKeyword()
    {
        _configCommand.Description.Should().Contain("Manage");
        _configCommand.Description.Should().Contain("configuration");
    }

    [TestMethod]
    public void Config_Init_HasPresetOption()
    {
        var init = _configCommand.Subcommands.First(c => c.Name == "init");
        init.Options.Any(o => o.Name == "preset").Should().BeTrue();
    }

    [TestMethod]
    public void Config_Init_HasForceOption()
    {
        var init = _configCommand.Subcommands.First(c => c.Name == "init");
        init.Options.Any(o => o.Name == "force").Should().BeTrue();
    }

    [TestMethod]
    public void Config_Init_HasPathArgument()
    {
        var init = _configCommand.Subcommands.First(c => c.Name == "init");
        init.Arguments.Any(a => a.Name == "project-path").Should().BeTrue();
    }

    [TestMethod]
    public void Config_Init_Description_ContainsExamples()
    {
        var init = _configCommand.Subcommands.First(c => c.Name == "init");
        init.Description.Should().Contain("Examples:");
        init.Description.Should().Contain("config init --preset ci");
    }

    [TestMethod]
    public void Config_Init_PresetOption_DescriptionListsPresets()
    {
        var init = _configCommand.Subcommands.First(c => c.Name == "init");
        var presetOption = init.Options.First(o => o.Name == "preset");

        presetOption.Description.Should().Contain("minimal");
        presetOption.Description.Should().Contain("recommended");
        presetOption.Description.Should().Contain("strict");
        presetOption.Description.Should().Contain("relaxed");
        presetOption.Description.Should().Contain("ci");
        presetOption.Description.Should().Contain("local");
        presetOption.Description.Should().Contain("team");
    }

    [TestMethod]
    public void Config_Show_HasEffectiveOption()
    {
        var show = _configCommand.Subcommands.First(c => c.Name == "show");
        show.Options.Any(o => o.Name == "effective").Should().BeTrue();
    }

    [TestMethod]
    public void Config_Show_HasFormatOption()
    {
        var show = _configCommand.Subcommands.First(c => c.Name == "show");
        show.Options.Any(o => o.Name == "format").Should().BeTrue();
    }

    [TestMethod]
    public void Config_Show_Description_ContainsExamples()
    {
        var show = _configCommand.Subcommands.First(c => c.Name == "show");
        show.Description.Should().Contain("Examples:");
        show.Description.Should().Contain("config show --effective");
        show.Description.Should().Contain("config show --format json");
    }

    [TestMethod]
    public void Config_Validate_HasExplainOption()
    {
        var validate = _configCommand.Subcommands.First(c => c.Name == "validate");
        validate.Options.Any(o => o.Name == "explain").Should().BeTrue();
    }

    [TestMethod]
    public void Config_Validate_Description_ContainsExamples()
    {
        var validate = _configCommand.Subcommands.First(c => c.Name == "validate");
        validate.Description.Should().Contain("Examples:");
        validate.Description.Should().Contain("config validate");
        validate.Description.Should().Contain("config validate --explain");
    }
}
