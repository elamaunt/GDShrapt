namespace GDShrapt.Reader
{
    public sealed class GDBreakKeyword : GDKeyword
    {
        public override string Sequence => "break";

        public override GDSyntaxToken Clone()
        {
            return new GDBreakKeyword();
        }
    }
}
