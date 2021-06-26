namespace GDShrapt.Reader
{
    public sealed class GDClassKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "class";

        public override GDSyntaxToken Clone()
        {
            return new GDClassKeyword();
        }
    }
}
