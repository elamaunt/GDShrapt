namespace GDShrapt.Reader
{
    public sealed class GDConstKeyword : GDKeyword
    {
        public override string Sequence => "const";

        public override GDSyntaxToken Clone()
        {
            return new GDConstKeyword();
        }
    }
}
