namespace GDShrapt.Reader
{
    public sealed class GDIfKeyword : GDKeyword
    {
        public override string Sequence => "if";

        public override GDSyntaxToken Clone()
        {
            return new GDIfKeyword();
        }
    }
}
