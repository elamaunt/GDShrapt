namespace GDShrapt.Reader
{
    public sealed class GDGetKeyword : GDKeyword
    {
        public override string Sequence => "get";

        public override GDSyntaxToken Clone()
        {
            return new GDGetKeyword();
        }
    }
}
