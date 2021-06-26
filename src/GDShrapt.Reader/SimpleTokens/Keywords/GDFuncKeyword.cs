namespace GDShrapt.Reader
{
    public sealed class GDFuncKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "func";

        public override GDSyntaxToken Clone()
        {
            return new GDFuncKeyword();
        }
    }
}
