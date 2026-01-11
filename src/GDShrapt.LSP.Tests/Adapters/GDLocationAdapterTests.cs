using GDShrapt.LSP;
using GDShrapt.Reader;
using Xunit;

namespace GDShrapt.LSP.Tests;

public class GDLocationAdapterTests
{
    private static readonly GDScriptReader Reader = new();

    [Fact]
    public void ToLspRange_ValidPosition_ConvertsToZeroBased()
    {
        // Arrange - GDShrapt uses 1-based, LSP uses 0-based
        // Note: GDLocationAdapter.ToLspRange converts start but keeps end as-is for exclusive range
        int startLine = 1;
        int startColumn = 1;
        int endLine = 1;
        int endColumn = 10;

        // Act
        var range = GDLocationAdapter.ToLspRange(startLine, startColumn, endLine, endColumn);

        // Assert
        Assert.NotNull(range);
        Assert.Equal(0, range.Start.Line);       // 1-based -> 0-based
        Assert.Equal(0, range.Start.Character);  // 1-based -> 0-based
        Assert.Equal(0, range.End.Line);
        Assert.Equal(10, range.End.Character);   // End character is exclusive in LSP
    }

    [Fact]
    public void FromNode_VariableDeclaration_ReturnsValidLocation()
    {
        // Arrange
        var code = "var health: int = 100";
        var classDecl = Reader.ParseFileContent(code);
        var filePath = "/test/script.gd";

        // Act
        var location = GDLocationAdapter.FromNode(classDecl, filePath);

        // Assert
        Assert.NotNull(location);
        Assert.Equal(GDDocumentManager.PathToUri(filePath), location.Uri);
        Assert.NotNull(location.Range);
    }

    [Fact]
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
        Assert.NotNull(location);
        Assert.Equal(GDDocumentManager.PathToUri(filePath), location.Uri);
    }

    [Fact]
    public void FromNode_NullNode_ReturnsNull()
    {
        // Arrange
        GDNode? node = null;

        // Act
        var location = GDLocationAdapter.FromNode(node!, "/test/script.gd");

        // Assert
        Assert.Null(location);
    }
}
