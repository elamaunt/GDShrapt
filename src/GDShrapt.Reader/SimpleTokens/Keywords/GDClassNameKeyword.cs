namespace GDShrapt.Reader
{
    public sealed class GDClassNameKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "class_name";

        public override GDSyntaxToken Clone()
        {
            return new GDClassNameKeyword();
        }
    }
}
