using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDConfigInitCommandTests
{
    private string? _tempDir;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"gdshrapt_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempDir != null && Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [TestMethod]
    public async Task Init_MinimalPreset_CreatesDefaultConfig()
    {
        // Arrange
        var command = new GDConfigInitCommand(_tempDir!, null, false);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Success);
        var configPath = Path.Combine(_tempDir!, ".gdshrapt.json");
        File.Exists(configPath).Should().BeTrue();

        var config = GDConfigLoader.LoadConfig(_tempDir!);
        config.Linting.MaxLineLength.Should().Be(120); // default
        config.Cli.FailOnWarning.Should().BeFalse();
    }

    [TestMethod]
    public async Task Init_CiPreset_FailOnWarningAndStrictLimits()
    {
        // Arrange
        var command = new GDConfigInitCommand(_tempDir!, "ci", false);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Success);

        var config = GDConfigLoader.LoadConfig(_tempDir!);
        config.Cli.FailOnWarning.Should().BeTrue();
        config.Linting.MaxLineLength.Should().Be(100);
        config.Linting.FormattingLevel.Should().Be(GDFormattingLevel.Full);
        config.AdvancedLinting.MaxCyclomaticComplexity.Should().Be(12);
        config.AdvancedLinting.MaxFunctionLength.Should().Be(50);
        config.AdvancedLinting.MaxParameters.Should().Be(5);
        config.AdvancedLinting.MaxNestingDepth.Should().Be(4);
        config.Validation.NullableStrictness.Should().Be("strict");
    }

    [TestMethod]
    public async Task Init_LocalPreset_AllWarningsNoFail()
    {
        // Arrange
        var command = new GDConfigInitCommand(_tempDir!, "local", false);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Success);

        var config = GDConfigLoader.LoadConfig(_tempDir!);
        config.Cli.FailOnWarning.Should().BeFalse();
        config.AdvancedLinting.WarnMagicNumbers.Should().BeTrue();
        config.AdvancedLinting.WarnNoElifReturn.Should().BeTrue();
        config.AdvancedLinting.WarnNoElseReturn.Should().BeTrue();
        config.AdvancedLinting.WarnPrivateMethodCall.Should().BeTrue();
        config.AdvancedLinting.WarnExpressionNotAssigned.Should().BeTrue();
        config.AdvancedLinting.WarnUselessAssignment.Should().BeTrue();
        config.AdvancedLinting.WarnInconsistentReturn.Should().BeTrue();
        config.AdvancedLinting.WarnNoLonelyIf.Should().BeTrue();
        config.AdvancedLinting.MaxCyclomaticComplexity.Should().Be(20);
        config.Validation.NullableStrictness.Should().Be("relaxed");
    }

    [TestMethod]
    public async Task Init_TeamPreset_FailOnWarningModerateLimits()
    {
        // Arrange
        var command = new GDConfigInitCommand(_tempDir!, "team", false);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Success);

        var config = GDConfigLoader.LoadConfig(_tempDir!);
        config.Cli.FailOnWarning.Should().BeTrue();
        config.Linting.MaxLineLength.Should().Be(120);
        config.AdvancedLinting.MaxCyclomaticComplexity.Should().Be(15);
        config.AdvancedLinting.MaxFunctionLength.Should().Be(60);
        config.AdvancedLinting.MaxParameters.Should().Be(6);
        config.AdvancedLinting.WarnUnusedSignals.Should().BeTrue();
        config.AdvancedLinting.WarnInconsistentReturn.Should().BeTrue();
        config.Validation.NullableStrictness.Should().Be("strict");
    }

    [TestMethod]
    public async Task Init_ConfigExists_NoForce_ReturnsError()
    {
        // Arrange - create config first
        var configPath = Path.Combine(_tempDir!, ".gdshrapt.json");
        File.WriteAllText(configPath, "{}");

        var command = new GDConfigInitCommand(_tempDir!, null, false);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Errors);
    }

    [TestMethod]
    public async Task Init_ConfigExists_WithForce_Overwrites()
    {
        // Arrange - create config first
        var configPath = Path.Combine(_tempDir!, ".gdshrapt.json");
        File.WriteAllText(configPath, "{}");

        var command = new GDConfigInitCommand(_tempDir!, "strict", true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Success);

        var config = GDConfigLoader.LoadConfig(_tempDir!);
        config.Cli.FailOnWarning.Should().BeTrue(); // strict preset applied
    }

    [TestMethod]
    public async Task Init_InvalidPreset_ReturnsError()
    {
        // Arrange
        var command = new GDConfigInitCommand(_tempDir!, "unknown", false);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Errors);
    }
}
