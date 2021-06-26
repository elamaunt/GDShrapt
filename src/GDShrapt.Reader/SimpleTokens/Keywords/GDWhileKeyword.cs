namespace GDShrapt.Reader
{
    public sealed class GDIfKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "if";

        public override GDSyntaxToken Clone()
        {
            return new GDIfKeyword();
        }
    }
}
