namespace GDShrapt.Reader
{
    public sealed class GDPassKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "pass";

        public override GDSyntaxToken Clone()
        {
            return new GDPassKeyword();
        }
    }
}
