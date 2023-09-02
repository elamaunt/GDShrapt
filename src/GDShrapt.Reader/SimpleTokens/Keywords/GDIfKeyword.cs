namespace GDShrapt.Reader
{
    public sealed class GDForKeyword : GDKeyword
    {
        public override string Sequence => "for";

        public override GDSyntaxToken Clone()
        {
            return new GDForKeyword();
        }
    }
}
