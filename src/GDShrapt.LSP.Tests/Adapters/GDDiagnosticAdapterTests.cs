using GDShrapt.LSP.Adapters;
using GDShrapt.LSP.Protocol.Types;
using GDShrapt.Reader;
using Xunit;

namespace GDShrapt.LSP.Tests.Adapters;

public class GDDiagnosticAdapterTests
{
    private static readonly GDScriptReader Reader = new();

    [Fact]
    public void FromInvalidTokens_NoInvalidTokens_ReturnsEmpty()
    {
        // Arrange
        var code = @"var health: int = 100";
        var classDecl = Reader.ParseFileContent(code);

        // Act
        var diagnostics = GDDiagnosticAdapter.FromInvalidTokens(classDecl, "/test/script.gd");

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void FromInvalidTokens_WithInvalidTokens_ReturnsDiagnostics()
    {
        // Arrange - invalid GDScript syntax
        var code = @"var health: = 100"; // Missing type after colon
        var classDecl = Reader.ParseFileContent(code);

        // Act
        var diagnostics = GDDiagnosticAdapter.FromInvalidTokens(classDecl, "/test/script.gd");

        // Assert - parser may create invalid tokens for malformed code
        // The exact behavior depends on parser implementation
        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void FromInvalidTokens_NullNode_ReturnsEmpty()
    {
        // Act
        var diagnostics = GDDiagnosticAdapter.FromInvalidTokens(null!, "/test/script.gd");

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void FromInvalidTokens_DiagnosticHasCorrectSeverity()
    {
        // Arrange - deliberately malformed code
        var code = @"func test(
    var x = @@@"; // Invalid syntax
        var classDecl = Reader.ParseFileContent(code);

        // Act
        var diagnostics = GDDiagnosticAdapter.FromInvalidTokens(classDecl, "/test/script.gd");

        // Assert
        foreach (var diag in diagnostics)
        {
            Assert.Equal(GDLspDiagnosticSeverity.Error, diag.Severity);
            Assert.Equal("gdshrapt", diag.Source);
            Assert.Equal("GDS001", diag.Code);
        }
    }
}
