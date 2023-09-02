namespace GDShrapt.Reader
{
    public sealed class GDStaticKeyword : GDKeyword
    {
        public override string Sequence => "static";

        public override GDSyntaxToken Clone()
        {
            return new GDStaticKeyword();
        }
    }
}
