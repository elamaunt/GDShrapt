namespace GDShrapt.Reader
{
    public sealed class GDReturnTypeKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "->";

        public override GDSyntaxToken Clone()
        {
            return new GDReturnTypeKeyword();
        }
    }
}
