namespace GDShrapt.Reader
{
    public sealed class GDContinueKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "continue";

        public override GDSyntaxToken Clone()
        {
            return new GDContinueKeyword();
        }
    }
}
