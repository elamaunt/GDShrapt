using System;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Represents a single member change during incremental parsing.
    /// </summary>
    public readonly struct GDMemberChange
    {
        /// <summary>
        /// Index of the changed member in the Members collection.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Snapshot of the old member before replacement.
        /// </summary>
        public GDClassMember OldMember { get; }

        /// <summary>
        /// The new member that replaced the old one.
        /// </summary>
        public GDClassMember NewMember { get; }

        public GDMemberChange(int index, GDClassMember oldMember, GDClassMember newMember)
        {
            Index = index;
            OldMember = oldMember;
            NewMember = newMember;
        }
    }

    /// <summary>
    /// Result of incremental parsing operation.
    /// Contains the updated tree and information about what changed for semantic model updates.
    /// </summary>
    public readonly struct GDIncrementalParseResult
    {
        /// <summary>
        /// The resulting AST tree after parsing.
        /// </summary>
        public GDClassDeclaration Tree { get; }

        /// <summary>
        /// List of changed members with their old and new values.
        /// Empty if full reparse or no changes.
        /// </summary>
        public IReadOnlyList<GDMemberChange> ChangedMembers { get; }

        /// <summary>
        /// True if a full reparse was performed instead of incremental update.
        /// </summary>
        public bool IsFullReparse { get; }

        /// <summary>
        /// True if incremental parsing was successful (not a full reparse).
        /// </summary>
        public bool IsIncremental => !IsFullReparse && ChangedMembers != null && ChangedMembers.Count > 0;

        private GDIncrementalParseResult(
            GDClassDeclaration tree,
            IReadOnlyList<GDMemberChange> changedMembers,
            bool isFullReparse)
        {
            Tree = tree;
            ChangedMembers = changedMembers ?? Array.Empty<GDMemberChange>();
            IsFullReparse = isFullReparse;
        }

        /// <summary>
        /// Creates a result indicating full reparse was performed.
        /// </summary>
        public static GDIncrementalParseResult FullReparse(GDClassDeclaration tree)
            => new GDIncrementalParseResult(tree, null, true);

        /// <summary>
        /// Creates a result indicating successful incremental update.
        /// </summary>
        public static GDIncrementalParseResult Incremental(
            GDClassDeclaration tree,
            IReadOnlyList<GDMemberChange> changedMembers)
            => new GDIncrementalParseResult(tree, changedMembers, false);

        /// <summary>
        /// Creates a result indicating no changes were needed.
        /// </summary>
        public static GDIncrementalParseResult NoChanges(GDClassDeclaration tree)
            => new GDIncrementalParseResult(tree, null, false);
    }
}
