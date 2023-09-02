namespace GDShrapt.Reader
{
    public sealed class GDElseKeyword : GDKeyword
    {
        public override string Sequence => "else";

        public override GDSyntaxToken Clone()
        {
            return new GDElseKeyword();
        }
    }
}
