using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDFormatCommandExcludeTests
{
    private string? _tempDir;

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempDir != null && Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [TestMethod]
    public async Task FormatDirectory_WithExclude_SkipsExcludedFiles()
    {
        // Arrange
        _tempDir = Path.Combine(Path.GetTempPath(), "gdshrapt_format_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, "addons", "plugin"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "scripts"));

        // Create .gd files with bad formatting (missing trailing newline to trigger formatting)
        var content = "extends Node\nvar x=1";
        File.WriteAllText(Path.Combine(_tempDir, "addons", "plugin", "excluded.gd"), content);
        File.WriteAllText(Path.Combine(_tempDir, "scripts", "included.gd"), content);

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var cmd = new GDFormatCommand(
            _tempDir,
            formatter,
            output,
            checkOnly: true,
            excludePatterns: new List<string> { "addons/**" });

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        var text = output.ToString();
        text.Should().NotContain("excluded.gd");
        text.Should().Contain("included.gd");
    }

    [TestMethod]
    public async Task FormatDirectory_WithNoExclude_ProcessesAllFiles()
    {
        // Arrange
        _tempDir = Path.Combine(Path.GetTempPath(), "gdshrapt_format_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, "addons", "plugin"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "scripts"));

        var content = "extends Node\nvar x=1";
        File.WriteAllText(Path.Combine(_tempDir, "addons", "plugin", "addon.gd"), content);
        File.WriteAllText(Path.Combine(_tempDir, "scripts", "script.gd"), content);

        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var cmd = new GDFormatCommand(
            _tempDir,
            formatter,
            output,
            checkOnly: true);

        // Act
        var result = await cmd.ExecuteAsync();

        // Assert
        var text = output.ToString();
        text.Should().Contain("2 file(s)");
    }
}
