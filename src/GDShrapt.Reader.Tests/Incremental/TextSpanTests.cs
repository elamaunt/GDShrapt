using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GDShrapt.Reader.Tests.Incremental
{
    [TestClass]
    public class TextSpanTests
    {
        #region Constructor Tests

        [TestMethod]
        public void Constructor_ValidParameters_CreatesSpan()
        {
            var span = new GDTextSpan(10, 5);

            span.Start.Should().Be(10);
            span.Length.Should().Be(5);
        }

        [TestMethod]
        public void Constructor_ZeroLength_CreatesEmptySpan()
        {
            var span = new GDTextSpan(10, 0);

            span.Start.Should().Be(10);
            span.Length.Should().Be(0);
            span.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void Constructor_NegativeStart_ThrowsArgumentOutOfRangeException()
        {
            Action action = () => new GDTextSpan(-1, 5);

            action.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("start");
        }

        [TestMethod]
        public void Constructor_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            Action action = () => new GDTextSpan(10, -1);

            action.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("length");
        }

        #endregion

        #region Computed Properties Tests

        [TestMethod]
        public void End_ReturnsStartPlusLength()
        {
            var span = new GDTextSpan(10, 5);

            span.End.Should().Be(15);
        }

        [TestMethod]
        public void IsEmpty_ZeroLength_ReturnsTrue()
        {
            var span = new GDTextSpan(10, 0);

            span.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void IsEmpty_NonZeroLength_ReturnsFalse()
        {
            var span = new GDTextSpan(10, 5);

            span.IsEmpty.Should().BeFalse();
        }

        #endregion

        #region Factory Methods Tests

        [TestMethod]
        public void FromBounds_ValidBounds_CreatesSpan()
        {
            var span = GDTextSpan.FromBounds(10, 15);

            span.Start.Should().Be(10);
            span.Length.Should().Be(5);
            span.End.Should().Be(15);
        }

        [TestMethod]
        public void FromBounds_SameStartAndEnd_CreatesEmptySpan()
        {
            var span = GDTextSpan.FromBounds(10, 10);

            span.Start.Should().Be(10);
            span.Length.Should().Be(0);
        }

        [TestMethod]
        public void FromBounds_EndLessThanStart_ThrowsArgumentOutOfRangeException()
        {
            Action action = () => GDTextSpan.FromBounds(15, 10);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void Empty_CreatesEmptySpanAtPosition()
        {
            var span = GDTextSpan.Empty(10);

            span.Start.Should().Be(10);
            span.Length.Should().Be(0);
            span.IsEmpty.Should().BeTrue();
        }

        #endregion

        #region Contains Position Tests

        [TestMethod]
        public void Contains_PositionInside_ReturnsTrue()
        {
            var span = new GDTextSpan(10, 5); // [10..15)

            span.Contains(12).Should().BeTrue();
        }

        [TestMethod]
        public void Contains_PositionAtStart_ReturnsTrue()
        {
            var span = new GDTextSpan(10, 5);

            span.Contains(10).Should().BeTrue();
        }

        [TestMethod]
        public void Contains_PositionAtEnd_ReturnsFalse()
        {
            var span = new GDTextSpan(10, 5); // [10..15)

            span.Contains(15).Should().BeFalse();
        }

        [TestMethod]
        public void Contains_PositionBefore_ReturnsFalse()
        {
            var span = new GDTextSpan(10, 5);

            span.Contains(5).Should().BeFalse();
        }

        [TestMethod]
        public void Contains_PositionAfter_ReturnsFalse()
        {
            var span = new GDTextSpan(10, 5);

            span.Contains(20).Should().BeFalse();
        }

        [TestMethod]
        public void ContainsInclusive_PositionAtEnd_ReturnsTrue()
        {
            var span = new GDTextSpan(10, 5);

            span.ContainsInclusive(15).Should().BeTrue();
        }

        #endregion

        #region Contains Span Tests

        [TestMethod]
        public void Contains_SpanInside_ReturnsTrue()
        {
            var outer = new GDTextSpan(10, 10); // [10..20)
            var inner = new GDTextSpan(12, 5);  // [12..17)

            outer.Contains(inner).Should().BeTrue();
        }

        [TestMethod]
        public void Contains_SpanSame_ReturnsTrue()
        {
            var span1 = new GDTextSpan(10, 5);
            var span2 = new GDTextSpan(10, 5);

            span1.Contains(span2).Should().BeTrue();
        }

        [TestMethod]
        public void Contains_SpanPartiallyOutside_ReturnsFalse()
        {
            var outer = new GDTextSpan(10, 5);  // [10..15)
            var inner = new GDTextSpan(12, 10); // [12..22)

            outer.Contains(inner).Should().BeFalse();
        }

        [TestMethod]
        public void Contains_SpanBefore_ReturnsFalse()
        {
            var outer = new GDTextSpan(10, 5);
            var inner = new GDTextSpan(5, 3);

            outer.Contains(inner).Should().BeFalse();
        }

        #endregion

        #region Overlaps Tests

        [TestMethod]
        public void Overlaps_SpansOverlap_ReturnsTrue()
        {
            var span1 = new GDTextSpan(10, 10); // [10..20)
            var span2 = new GDTextSpan(15, 10); // [15..25)

            span1.Overlaps(span2).Should().BeTrue();
            span2.Overlaps(span1).Should().BeTrue();
        }

        [TestMethod]
        public void Overlaps_SpanContained_ReturnsTrue()
        {
            var outer = new GDTextSpan(10, 10);
            var inner = new GDTextSpan(12, 5);

            outer.Overlaps(inner).Should().BeTrue();
        }

        [TestMethod]
        public void Overlaps_SpansAdjacent_ReturnsFalse()
        {
            var span1 = new GDTextSpan(10, 5); // [10..15)
            var span2 = new GDTextSpan(15, 5); // [15..20)

            span1.Overlaps(span2).Should().BeFalse();
        }

        [TestMethod]
        public void Overlaps_SpansDisjoint_ReturnsFalse()
        {
            var span1 = new GDTextSpan(10, 5);
            var span2 = new GDTextSpan(20, 5);

            span1.Overlaps(span2).Should().BeFalse();
        }

        [TestMethod]
        public void OverlapsOrAdjacent_SpansAdjacent_ReturnsTrue()
        {
            var span1 = new GDTextSpan(10, 5); // [10..15)
            var span2 = new GDTextSpan(15, 5); // [15..20)

            span1.OverlapsOrAdjacent(span2).Should().BeTrue();
        }

        [TestMethod]
        public void OverlapsOrAdjacent_SpansDisjoint_ReturnsFalse()
        {
            var span1 = new GDTextSpan(10, 5);
            var span2 = new GDTextSpan(20, 5);

            span1.OverlapsOrAdjacent(span2).Should().BeFalse();
        }

        #endregion

        #region IntersectsWith Tests

        [TestMethod]
        public void IntersectsWith_ChangeInsideSpan_ReturnsTrue()
        {
            var span = new GDTextSpan(10, 10); // [10..20)
            var change = GDTextChange.Delete(12, 3);

            span.IntersectsWith(change).Should().BeTrue();
        }

        [TestMethod]
        public void IntersectsWith_ChangeOverlapping_ReturnsTrue()
        {
            var span = new GDTextSpan(10, 10); // [10..20)
            var change = GDTextChange.Replace(15, 10, "new");

            span.IntersectsWith(change).Should().BeTrue();
        }

        [TestMethod]
        public void IntersectsWith_ChangeBefore_ReturnsFalse()
        {
            var span = new GDTextSpan(20, 10);
            var change = GDTextChange.Delete(5, 10);

            span.IntersectsWith(change).Should().BeFalse();
        }

        [TestMethod]
        public void IntersectsWith_ChangeAfter_ReturnsFalse()
        {
            var span = new GDTextSpan(10, 5);
            var change = GDTextChange.Delete(20, 5);

            span.IntersectsWith(change).Should().BeFalse();
        }

        [TestMethod]
        public void IntersectsWith_ChangeAdjacentBefore_ReturnsFalse()
        {
            var span = new GDTextSpan(10, 5);
            var change = GDTextChange.Delete(5, 5); // ends at 10

            span.IntersectsWith(change).Should().BeFalse();
        }

        [TestMethod]
        public void IntersectsWith_ChangeAdjacentAfter_ReturnsFalse()
        {
            var span = new GDTextSpan(10, 5); // [10..15)
            var change = GDTextChange.Delete(15, 5);

            span.IntersectsWith(change).Should().BeFalse();
        }

        #endregion

        #region Intersection Tests

        [TestMethod]
        public void Intersection_Overlapping_ReturnsIntersection()
        {
            var span1 = new GDTextSpan(10, 10); // [10..20)
            var span2 = new GDTextSpan(15, 10); // [15..25)

            var intersection = span1.Intersection(span2);

            intersection.Should().Be(new GDTextSpan(15, 5));
        }

        [TestMethod]
        public void Intersection_Contained_ReturnsContained()
        {
            var outer = new GDTextSpan(10, 20);
            var inner = new GDTextSpan(15, 5);

            var intersection = outer.Intersection(inner);

            intersection.Should().Be(inner);
        }

        [TestMethod]
        public void Intersection_NoOverlap_ReturnsNull()
        {
            var span1 = new GDTextSpan(10, 5);
            var span2 = new GDTextSpan(20, 5);

            var intersection = span1.Intersection(span2);

            intersection.Should().BeNull();
        }

        [TestMethod]
        public void Intersection_Adjacent_ReturnsNull()
        {
            var span1 = new GDTextSpan(10, 5); // [10..15)
            var span2 = new GDTextSpan(15, 5); // [15..20)

            var intersection = span1.Intersection(span2);

            intersection.Should().BeNull();
        }

        #endregion

        #region Union Tests

        [TestMethod]
        public void Union_Overlapping_ReturnsUnion()
        {
            var span1 = new GDTextSpan(10, 10); // [10..20)
            var span2 = new GDTextSpan(15, 10); // [15..25)

            var union = span1.Union(span2);

            union.Should().Be(new GDTextSpan(10, 15)); // [10..25)
        }

        [TestMethod]
        public void Union_Disjoint_ReturnsEncompassingSpan()
        {
            var span1 = new GDTextSpan(10, 5);  // [10..15)
            var span2 = new GDTextSpan(20, 5);  // [20..25)

            var union = span1.Union(span2);

            union.Should().Be(new GDTextSpan(10, 15)); // [10..25)
        }

        [TestMethod]
        public void Union_Contained_ReturnsOuter()
        {
            var outer = new GDTextSpan(10, 20);
            var inner = new GDTextSpan(15, 5);

            var union = outer.Union(inner);

            union.Should().Be(outer);
        }

        #endregion

        #region GetText Tests

        [TestMethod]
        public void GetText_ValidSpan_ReturnsSubstring()
        {
            var text = "Hello World";
            var span = new GDTextSpan(6, 5);

            var result = span.GetText(text);

            result.Should().Be("World");
        }

        [TestMethod]
        public void GetText_EmptySpan_ReturnsEmptyString()
        {
            var text = "Hello World";
            var span = new GDTextSpan(6, 0);

            var result = span.GetText(text);

            result.Should().Be(string.Empty);
        }

        [TestMethod]
        public void GetText_NullText_ThrowsArgumentNullException()
        {
            var span = new GDTextSpan(0, 5);

            Action action = () => span.GetText(null);

            action.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void GetText_SpanOutOfBounds_ThrowsArgumentOutOfRangeException()
        {
            var text = "Hello";
            var span = new GDTextSpan(3, 10);

            Action action = () => span.GetText(text);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        #endregion

        #region Equality Tests

        [TestMethod]
        public void Equals_SameValues_ReturnsTrue()
        {
            var span1 = new GDTextSpan(10, 5);
            var span2 = new GDTextSpan(10, 5);

            span1.Equals(span2).Should().BeTrue();
            (span1 == span2).Should().BeTrue();
            (span1 != span2).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_DifferentStart_ReturnsFalse()
        {
            var span1 = new GDTextSpan(10, 5);
            var span2 = new GDTextSpan(11, 5);

            span1.Equals(span2).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_DifferentLength_ReturnsFalse()
        {
            var span1 = new GDTextSpan(10, 5);
            var span2 = new GDTextSpan(10, 6);

            span1.Equals(span2).Should().BeFalse();
        }

        [TestMethod]
        public void GetHashCode_SameValues_SameHash()
        {
            var span1 = new GDTextSpan(10, 5);
            var span2 = new GDTextSpan(10, 5);

            span1.GetHashCode().Should().Be(span2.GetHashCode());
        }

        #endregion

        #region Comparison Tests

        [TestMethod]
        public void CompareTo_SameSpan_ReturnsZero()
        {
            var span1 = new GDTextSpan(10, 5);
            var span2 = new GDTextSpan(10, 5);

            span1.CompareTo(span2).Should().Be(0);
        }

        [TestMethod]
        public void CompareTo_EarlierStart_ReturnsNegative()
        {
            var span1 = new GDTextSpan(10, 5);
            var span2 = new GDTextSpan(15, 5);

            span1.CompareTo(span2).Should().BeNegative();
            (span1 < span2).Should().BeTrue();
        }

        [TestMethod]
        public void CompareTo_LaterStart_ReturnsPositive()
        {
            var span1 = new GDTextSpan(15, 5);
            var span2 = new GDTextSpan(10, 5);

            span1.CompareTo(span2).Should().BePositive();
            (span1 > span2).Should().BeTrue();
        }

        [TestMethod]
        public void CompareTo_SameStartShorterLength_ReturnsNegative()
        {
            var span1 = new GDTextSpan(10, 3);
            var span2 = new GDTextSpan(10, 5);

            span1.CompareTo(span2).Should().BeNegative();
        }

        #endregion

        #region ToString Tests

        [TestMethod]
        public void ToString_FormatsCorrectly()
        {
            var span = new GDTextSpan(10, 5);

            span.ToString().Should().Be("[10..15)");
        }

        [TestMethod]
        public void ToString_EmptySpan_FormatsCorrectly()
        {
            var span = new GDTextSpan(10, 0);

            span.ToString().Should().Be("[10..10)");
        }

        #endregion
    }
}
