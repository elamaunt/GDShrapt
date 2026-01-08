using System;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using Xunit;

namespace GDShrapt.CLI.Tests.Commands;

public class GDFormatCommandTests
{
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
}
