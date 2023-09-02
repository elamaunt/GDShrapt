namespace GDShrapt.Reader
{
    public sealed class GDClassNameKeyword : GDKeyword
    {
        public override string Sequence => "class_name";

        public override GDSyntaxToken Clone()
        {
            return new GDClassNameKeyword();
        }
    }
}
