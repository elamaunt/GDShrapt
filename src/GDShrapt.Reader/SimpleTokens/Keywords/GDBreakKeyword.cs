namespace GDShrapt.Reader
{
    public sealed class GDBreakKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "break";

        public override GDSyntaxToken Clone()
        {
            return new GDBreakKeyword();
        }
    }
}
