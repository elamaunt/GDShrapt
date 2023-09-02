namespace GDShrapt.Reader
{
    public sealed class GDVarKeyword : GDKeyword
    {
        public override string Sequence => "var";

        public override GDSyntaxToken Clone()
        {
            return new GDVarKeyword();
        }
    }
}
