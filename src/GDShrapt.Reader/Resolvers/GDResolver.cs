namespace GDShrapt.Reader
{
    internal abstract class GDResolver : GDNode
    {
        public ITokensContainer Owner { get; }

        public GDResolver(ITokensContainer owner)
        {
            Owner = owner;
        }

        protected void Append(GDSyntaxToken token)
        {
            Owner.Append(token);
        }

        protected void AppendExpressionSkip()
        {
            Owner.AppendExpressionSkip();
        }

        protected void AppendKeywordSkip()
        {
            Owner.AppendKeywordSkip();
        }
    }
}