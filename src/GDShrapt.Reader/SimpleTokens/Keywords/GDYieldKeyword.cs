namespace GDShrapt.Reader
{
    public sealed class GDYieldKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "yield";

        public override GDSyntaxToken Clone()
        {
            return new GDYieldKeyword();
        }
    }
}
