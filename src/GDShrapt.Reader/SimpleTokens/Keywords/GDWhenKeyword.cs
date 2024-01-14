namespace GDShrapt.Reader
{
    public sealed class GDWhenKeyword : GDKeyword
    {
        public override string Sequence => "when";

        public override GDSyntaxToken Clone()
        {
            return new GDWhenKeyword();
        }
    }
}
