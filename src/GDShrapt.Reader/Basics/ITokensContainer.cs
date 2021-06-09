namespace GDShrapt.Reader
{
    internal interface ITokensContainer
    {
        void Append(GDSyntaxToken token);
        void AppendExpressionSkip();
        void AppendKeywordSkip();
    }
}