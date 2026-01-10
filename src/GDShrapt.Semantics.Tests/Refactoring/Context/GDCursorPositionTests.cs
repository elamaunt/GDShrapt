using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Refactoring.Context;

[TestClass]
public class GDCursorPositionTests
{
    [TestMethod]
    public void Zero_ReturnsPositionAtOrigin()
    {
        var position = GDCursorPosition.Zero;

        Assert.AreEqual(0, position.Line);
        Assert.AreEqual(0, position.Column);
    }

    [TestMethod]
    public void Constructor_SetsLineAndColumn()
    {
        var position = new GDCursorPosition(5, 10);

        Assert.AreEqual(5, position.Line);
        Assert.AreEqual(10, position.Column);
    }

    [TestMethod]
    public void IsBefore_SameLineEarlierColumn_ReturnsTrue()
    {
        var earlier = new GDCursorPosition(5, 3);
        var later = new GDCursorPosition(5, 10);

        Assert.IsTrue(earlier.IsBefore(later));
        Assert.IsFalse(later.IsBefore(earlier));
    }

    [TestMethod]
    public void IsBefore_EarlierLine_ReturnsTrue()
    {
        var earlier = new GDCursorPosition(3, 10);
        var later = new GDCursorPosition(5, 5);

        Assert.IsTrue(earlier.IsBefore(later));
        Assert.IsFalse(later.IsBefore(earlier));
    }

    [TestMethod]
    public void IsBefore_SamePosition_ReturnsFalse()
    {
        var pos1 = new GDCursorPosition(5, 10);
        var pos2 = new GDCursorPosition(5, 10);

        Assert.IsFalse(pos1.IsBefore(pos2));
    }

    [TestMethod]
    public void IsAfter_SameLineLaterColumn_ReturnsTrue()
    {
        var earlier = new GDCursorPosition(5, 3);
        var later = new GDCursorPosition(5, 10);

        Assert.IsTrue(later.IsAfter(earlier));
        Assert.IsFalse(earlier.IsAfter(later));
    }

    [TestMethod]
    public void IsAfter_LaterLine_ReturnsTrue()
    {
        var earlier = new GDCursorPosition(3, 10);
        var later = new GDCursorPosition(5, 5);

        Assert.IsTrue(later.IsAfter(earlier));
        Assert.IsFalse(earlier.IsAfter(later));
    }

    [TestMethod]
    public void IsAtOrBefore_SamePosition_ReturnsTrue()
    {
        var pos1 = new GDCursorPosition(5, 10);
        var pos2 = new GDCursorPosition(5, 10);

        Assert.IsTrue(pos1.IsAtOrBefore(pos2));
    }

    [TestMethod]
    public void IsAtOrAfter_SamePosition_ReturnsTrue()
    {
        var pos1 = new GDCursorPosition(5, 10);
        var pos2 = new GDCursorPosition(5, 10);

        Assert.IsTrue(pos1.IsAtOrAfter(pos2));
    }

    [TestMethod]
    public void ToString_FormatsCorrectly()
    {
        var position = new GDCursorPosition(5, 10);

        Assert.AreEqual("L5:C10", position.ToString());
    }

    [TestMethod]
    public void Equality_SameValues_AreEqual()
    {
        var pos1 = new GDCursorPosition(5, 10);
        var pos2 = new GDCursorPosition(5, 10);

        Assert.AreEqual(pos1, pos2);
        Assert.IsTrue(pos1 == pos2);
    }

    [TestMethod]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var pos1 = new GDCursorPosition(5, 10);
        var pos2 = new GDCursorPosition(5, 11);

        Assert.AreNotEqual(pos1, pos2);
        Assert.IsTrue(pos1 != pos2);
    }
}
