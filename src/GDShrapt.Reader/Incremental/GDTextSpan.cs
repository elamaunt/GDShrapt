using System;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Represents a span of text in source code.
    /// Immutable struct for efficient storage and passing.
    /// </summary>
    public readonly struct GDTextSpan : IEquatable<GDTextSpan>, IComparable<GDTextSpan>
    {
        /// <summary>
        /// Zero-based start position of the span.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Length of the span in characters.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// End position of the span (exclusive).
        /// </summary>
        public int End => Start + Length;

        /// <summary>
        /// True if this span has zero length.
        /// </summary>
        public bool IsEmpty => Length == 0;

        /// <summary>
        /// Creates a new text span.
        /// </summary>
        /// <param name="start">Zero-based start position.</param>
        /// <param name="length">Length of the span.</param>
        /// <exception cref="ArgumentOutOfRangeException">If start or length is negative.</exception>
        public GDTextSpan(int start, int length)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start), start, "Start position cannot be negative.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "Length cannot be negative.");

            Start = start;
            Length = length;
        }

        #region Factory Methods

        /// <summary>
        /// Creates a span from start and end positions.
        /// </summary>
        /// <param name="start">Start position (inclusive).</param>
        /// <param name="end">End position (exclusive).</param>
        public static GDTextSpan FromBounds(int start, int end)
        {
            if (end < start)
                throw new ArgumentOutOfRangeException(nameof(end), end,
                    $"End ({end}) cannot be less than start ({start}).");
            return new GDTextSpan(start, end - start);
        }

        /// <summary>
        /// Creates an empty span at the given position.
        /// </summary>
        /// <param name="position">Position for the empty span.</param>
        public static GDTextSpan Empty(int position)
            => new GDTextSpan(position, 0);

        #endregion

        #region Position Tests

        /// <summary>
        /// Checks if a position is within this span (inclusive start, exclusive end).
        /// </summary>
        /// <param name="position">Position to check.</param>
        public bool Contains(int position)
            => position >= Start && position < End;

        /// <summary>
        /// Checks if a position is within or at the boundaries of this span.
        /// </summary>
        /// <param name="position">Position to check.</param>
        public bool ContainsInclusive(int position)
            => position >= Start && position <= End;

        /// <summary>
        /// Checks if another span is entirely contained within this span.
        /// </summary>
        /// <param name="span">Span to check.</param>
        public bool Contains(GDTextSpan span)
            => span.Start >= Start && span.End <= End;

        /// <summary>
        /// Checks if this span overlaps with another span.
        /// Two spans overlap if they share at least one position.
        /// </summary>
        /// <param name="span">Span to check for overlap.</param>
        public bool Overlaps(GDTextSpan span)
        {
            // No overlap if one span ends before the other starts
            if (End <= span.Start || span.End <= Start)
                return false;
            return true;
        }

        /// <summary>
        /// Checks if this span overlaps with or is adjacent to another span.
        /// </summary>
        /// <param name="span">Span to check.</param>
        public bool OverlapsOrAdjacent(GDTextSpan span)
        {
            if (End < span.Start || span.End < Start)
                return false;
            return true;
        }

        /// <summary>
        /// Checks if this span intersects with a text change.
        /// </summary>
        /// <param name="change">Change to check.</param>
        public bool IntersectsWith(GDTextChange change)
        {
            // No intersection if change is entirely before or after this span
            if (change.OldEnd <= Start || change.Start >= End)
                return false;
            return true;
        }

        #endregion

        #region Set Operations

        /// <summary>
        /// Returns the intersection of this span with another span.
        /// Returns null if the spans don't overlap.
        /// </summary>
        /// <param name="span">Span to intersect with.</param>
        public GDTextSpan? Intersection(GDTextSpan span)
        {
            var start = Math.Max(Start, span.Start);
            var end = Math.Min(End, span.End);

            if (start >= end)
                return null;

            return FromBounds(start, end);
        }

        /// <summary>
        /// Returns the union of this span with another span.
        /// Returns the smallest span that contains both spans.
        /// </summary>
        /// <param name="span">Span to union with.</param>
        public GDTextSpan Union(GDTextSpan span)
        {
            var start = Math.Min(Start, span.Start);
            var end = Math.Max(End, span.End);
            return FromBounds(start, end);
        }

        #endregion

        #region Text Operations

        /// <summary>
        /// Extracts the text covered by this span from a string.
        /// </summary>
        /// <param name="text">Text to extract from.</param>
        /// <returns>The substring covered by this span.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If span is out of bounds.</exception>
        public string GetText(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (End > text.Length)
                throw new ArgumentOutOfRangeException(nameof(text),
                    $"Span end ({End}) exceeds text length ({text.Length}).");

            return text.Substring(Start, Length);
        }

        #endregion

        #region Equality and Comparison

        public bool Equals(GDTextSpan other)
            => Start == other.Start && Length == other.Length;

        public override bool Equals(object obj)
            => obj is GDTextSpan other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start * 397) ^ Length;
            }
        }

        public int CompareTo(GDTextSpan other)
        {
            var startComparison = Start.CompareTo(other.Start);
            return startComparison != 0 ? startComparison : Length.CompareTo(other.Length);
        }

        public static bool operator ==(GDTextSpan left, GDTextSpan right)
            => left.Equals(right);

        public static bool operator !=(GDTextSpan left, GDTextSpan right)
            => !left.Equals(right);

        public static bool operator <(GDTextSpan left, GDTextSpan right)
            => left.CompareTo(right) < 0;

        public static bool operator >(GDTextSpan left, GDTextSpan right)
            => left.CompareTo(right) > 0;

        public static bool operator <=(GDTextSpan left, GDTextSpan right)
            => left.CompareTo(right) <= 0;

        public static bool operator >=(GDTextSpan left, GDTextSpan right)
            => left.CompareTo(right) >= 0;

        #endregion

        public override string ToString()
            => $"[{Start}..{End})";
    }
}
