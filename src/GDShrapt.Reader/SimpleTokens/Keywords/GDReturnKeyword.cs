namespace GDShrapt.Reader
{
    public sealed class GDReturnKeyword : GDKeyword
    {
        public override string Sequence => "return";

        public override GDSyntaxToken Clone()
        {
            return new GDReturnKeyword();
        }
    }
}
