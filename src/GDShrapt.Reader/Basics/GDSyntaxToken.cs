﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Basic syntax node.
    /// </summary>
    public abstract class GDSyntaxToken
    {
        readonly WeakReference<GDNode> _parentWeakRef = new WeakReference<GDNode>(null);
        WeakReference<LinkedListNode<GDSyntaxToken>> _parentListNodeWeakRef = new WeakReference<LinkedListNode<GDSyntaxToken>>(null);

        /// <summary>
        /// Name of the node Type class.
        /// </summary>
        public string NodeName => GetType().Name;

        /// <summary>
        /// Parent node in a lexical tree
        /// </summary>
        public GDNode Parent
        {
            get
            {
                _parentWeakRef.TryGetTarget(out GDNode parent);
                return parent;
            }

            internal set
            {
                _parentWeakRef.SetTarget(value);
            }
        }

        /// <summary>
        /// LinkedListNode reference, if the node has a parent. Used by <see cref="RemoveChild"/> method.
        /// </summary>
        internal LinkedListNode<GDSyntaxToken> ParentLinkedListNode
        {
            get
            {
                _parentListNodeWeakRef.TryGetTarget(out LinkedListNode<GDSyntaxToken> linkedListNode);
                return linkedListNode;
            }
        }

        /// <summary>
        /// Removes this node parent or do nothing if <see cref="Parent"/> <see langword="null"/>
        /// </summary>
        public void RemoveFromParent()
        {
            Parent?.RemoveChild(this);
        }

        /// <summary>
        /// Pass single character in the node. 
        /// If the node can't handle the character it may return the character to previous node in reading state.
        /// </summary>
        /// <param name="c">Character</param>
        /// <param name="state">Current reading state</param>
        internal abstract void HandleChar(char c, GDReadingState state);

        /// <summary>
        /// The same <see cref="HandleChar(char, GDReadingState)"/> but separated method for new line character
        /// </summary>
        /// <param name="state">Current reading state</param>
        internal abstract void HandleLineFinish(GDReadingState state);

        /// <summary>
        /// Simple check on whitespace characters ' ' and '\t'.
        /// </summary>
        /// <param name="c">One char to check</param>
        internal bool IsSpace(char c) => c == ' ' || c == '\t';

        /// <summary>
        /// The same <see cref="HandleChar(char, GDReadingState)"/> but separated method for sharp (line commentary) character.
        /// Default implementation will add a new comment token in the reading state.
        /// </summary>
        /// <param name="state">Current reading state</param>
        internal abstract void HandleSharpChar(GDReadingState state);

        /// <summary>
        /// Force completes token characters handling process in terms of current reading state.
        /// Used for situation when the reading code has ended.
        /// </summary>
        /// <param name="state">Current reading state</param>
        internal abstract void ForceComplete(GDReadingState state);

        /// <summary>
        /// Adds token string representation to <see cref="StringBuilder"/> instance.
        /// </summary>
        /// <param name="builder"></param>
        public abstract void AppendTo(StringBuilder builder);

        public override string ToString()
        {
            var builder = new StringBuilder();
            AppendTo(builder);
            return builder.ToString();
        }
    }
}