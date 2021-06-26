namespace GDShrapt.Reader
{
    public sealed class GDElseKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "else";

        public override GDSyntaxToken Clone()
        {
            return new GDElseKeyword();
        }
    }
}
