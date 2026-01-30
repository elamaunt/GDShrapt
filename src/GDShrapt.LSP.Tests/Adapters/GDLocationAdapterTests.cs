using GDShrapt.LSP;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDLocationAdapterTests
{
    private static readonly GDScriptReader Reader = new();

    [TestMethod]
    public void ToLspRange_ValidPosition_ConvertsToZeroBased()
    {
        // Arrange - Line is 1-based (diagnostics), Column is 0-based (Godot standard)
        // ToLspRange converts Line from 1-based to 0-based, Column stays 0-based
        int startLine = 1;
        int startColumn = 0;  // 0-based (Godot standard)
        int endLine = 1;
        int endColumn = 10;   // 0-based (Godot standard)

        // Act
        var range = GDLocationAdapter.ToLspRange(startLine, startColumn, endLine, endColumn);

        // Assert
        range.Should().NotBeNull();
        range.Start.Line.Should().Be(0);       // Line: 1-based -> 0-based
        range.Start.Character.Should().Be(0);  // Column: 0-based (no conversion)
        range.End.Line.Should().Be(0);         // Line: 1-based -> 0-based
        range.End.Character.Should().Be(10);   // Column: 0-based (no conversion)
    }

    [TestMethod]
    public void FromNode_VariableDeclaration_ReturnsValidLocation()
    {
        // Arrange
        var code = "var health: int = 100";
        var classDecl = Reader.ParseFileContent(code);
        var filePath = "/test/script.gd";

        // Act
        var location = GDLocationAdapter.FromNode(classDecl, filePath);

        // Assert
        location.Should().NotBeNull();
        location.Uri.Should().Be(GDDocumentManager.PathToUri(filePath));
        location.Range.Should().NotBeNull();
    }

    [TestMethod]
    public void FromNode_MethodDeclaration_ReturnsValidLocation()
    {
        // Arrange
        var code = @"
func test_method():
    pass
";
        var classDecl = Reader.ParseFileContent(code);
        var filePath = "/test/script.gd";

        // Act
        var location = GDLocationAdapter.FromNode(classDecl, filePath);

        // Assert
        location.Should().NotBeNull();
        location.Uri.Should().Be(GDDocumentManager.PathToUri(filePath));
    }

    [TestMethod]
    public void FromNode_NullNode_ReturnsNull()
    {
        // Arrange
        GDNode? node = null;

        // Act
        var location = GDLocationAdapter.FromNode(node!, "/test/script.gd");

        // Assert
        location.Should().BeNull();
    }
}
