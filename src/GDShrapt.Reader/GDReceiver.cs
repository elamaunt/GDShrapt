using System.Collections.Generic;

namespace GDShrapt.Reader
{
    internal class GDReceiver : IStatementsReceiver, IExpressionsReceiver
    {
        public List<GDSyntaxToken> Tokens { get; } = new List<GDSyntaxToken>();

        public void HandleReceivedAbstractToken(GDSyntaxToken token)
        {
            Tokens.Add(token);
        }

        public void HandleReceivedToken(GDStatement token)
        {
            Tokens.Add(token);
        }

        public void HandleReceivedToken(GDComment token)
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
        
        public void HandleReceivedExpressionSkip()
        {

        }
    }
}