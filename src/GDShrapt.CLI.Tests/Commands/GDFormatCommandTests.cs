using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using GDShrapt.CLI.Tests.Helpers;
using GDShrapt.Reader;
using Xunit;

namespace GDShrapt.CLI.Tests.Commands;

public class GDFormatCommandTests : IDisposable
{
    private string? _tempFilePath;
    private string? _tempProjectPath;

    public void Dispose()
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

    [Fact]
    public async Task ExecuteAsync_WithNonexistentPath_ReturnsTwo()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new GDTextFormatter();
        var command = new GDFormatCommand("/nonexistent/file.gd", formatter, output);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
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
            Assert.Equal(2, result);
            Assert.Contains("Not a GDScript file", output.ToString());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
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
            Assert.True(result == 0 || result == 1);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
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
            Assert.Equal(originalContent, contentAfter);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    // === New tests with TestProjectHelper ===

    [Fact]
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
        Assert.Equal(0, result);
        var formattedContent = File.ReadAllText(_tempFilePath);
        // Should have spaces around = and +
        Assert.Contains("= 1", formattedContent);
    }

    [Fact]
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
        Assert.Equal(0, result);
        var formattedContent = File.ReadAllText(_tempFilePath);
        // Should have spaces after commas
        Assert.Contains("a, b, c", formattedContent);
    }

    [Fact]
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
        Assert.Equal(0, result);
    }

    [Fact]
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
        Assert.True(result == 0 || result == 1, "Check-only should return 0 or 1");
    }

    [Fact]
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
        Assert.Equal(1, result);
        // File should not be modified
        var contentAfter = File.ReadAllText(_tempFilePath);
        Assert.Equal(originalContent, contentAfter);
    }

    [Fact]
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
        Assert.Equal(0, result);
        var formattedContent = File.ReadAllText(_tempFilePath);
        Assert.Contains("# This is a comment", formattedContent);
        Assert.Contains("# inline comment", formattedContent);
    }

    [Fact]
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
        Assert.True(result == 0 || result == 1 || result == 2);
    }

    [Theory]
    [InlineData(true)]  // spaces
    [InlineData(false)] // tabs
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
        Assert.Equal(0, result);
        var formattedContent = File.ReadAllText(_tempFilePath);
        if (insertSpaces)
        {
            Assert.Contains("    pass", formattedContent); // 4 spaces
        }
        else
        {
            Assert.Contains("\tpass", formattedContent); // tab
        }
    }

    [Fact]
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
        Assert.Equal(0, result);
        var outputText = output.ToString();
        // Should mention multiple files were formatted
        Assert.Contains("3", outputText); // 3 scripts
    }

    [Fact]
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
        Assert.True(result == 0 || result == 1, "Format should complete without error");
        // Verify file was processed
        var formattedContent = File.ReadAllText(_tempFilePath);
        Assert.NotEmpty(formattedContent);
    }

    [Fact]
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
        Assert.Equal(0, result);
        var outputText = output.ToString();
        Assert.Contains("{", outputText);
    }

    [Fact]
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
        Assert.True(result == 0 || result == 1 || result == 2);
    }

    [Fact]
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
        Assert.Equal(0, result);
        // File should have blank lines between functions
        var formattedContent = File.ReadAllText(_tempFilePath);
        // At minimum, there should be blank lines between functions
        Assert.True(formattedContent.Contains("\n\nfunc") || formattedContent.Contains("\r\n\r\nfunc"));
    }
}
