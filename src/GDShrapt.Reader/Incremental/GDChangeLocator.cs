namespace GDShrapt.Reader
{
    /// <summary>
    /// Locates the node affected by a text change. Zero-allocation using readonly struct.
    /// </summary>
    public static class GDChangeLocator
    {
        /// <summary>
        /// Result of locating the affected node.
        /// </summary>
        public readonly struct LocateResult
        {
            /// <summary>
            /// The node that contains the change.
            /// </summary>
            public readonly GDNode Node;

            /// <summary>
            /// Character offset of the node start in the original text.
            /// </summary>
            public readonly int NodeOffset;

            /// <summary>
            /// Index of the member in the class (-1 if not a class member).
            /// </summary>
            public readonly int MemberIndex;

            public LocateResult(GDNode node, int offset, int memberIndex)
            {
                Node = node;
                NodeOffset = offset;
                MemberIndex = memberIndex;
            }

            /// <summary>
            /// Creates a result indicating a class-level change.
            /// </summary>
            public static LocateResult ClassLevel(GDClassDeclaration tree)
                => new LocateResult(tree, 0, -1);

            /// <summary>
            /// Indicates if the change is at class level (not within a specific member).
            /// </summary>
            public bool IsClassLevel => MemberIndex < 0;
        }

        /// <summary>
        /// Locates the node that contains the specified character offset.
        /// </summary>
        /// <param name="tree">The class declaration tree to search in.</param>
        /// <param name="charOffset">Character offset of the change in the original text.</param>
        /// <returns>Location result with node and offset information.</returns>
        public static LocateResult Locate(GDClassDeclaration tree, int charOffset)
        {
            if (tree == null)
                return new LocateResult(null, 0, -1);

            int currentOffset = 0;
            int memberIndex = 0;

            // Walk through tokens until we reach Members
            foreach (var token in tree.Tokens)
            {
                if (token == tree.Members)
                    break;
                currentOffset += token.Length;
            }

            int membersStartOffset = currentOffset;

            // If change is before Members -> class-level
            if (charOffset < membersStartOffset)
                return LocateResult.ClassLevel(tree);

            // Walk through members
            foreach (var member in tree.Members)
            {
                int memberEnd = currentOffset + member.Length;

                if (charOffset >= currentOffset && charOffset < memberEnd)
                {
                    // Change is within this member
                    return new LocateResult(member, currentOffset, memberIndex);
                }

                currentOffset = memberEnd;
                memberIndex++;
            }

            // Change is after all members -> class-level
            return LocateResult.ClassLevel(tree);
        }

        /// <summary>
        /// Locates the deepest node that contains the specified character offset.
        /// </summary>
        /// <param name="startNode">The node to start searching from.</param>
        /// <param name="charOffset">Character offset relative to the start node.</param>
        /// <param name="nodeStartOffset">Character offset of the start node in the text.</param>
        /// <returns>The deepest node containing the offset, or the start node if no child contains it.</returns>
        public static GDNode LocateDeepest(GDNode startNode, int charOffset, int nodeStartOffset = 0)
        {
            if (startNode == null)
                return null;

            int currentOffset = nodeStartOffset;

            foreach (var token in startNode.Tokens)
            {
                int tokenEnd = currentOffset + token.Length;

                if (charOffset >= currentOffset && charOffset < tokenEnd)
                {
                    if (token is GDNode childNode)
                    {
                        // Recursively search in child
                        return LocateDeepest(childNode, charOffset, currentOffset);
                    }
                    // Found at this level
                    return startNode;
                }

                currentOffset = tokenEnd;
            }

            // Offset is within this node but not in any child
            return startNode;
        }
    }
}
