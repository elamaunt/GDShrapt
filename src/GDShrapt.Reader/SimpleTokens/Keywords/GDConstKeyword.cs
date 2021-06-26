namespace GDShrapt.Reader
{
    public sealed class GDConstKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "const";

        public override GDSyntaxToken Clone()
        {
            return new GDConstKeyword();
        }
    }
}
