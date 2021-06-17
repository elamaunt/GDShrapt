using System.Collections.Generic;
using System.Text;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Basic GDScript node, may contains multiple tokens
    /// </summary>
    public abstract partial class GDNode : GDSyntaxToken, IStyleTokensReceiver
    {
        // TODO: remove virtual. Should be abstract
        internal abstract GDTokensForm Form { get; }
        internal GDTokensForm BaseForm
        {
            set => Form.MoveTokens(value);
        }

        public IEnumerable<GDSyntaxToken> Tokens => Form;

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

        internal override void HandleSharpChar(GDReadingState state)
        {
            var comment = new GDComment();
            Form.AddBeforeActiveToken(comment);
            state.Push(comment);
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
    }
}