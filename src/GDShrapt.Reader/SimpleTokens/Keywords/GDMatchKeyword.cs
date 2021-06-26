namespace GDShrapt.Reader
{
    public sealed class GDMatchKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "match";

        public override GDSyntaxToken Clone()
        {
            return new GDMatchKeyword();
        }
    }
}
