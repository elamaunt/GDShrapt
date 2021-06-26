namespace GDShrapt.Reader
{
    public sealed class GDVarKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "var";

        public override GDSyntaxToken Clone()
        {
            return new GDVarKeyword();
        }
    }
}
