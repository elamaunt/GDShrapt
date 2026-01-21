using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

/// <summary>
/// Tests for GDExtractStyleCommand.
/// Exit codes: 0=Success, 2=File error.
/// </summary>
[TestClass]
public class GDExtractStyleCommandTests
{
    private string? _tempFilePath;

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempFilePath != null)
        {
            TestProjectHelper.DeleteTempFile(_tempFilePath);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_ValidFile_ReturnsZero()
    {
        // Arrange
        _tempFilePath = TestProjectHelper.CreateTempScript(@"extends Node

func _ready():
    pass
");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDExtractStyleCommand(_tempFilePath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task ExecuteAsync_NonexistentFile_ReturnsTwo()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDExtractStyleCommand("/nonexistent/file.gd", formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(2);
        output.ToString().Should().Contain("File not found");
    }

    [TestMethod]
    public async Task ExecuteAsync_NonGdFile_ReturnsTwo()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), "test.txt");
        File.WriteAllText(tempFile, "not a gdscript file");

        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDExtractStyleCommand(tempFile, formatter, output);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            result.Should().Be(2);
            output.ToString().Should().Contain("Not a GDScript file");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_TomlFormat_OutputsValidToml()
    {
        // Arrange
        _tempFilePath = TestProjectHelper.CreateTempScript(@"extends Node

func test():
    pass
");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDExtractStyleCommand(
            _tempFilePath,
            formatter,
            output,
            outputFormat: GDExtractStyleOutputFormat.Toml);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("[formatting]");
        outputText.Should().Contain("indent_style");
        outputText.Should().Contain("indent_size");
    }

    [TestMethod]
    public async Task ExecuteAsync_JsonFormat_OutputsValidJson()
    {
        // Arrange
        _tempFilePath = TestProjectHelper.CreateTempScript(@"extends Node

func test():
    pass
");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDExtractStyleCommand(
            _tempFilePath,
            formatter,
            output,
            outputFormat: GDExtractStyleOutputFormat.Json);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString().Trim();
        outputText.Should().StartWith("{");
        outputText.Should().EndWith("}");
        outputText.Should().Contain("\"formatting\":");
        outputText.Should().Contain("\"indentStyle\":");
    }

    [TestMethod]
    public async Task ExecuteAsync_TextFormat_OutputsHumanReadable()
    {
        // Arrange
        _tempFilePath = TestProjectHelper.CreateTempScript(@"extends Node

func test():
    pass
");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDExtractStyleCommand(
            _tempFilePath,
            formatter,
            output,
            outputFormat: GDExtractStyleOutputFormat.Text);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("Extracted Formatting Style");
        outputText.Should().Contain("Indentation:");
        outputText.Should().Contain("Spacing:");
    }

    [TestMethod]
    public async Task ExecuteAsync_TabIndented_DetectsTabStyle()
    {
        // Arrange - use tabs for indentation
        _tempFilePath = TestProjectHelper.CreateTempScript("extends Node\n\nfunc test():\n\tvar x = 1\n\treturn x\n");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDExtractStyleCommand(
            _tempFilePath,
            formatter,
            output,
            outputFormat: GDExtractStyleOutputFormat.Toml);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("indent_style = \"tabs\"");
    }

    [TestMethod]
    public async Task ExecuteAsync_SpaceIndented_DetectsSpaceStyle()
    {
        // Arrange - use 4 spaces for indentation
        _tempFilePath = TestProjectHelper.CreateTempScript("extends Node\n\nfunc test():\n    var x = 1\n    return x\n");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDExtractStyleCommand(
            _tempFilePath,
            formatter,
            output,
            outputFormat: GDExtractStyleOutputFormat.Toml);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("indent_style = \"spaces\"");
    }
}
