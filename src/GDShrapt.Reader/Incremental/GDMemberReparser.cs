using System;
using System.Linq;
using System.Threading;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Reparses a single class member from new text.
    /// </summary>
    public class GDMemberReparser
    {
        private readonly GDScriptReader _reader;

        public GDMemberReparser(GDScriptReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <summary>
        /// Extracts member text from the new text and parses it.
        /// </summary>
        /// <param name="newText">The complete new text of the file.</param>
        /// <param name="memberStartOffset">Start offset of the member in the new text.</param>
        /// <param name="approximateEndOffset">Approximate end offset of the member.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The parsed member, or null if parsing failed.</returns>
        public GDClassMember ReparseMember(
            string newText,
            int memberStartOffset,
            int approximateEndOffset,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(newText))
                return null;

            if (memberStartOffset < 0 || memberStartOffset >= newText.Length)
                return null;

            // Extract the text for this member
            string memberText = ExtractMemberText(newText, memberStartOffset, approximateEndOffset);

            if (string.IsNullOrWhiteSpace(memberText))
                return null;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Parse as a file and take the first member
                var tempTree = _reader.ParseFileContent(memberText, cancellationToken);

                if (tempTree?.Members == null)
                    return null;

                // If parsing produced multiple members, the edit likely broke member boundaries
                // Return null to trigger full reparse for safety
                var members = tempTree.Members.ToList();
                if (members.Count != 1)
                    return null;

                var parsedMember = members[0];

                // Verify the parsed member's text matches our extracted text
                // If not, there might be parsing issues that require full reparse
                if (parsedMember.ToString() != memberText.TrimEnd())
                    return null;

                return parsedMember;
            }
            catch
            {
                // Parsing failed (e.g., invalid syntax after edit)
                // Return null to trigger full reparse
                return null;
            }
        }

        /// <summary>
        /// Extracts the text for a single member, finding proper boundaries.
        /// </summary>
        private string ExtractMemberText(string text, int start, int approximateEnd)
        {
            // Find the start of the line containing the start offset
            int lineStart = start;
            while (lineStart > 0 && text[lineStart - 1] != '\n')
                lineStart--;

            // Find the end of the member by looking for the next member
            // (a line starting with non-whitespace at column 0)
            // Start searching from approximateEnd or lineStart+1 if approximateEnd is too small
            int searchStart = Math.Max(lineStart + 1, approximateEnd);

            // Use dynamic lookahead based on the approximate member size
            int approximateSize = approximateEnd - start;
            int lookahead = Math.Max(1000, approximateSize * 2);
            int searchEnd = Math.Min(text.Length, searchStart + lookahead);

            int memberEnd = text.Length; // Default to end of text

            for (int i = searchStart; i < searchEnd; i++)
            {
                if (i > 0 && text[i - 1] == '\n')
                {
                    // Check if this line starts a new member (non-whitespace at column 0)
                    if (i < text.Length && !char.IsWhiteSpace(text[i]))
                    {
                        memberEnd = i;
                        break;
                    }
                }
            }

            // Sanity check
            if (memberEnd < lineStart)
                memberEnd = text.Length;

            return text.Substring(lineStart, memberEnd - lineStart);
        }

        /// <summary>
        /// Calculates the adjusted offset in the new text after applying changes.
        /// </summary>
        /// <param name="originalOffset">Original offset in the old text.</param>
        /// <param name="changes">The list of text changes.</param>
        /// <returns>Adjusted offset in the new text.</returns>
        public static int AdjustOffset(int originalOffset, System.Collections.Generic.IReadOnlyList<GDTextChange> changes)
        {
            if (changes == null || changes.Count == 0)
                return originalOffset;

            int adjustment = 0;
            foreach (var change in changes)
            {
                if (change.Start <= originalOffset)
                    adjustment += change.Delta;
            }
            return originalOffset + adjustment;
        }
    }
}
