namespace GDShrapt.Reader
{
    public sealed class GDMatchKeyword : GDKeyword
    {
        public override string Sequence => "match";

        public override GDSyntaxToken Clone()
        {
            return new GDMatchKeyword();
        }
    }
}
