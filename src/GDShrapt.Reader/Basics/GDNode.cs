using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Basic GDScript node, may contains multiple tokens
    /// </summary>
    public abstract class GDNode : GDSyntaxToken, ITokensContainer
    {
        internal LinkedList<GDSyntaxToken> TokensList { get; } = new LinkedList<GDSyntaxToken>();

        public IEnumerable<GDSyntaxToken> Tokens()
        {
            foreach (var token in TokensList.OfType<GDSyntaxToken>())
                yield return token;
        }

        /// <summary>
        /// Removes child node or does nothing if node is already removed.
        /// </summary>
        /// <param name="token">Child token</param>
        public virtual void RemoveChild(GDSyntaxToken token)
        {
            if (!ReferenceEquals(Parent, this))
                throw new InvalidOperationException("The specified node has a different parent.");

            var node = token.ParentLinkedListNode;

            if (node == null || !ReferenceEquals(node.List, TokensList))
                throw new InvalidOperationException("The specified node hasn't a linkedList reference to the parent.");

            TokensList.Remove(node);
        }

        internal void SwitchTo(GDSimpleSyntaxToken token, GDReadingState state)
        {
            TokensList.AddLast(token);
            state.Push(token);
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            SwitchTo(new GDComment(), state);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            state.Pop();
        }

        public override void AppendTo(StringBuilder builder)
        {
            foreach (var token in TokensList)
                token.AppendTo(builder);
        }

        internal void AppendToThisNode(GDSyntaxToken token)
        {
            TokensList.AddLast(token);
        }

        void ITokensContainer.Append(GDSyntaxToken token)
        {
            AppendToThisNode(token);
        }

        void ITokensContainer.AppendExpressionSkip()
        {
            throw new NotImplementedException();
        }

        void ITokensContainer.AppendKeywordSkip()
        {
            throw new NotImplementedException();
        }
    }
}