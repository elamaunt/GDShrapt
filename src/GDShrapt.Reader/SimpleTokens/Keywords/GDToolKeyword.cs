namespace GDShrapt.Reader
{
    public sealed class GDToolKeyword : GDKeyword
    {
        public override string Sequence => "tool";

        public override GDSyntaxToken Clone()
        {
            return new GDToolKeyword();
        }
    }
}
