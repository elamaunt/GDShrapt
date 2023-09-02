namespace GDShrapt.Reader
{
    public sealed class GDPassKeyword : GDKeyword
    {
        public override string Sequence => "pass";

        public override GDSyntaxToken Clone()
        {
            return new GDPassKeyword();
        }
    }
}
