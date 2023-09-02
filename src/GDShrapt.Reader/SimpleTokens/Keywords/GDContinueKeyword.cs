namespace GDShrapt.Reader
{
    public sealed class GDContinueKeyword : GDKeyword
    {
        public override string Sequence => "continue";

        public override GDSyntaxToken Clone()
        {
            return new GDContinueKeyword();
        }
    }
}
