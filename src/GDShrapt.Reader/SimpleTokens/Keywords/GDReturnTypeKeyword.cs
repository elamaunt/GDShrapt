namespace GDShrapt.Reader
{
    public sealed class GDReturnTypeKeyword : GDKeyword
    {
        public override string Sequence => "->";

        public override GDSyntaxToken Clone()
        {
            return new GDReturnTypeKeyword();
        }
    }
}
