namespace GDShrapt.Reader
{
    public sealed class GDExtendsKeyword : GDKeyword
    {
        public override string Sequence => "extends";

        public override GDSyntaxToken Clone()
        {
            return new GDExtendsKeyword();
        }
    }
}
