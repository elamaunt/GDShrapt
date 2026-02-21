using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDConfigShowCommandTests
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
    public async Task Show_Raw_DisplaysConfigContent()
    {
        // Arrange - create a strict config
        var initCmd = new GDConfigInitCommand(_tempDir!, "strict", false);
        await initCmd.ExecuteAsync();

        var output = new StringWriter();
        var showCmd = new GDConfigShowCommand(_tempDir!, effective: false, format: "text", output: output);

        // Act
        var result = await showCmd.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Success);
        var text = output.ToString();
        text.Should().Contain("FailOnWarning");
        text.Should().Contain("True");
        text.Should().Contain("Configuration (raw):");
    }

    [TestMethod]
    public async Task Show_Effective_NoFile_ShowsDefaults()
    {
        // Arrange - no config file, use --effective
        var output = new StringWriter();
        var showCmd = new GDConfigShowCommand(_tempDir!, effective: true, format: "text", output: output);

        // Act
        var result = await showCmd.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Success);
        var text = output.ToString();
        text.Should().Contain("Configuration (effective):");
        text.Should().Contain("MaxLineLength:    120"); // default value
    }

    [TestMethod]
    public async Task Show_Json_OutputsValidJson()
    {
        // Arrange - create a ci config
        var initCmd = new GDConfigInitCommand(_tempDir!, "ci", false);
        await initCmd.ExecuteAsync();

        var output = new StringWriter();
        var showCmd = new GDConfigShowCommand(_tempDir!, effective: false, format: "json", output: output);

        // Act
        var result = await showCmd.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Success);
        var json = output.ToString();

        // Should be valid JSON
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        parsed.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [TestMethod]
    public async Task Show_Raw_NoFile_ReturnsFatal()
    {
        // Arrange - no config file, raw mode
        var output = new StringWriter();
        var showCmd = new GDConfigShowCommand(_tempDir!, effective: false, format: "text", output: output);

        // Act
        var result = await showCmd.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Fatal);
    }

    [TestMethod]
    public async Task Show_DirectoryNotFound_ReturnsFatal()
    {
        // Arrange - nonexistent directory
        var output = new StringWriter();
        var showCmd = new GDConfigShowCommand(
            Path.Combine(_tempDir!, "nonexistent_dir"),
            effective: false, format: "text", output: output);

        // Act
        var result = await showCmd.ExecuteAsync();

        // Assert
        result.Should().Be(GDExitCode.Fatal);
    }
}
