using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Provides incremental parsing capabilities for GDScript.
    /// Uses member-level reparsing strategy: identifies affected class members
    /// and reparses only those, preserving unchanged portions of the AST.
    /// </summary>
    public class GDIncrementalParser : IGDIncrementalParser
    {
        private readonly GDScriptReader _reader;

        /// <summary>
        /// Threshold ratio of changed characters to total characters.
        /// If changes exceed this ratio, full reparse is performed instead of incremental.
        /// Default is 0.5 (50%).
        /// </summary>
        public double FullReparseThreshold { get; set; } = 0.5;

        /// <summary>
        /// Creates a new incremental parser with default settings.
        /// </summary>
        public GDIncrementalParser()
            : this(new GDScriptReader())
        {
        }

        /// <summary>
        /// Creates a new incremental parser with custom settings.
        /// </summary>
        /// <param name="settings">Parser settings.</param>
        public GDIncrementalParser(GDReadSettings settings)
            : this(new GDScriptReader(settings))
        {
        }

        /// <summary>
        /// Creates a new incremental parser with an existing reader.
        /// </summary>
        /// <param name="reader">Reader to use for parsing.</param>
        public GDIncrementalParser(GDScriptReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <inheritdoc/>
        public GDClassDeclaration ParseIncremental(
            GDClassDeclaration oldTree,
            string newText,
            IReadOnlyList<GDTextChange> changes,
            CancellationToken cancellationToken = default)
        {
            if (newText == null)
                throw new ArgumentNullException(nameof(newText));

            // Fallback to full parse if no old tree
            if (oldTree == null)
                return _reader.ParseFileContent(newText, cancellationToken);

            // Fallback to full parse if no changes or empty changes
            if (changes == null || changes.Count == 0)
                return (GDClassDeclaration)oldTree.Clone();

            // Calculate total change magnitude
            var totalDelta = changes.Sum(c => Math.Max(c.OldLength, c.NewLength));
            var originalLength = newText.Length - changes.Sum(c => c.Delta);

            // If changes are too large, full reparse is more efficient
            if (originalLength > 0 && (double)totalDelta / originalLength > FullReparseThreshold)
                return _reader.ParseFileContent(newText, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // Find affected members
            var affectedMembers = FindAffectedMembers(oldTree, changes);

            // If no specific members affected (e.g., changes in attributes/header), full reparse
            if (affectedMembers.Count == 0)
            {
                // Check if changes are at the very beginning (class attributes)
                var minChangeStart = changes.Min(c => c.Start);
                var firstMemberStart = GetFirstMemberCharOffset(oldTree);

                if (minChangeStart < firstMemberStart)
                    return _reader.ParseFileContent(newText, cancellationToken);

                // No affected members and changes not in header - just clone
                return (GDClassDeclaration)oldTree.Clone();
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Clone the tree and replace affected members
            var newTree = (GDClassDeclaration)oldTree.Clone();

            // Reparse and replace each affected member
            foreach (var info in affectedMembers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var reparsedMember = ReparseMember(newText, info, cancellationToken);
                if (reparsedMember != null)
                {
                    ReplaceMember(newTree, info.MemberIndex, reparsedMember);
                }
            }

            return newTree;
        }

        /// <inheritdoc/>
        public IReadOnlyList<GDTextSpan> GetChangedRanges(
            GDClassDeclaration oldTree,
            GDClassDeclaration newTree)
        {
            if (oldTree == null || newTree == null)
                return Array.Empty<GDTextSpan>();

            var changedRanges = new List<GDTextSpan>();

            var oldMembers = oldTree.Members.ToList();
            var newMembers = newTree.Members.ToList();

            // Compare members by index
            var maxCount = Math.Max(oldMembers.Count, newMembers.Count);

            int currentOffset = 0;

            for (int i = 0; i < maxCount; i++)
            {
                if (i >= oldMembers.Count)
                {
                    // New member added
                    var newMember = newMembers[i];
                    var length = newMember.Length;
                    changedRanges.Add(new GDTextSpan(currentOffset, length));
                    currentOffset += length;
                }
                else if (i >= newMembers.Count)
                {
                    // Old member deleted - no span in new tree
                }
                else
                {
                    var oldMember = oldMembers[i];
                    var newMember = newMembers[i];

                    var oldText = oldMember.ToString();
                    var newText = newMember.ToString();

                    if (oldText != newText)
                    {
                        // Member changed
                        changedRanges.Add(new GDTextSpan(currentOffset, newText.Length));
                    }

                    currentOffset += newText.Length;
                }
            }

            return changedRanges;
        }

        #region Private Methods

        /// <summary>
        /// Information about an affected member for reparsing.
        /// </summary>
        private class AffectedMemberInfo
        {
            public int MemberIndex { get; set; }
            public GDClassMember Member { get; set; }
            public int StartOffset { get; set; }
            public int EndOffset { get; set; }
            public int AdjustedStartOffset { get; set; }
            public int AdjustedEndOffset { get; set; }
        }

        /// <summary>
        /// Finds members affected by the given text changes.
        /// </summary>
        private List<AffectedMemberInfo> FindAffectedMembers(
            GDClassDeclaration tree,
            IReadOnlyList<GDTextChange> changes)
        {
            var affected = new List<AffectedMemberInfo>();
            var members = tree.Members.ToList();

            if (members.Count == 0)
                return affected;

            // Sort changes by position (ascending) for proper cumulative delta calculation
            var sortedChanges = changes.OrderBy(c => c.Start).ToList();

            // Calculate character offsets for each member
            int currentOffset = 0;
            var memberOffsets = new List<(int Start, int End)>();

            // Account for any content before the first member
            var treeText = tree.ToString();

            foreach (var member in members)
            {
                var memberText = member.ToString();
                var memberStart = treeText.IndexOf(memberText, currentOffset, StringComparison.Ordinal);

                if (memberStart >= 0)
                {
                    memberOffsets.Add((memberStart, memberStart + memberText.Length));
                    currentOffset = memberStart + memberText.Length;
                }
                else
                {
                    // Fallback: sequential offset
                    memberOffsets.Add((currentOffset, currentOffset + memberText.Length));
                    currentOffset += memberText.Length;
                }
            }

            // Check which members intersect with changes
            for (int i = 0; i < members.Count; i++)
            {
                var (memberStart, memberEnd) = memberOffsets[i];
                var memberSpan = GDTextSpan.FromBounds(memberStart, memberEnd);

                bool isAffected = sortedChanges.Any(change => memberSpan.IntersectsWith(change));

                if (isAffected)
                {
                    // Calculate adjusted offsets after applying ALL changes
                    // This is the position in the NEW text
                    var (adjustedStart, adjustedEnd) = CalculateAdjustedSpan(
                        memberStart, memberEnd, sortedChanges);

                    affected.Add(new AffectedMemberInfo
                    {
                        MemberIndex = i,
                        Member = members[i],
                        StartOffset = memberStart,
                        EndOffset = memberEnd,
                        AdjustedStartOffset = Math.Max(0, adjustedStart),
                        AdjustedEndOffset = adjustedEnd
                    });
                }
            }

            return affected;
        }

        /// <summary>
        /// Calculates the adjusted span position after applying all changes.
        /// The result is the position in the NEW text.
        /// </summary>
        private (int Start, int End) CalculateAdjustedSpan(
            int originalStart,
            int originalEnd,
            List<GDTextChange> sortedChanges)
        {
            int newStart = originalStart;
            int newEnd = originalEnd;

            foreach (var change in sortedChanges)
            {
                // Change is entirely before the span
                if (change.OldEnd <= originalStart)
                {
                    // Shift both start and end by the delta
                    newStart += change.Delta;
                    newEnd += change.Delta;
                }
                // Change is entirely after the span
                else if (change.Start >= originalEnd)
                {
                    // No effect on this span
                    // Since changes are sorted, all subsequent changes are also after
                    break;
                }
                // Change overlaps with the span start (starts before, extends into)
                else if (change.Start < originalStart && change.OldEnd > originalStart)
                {
                    // Expand start to include the change start
                    newStart = change.Start;
                    // Adjust end for the delta
                    newEnd += change.Delta;
                }
                // Change overlaps with the span end (starts inside, extends past)
                else if (change.Start >= originalStart && change.Start < originalEnd && change.OldEnd > originalEnd)
                {
                    // Expand end to account for the change
                    newEnd += change.Delta;
                }
                // Change is entirely inside the span
                else if (change.Start >= originalStart && change.OldEnd <= originalEnd)
                {
                    // Just adjust end by the delta
                    newEnd += change.Delta;
                }
                // Change completely encompasses the span
                else if (change.Start <= originalStart && change.OldEnd >= originalEnd)
                {
                    // The entire span is affected - adjust both boundaries
                    newStart = change.Start;
                    newEnd = originalEnd + change.Delta;
                }
            }

            return (newStart, Math.Max(newStart, newEnd));
        }

        /// <summary>
        /// Gets the character offset of the first class member.
        /// </summary>
        private int GetFirstMemberCharOffset(GDClassDeclaration tree)
        {
            var firstMember = tree.Members.FirstOrDefault();
            if (firstMember == null)
                return tree.Length;

            var treeText = tree.ToString();
            var memberText = firstMember.ToString();

            var index = treeText.IndexOf(memberText, StringComparison.Ordinal);
            return index >= 0 ? index : 0;
        }

        /// <summary>
        /// Reparses a single member from the new text.
        /// </summary>
        private GDClassMember ReparseMember(
            string newText,
            AffectedMemberInfo info,
            CancellationToken cancellationToken)
        {
            // Extract the region containing the member
            var start = info.AdjustedStartOffset;
            var end = Math.Min(info.AdjustedEndOffset, newText.Length);

            if (start >= newText.Length || start >= end)
                return null;

            // Find the actual member boundaries by looking for newlines at indent level 0
            // This is a simplified approach - real implementation would need more sophisticated boundary detection
            var memberText = ExtractMemberText(newText, start, end);

            if (string.IsNullOrWhiteSpace(memberText))
                return null;

            // Parse the member text as part of a class
            var wrappedContent = memberText;
            var tempTree = _reader.ParseFileContent(wrappedContent, cancellationToken);

            // Extract the first member from the parsed tree
            return tempTree.Members.FirstOrDefault();
        }

        /// <summary>
        /// Extracts the text for a single member, finding proper boundaries.
        /// </summary>
        private string ExtractMemberText(string text, int approximateStart, int approximateEnd)
        {
            // Find the start of the line containing approximateStart
            var lineStart = approximateStart;
            while (lineStart > 0 && text[lineStart - 1] != '\n')
                lineStart--;

            // Find the end by looking for the next member (line starting with non-whitespace after empty line or specific keywords)
            var searchEnd = Math.Min(approximateEnd + 1000, text.Length); // Look a bit beyond
            var memberEnd = approximateEnd;

            for (int i = approximateEnd; i < searchEnd; i++)
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

            if (memberEnd > text.Length)
                memberEnd = text.Length;

            return text.Substring(lineStart, memberEnd - lineStart);
        }

        /// <summary>
        /// Replaces a member in the tree at the specified index.
        /// </summary>
        private void ReplaceMember(GDClassDeclaration tree, int index, GDClassMember newMember)
        {
            var members = tree.Members;

            if (index < 0 || index >= members.Count)
                return;

            // Clone the new member to ensure it's not shared
            var clonedMember = (GDClassMember)newMember.Clone();

            // Use the indexer to replace the member directly
            // This properly handles parent references
            members[index] = clonedMember;
        }

        #endregion
    }
}
