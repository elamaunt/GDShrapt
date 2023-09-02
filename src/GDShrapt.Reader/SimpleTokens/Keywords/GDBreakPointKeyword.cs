namespace GDShrapt.Reader
{
    public sealed class GDBreakPointKeyword : GDKeyword
    {
        public override string Sequence => "breakpoint";

        public override GDSyntaxToken Clone()
        {
            return new GDBreakPointKeyword();
        }
    }
}
