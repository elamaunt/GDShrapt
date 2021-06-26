namespace GDShrapt.Reader
{
    public sealed class GDStaticKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "static";

        public override GDSyntaxToken Clone()
        {
            return new GDStaticKeyword();
        }
    }
}
