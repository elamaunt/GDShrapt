namespace GDShrapt.Reader
{
    public sealed class GDReturnKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "return";

        public override GDSyntaxToken Clone()
        {
            return new GDReturnKeyword();
        }
    }
}
