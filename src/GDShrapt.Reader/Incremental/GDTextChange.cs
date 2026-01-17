using System;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Represents a text change (edit) in source code.
    /// Immutable struct for efficient storage and passing.
    /// </summary>
    public readonly struct GDTextChange : IEquatable<GDTextChange>
    {
        /// <summary>
        /// Zero-based character offset where the change starts.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Number of characters removed from the original text (0 for pure insertion).
        /// </summary>
        public int OldLength { get; }

        /// <summary>
        /// The text inserted at the change position. Empty string for pure deletion.
        /// </summary>
        public string NewText { get; }

        /// <summary>
        /// Length of the inserted text.
        /// </summary>
        public int NewLength => NewText?.Length ?? 0;

        /// <summary>
        /// End position of the removed text (exclusive) in the original document.
        /// </summary>
        public int OldEnd => Start + OldLength;

        /// <summary>
        /// End position of the inserted text (exclusive) in the new document.
        /// </summary>
        public int NewEnd => Start + NewLength;

        /// <summary>
        /// Net change in document length (positive = longer, negative = shorter).
        /// </summary>
        public int Delta => NewLength - OldLength;

        /// <summary>
        /// True if this change only inserts text without removing any.
        /// </summary>
        public bool IsInsertion => OldLength == 0 && NewLength > 0;

        /// <summary>
        /// True if this change only removes text without inserting any.
        /// </summary>
        public bool IsDeletion => OldLength > 0 && NewLength == 0;

        /// <summary>
        /// True if this change both removes and inserts text.
        /// </summary>
        public bool IsReplacement => OldLength > 0 && NewLength > 0;

        /// <summary>
        /// True if this change has no effect (empty deletion and insertion).
        /// </summary>
        public bool IsEmpty => OldLength == 0 && NewLength == 0;

        /// <summary>
        /// Creates a new text change.
        /// </summary>
        /// <param name="start">Zero-based character offset where the change starts.</param>
        /// <param name="oldLength">Number of characters removed (0 for insertion).</param>
        /// <param name="newText">Text inserted (empty/null for deletion).</param>
        /// <exception cref="ArgumentOutOfRangeException">If start or oldLength is negative.</exception>
        public GDTextChange(int start, int oldLength, string newText)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start), start, "Start position cannot be negative.");
            if (oldLength < 0)
                throw new ArgumentOutOfRangeException(nameof(oldLength), oldLength, "Old length cannot be negative.");

            Start = start;
            OldLength = oldLength;
            NewText = newText ?? string.Empty;
        }

        #region Factory Methods

        /// <summary>
        /// Creates an insertion change (no text removed).
        /// </summary>
        /// <param name="position">Position to insert at.</param>
        /// <param name="text">Text to insert.</param>
        public static GDTextChange Insert(int position, string text)
            => new GDTextChange(position, 0, text);

        /// <summary>
        /// Creates a deletion change (no text inserted).
        /// </summary>
        /// <param name="start">Start of the region to delete.</param>
        /// <param name="length">Length of the region to delete.</param>
        public static GDTextChange Delete(int start, int length)
            => new GDTextChange(start, length, string.Empty);

        /// <summary>
        /// Creates a replacement change (removes and inserts text).
        /// </summary>
        /// <param name="start">Start of the region to replace.</param>
        /// <param name="oldLength">Length of the region to replace.</param>
        /// <param name="newText">New text to insert.</param>
        public static GDTextChange Replace(int start, int oldLength, string newText)
            => new GDTextChange(start, oldLength, newText);

        #endregion

        #region Application

        /// <summary>
        /// Applies this change to a string and returns the result.
        /// </summary>
        /// <param name="original">The original text.</param>
        /// <returns>The text with this change applied.</returns>
        /// <exception cref="ArgumentNullException">If original is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the change is out of bounds.</exception>
        public string Apply(string original)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            if (Start > original.Length)
                throw new ArgumentOutOfRangeException(nameof(original),
                    $"Change start ({Start}) is beyond string length ({original.Length}).");

            if (OldEnd > original.Length)
                throw new ArgumentOutOfRangeException(nameof(original),
                    $"Change end ({OldEnd}) is beyond string length ({original.Length}).");

            if (IsEmpty)
                return original;

            // Build the result: prefix + new text + suffix
            return original.Substring(0, Start) + NewText + original.Substring(OldEnd);
        }

        /// <summary>
        /// Creates the inverse change that would undo this change.
        /// Requires the original text to extract the removed portion.
        /// </summary>
        /// <param name="originalText">The original text before this change was applied.</param>
        /// <returns>A change that undoes this change.</returns>
        public GDTextChange CreateInverse(string originalText)
        {
            if (originalText == null)
                throw new ArgumentNullException(nameof(originalText));

            var removedText = originalText.Substring(Start, OldLength);
            return new GDTextChange(Start, NewLength, removedText);
        }

        #endregion

        #region Position Adjustment

        /// <summary>
        /// Adjusts a position in the original document to account for this change.
        /// </summary>
        /// <param name="position">Position in the original document.</param>
        /// <returns>Adjusted position in the new document.</returns>
        public int AdjustPosition(int position)
        {
            if (position <= Start)
                return position; // Before change, unchanged

            if (position >= OldEnd)
                return position + Delta; // After change, shifted by delta

            // Inside the changed region - map to end of new text
            return Start + NewLength;
        }

        /// <summary>
        /// Adjusts a span in the original document to account for this change.
        /// </summary>
        /// <param name="span">Span in the original document.</param>
        /// <returns>Adjusted span in the new document, or null if the span was deleted.</returns>
        public GDTextSpan? AdjustSpan(GDTextSpan span)
        {
            // Span is entirely before the change
            if (span.End <= Start)
                return span;

            // Span is entirely after the change
            if (span.Start >= OldEnd)
                return new GDTextSpan(span.Start + Delta, span.Length);

            // Span is entirely within the deleted region
            if (span.Start >= Start && span.End <= OldEnd)
            {
                // Map to a zero-length span at the insertion point
                return new GDTextSpan(Start + NewLength, 0);
            }

            // Partial overlap - adjust both ends
            var newStart = span.Start < Start ? span.Start : Start + NewLength;
            var newEnd = span.End <= OldEnd ? Start + NewLength : span.End + Delta;

            var newLength = newEnd - newStart;
            return newLength >= 0 ? new GDTextSpan(newStart, newLength) : (GDTextSpan?)null;
        }

        #endregion

        #region Equality

        public bool Equals(GDTextChange other)
            => Start == other.Start
               && OldLength == other.OldLength
               && NewText == other.NewText;

        public override bool Equals(object obj)
            => obj is GDTextChange other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + Start;
                hash = hash * 31 + OldLength;
                hash = hash * 31 + (NewText?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public static bool operator ==(GDTextChange left, GDTextChange right)
            => left.Equals(right);

        public static bool operator !=(GDTextChange left, GDTextChange right)
            => !left.Equals(right);

        #endregion

        public override string ToString()
        {
            if (IsInsertion)
                return $"Insert({Start}, \"{Truncate(NewText, 20)}\")";
            if (IsDeletion)
                return $"Delete({Start}, {OldLength})";
            if (IsReplacement)
                return $"Replace({Start}, {OldLength}, \"{Truncate(NewText, 20)}\")";
            return $"NoOp({Start})";
        }

        private static string Truncate(string s, int maxLength)
        {
            if (s == null || s.Length <= maxLength)
                return s;
            return s.Substring(0, maxLength - 3) + "...";
        }
    }
}
