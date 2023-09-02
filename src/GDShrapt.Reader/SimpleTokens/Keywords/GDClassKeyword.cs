namespace GDShrapt.Reader
{
    public sealed class GDClassKeyword : GDKeyword
    {
        public override string Sequence => "class";

        public override GDSyntaxToken Clone()
        {
            return new GDClassKeyword();
        }
    }
}
