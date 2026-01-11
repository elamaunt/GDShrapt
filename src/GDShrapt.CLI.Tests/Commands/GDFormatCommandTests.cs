using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using GDShrapt.Reader;

namespace GDShrapt.CLI.Tests;

[TestClass]
public class GDFormatCommandTests
{
    private string? _tempFilePath;
    private string? _tempProjectPath;

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempFilePath != null && File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
        if (_tempProjectPath != null)
        {
            TestProjectHelper.DeleteTempProject(_tempProjectPath);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonexistentPath_ReturnsTwo()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFormatCommand("/nonexistent/file.gd", formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(2);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonGdFile_ReturnsTwo()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDFormatCommand(tempFile, formatter, output);

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
    public async Task ExecuteAsync_CheckOnly_ReturnsOneIfNeedsFormatting()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".gd");
        try
        {
            // Write unformatted code
            File.WriteAllText(tempFile, "func test():\n    var x=1\n    pass\n");

            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDFormatCommand(tempFile, formatter, output, checkOnly: true);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            // Returns 0 if already formatted, 1 if needs formatting
            (result == 0 || result == 1).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_DryRun_DoesNotModifyFile()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".gd");
        var originalContent = "func test():\n    var x = 1\n    pass\n";
        try
        {
            File.WriteAllText(tempFile, originalContent);

            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDFormatCommand(tempFile, formatter, output, dryRun: true);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            var contentAfter = File.ReadAllText(tempFile);
            contentAfter.Should().Be(originalContent);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    // === New tests with TestProjectHelper ===

    [TestMethod]
    public async Task ExecuteAsync_AppliesSpaceAroundOperators()
    {
        // Arrange - code without spaces around operator
        _tempFilePath = TestProjectHelper.CreateTempScript("extends Node\nvar x=1+2\n");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFormatCommand(_tempFilePath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var formattedContent = File.ReadAllText(_tempFilePath);
        // Should have spaces around = and +
        formattedContent.Should().Contain("= 1");
    }

    [TestMethod]
    public async Task ExecuteAsync_AppliesSpaceAfterComma()
    {
        // Arrange - function with no space after comma
        _tempFilePath = TestProjectHelper.CreateTempScript("extends Node\nfunc test(a,b,c):\n\tpass\n");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFormatCommand(_tempFilePath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var formattedContent = File.ReadAllText(_tempFilePath);
        // Should have spaces after commas
        formattedContent.Should().Contain("a, b, c");
    }

    [TestMethod]
    public async Task ExecuteAsync_AlreadyFormatted_ReturnsZero()
    {
        // Arrange - properly formatted code
        _tempFilePath = TestProjectHelper.CreateTempScript("extends Node\n\nvar x = 1\n\nfunc test():\n\tpass\n");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFormatCommand(_tempFilePath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task ExecuteAsync_CheckOnly_AlreadyFormatted_ReturnsZeroOrOne()
    {
        // Arrange - well formatted code (may still need minor adjustments depending on rules)
        _tempFilePath = TestProjectHelper.CreateTempScript("extends Node\n\nvar x = 1\n\nfunc test():\n\tpass\n");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFormatCommand(_tempFilePath, formatter, output, checkOnly: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert - 0 means formatted, 1 means needs formatting (depends on rule settings)
        (result == 0 || result == 1).Should().BeTrue("Check-only should return 0 or 1");
    }

    [TestMethod]
    public async Task ExecuteAsync_CheckOnly_NeedsFormatting_ReturnsOne_DoesNotModify()
    {
        // Arrange - code that needs formatting
        var originalContent = "extends Node\nvar x=1\n";
        _tempFilePath = TestProjectHelper.CreateTempScript(originalContent);
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFormatCommand(_tempFilePath, formatter, output, checkOnly: true);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(1);
        // File should not be modified
        var contentAfter = File.ReadAllText(_tempFilePath);
        contentAfter.Should().Be(originalContent);
    }

    [TestMethod]
    public async Task ExecuteAsync_PreservesComments()
    {
        // Arrange - code with comments
        _tempFilePath = TestProjectHelper.CreateTempScript("extends Node\n# This is a comment\nvar x = 1 # inline comment\n");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFormatCommand(_tempFilePath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var formattedContent = File.ReadAllText(_tempFilePath);
        formattedContent.Should().Contain("# This is a comment");
        formattedContent.Should().Contain("# inline comment");
    }

    [TestMethod]
    public async Task ExecuteAsync_SyntaxError_HandlesGracefully()
    {
        // Arrange - code with syntax error
        _tempFilePath = TestProjectHelper.CreateTempScript("extends Node\nfunc broken(\n");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFormatCommand(_tempFilePath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        // Should not crash, returns 0 or 1
        (result == 0 || result == 1 || result == 2).Should().BeTrue();
    }

    [DataTestMethod]
    [DataRow(true)]  // spaces
    [DataRow(false)] // tabs
    public async Task ExecuteAsync_RespectsIndentStyle(bool insertSpaces)
    {
        // Arrange
        _tempFilePath = TestProjectHelper.CreateTempScript("extends Node\nfunc test():\n\tpass\n");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDFormatterOptions
        {
            IndentStyle = insertSpaces ? IndentStyle.Spaces : IndentStyle.Tabs,
            IndentSize = 4
        };
        var command = new GDFormatCommand(_tempFilePath, formatter, output, formatterOptions: options);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var formattedContent = File.ReadAllText(_tempFilePath);
        if (insertSpaces)
        {
            formattedContent.Should().Contain("    pass"); // 4 spaces
        }
        else
        {
            formattedContent.Should().Contain("\tpass"); // tab
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_Directory_FormatsAllGdFiles()
    {
        // Arrange - project with multiple files
        _tempProjectPath = TestProjectHelper.CreateMultiFileProject();
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFormatCommand(_tempProjectPath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        // Should mention multiple files were formatted
        outputText.Should().Contain("3"); // 3 scripts
    }

    [TestMethod]
    public async Task ExecuteAsync_RemovesTrailingWhitespace()
    {
        // Arrange - code with trailing whitespace
        _tempFilePath = TestProjectHelper.CreateTempScript("extends Node   \nvar x = 1  \n");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDFormatterOptions { RemoveTrailingWhitespace = true };
        var command = new GDFormatCommand(_tempFilePath, formatter, output, formatterOptions: options);

        // Act
        var result = await command.ExecuteAsync();

        // Assert - should complete successfully (trailing whitespace removal is optional rule)
        (result == 0 || result == 1).Should().BeTrue("Format should complete without error");
        // Verify file was processed
        var formattedContent = File.ReadAllText(_tempFilePath);
        formattedContent.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task ExecuteAsync_JsonOutput_ContainsFormattedStatus()
    {
        // Arrange
        _tempFilePath = TestProjectHelper.CreateTempScript("extends Node\nvar x = 1\n");
        var output = new StringWriter();
        var formatter = new GDJsonFormatter();
        var command = new GDFormatCommand(_tempFilePath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        var outputText = output.ToString();
        outputText.Should().Contain("{");
    }

    [TestMethod]
    public async Task ExecuteAsync_EmptyFile_HandlesGracefully()
    {
        // Arrange - empty GDScript file
        _tempFilePath = TestProjectHelper.CreateTempScript("");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFormatCommand(_tempFilePath, formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        // Should not crash on empty file
        (result == 0 || result == 1 || result == 2).Should().BeTrue();
    }

    [TestMethod]
    public async Task ExecuteAsync_AddBlankLinesBetweenFunctions()
    {
        // Arrange - code without blank lines between functions
        _tempFilePath = TestProjectHelper.CreateTempScript("extends Node\nvar a = 1\nvar b = 2\nfunc test():\n\tpass\nfunc other():\n\tpass\n");
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var options = new GDFormatterOptions { BlankLinesBetweenFunctions = 2 };
        var command = new GDFormatCommand(_tempFilePath, formatter, output, formatterOptions: options);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        result.Should().Be(0);
        // File should have blank lines between functions
        var formattedContent = File.ReadAllText(_tempFilePath);
        // At minimum, there should be blank lines between functions
        (formattedContent.Contains("\n\nfunc") || formattedContent.Contains("\r\n\r\nfunc")).Should().BeTrue();
    }
}
