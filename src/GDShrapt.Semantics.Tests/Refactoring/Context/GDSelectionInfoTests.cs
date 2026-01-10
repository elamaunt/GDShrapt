using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Refactoring.Context;

[TestClass]
public class GDSelectionInfoTests
{
    [TestMethod]
    public void Constructor_WithCoordinates_SetsProperties()
    {
        var selection = new GDSelectionInfo(1, 5, 3, 10, "selected text");

        Assert.AreEqual(1, selection.StartLine);
        Assert.AreEqual(5, selection.StartColumn);
        Assert.AreEqual(3, selection.EndLine);
        Assert.AreEqual(10, selection.EndColumn);
        Assert.AreEqual("selected text", selection.Text);
    }

    [TestMethod]
    public void Constructor_WithPositions_SetsProperties()
    {
        var start = new GDCursorPosition(1, 5);
        var end = new GDCursorPosition(3, 10);
        var selection = new GDSelectionInfo(start, end, "selected text");

        Assert.AreEqual(start, selection.Start);
        Assert.AreEqual(end, selection.End);
        Assert.AreEqual("selected text", selection.Text);
    }

    [TestMethod]
    public void HasSelection_WithText_ReturnsTrue()
    {
        var selection = new GDSelectionInfo(0, 0, 0, 5, "hello");

        Assert.IsTrue(selection.HasSelection);
    }

    [TestMethod]
    public void HasSelection_EmptyText_ReturnsFalse()
    {
        var selection = new GDSelectionInfo(0, 0, 0, 0, "");

        Assert.IsFalse(selection.HasSelection);
    }

    [TestMethod]
    public void HasSelection_NullText_ReturnsFalse()
    {
        var selection = new GDSelectionInfo(0, 0, 0, 0, null!);

        Assert.IsFalse(selection.HasSelection);
    }

    [TestMethod]
    public void IsMultiLine_SameLine_ReturnsFalse()
    {
        var selection = new GDSelectionInfo(5, 0, 5, 10, "same line");

        Assert.IsFalse(selection.IsMultiLine);
    }

    [TestMethod]
    public void IsMultiLine_DifferentLines_ReturnsTrue()
    {
        var selection = new GDSelectionInfo(5, 0, 7, 10, "multi\nline");

        Assert.IsTrue(selection.IsMultiLine);
    }

    [TestMethod]
    public void None_ReturnsEmptySelection()
    {
        var selection = GDSelectionInfo.None;

        Assert.AreEqual(0, selection.StartLine);
        Assert.AreEqual(0, selection.StartColumn);
        Assert.AreEqual(0, selection.EndLine);
        Assert.AreEqual(0, selection.EndColumn);
        Assert.IsFalse(selection.HasSelection);
    }

    [TestMethod]
    public void Empty_ReturnsEmptySelectionAtPosition()
    {
        var position = new GDCursorPosition(5, 10);
        var selection = GDSelectionInfo.Empty(position);

        Assert.AreEqual(position, selection.Start);
        Assert.AreEqual(position, selection.End);
        Assert.IsFalse(selection.HasSelection);
    }

    [TestMethod]
    public void Contains_PositionInside_ReturnsTrue()
    {
        var selection = new GDSelectionInfo(1, 5, 3, 10, "text");
        var inside = new GDCursorPosition(2, 0);

        Assert.IsTrue(selection.Contains(inside));
    }

    [TestMethod]
    public void Contains_PositionAtStart_ReturnsTrue()
    {
        var selection = new GDSelectionInfo(1, 5, 3, 10, "text");
        var atStart = new GDCursorPosition(1, 5);

        Assert.IsTrue(selection.Contains(atStart));
    }

    [TestMethod]
    public void Contains_PositionAtEnd_ReturnsTrue()
    {
        var selection = new GDSelectionInfo(1, 5, 3, 10, "text");
        var atEnd = new GDCursorPosition(3, 10);

        Assert.IsTrue(selection.Contains(atEnd));
    }

    [TestMethod]
    public void Contains_PositionBefore_ReturnsFalse()
    {
        var selection = new GDSelectionInfo(1, 5, 3, 10, "text");
        var before = new GDCursorPosition(0, 0);

        Assert.IsFalse(selection.Contains(before));
    }

    [TestMethod]
    public void Contains_PositionAfter_ReturnsFalse()
    {
        var selection = new GDSelectionInfo(1, 5, 3, 10, "text");
        var after = new GDCursorPosition(5, 0);

        Assert.IsFalse(selection.Contains(after));
    }

    [TestMethod]
    public void Contains_LineAndColumn_ReturnsCorrectly()
    {
        var selection = new GDSelectionInfo(1, 5, 3, 10, "text");

        Assert.IsTrue(selection.Contains(2, 0));
        Assert.IsFalse(selection.Contains(0, 0));
    }

    [TestMethod]
    public void ToString_WithSelection_FormatsCorrectly()
    {
        var selection = new GDSelectionInfo(1, 5, 1, 10, "hello");

        var result = selection.ToString();

        Assert.IsTrue(result.Contains("L1:C5"));
        Assert.IsTrue(result.Contains("L1:C10"));
        Assert.IsTrue(result.Contains("hello"));
    }

    [TestMethod]
    public void ToString_WithoutSelection_IndicatesNoSelection()
    {
        var selection = GDSelectionInfo.None;

        var result = selection.ToString();

        Assert.IsTrue(result.Contains("no selection"));
    }

    [TestMethod]
    public void ToString_LongText_Truncates()
    {
        var longText = "This is a very long selected text that should be truncated";
        var selection = new GDSelectionInfo(0, 0, 0, 50, longText);

        var result = selection.ToString();

        Assert.IsTrue(result.Contains("..."));
        Assert.IsTrue(result.Length < longText.Length + 30); // Some margin for formatting
    }
}
