namespace GDShrapt.Reader
{
    public sealed class GDToolKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "tool";

        public override GDSyntaxToken Clone()
        {
            return new GDToolKeyword();
        }
    }
}
