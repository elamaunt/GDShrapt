using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Basic GDScript node, may contains multiple tokens
    /// </summary>
    public abstract partial class GDNode : GDSyntaxToken, IEnumerable<GDSyntaxToken>
    {
        // internal abstract ICollection<GDSyntaxToken> Collection { get; }
        internal virtual ICollection<GDSyntaxToken> Collection => throw new NotImplementedException();

        public IEnumerable<GDSyntaxToken> Tokens()
        {
            foreach (var token in Collection)
                yield return token;
        }

        /*
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
        }*/

        internal virtual void AppendAndPush(GDSyntaxToken token, GDReadingState state)
        {
            Collection.Add(token);
            state.Push(token);
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            AppendAndPush(new GDComment(), state);
        }

        public override void AppendTo(StringBuilder builder)
        {
            foreach (var token in Collection)
                token.AppendTo(builder);
        }

        /*void ITokensReceiver.HandleReceivedToken(GDKeywordToken token) => HandleReceivedToken(token);
        internal virtual void HandleReceivedToken(GDKeywordToken token)
        {
            throw new GDInvalidReadingStateException($"This node '{NodeName}' doesn't support handling tokens");
        }

        void ITokensReceiver.HandleReceivedExpressionSkip() => HandleReceivedExpressionSkip();
        internal virtual void HandleReceivedExpressionSkip()
        {
            throw new GDInvalidReadingStateException($"This node '{NodeName}' doesn't support handling Expression skip");
        }*/

        /*void ITokensReceiver.HandleReceivedKeywordSkip() => HandleReceivedKeywordSkip();
        internal virtual void HandleReceivedKeywordSkip()
        {
            throw new GDInvalidReadingStateException($"This node '{NodeName}' doesn't support handling Keyword skip");
        }*/

        public override string ToString()
        {
            var builder = new StringBuilder();
            
            foreach (var token in Collection)
                token.AppendTo(builder);

            return builder.ToString();
        }

        public IEnumerator<GDSyntaxToken> GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Collection.GetEnumerator();
        }
    }
}