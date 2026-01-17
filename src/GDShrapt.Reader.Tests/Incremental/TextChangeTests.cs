using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GDShrapt.Reader.Tests.Incremental
{
    [TestClass]
    public class TextChangeTests
    {
        #region Constructor Tests

        [TestMethod]
        public void Constructor_ValidParameters_CreatesChange()
        {
            var change = new GDTextChange(10, 5, "hello");

            change.Start.Should().Be(10);
            change.OldLength.Should().Be(5);
            change.NewText.Should().Be("hello");
        }

        [TestMethod]
        public void Constructor_NullNewText_TreatsAsEmptyString()
        {
            var change = new GDTextChange(10, 5, null);

            change.NewText.Should().Be(string.Empty);
            change.NewLength.Should().Be(0);
        }

        [TestMethod]
        public void Constructor_NegativeStart_ThrowsArgumentOutOfRangeException()
        {
            Action action = () => new GDTextChange(-1, 5, "text");

            action.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("start");
        }

        [TestMethod]
        public void Constructor_NegativeOldLength_ThrowsArgumentOutOfRangeException()
        {
            Action action = () => new GDTextChange(10, -1, "text");

            action.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("oldLength");
        }

        #endregion

        #region Computed Properties Tests

        [TestMethod]
        public void NewLength_ReturnsLengthOfNewText()
        {
            var change = new GDTextChange(0, 0, "hello");

            change.NewLength.Should().Be(5);
        }

        [TestMethod]
        public void OldEnd_ReturnsStartPlusOldLength()
        {
            var change = new GDTextChange(10, 5, "");

            change.OldEnd.Should().Be(15);
        }

        [TestMethod]
        public void NewEnd_ReturnsStartPlusNewLength()
        {
            var change = new GDTextChange(10, 5, "hello world");

            change.NewEnd.Should().Be(21);
        }

        [TestMethod]
        public void Delta_ReturnsLengthDifference()
        {
            var change = new GDTextChange(10, 5, "hello world");

            change.Delta.Should().Be(6); // 11 - 5 = 6
        }

        [TestMethod]
        public void Delta_Deletion_ReturnsNegative()
        {
            var change = new GDTextChange(10, 5, "");

            change.Delta.Should().Be(-5);
        }

        #endregion

        #region Type Tests

        [TestMethod]
        public void IsInsertion_NoOldText_ReturnsTrue()
        {
            var change = GDTextChange.Insert(10, "hello");

            change.IsInsertion.Should().BeTrue();
            change.IsDeletion.Should().BeFalse();
            change.IsReplacement.Should().BeFalse();
        }

        [TestMethod]
        public void IsDeletion_NoNewText_ReturnsTrue()
        {
            var change = GDTextChange.Delete(10, 5);

            change.IsDeletion.Should().BeTrue();
            change.IsInsertion.Should().BeFalse();
            change.IsReplacement.Should().BeFalse();
        }

        [TestMethod]
        public void IsReplacement_BothOldAndNewText_ReturnsTrue()
        {
            var change = GDTextChange.Replace(10, 5, "hello");

            change.IsReplacement.Should().BeTrue();
            change.IsInsertion.Should().BeFalse();
            change.IsDeletion.Should().BeFalse();
        }

        [TestMethod]
        public void IsEmpty_NoChanges_ReturnsTrue()
        {
            var change = new GDTextChange(10, 0, "");

            change.IsEmpty.Should().BeTrue();
        }

        #endregion

        #region Factory Methods Tests

        [TestMethod]
        public void Insert_CreatesInsertionChange()
        {
            var change = GDTextChange.Insert(5, "hello");

            change.Start.Should().Be(5);
            change.OldLength.Should().Be(0);
            change.NewText.Should().Be("hello");
        }

        [TestMethod]
        public void Delete_CreatesDeletionChange()
        {
            var change = GDTextChange.Delete(5, 10);

            change.Start.Should().Be(5);
            change.OldLength.Should().Be(10);
            change.NewText.Should().Be(string.Empty);
        }

        [TestMethod]
        public void Replace_CreatesReplacementChange()
        {
            var change = GDTextChange.Replace(5, 10, "hello");

            change.Start.Should().Be(5);
            change.OldLength.Should().Be(10);
            change.NewText.Should().Be("hello");
        }

        #endregion

        #region Apply Tests

        [TestMethod]
        public void Apply_Insertion_InsertsText()
        {
            var original = "Hello World";
            var change = GDTextChange.Insert(6, "Beautiful ");

            var result = change.Apply(original);

            result.Should().Be("Hello Beautiful World");
        }

        [TestMethod]
        public void Apply_Deletion_RemovesText()
        {
            var original = "Hello Beautiful World";
            var change = GDTextChange.Delete(6, 10);

            var result = change.Apply(original);

            result.Should().Be("Hello World");
        }

        [TestMethod]
        public void Apply_Replacement_ReplacesText()
        {
            var original = "Hello World";
            var change = GDTextChange.Replace(6, 5, "Universe");

            var result = change.Apply(original);

            result.Should().Be("Hello Universe");
        }

        [TestMethod]
        public void Apply_AtStart_Works()
        {
            var original = "Hello";
            var change = GDTextChange.Insert(0, "Say ");

            var result = change.Apply(original);

            result.Should().Be("Say Hello");
        }

        [TestMethod]
        public void Apply_AtEnd_Works()
        {
            var original = "Hello";
            var change = GDTextChange.Insert(5, " World");

            var result = change.Apply(original);

            result.Should().Be("Hello World");
        }

        [TestMethod]
        public void Apply_Empty_ReturnsOriginal()
        {
            var original = "Hello";
            var change = new GDTextChange(2, 0, "");

            var result = change.Apply(original);

            result.Should().Be("Hello");
        }

        [TestMethod]
        public void Apply_NullOriginal_ThrowsArgumentNullException()
        {
            var change = GDTextChange.Insert(0, "text");

            Action action = () => change.Apply(null);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("original");
        }

        [TestMethod]
        public void Apply_StartBeyondLength_ThrowsArgumentOutOfRangeException()
        {
            var change = GDTextChange.Insert(100, "text");

            Action action = () => change.Apply("Hello");

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void Apply_EndBeyondLength_ThrowsArgumentOutOfRangeException()
        {
            var change = GDTextChange.Delete(3, 10);

            Action action = () => change.Apply("Hello");

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        #endregion

        #region CreateInverse Tests

        [TestMethod]
        public void CreateInverse_Insertion_CreatesDeletion()
        {
            var original = "Hello World";
            var change = GDTextChange.Insert(6, "Beautiful ");

            var inverse = change.CreateInverse(original);

            inverse.Start.Should().Be(6);
            inverse.OldLength.Should().Be(10);
            inverse.NewText.Should().Be("");
        }

        [TestMethod]
        public void CreateInverse_Deletion_CreatesInsertion()
        {
            var original = "Hello Beautiful World";
            var change = GDTextChange.Delete(6, 10);

            var inverse = change.CreateInverse(original);

            inverse.Start.Should().Be(6);
            inverse.OldLength.Should().Be(0);
            inverse.NewText.Should().Be("Beautiful ");
        }

        [TestMethod]
        public void CreateInverse_Roundtrip_RestoresOriginal()
        {
            var original = "Hello World";
            var change = GDTextChange.Replace(6, 5, "Universe");

            var modified = change.Apply(original);
            var inverse = change.CreateInverse(original);
            var restored = inverse.Apply(modified);

            restored.Should().Be(original);
        }

        #endregion

        #region AdjustPosition Tests

        [TestMethod]
        public void AdjustPosition_BeforeChange_Unchanged()
        {
            var change = GDTextChange.Replace(10, 5, "hello");

            var adjusted = change.AdjustPosition(5);

            adjusted.Should().Be(5);
        }

        [TestMethod]
        public void AdjustPosition_AtChangeStart_Unchanged()
        {
            var change = GDTextChange.Replace(10, 5, "hello");

            var adjusted = change.AdjustPosition(10);

            adjusted.Should().Be(10);
        }

        [TestMethod]
        public void AdjustPosition_AfterChange_ShiftedByDelta()
        {
            var change = GDTextChange.Replace(10, 5, "hello world"); // delta = 6

            var adjusted = change.AdjustPosition(20);

            adjusted.Should().Be(26); // 20 + 6
        }

        [TestMethod]
        public void AdjustPosition_InsideChange_MappedToNewEnd()
        {
            var change = GDTextChange.Replace(10, 5, "hello");

            var adjusted = change.AdjustPosition(12);

            adjusted.Should().Be(15); // 10 + 5 (length of "hello")
        }

        #endregion

        #region AdjustSpan Tests

        [TestMethod]
        public void AdjustSpan_BeforeChange_Unchanged()
        {
            var change = GDTextChange.Replace(20, 5, "hello");
            var span = new GDTextSpan(5, 10);

            var adjusted = change.AdjustSpan(span);

            adjusted.Should().Be(new GDTextSpan(5, 10));
        }

        [TestMethod]
        public void AdjustSpan_AfterChange_ShiftedByDelta()
        {
            var change = GDTextChange.Replace(10, 5, "hello world"); // delta = 6
            var span = new GDTextSpan(20, 10);

            var adjusted = change.AdjustSpan(span);

            adjusted.Should().Be(new GDTextSpan(26, 10));
        }

        [TestMethod]
        public void AdjustSpan_ContainedInDeletion_BecomesZeroLength()
        {
            var change = GDTextChange.Delete(10, 20);
            var span = new GDTextSpan(12, 5);

            var adjusted = change.AdjustSpan(span);

            adjusted.Should().Be(new GDTextSpan(10, 0));
        }

        #endregion

        #region Equality Tests

        [TestMethod]
        public void Equals_SameValues_ReturnsTrue()
        {
            var change1 = new GDTextChange(10, 5, "hello");
            var change2 = new GDTextChange(10, 5, "hello");

            change1.Equals(change2).Should().BeTrue();
            (change1 == change2).Should().BeTrue();
            (change1 != change2).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_DifferentStart_ReturnsFalse()
        {
            var change1 = new GDTextChange(10, 5, "hello");
            var change2 = new GDTextChange(11, 5, "hello");

            change1.Equals(change2).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_DifferentOldLength_ReturnsFalse()
        {
            var change1 = new GDTextChange(10, 5, "hello");
            var change2 = new GDTextChange(10, 6, "hello");

            change1.Equals(change2).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_DifferentNewText_ReturnsFalse()
        {
            var change1 = new GDTextChange(10, 5, "hello");
            var change2 = new GDTextChange(10, 5, "world");

            change1.Equals(change2).Should().BeFalse();
        }

        [TestMethod]
        public void GetHashCode_SameValues_SameHash()
        {
            var change1 = new GDTextChange(10, 5, "hello");
            var change2 = new GDTextChange(10, 5, "hello");

            change1.GetHashCode().Should().Be(change2.GetHashCode());
        }

        #endregion

        #region ToString Tests

        [TestMethod]
        public void ToString_Insertion_FormatsCorrectly()
        {
            var change = GDTextChange.Insert(10, "hello");

            change.ToString().Should().Be("Insert(10, \"hello\")");
        }

        [TestMethod]
        public void ToString_Deletion_FormatsCorrectly()
        {
            var change = GDTextChange.Delete(10, 5);

            change.ToString().Should().Be("Delete(10, 5)");
        }

        [TestMethod]
        public void ToString_Replacement_FormatsCorrectly()
        {
            var change = GDTextChange.Replace(10, 5, "hello");

            change.ToString().Should().Be("Replace(10, 5, \"hello\")");
        }

        [TestMethod]
        public void ToString_LongText_Truncates()
        {
            var longText = "This is a very long text that should be truncated";
            var change = GDTextChange.Insert(0, longText);

            change.ToString().Should().Contain("...");
        }

        #endregion
    }
}
