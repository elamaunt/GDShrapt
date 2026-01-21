using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

/// <summary>
/// Tests for GDParseCommand.
/// Exit codes: 0=Success, 1=Syntax errors, 2=File error.
/// </summary>
[TestClass]
public class GDParseCommandTests
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
        var command = new GDParseCommand(_tempFilePath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        output.ToString().Should().Contain("GDClassDeclaration");
    }

    [TestMethod]
    public async Task ExecuteAsync_FileWithSyntaxErrors_ReturnsOne()
    {
        // Arrange
        _tempFilePath = TestProjectHelper.CreateTempScript(@"extends Node

func _ready(:
    pass
@@@@!!!!
");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDParseCommand(_tempFilePath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(1, "File with syntax errors should return exit code 1");
    }

    [TestMethod]
    public async Task ExecuteAsync_NonexistentFile_ReturnsTwo()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDParseCommand("/nonexistent/file.gd", formatter, output);

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
            var command = new GDParseCommand(tempFile, formatter, output);

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
    public async Task ExecuteAsync_TreeFormat_OutputsIndentedTree()
    {
        // Arrange
        _tempFilePath = TestProjectHelper.CreateTempScript(@"extends Node

func test():
    pass
");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDParseCommand(
            _tempFilePath,
            formatter,
            output,
            outputFormat: GDParseOutputFormat.Tree);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("GDClassDeclaration");
        outputText.Should().Contain("GDMethodDeclaration");
        // Tree format uses indentation
        outputText.Should().Contain("  "); // At least some indentation
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
        var command = new GDParseCommand(
            _tempFilePath,
            formatter,
            output,
            outputFormat: GDParseOutputFormat.Json);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString().Trim();
        outputText.Should().StartWith("{");
        outputText.Should().EndWith("}");
        outputText.Should().Contain("\"type\":");
        outputText.Should().Contain("\"children\":");
    }

    [TestMethod]
    public async Task ExecuteAsync_TokensFormat_OutputsTokenList()
    {
        // Arrange
        _tempFilePath = TestProjectHelper.CreateTempScript(@"extends Node

func test():
    pass
");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDParseCommand(
            _tempFilePath,
            formatter,
            output,
            outputFormat: GDParseOutputFormat.Tokens);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        // Tokens format outputs token types
        outputText.Should().Contain("GDExtendsKeyword");
        outputText.Should().Contain("GDFuncKeyword");
        outputText.Should().Contain("GDPassKeyword");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithPositions_IncludesLineColumn()
    {
        // Arrange
        _tempFilePath = TestProjectHelper.CreateTempScript(@"extends Node

func test():
    pass
");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDParseCommand(
            _tempFilePath,
            formatter,
            output,
            outputFormat: GDParseOutputFormat.Tree,
            showPositions: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        // Position format: [line:col-line:col]
        outputText.Should().MatchRegex(@"\[\d+:\d+-\d+:\d+\]");
    }
}
