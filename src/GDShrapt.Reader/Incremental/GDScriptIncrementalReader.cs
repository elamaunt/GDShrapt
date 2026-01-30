using System;
using System.Collections.Generic;
using System.Threading;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Incremental GDScript reader that reparses only affected members.
    /// Uses AST-based position calculation with OriginLength for CRLF-safe coordinates.
    /// Optimized for minimal memory allocation using in-place modification with member snapshots.
    /// </summary>
    public class GDScriptIncrementalReader : IGDIncrementalParser
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
        /// Maximum number of affected members for incremental parsing.
        /// If more members are affected, full reparse is triggered.
        /// Default is 3.
        /// </summary>
        public int MaxAffectedMembersForIncremental { get; set; } = 3;

        /// <summary>
        /// Creates a new incremental reader with default settings.
        /// </summary>
        public GDScriptIncrementalReader()
            : this(new GDScriptReader())
        {
        }

        /// <summary>
        /// Creates a new incremental reader with custom settings.
        /// </summary>
        public GDScriptIncrementalReader(GDReadSettings settings)
            : this(new GDScriptReader(settings))
        {
        }

        /// <summary>
        /// Creates a new incremental reader with an existing reader.
        /// </summary>
        public GDScriptIncrementalReader(GDScriptReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _memberReparser = new GDMemberReparser(reader);
        }

        /// <summary>
        /// Configures incremental parsing thresholds.
        /// </summary>
        /// <param name="fullReparseThreshold">Threshold ratio (0.0-1.0) for triggering full reparse.</param>
        /// <param name="maxAffectedMembers">Maximum affected members before full reparse.</param>
        public void Configure(double fullReparseThreshold, int maxAffectedMembers)
        {
            FullReparseThreshold = fullReparseThreshold;
            MaxAffectedMembersForIncremental = maxAffectedMembers;
        }

        #region Internal Structs

        private struct MemberOffset
        {
            public int Index;
            public int StartOffset;
            public int EndOffset;
            public int OriginLength;
        }

        private struct ChangeGroup
        {
            public int MemberIndex;
            public int MemberStartOffset;
            public int MemberEndOffset;
            public int OriginalMemberLength;
        }

        #endregion

        /// <inheritdoc/>
        public GDIncrementalParseResult ParseIncremental(
            GDClassDeclaration oldTree,
            string newText,
            IReadOnlyList<GDTextChange> changes,
            CancellationToken cancellationToken = default)
        {
            if (newText == null)
                throw new ArgumentNullException(nameof(newText));

            // Fallback: no old tree
            if (oldTree == null)
                return GDIncrementalParseResult.FullReparse(
                    _reader.ParseFileContent(newText, cancellationToken));

            // No changes - return as-is
            if (changes == null || changes.Count == 0)
                return GDIncrementalParseResult.NoChanges(oldTree);

            // Calculate total change magnitude without LINQ
            int totalDelta = CalculateTotalDelta(changes);
            int deltaSum = CalculateDeltaSum(changes);
            int originalLength = newText.Length - deltaSum;

            // If changes are too large, full reparse is more efficient
            if (originalLength > 0 && (double)totalDelta / originalLength > FullReparseThreshold)
                return GDIncrementalParseResult.FullReparse(
                    _reader.ParseFileContent(newText, cancellationToken));

            cancellationToken.ThrowIfCancellationRequested();

            // Build member offset table
            var memberOffsets = BuildMemberOffsetTable(oldTree);

            if (memberOffsets.Length == 0)
                return GDIncrementalParseResult.FullReparse(
                    _reader.ParseFileContent(newText, cancellationToken));

            // Group changes by affected member
            var affectedMembers = GroupChangesByMember(changes, memberOffsets, oldTree);

            // If grouping failed (cross-member edit or class attributes), full reparse
            if (affectedMembers == null || affectedMembers.Count == 0)
                return GDIncrementalParseResult.FullReparse(
                    _reader.ParseFileContent(newText, cancellationToken));

            // If too many members affected, full reparse is more efficient
            if (affectedMembers.Count > MaxAffectedMembersForIncremental)
                return GDIncrementalParseResult.FullReparse(
                    _reader.ParseFileContent(newText, cancellationToken));

            cancellationToken.ThrowIfCancellationRequested();

            // Clone the tree to avoid modifying the original
            var newTree = (GDClassDeclaration)oldTree.Clone();

            // Reparse each affected member
            var memberChanges = new GDMemberChange[affectedMembers.Count];

            for (int i = 0; i < affectedMembers.Count; i++)
            {
                var group = affectedMembers[i];

                // Calculate adjusted offsets based on original change positions
                // Find the delta sum of changes before this member (by original position)
                int deltaFromPriorChanges = 0;
                int deltaFromThisMember = 0;
                for (int j = 0; j < changes.Count; j++)
                {
                    if (changes[j].OldEnd <= group.MemberStartOffset)
                        deltaFromPriorChanges += changes[j].Delta;
                    else if (changes[j].Start >= group.MemberStartOffset && changes[j].Start < group.MemberEndOffset)
                        deltaFromThisMember += changes[j].Delta;
                }

                // The start is shifted by prior changes
                int adjustedStart = group.MemberStartOffset + deltaFromPriorChanges;
                // The end is shifted by prior changes plus the change in this member
                int adjustedEnd = group.MemberEndOffset + deltaFromPriorChanges + deltaFromThisMember;

                // Validate adjusted bounds
                if (adjustedEnd <= adjustedStart ||
                    adjustedStart >= newText.Length ||
                    (group.OriginalMemberLength > 10 && (adjustedEnd - adjustedStart) < group.OriginalMemberLength / 5))
                {
                    return GDIncrementalParseResult.FullReparse(
                        _reader.ParseFileContent(newText, cancellationToken));
                }

                // Reparse the affected member
                var newMember = _memberReparser.ReparseMember(
                    newText, adjustedStart, adjustedEnd, cancellationToken);

                if (newMember == null)
                    return GDIncrementalParseResult.FullReparse(
                        _reader.ParseFileContent(newText, cancellationToken));

                // Snapshot the old member
                var oldMemberSnapshot = (GDClassMember)newTree.Members[group.MemberIndex].Clone();

                // Replace member in the cloned tree
                newTree.Members[group.MemberIndex] = newMember;

                // Record the change
                memberChanges[i] = new GDMemberChange(group.MemberIndex, oldMemberSnapshot, newMember);

                cancellationToken.ThrowIfCancellationRequested();
            }

            return GDIncrementalParseResult.Incremental(newTree, memberChanges);
        }

        /// <summary>
        /// Builds a table of member offsets for binary search.
        /// The offsets are absolute positions in the file, accounting for class attributes before members.
        /// </summary>
        private MemberOffset[] BuildMemberOffsetTable(GDClassDeclaration tree)
        {
            if (tree?.Members == null)
                return Array.Empty<MemberOffset>();

            int memberCount = tree.Members.Count;
            if (memberCount == 0)
                return Array.Empty<MemberOffset>();

            var table = new MemberOffset[memberCount];

            // First, calculate offset to where Members section starts
            // by iterating through tree.Tokens until we reach tree.Members
            int membersStartOffset = 0;
            foreach (var token in tree.Tokens)
            {
                if (ReferenceEquals(token, tree.Members))
                    break;
                membersStartOffset += token.OriginLength;
            }

            // Now iterate through Members.Tokens with the base offset
            int currentOffset = membersStartOffset;
            int memberIndex = 0;

            foreach (var token in tree.Members.Tokens)
            {
                if (token is GDClassMember)
                {
                    table[memberIndex] = new MemberOffset
                    {
                        Index = memberIndex,
                        StartOffset = currentOffset,
                        EndOffset = currentOffset + token.OriginLength,
                        OriginLength = token.OriginLength
                    };
                    memberIndex++;
                }
                currentOffset += token.OriginLength;
            }

            return table;
        }

        /// <summary>
        /// Finds the member index containing the specified offset using binary search.
        /// Returns -1 if offset is not within any member (e.g., class attributes or whitespace).
        /// </summary>
        private static int FindMemberByOffset(int offset, MemberOffset[] table)
        {
            if (table.Length == 0)
                return -1;

            int left = 0;
            int right = table.Length - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;

                if (offset < table[mid].StartOffset)
                    right = mid - 1;
                else if (offset >= table[mid].EndOffset)
                    left = mid + 1;
                else
                    return mid;
            }

            return -1;
        }

        /// <summary>
        /// Groups changes by affected member.
        /// Returns null if any change crosses member boundaries, affects class attributes, or is in whitespace.
        /// </summary>
        private List<ChangeGroup> GroupChangesByMember(
            IReadOnlyList<GDTextChange> changes,
            MemberOffset[] memberOffsets,
            GDClassDeclaration tree)
        {
            if (memberOffsets.Length == 0)
                return null;

            var groups = new List<ChangeGroup>();
            var seenMembers = new bool[memberOffsets.Length];

            for (int i = 0; i < changes.Count; i++)
            {
                var change = changes[i];

                // Find member containing change start
                int startMemberIndex = FindMemberByOffset(change.Start, memberOffsets);

                // Change starts before first member or in whitespace - class level
                if (startMemberIndex < 0)
                    return null;

                // Check if the affected member is a class attribute (extends, class_name, etc.)
                // Class attributes affect the entire class semantics and should trigger full reparse
                var affectedMember = GetMemberAtIndex(tree, startMemberIndex);
                if (affectedMember is GDClassAttribute)
                    return null;

                // Check if change extends beyond member boundary
                int endOffset = change.Start + change.OldLength;
                if (endOffset > memberOffsets[startMemberIndex].EndOffset)
                {
                    // Check if end is in a different member
                    int endMemberIndex = FindMemberByOffset(endOffset - 1, memberOffsets);
                    if (endMemberIndex != startMemberIndex && endMemberIndex >= 0)
                        return null; // Cross-member edit
                }

                // Add to groups if not already seen
                if (!seenMembers[startMemberIndex])
                {
                    seenMembers[startMemberIndex] = true;
                    groups.Add(new ChangeGroup
                    {
                        MemberIndex = startMemberIndex,
                        MemberStartOffset = memberOffsets[startMemberIndex].StartOffset,
                        MemberEndOffset = memberOffsets[startMemberIndex].EndOffset,
                        OriginalMemberLength = memberOffsets[startMemberIndex].OriginLength
                    });
                }
            }

            // Sort by member index (ascending)
            groups.Sort((a, b) => a.MemberIndex.CompareTo(b.MemberIndex));

            return groups;
        }

        /// <summary>
        /// Gets the member at the specified index from the tree.
        /// </summary>
        private static GDClassMember GetMemberAtIndex(GDClassDeclaration tree, int index)
        {
            int currentIndex = 0;
            foreach (var token in tree.Members.Tokens)
            {
                if (token is GDClassMember member)
                {
                    if (currentIndex == index)
                        return member;
                    currentIndex++;
                }
            }
            return null;
        }

        /// <summary>
        /// Adjusts an offset for a specific member, considering only changes
        /// that affect positions before the member's original start.
        /// </summary>
        private static int AdjustOffsetForMember(int offset, IReadOnlyList<GDTextChange> changes, int memberOriginalStart)
        {
            int adjusted = offset;

            for (int i = 0; i < changes.Count; i++)
            {
                var change = changes[i];

                // Only apply changes that are entirely before this member's original position
                if (change.OldEnd <= memberOriginalStart)
                {
                    adjusted += change.Delta;
                }
                // Change starts before but extends into/past the member
                else if (change.Start < memberOriginalStart && change.OldEnd > memberOriginalStart)
                {
                    // Partial overlap - shift by full delta since change is before
                    adjusted += change.Delta;
                }
            }

            return Math.Max(0, adjusted);
        }

        /// <summary>
        /// Locates the member containing the specified character offset.
        /// Uses AST iteration with OriginLength for CRLF-safe coordinate calculation.
        /// No string operations (IndexOf, ToString).
        /// </summary>
        private GDChangeLocator.LocateResult LocateMemberByOffset(GDClassDeclaration tree, int charOffset)
        {
            if (tree?.Members == null)
                return GDChangeLocator.LocateResult.ClassLevel(tree);

            int currentOffset = 0;
            int memberIndex = 0;

            foreach (var token in tree.Members.Tokens)
            {
                int tokenEnd = currentOffset + token.OriginLength;

                if (token is GDClassMember member)
                {
                    if (charOffset >= currentOffset && charOffset < tokenEnd)
                        return new GDChangeLocator.LocateResult(member, currentOffset, memberIndex);

                    memberIndex++;
                }
                else if (charOffset >= currentOffset && charOffset < tokenEnd)
                {
                    return GDChangeLocator.LocateResult.ClassLevel(tree);
                }

                currentOffset = tokenEnd;
            }

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

            int oldCount = oldTree.Members.Count;
            int newCount = newTree.Members.Count;
            int maxCount = Math.Max(oldCount, newCount);

            int currentOffset = 0;

            foreach (var token in newTree.Tokens)
            {
                if (ReferenceEquals(token, newTree.Members))
                    break;
                currentOffset += token.OriginLength;
            }

            for (int i = 0; i < maxCount; i++)
            {
                if (i >= oldCount)
                {
                    var newMember = newTree.Members[i];
                    changedRanges.Add(new GDTextSpan(currentOffset, newMember.OriginLength));
                    currentOffset += newMember.OriginLength;
                }
                else if (i >= newCount)
                {
                    // Deleted
                }
                else
                {
                    var oldMember = oldTree.Members[i];
                    var newMember = newTree.Members[i];

                    bool isChanged;
                    if (ReferenceEquals(oldMember, newMember))
                    {
                        isChanged = false;
                    }
                    else if (oldMember.OriginLength != newMember.OriginLength)
                    {
                        isChanged = true;
                    }
                    else
                    {
                        isChanged = oldMember.ToOriginalString() != newMember.ToOriginalString();
                    }

                    if (isChanged)
                    {
                        changedRanges.Add(new GDTextSpan(currentOffset, newMember.OriginLength));
                    }

                    currentOffset += newMember.OriginLength;
                }
            }

            return changedRanges;
        }

        #region Helper Methods (No LINQ)

        private static GDTextChange FindFirstChange(IReadOnlyList<GDTextChange> changes)
        {
            var first = changes[0];
            for (int i = 1; i < changes.Count; i++)
            {
                if (changes[i].Start < first.Start)
                    first = changes[i];
            }
            return first;
        }

        private static int CalculateTotalDelta(IReadOnlyList<GDTextChange> changes)
        {
            int total = 0;
            for (int i = 0; i < changes.Count; i++)
                total += Math.Max(changes[i].OldLength, changes[i].NewLength);
            return total;
        }

        private static int CalculateDeltaSum(IReadOnlyList<GDTextChange> changes)
        {
            int sum = 0;
            for (int i = 0; i < changes.Count; i++)
                sum += changes[i].Delta;
            return sum;
        }

        private static int AdjustOffset(int originalOffset, IReadOnlyList<GDTextChange> changes)
        {
            int adjusted = originalOffset;

            for (int i = 0; i < changes.Count; i++)
            {
                var change = changes[i];

                if (change.OldEnd <= originalOffset)
                {
                    adjusted += change.Delta;
                }
                else if (change.Start < originalOffset)
                {
                    adjusted = change.Start + change.NewLength;
                }
            }

            return Math.Max(0, adjusted);
        }

        #endregion
    }
}
