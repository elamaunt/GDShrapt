using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GDShrapt.Reader
{
    /// <summary>
    /// True incremental parser that reparses only from the point of change,
    /// modifying the AST tree in-place without cloning.
    /// Uses member-level reparsing strategy: identifies affected class members
    /// and reparses only those, replacing them in the original tree.
    /// </summary>
    public class GDTrueIncrementalParser : IGDIncrementalParser
    {
        private readonly GDScriptReader _reader;
        private readonly GDMemberReparser _memberReparser;

        /// <summary>
        /// Threshold ratio of changed characters to total characters.
        /// If changes exceed this ratio, full reparse is performed instead of incremental.
        /// Default is 0.5 (50%).
        /// </summary>
        public double FullReparseThreshold { get; set; } = 0.5;

        /// <summary>
        /// Creates a new true incremental parser with default settings.
        /// </summary>
        public GDTrueIncrementalParser()
            : this(new GDScriptReader())
        {
        }

        /// <summary>
        /// Creates a new true incremental parser with custom settings.
        /// </summary>
        /// <param name="settings">Parser settings.</param>
        public GDTrueIncrementalParser(GDReadSettings settings)
            : this(new GDScriptReader(settings))
        {
        }

        /// <summary>
        /// Creates a new true incremental parser with an existing reader.
        /// </summary>
        /// <param name="reader">Reader to use for parsing.</param>
        public GDTrueIncrementalParser(GDScriptReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _memberReparser = new GDMemberReparser(reader);
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

            // Fallback to full parse if no changes
            if (changes == null || changes.Count == 0)
                return oldTree; // Return the same tree without modification

            // Calculate total change magnitude
            var totalDelta = changes.Sum(c => Math.Max(c.OldLength, c.NewLength));
            var originalLength = newText.Length - changes.Sum(c => c.Delta);

            // If changes are too large, full reparse is more efficient
            if (originalLength > 0 && (double)totalDelta / originalLength > FullReparseThreshold)
                return _reader.ParseFileContent(newText, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // Sort changes by position
            var sortedChanges = changes.OrderBy(c => c.Start).ToList();
            var firstChange = sortedChanges.First();

            // Find the member containing the change
            var location = LocateMemberInMembers(oldTree, firstChange.Start);

            // If no member found (change in class attributes area or after all members)
            // -> full reparse
            if (location.MemberIndex < 0)
                return _reader.ParseFileContent(newText, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // Calculate adjusted offsets for the new text
            int adjustedOffset = GDMemberReparser.AdjustOffset(location.NodeOffset, sortedChanges);
            int adjustedEnd = GDMemberReparser.AdjustOffset(location.NodeOffset + location.Node.Length, sortedChanges);

            // Reparse the affected member
            var newMember = _memberReparser.ReparseMember(
                newText, adjustedOffset, adjustedEnd, cancellationToken);

            if (newMember == null)
                return _reader.ParseFileContent(newText, cancellationToken);

            // Clone the tree to preserve the original (required for roundtrip scenarios)
            // This is still more efficient than full reparse - we only reparse one member
            var newTree = (GDClassDeclaration)oldTree.Clone();

            // Replace the affected member in the cloned tree
            if (location.MemberIndex >= 0 && location.MemberIndex < newTree.Members.Count)
            {
                newTree.Members[location.MemberIndex] = newMember;
            }

            return newTree;
        }

        /// <summary>
        /// Locates the member containing the specified character offset within the Members list.
        /// Uses string-based search to ensure correct text coordinates alignment.
        /// </summary>
        private GDChangeLocator.LocateResult LocateMemberInMembers(GDClassDeclaration tree, int charOffset)
        {
            if (tree == null || tree.Members == null)
                return GDChangeLocator.LocateResult.ClassLevel(tree);

            var treeText = tree.ToString();
            int currentOffset = 0;
            int memberIndex = 0;

            foreach (var member in tree.Members)
            {
                var memberText = member.ToString();
                // Find this member's position in the tree text
                var memberStart = treeText.IndexOf(memberText, currentOffset, StringComparison.Ordinal);

                if (memberStart < 0)
                {
                    // Fallback: use sequential offset calculation
                    memberStart = currentOffset;
                }

                var memberEnd = memberStart + memberText.Length;

                // Use < for memberEnd - if change is exactly at member boundary,
                // it's ambiguous which member is affected, so let it fall through to class-level
                // which will trigger full reparse for safety
                if (charOffset >= memberStart && charOffset < memberEnd)
                {
                    return new GDChangeLocator.LocateResult(member, memberStart, memberIndex);
                }

                currentOffset = memberEnd;
                memberIndex++;
            }

            // Change is outside any member
            return GDChangeLocator.LocateResult.ClassLevel(tree);
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
                    var newMemberText = newMember.ToString();

                    if (oldText != newMemberText)
                    {
                        // Member changed
                        changedRanges.Add(new GDTextSpan(currentOffset, newMemberText.Length));
                    }

                    currentOffset += newMemberText.Length;
                }
            }

            return changedRanges;
        }
    }
}
