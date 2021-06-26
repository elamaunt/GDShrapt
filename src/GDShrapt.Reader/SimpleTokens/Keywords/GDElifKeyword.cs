namespace GDShrapt.Reader
{
    public sealed class GDElifKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "elif";

        public override GDSyntaxToken Clone()
        {
            return new GDElifKeyword();
        }
    }
}
