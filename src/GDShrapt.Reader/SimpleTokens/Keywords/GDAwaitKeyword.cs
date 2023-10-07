namespace GDShrapt.Reader
{
    public sealed class GDAwaitKeyword : GDKeyword
    {
        public override string Sequence => "await";

        public override GDSyntaxToken Clone()
        {
            return new GDAwaitKeyword();
        }
    }
}
