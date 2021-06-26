using System;
using System.Collections.Generic;
using System.Text;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Basic GDScript node, may contains multiple tokens
    /// </summary>
    public abstract class GDNode : GDSyntaxToken, IStyleTokensReceiver
    {
        internal abstract GDTokensForm Form { get; }
        
        public IEnumerable<GDSyntaxToken> Tokens => Form;

        public IEnumerable<GDSyntaxToken> AllTokens
        {
            get
            {
                foreach (var token in Form)
                {
                    if (token is GDNode node)
                    {
                        foreach (var nodeToken in node.AllTokens)
                            yield return nodeToken;
                    }
                    else
                        yield return token;
                }
            }
        }

        public IEnumerable<GDNode> AllNodes
        {
            get
            {
                foreach (var token in Form)
                {
                    if (token is GDNode node)
                    {
                        yield return node;

                        foreach (var nodeToken in node.AllNodes)
                            yield return nodeToken;
                    }
                }
            }
        }

        /// <summary>
        /// Removes child node or does nothing if node is already removed.
        /// </summary>
        /// <param name="token">Child token</param>
        public bool RemoveChild(GDSyntaxToken token)
        {
            if (!ReferenceEquals(token.Parent, this))
                throw new InvalidOperationException("The specified node has a different parent.");

            return Form.Remove(token);
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            var comment = new GDComment();
            Form.AddBeforeActiveToken(comment);
            state.Push(comment);
            state.PassSharpChar();
        }

        public override void AppendTo(StringBuilder builder)
        {
            foreach (var token in Form)
                token.AppendTo(builder);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            
            foreach (var token in Form)
                token.AppendTo(builder);

            return builder.ToString();
        }

        /// <summary>
        /// Creates empty instance of this node type
        /// </summary>
        /// <returns>New empty instance</returns>
        public abstract GDNode CreateEmptyInstance();

        public override GDSyntaxToken Clone()
        {
            var node = CreateEmptyInstance();
            node.Form.CloneFrom(node.Form);
            return node;
        }

        void ITokenReceiver.HandleReceivedToken(GDInvalidToken token)
        {
            Form.AddBeforeActiveToken(token);
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDComment token)
        {
            Form.AddBeforeActiveToken(token);
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDNewLine token)
        {
            Form.AddBeforeActiveToken(token);
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDSpace token)
        {
            Form.AddBeforeActiveToken(token);
        }

        void ITokenReceiver.HandleAbstractToken(GDSyntaxToken token)
        {
            Form.AddBeforeActiveToken(token);
        }
    }
}