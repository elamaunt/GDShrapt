using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDConfigValidateCommandTests
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
    public async Task Validate_ValidConfig_ReturnsSuccess()
    {
        // Arrange
        var initCmd = new GDConfigInitCommand(_tempDir!, "recommended", false);
        await initCmd.ExecuteAsync();

        var output = new StringWriter();
        var validateCmd = new GDConfigValidateCommand(_tempDir!, explain: false, output: output);

        // Act
        var result = await validateCmd.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Success);
        output.ToString().Should().Contain("Configuration is valid");
    }

    [TestMethod]
    public async Task Validate_InvalidJson_ReturnsError()
    {
        // Arrange - write invalid JSON
        var configPath = Path.Combine(_tempDir!, ".gdshrapt.json");
        File.WriteAllText(configPath, "{ invalid json }");

        var output = new StringWriter();
        var validateCmd = new GDConfigValidateCommand(_tempDir!, explain: false, output: output);

        // Act
        var result = await validateCmd.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Errors);
        output.ToString().Should().Contain("Invalid JSON");
    }

    [TestMethod]
    public async Task Validate_NegativeMaxLineLength_ReturnsError()
    {
        // Arrange - write config with negative MaxLineLength
        var config = new { linting = new { maxLineLength = -1 } };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir!, ".gdshrapt.json"), json);

        var output = new StringWriter();
        var validateCmd = new GDConfigValidateCommand(_tempDir!, explain: false, output: output);

        // Act
        var result = await validateCmd.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Errors);
        output.ToString().Should().Contain("linting.maxLineLength");
    }

    [TestMethod]
    public async Task Validate_ZeroTabWidth_ReturnsError()
    {
        // Arrange - write config with zero tabWidth
        var config = new { linting = new { tabWidth = 0 } };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir!, ".gdshrapt.json"), json);

        var output = new StringWriter();
        var validateCmd = new GDConfigValidateCommand(_tempDir!, explain: false, output: output);

        // Act
        var result = await validateCmd.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Errors);
        output.ToString().Should().Contain("linting.tabWidth");
    }

    [TestMethod]
    public async Task Validate_InvalidNullableStrictness_ReturnsError()
    {
        // Arrange - write config with invalid NullableStrictness
        var config = new { validation = new { nullableStrictness = "invalid" } };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir!, ".gdshrapt.json"), json);

        var output = new StringWriter();
        var validateCmd = new GDConfigValidateCommand(_tempDir!, explain: false, output: output);

        // Act
        var result = await validateCmd.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Errors);
        output.ToString().Should().Contain("nullableStrictness");
    }

    [TestMethod]
    public async Task Validate_ConflictingLineLengths_ShowsWarning()
    {
        // Arrange - linting=100, formatter=120
        var config = new
        {
            linting = new { maxLineLength = 100 },
            formatter = new { maxLineLength = 120 }
        };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir!, ".gdshrapt.json"), json);

        var output = new StringWriter();
        var validateCmd = new GDConfigValidateCommand(_tempDir!, explain: false, output: output);

        // Act
        var result = await validateCmd.ExecuteAsync();

        // Assert
        // Warnings don't cause validation failure
        var text = output.ToString();
        text.Should().Contain("warning");
        text.Should().Contain("Conflicting max line lengths");
    }

    [TestMethod]
    public async Task Validate_LintingDisabledWithAdvanced_ShowsWarning()
    {
        // Arrange - linting disabled but advanced linting configured
        var config = new
        {
            linting = new { enabled = false },
            advancedLinting = new { warnUnusedVariables = true }
        };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir!, ".gdshrapt.json"), json);

        var output = new StringWriter();
        var validateCmd = new GDConfigValidateCommand(_tempDir!, explain: false, output: output);

        // Act
        var result = await validateCmd.ExecuteAsync();

        // Assert
        var text = output.ToString();
        text.Should().Contain("warning");
        text.Should().Contain("Linting is disabled");
    }

    [TestMethod]
    public async Task Validate_WithExplain_ShowsDetails()
    {
        // Arrange - write config with zero tabWidth
        var config = new { linting = new { tabWidth = 0 } };
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir!, ".gdshrapt.json"), json);

        var output = new StringWriter();
        var validateCmd = new GDConfigValidateCommand(_tempDir!, explain: true, output: output);

        // Act
        var result = await validateCmd.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Errors);
        var text = output.ToString();
        text.Should().Contain("linting.tabWidth");
        text.Should().Contain("positive integer"); // explanation detail
    }

    [TestMethod]
    public async Task Validate_NoConfigFile_ReturnsFatal()
    {
        // Arrange - no config file
        var output = new StringWriter();
        var validateCmd = new GDConfigValidateCommand(_tempDir!, explain: false, output: output);

        // Act
        var result = await validateCmd.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Fatal);
    }

    [TestMethod]
    public async Task Validate_AllPresets_AreValid()
    {
        var presets = new[] { "minimal", "recommended", "strict", "relaxed", "ci", "local", "team" };

        foreach (var preset in presets)
        {
            // Clean up between presets
            var configPath = Path.Combine(_tempDir!, ".gdshrapt.json");
            if (File.Exists(configPath))
                File.Delete(configPath);

            // Init with preset
            var initCmd = new GDConfigInitCommand(_tempDir!, preset, true);
            var initResult = await initCmd.ExecuteAsync();
            initResult.Should().Be(GDExitCode.Success, $"init should succeed for preset '{preset}'");

            // Validate
            var output = new StringWriter();
            var validateCmd = new GDConfigValidateCommand(_tempDir!, explain: false, output: output);
            var validateResult = await validateCmd.ExecuteAsync();

            validateResult.Should().Be(GDExitCode.Success,
                $"preset '{preset}' should produce valid config, but got: {output}");
        }
    }
}
