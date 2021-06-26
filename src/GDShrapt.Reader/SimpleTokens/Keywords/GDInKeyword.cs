namespace GDShrapt.Reader
{
    public sealed class GDInKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "in";

        public override GDSyntaxToken Clone()
        {
            return new GDInKeyword();
        }
    }
}
