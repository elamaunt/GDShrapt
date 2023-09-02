namespace GDShrapt.Reader
{
    public sealed class GDYieldKeyword : GDKeyword
    {
        public override string Sequence => "yield";

        public override GDSyntaxToken Clone()
        {
            return new GDYieldKeyword();
        }
    }
}