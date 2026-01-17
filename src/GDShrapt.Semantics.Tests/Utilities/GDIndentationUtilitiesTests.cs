using FluentAssertions;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Semantics;

[TestClass]
public class GDIndentationUtilitiesTests
{
    #region GetIndentationFromText Tests

    [TestMethod]
    [DataRow("\tvar x = 1", "\t")]
    [DataRow("\t\tvar x = 1", "\t\t")]
    [DataRow("    var x = 1", "    ")]
    [DataRow("var x = 1", "")]
    [DataRow("\t    mixed", "\t    ")]
    public void GetIndentationFromText_ExtractsCorrectly(string input, string expected)
    {
        GDIndentationUtilities.GetIndentationFromText(input).Should().Be(expected);
    }

    [TestMethod]
    public void GetIndentationFromText_EmptyOrNull_ReturnsEmpty()
    {
        GDIndentationUtilities.GetIndentationFromText("").Should().Be("");
        GDIndentationUtilities.GetIndentationFromText(null).Should().Be("");
    }

    #endregion

    #region IndentCode Tests

    [TestMethod]
    public void IndentCode_SingleLine_AddsIndent()
    {
        var code = "var x = 1";
        var result = GDIndentationUtilities.IndentCode(code, "\t");
        result.Should().Be("\tvar x = 1");
    }

    [TestMethod]
    public void IndentCode_MultipleLines_IndentsAll()
    {
        var code = "var x = 1\nvar y = 2\nvar z = 3";
        var result = GDIndentationUtilities.IndentCode(code, "\t");
        result.Should().Be("\tvar x = 1\n\tvar y = 2\n\tvar z = 3");
    }

    [TestMethod]
    public void IndentCode_EmptyLines_NotIndented()
    {
        var code = "var x = 1\n\nvar y = 2";
        var result = GDIndentationUtilities.IndentCode(code, "\t");
        result.Should().Be("\tvar x = 1\n\n\tvar y = 2");
    }

    [TestMethod]
    public void IndentCode_WithLevel_UsesCorrectIndent()
    {
        var code = "var x = 1";

        var resultTabs = GDIndentationUtilities.IndentCode(code, 2, useTabs: true);
        resultTabs.Should().Be("\t\tvar x = 1");

        var resultSpaces = GDIndentationUtilities.IndentCode(code, 1, useTabs: false);
        resultSpaces.Should().Be("    var x = 1");
    }

    #endregion

    #region BuildIndentation Tests

    [TestMethod]
    public void BuildIndentation_WithTabs_ReturnsCorrectTabs()
    {
        GDIndentationUtilities.BuildIndentation(1, useTabs: true).Should().Be("\t");
        GDIndentationUtilities.BuildIndentation(2, useTabs: true).Should().Be("\t\t");
        GDIndentationUtilities.BuildIndentation(3, useTabs: true).Should().Be("\t\t\t");
    }

    [TestMethod]
    public void BuildIndentation_WithSpaces_ReturnsCorrectSpaces()
    {
        GDIndentationUtilities.BuildIndentation(1, useTabs: false).Should().Be("    ");
        GDIndentationUtilities.BuildIndentation(2, useTabs: false).Should().Be("        ");
    }

    [TestMethod]
    public void BuildIndentation_ZeroOrNegative_ReturnsEmpty()
    {
        GDIndentationUtilities.BuildIndentation(0).Should().Be("");
        GDIndentationUtilities.BuildIndentation(-1).Should().Be("");
    }

    #endregion

    #region DefaultIndentChar Tests

    [TestMethod]
    public void DefaultIndentChar_IsTab()
    {
        GDIndentationUtilities.DefaultIndentChar.Should().Be('\t');
    }

    [TestMethod]
    public void DefaultSpacesPerIndent_IsFour()
    {
        GDIndentationUtilities.DefaultSpacesPerIndent.Should().Be(4);
    }

    #endregion
}
