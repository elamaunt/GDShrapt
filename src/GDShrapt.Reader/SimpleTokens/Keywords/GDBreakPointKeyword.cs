namespace GDShrapt.Reader
{
    public sealed class GDBreakPointKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "breakpoint";

        public override GDSyntaxToken Clone()
        {
            return new GDBreakPointKeyword();
        }
    }
}
