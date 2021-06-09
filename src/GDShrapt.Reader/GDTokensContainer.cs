using System.Collections.Generic;

namespace GDShrapt.Reader
{
    internal class GDTokensContainer : ITokensContainer
    {
        public LinkedList<GDSyntaxToken> TokensList { get; } = new LinkedList<GDSyntaxToken>();

        public void Append(GDSyntaxToken token)
        {
            TokensList.AddLast(token);
        }

        public void AppendExpressionSkip()
        {
            // Nothing
        }

        public void AppendKeywordSkip()
        {
            // Nothing
        }
    }
}