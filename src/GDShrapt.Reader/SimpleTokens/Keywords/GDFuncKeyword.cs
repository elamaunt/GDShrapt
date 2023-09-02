namespace GDShrapt.Reader
{
    public sealed class GDFuncKeyword : GDKeyword
    {
        public override string Sequence => "func";

        public override GDSyntaxToken Clone()
        {
            return new GDFuncKeyword();
        }
    }
}
