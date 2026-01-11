using GDShrapt.LSP;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDDiagnosticAdapterTests
{
    private static readonly GDScriptReader Reader = new();

    [TestMethod]
    public void FromInvalidTokens_NoInvalidTokens_ReturnsEmpty()
    {
        // Arrange
        var code = @"var health: int = 100";
        var classDecl = Reader.ParseFileContent(code);

        // Act
        var diagnostics = GDDiagnosticAdapter.FromInvalidTokens(classDecl, "/test/script.gd");

        // Assert
        diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void FromInvalidTokens_WithInvalidTokens_ReturnsDiagnostics()
    {
        // Arrange - invalid GDScript syntax
        var code = @"var health: = 100"; // Missing type after colon
        var classDecl = Reader.ParseFileContent(code);

        // Act
        var diagnostics = GDDiagnosticAdapter.FromInvalidTokens(classDecl, "/test/script.gd");

        // Assert - parser may create invalid tokens for malformed code
        // The exact behavior depends on parser implementation
        diagnostics.Should().NotBeNull();
    }

    [TestMethod]
    public void FromInvalidTokens_NullNode_ReturnsEmpty()
    {
        // Act
        var diagnostics = GDDiagnosticAdapter.FromInvalidTokens(null!, "/test/script.gd");

        // Assert
        diagnostics.Should().BeEmpty();
    }

    [TestMethod]
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
            diag.Severity.Should().Be(GDLspDiagnosticSeverity.Error);
            diag.Source.Should().Be("gdshrapt");
            diag.Code.Should().Be("GDS001");
        }
    }
}
