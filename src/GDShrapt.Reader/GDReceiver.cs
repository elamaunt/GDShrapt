using System.Collections.Generic;

namespace GDShrapt.Reader
{
    internal class GDReceiver :
        IIntendedTokenReceiver<GDStatement>,
        ITokenOrSkipReceiver<GDExpression>,
        IIntendedTokenReceiver<GDNode>,
        ITokenOrSkipReceiver<GDTypeNode>
    {
        public List<GDSyntaxToken> Tokens { get; } = new List<GDSyntaxToken>();

        public void HandleReceivedToken(GDStatement token)
        {
            Tokens.Add(token);
        }

        public void HandleReceivedToken(GDComment token)
        {
            Tokens.Add(token);
        }

        public void HandleReceivedToken(GDAttribute token)
        {
            Tokens.Add(token);
        }

        public void HandleReceivedToken(GDNewLine token)
        {
            Tokens.Add(token);
        }

        public void HandleReceivedToken(GDSpace token)
        {
            Tokens.Add(token);
        }

        public void HandleReceivedToken(GDInvalidToken token)
        {
            Tokens.Add(token);
        }

        public void HandleReceivedToken(GDExpression token)
        {
            Tokens.Add(token);
        }

        public void HandleReceivedToken(GDIntendation token)
        {
            Tokens.Add(token);
        }

        public void HandleReceivedToken(GDNode token)
        {
            Tokens.Add(token);
        }

        public void HandleReceivedToken(GDTypeNode token)
        {
            Tokens.Add(token);
        }

        public void HandleReceivedToken(GDMultiLineSplitToken token)
        {
            Tokens.Add(token);
        }

        public void HandleReceivedTokenSkip()
        {

        }
    }
}