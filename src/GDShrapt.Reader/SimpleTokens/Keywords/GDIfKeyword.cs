namespace GDShrapt.Reader
{
    public sealed class GDForKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "for";

        public override GDSyntaxToken Clone()
        {
            return new GDForKeyword();
        }
    }
}
